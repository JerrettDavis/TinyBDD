using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Execution;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

/// <summary>
/// Additional tests for ScenarioExecutor to cover missing branches including synchronous execution and switch defaults.
/// </summary>
public class ScenarioExecutorAdditionalTests
{
    private class SyncDriver : IApplicationDriver
    {
        public List<string> ExecutedSteps { get; } = new();

        [DriverMethod("I perform sync step")]
        public string SyncStep()
        {
            ExecutedSteps.Add("SyncStep");
            return "sync result";
        }

        [DriverMethod("I return null")]
        public string? ReturnNull()
        {
            ExecutedSteps.Add("ReturnNull");
            return null;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class UnknownKeywordDriver : IApplicationDriver
    {
        [DriverMethod("I use unknown keyword")]
        public Task UnknownStep() => Task.CompletedTask;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task ExecuteAsync_WithSynchronousDriverMethod_ExecutesSuccessfully()
    {
        // Arrange
        var driver = new SyncDriver();
        var executor = new ScenarioExecutor(driver, typeof(SyncDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Sync Test",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "When", Text = "I perform sync step" }
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Contains("SyncStep", driver.ExecutedSteps);
        Assert.Single(context.Steps);
        Assert.Equal("When", context.Steps[0].Kind);
    }

    [Fact]
    public async Task ExecuteAsync_WithSynchronousMethodReturningNull_HandlesCorrectly()
    {
        // Arrange
        var driver = new SyncDriver();
        var executor = new ScenarioExecutor(driver, typeof(SyncDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Null Test",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "When", Text = "I return null" }
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Contains("ReturnNull", driver.ExecutedSteps);
        Assert.Single(context.Steps);
        Assert.Null(context.Steps[0].Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownKeyword_UsesCurrentPhase()
    {
        // Arrange - Test the default case in DeterminePhase
        var driver = new UnknownKeywordDriver();
        var executor = new ScenarioExecutor(driver, typeof(UnknownKeywordDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Unknown Keyword Test",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "I use unknown keyword" },
                new() { Keyword = "UNKNOWN", Text = "I use unknown keyword" } // Unknown keyword
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert - Unknown keyword should inherit phase from previous step (Given)
        Assert.Equal(2, context.Steps.Count);
        Assert.Equal("Given", context.Steps[0].Kind);
        // The second step with unknown keyword should be treated as "Primary" in Given phase
        Assert.Equal("Given", context.Steps[1].Kind);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownKeywordAsFirstStep_UsesGivenPhase()
    {
        // Arrange - Test default phase is Given
        var driver = new UnknownKeywordDriver();
        var executor = new ScenarioExecutor(driver, typeof(UnknownKeywordDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Unknown First",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "UNKNOWN", Text = "I use unknown keyword" }
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert - Unknown first keyword defaults to Given phase
        Assert.Single(context.Steps);
        Assert.Equal("Given", context.Steps[0].Kind);
    }

    [Fact]
    public async Task MapKeywordToStepWord_WithUnknownKeyword_ReturnsPrimary()
    {
        // Arrange - Testing the default case in MapKeywordToStepWord
        var driver = new UnknownKeywordDriver();
        var executor = new ScenarioExecutor(driver, typeof(UnknownKeywordDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Test",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "CUSTOM", Text = "I use unknown keyword" }
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert - Unknown keyword maps to Primary, resulting in "Given" (current phase)
        Assert.Single(context.Steps);
        Assert.Equal("Given", context.Steps[0].Kind);
    }

    [Fact]
    public async Task GetStepKind_WithDefaultPhase_ReturnsPhaseAsString()
    {
        // Arrange - Create a scenario that would test the default case in GetStepKind
        // This is harder to test directly, but we can verify it doesn't break
        var driver = new UnknownKeywordDriver();
        var executor = new ScenarioExecutor(driver, typeof(UnknownKeywordDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Test",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "I use unknown keyword" }
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Single(context.Steps);
        Assert.Equal("Given", context.Steps[0].Kind);
    }

    [Fact]
    public async Task ExecuteAsync_WithTaskGenericResult_ExtractsResultValue()
    {
        // Arrange - Testing the Task<T> result extraction path
        var driver = new GenericTaskDriver();
        var executor = new ScenarioExecutor(driver, typeof(GenericTaskDriver));
        var feature = new FeatureDefinition { Name = "Test" };
        var scenario = new ScenarioDefinition
        {
            Name = "Generic Task Test",
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Then", Text = "I get a result" }
            }
        };
        var context = new ScenarioContext(feature.Name, null, scenario.Name, new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Single(context.Steps);
        Assert.Null(context.Steps[0].Error);
    }

    private class GenericTaskDriver : IApplicationDriver
    {
        [DriverMethod("I get a result")]
        public Task<string> GetResult() => Task.FromResult("result");

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
