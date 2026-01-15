using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Execution;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

public class ScenarioExecutorTests
{
    private class TestDriver : IApplicationDriver
    {
        public bool InitializeCalled { get; private set; }
        public bool CleanupCalled { get; private set; }
        public List<string> ExecutedSteps { get; } = new();

        [DriverMethod("a test driver")]
        public Task Initialize()
        {
            ExecutedSteps.Add("Initialize");
            return Task.CompletedTask;
        }

        [DriverMethod("I execute step {name}")]
        public Task ExecuteStep(string name)
        {
            ExecutedSteps.Add($"ExecuteStep:{name}");
            return Task.CompletedTask;
        }

        [DriverMethod("I verify {value}")]
        public Task<bool> Verify(string value)
        {
            ExecutedSteps.Add($"Verify:{value}");
            return Task.FromResult(value == "success");
        }

        [DriverMethod("I fail")]
        public Task FailingStep()
        {
            throw new InvalidOperationException("Intentional failure");
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        public Task CleanupAsync(CancellationToken cancellationToken = default)
        {
            CleanupCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Constructor_WithNullDriver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ScenarioExecutor(null!, typeof(TestDriver)));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidScenario_ExecutesAllSteps()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition
        {
            Name = "Test Feature",
            Tags = new List<string>()
        };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new List<string>(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "a test driver", Parameters = new() },
                new() { Keyword = "When", Text = "I execute step first", Parameters = new() { ["name"] = "first" } },
                new() { Keyword = "Then", Text = "I verify success", Parameters = new() { ["value"] = "success" } }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.True(driver.InitializeCalled);
        Assert.True(driver.CleanupCalled);
        Assert.Equal(3, driver.ExecutedSteps.Count);
        Assert.Equal(3, context.Steps.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullFeature_ThrowsArgumentNullException()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var scenario = new ScenarioDefinition { Name = "Test", Tags = new(), Steps = new() };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            executor.ExecuteAsync(null!, scenario, context));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullScenario_ThrowsArgumentNullException()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            executor.ExecuteAsync(feature, null!, context));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition { Name = "Test", Tags = new(), Steps = new() };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            executor.ExecuteAsync(feature, scenario, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithFeatureTags_AddsTagsToContext()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition
        {
            Name = "Test Feature",
            Tags = new List<string> { "feature-tag", "smoke" }
        };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new List<string> { "scenario-tag" },
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "a test driver", Parameters = new() }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Contains("feature-tag", context.Tags);
        Assert.Contains("smoke", context.Tags);
        Assert.Contains("scenario-tag", context.Tags);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnresolvedStep_ThrowsInvalidOperationException()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "an unknown step that doesn't exist", Parameters = new() }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            executor.ExecuteAsync(feature, scenario, context));
        Assert.Contains("No driver method found", exception.Message);
        Assert.Contains("an unknown step that doesn't exist", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingStep_ThrowsBddStepException()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "I fail", Parameters = new() }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BddStepException>(() => 
            executor.ExecuteAsync(feature, scenario, context));
        Assert.Contains("Step failed", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Contains("Intentional failure", exception.InnerException.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingAssertion_ThrowsBddStepException()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Then", Text = "I verify failure", Parameters = new() { ["value"] = "failure" } }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BddStepException>(() => 
            executor.ExecuteAsync(feature, scenario, context));
        Assert.Contains("Assertion failed", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_CallsCleanup_EvenAfterFailure()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "I fail", Parameters = new() }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act
        try
        {
            await executor.ExecuteAsync(feature, scenario, context);
        }
        catch (BddStepException)
        {
            // Expected
        }

        // Assert
        Assert.True(driver.InitializeCalled);
        Assert.True(driver.CleanupCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithAndKeyword_InheritsPhaseFromPreviousStep()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "a test driver", Parameters = new() },
                new() { Keyword = "And", Text = "I execute step first", Parameters = new() { ["name"] = "first" } }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Equal(2, context.Steps.Count);
        Assert.Equal("Given", context.Steps[0].Kind);
        Assert.Equal("And", context.Steps[1].Kind);
    }

    [Fact]
    public async Task ExecuteAsync_WithButKeyword_InheritsPhaseFromPreviousStep()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Then", Text = "I verify success", Parameters = new() { ["value"] = "success" } },
                new() { Keyword = "But", Text = "I execute step final", Parameters = new() { ["name"] = "final" } }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Equal(2, context.Steps.Count);
        Assert.Equal("Then", context.Steps[0].Kind);
        Assert.Equal("But", context.Steps[1].Kind);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsElapsedTime_ForEachStep()
    {
        // Arrange
        var driver = new TestDriver();
        var executor = new ScenarioExecutor(driver, typeof(TestDriver));
        var feature = new FeatureDefinition { Name = "Test", Tags = new() };
        var scenario = new ScenarioDefinition
        {
            Name = "Test Scenario",
            Tags = new(),
            Steps = new List<StepDefinition>
            {
                new() { Keyword = "Given", Text = "a test driver", Parameters = new() }
            }
        };
        var context = new ScenarioContext("Test Feature", null, "Test Scenario", new NullTraitBridge(), new ScenarioOptions());

        // Act
        await executor.ExecuteAsync(feature, scenario, context);

        // Assert
        Assert.Single(context.Steps);
        Assert.True(context.Steps[0].Elapsed > TimeSpan.Zero);
    }
}
