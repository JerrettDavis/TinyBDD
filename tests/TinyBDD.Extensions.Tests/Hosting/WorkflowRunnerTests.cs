using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD.Extensions.DependencyInjection;
using TinyBDD.Extensions.Hosting;

namespace TinyBDD.Extensions.Tests.Hosting;

public class WorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_WithWorkflowDefinition_ExecutesWorkflow()
    {
        // Arrange
        var runner = CreateRunner();
        var workflow = new TestWorkflow();

        // Act
        var context = await runner.RunAsync(workflow);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(3, context.Steps.Count);
        Assert.All(context.Steps, step => Assert.Null(step.Error));
    }

    [Fact]
    public async Task RunAsync_WithDelegate_ExecutesWorkflow()
    {
        // Arrange
        var runner = CreateRunner();

        // Act
        var context = await runner.RunAsync(
            "Test Feature",
            "Test Scenario",
            async (ctx, ct) =>
            {
                await Bdd.Given(ctx, "value", () => 1)
                    .When("incremented", v => v + 1)
                    .Then("is 2", v => v == 2);
            });

        // Assert
        Assert.NotNull(context);
        Assert.Equal(3, context.Steps.Count);
    }

    [Fact]
    public async Task RunAsync_WithFailingStep_ThrowsException()
    {
        // Arrange
        var runner = CreateRunner();
        var workflow = new FailingWorkflow();

        // Act & Assert
        await Assert.ThrowsAsync<BddStepException>(
            () => runner.RunAsync(workflow));
    }

    [Fact]
    public async Task RunAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var runner = CreateRunner();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var workflow = new DelayedWorkflow();

        // Act & Assert - cancellation is wrapped in BddStepException
        var ex = await Assert.ThrowsAsync<BddStepException>(
            () => runner.RunAsync(workflow, cts.Token));
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact]
    public async Task RunAsync_RecordsStepTiming()
    {
        // Arrange
        var runner = CreateRunner();
        var workflow = new TestWorkflow();

        // Act
        var context = await runner.RunAsync(workflow);

        // Assert
        Assert.All(context.Steps, step =>
        {
            Assert.True(step.Elapsed >= TimeSpan.Zero);
        });
    }

    [Fact]
    public async Task RunAsync_SetsCurrentItemToLastStepOutput()
    {
        // Arrange
        var runner = CreateRunner();

        // Act
        var context = await runner.RunAsync(
            "Test",
            "Current Item",
            async (ctx, ct) =>
            {
                await Bdd.Given(ctx, "start", () => 10)
                    .When("doubled", v => v * 2)
                    .Then("is 20", v => v == 20);
            });

        // Assert
        Assert.Equal(20, context.CurrentItem);
    }

    [Fact]
    public async Task RunAsync_RecordsStepIO()
    {
        // Arrange
        var runner = CreateRunner();

        // Act
        var context = await runner.RunAsync(
            "Test",
            "IO Tracking",
            async (ctx, ct) =>
            {
                await Bdd.Given(ctx, "start", () => 5)
                    .When("tripled", v => v * 3)
                    .Then("is 15", v => v == 15);
            });

        // Assert
        Assert.Equal(3, context.IO.Count);
        Assert.Equal(5, context.IO[0].Output);
        Assert.Equal(5, context.IO[1].Input);
        Assert.Equal(15, context.IO[1].Output);
    }

    [Fact]
    public async Task RunAsync_WithFailedAssertions_LogsWarningAndReturnsContext()
    {
        // Arrange - use a workflow that has a failing assertion but uses ContinueOnError
        var runner = CreateRunner();
        var workflow = new FailingAssertionWorkflow();

        // Act
        var context = await runner.RunAsync(workflow);

        // Assert
        Assert.NotNull(context);
        Assert.Contains(context.Steps, s => s.Error is not null);
    }

    private class FailingAssertionWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Failing Assertion Feature";
        public string ScenarioName => "Failing Assertion Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            // Run with HaltOnFailedAssertion = false through the pipeline
            await Bdd.Given(context, "value", () => 1)
                .Then("fails", v => v == 999) // This will record a failure
                .AssertFailed(); // Use AssertFailed since we expect failure
        }
    }

    [Fact]
    public async Task RunAsync_WithWorkflowException_ThrowsAndLogs()
    {
        // Arrange
        var runner = CreateRunner();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(new ExceptionThrowingWorkflow()));
    }

    private class ExceptionThrowingWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Exception Feature";
        public string ScenarioName => "Exception Scenario";
        public string? FeatureDescription => null;

        public ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            throw new InvalidOperationException("Deliberate exception for testing");
        }
    }

    private static IWorkflowRunner CreateRunner()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTinyBddHosting();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IWorkflowRunner>();
    }

    private class TestWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Test Feature";
        public string ScenarioName => "Test Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            await Bdd.Given(context, "initial value", () => 42)
                .When("processed", v => v * 2)
                .Then("result is correct", v => v == 84);
        }
    }

    private class FailingWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Failing Feature";
        public string ScenarioName => "Failing Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            await Bdd.Given(context, "value", () => 1)
#pragma warning disable CS0162 // Unreachable code detected
                .When("fails", (int _) => { throw new InvalidOperationException("Test failure"); return 0; })
#pragma warning restore CS0162
                .Then("never reached", _ => true);
        }
    }

    private class DelayedWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Delayed Feature";
        public string ScenarioName => "Delayed Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            Func<Task<int>> setup = async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return 1;
            };
            await Bdd.Given(context, "start", setup)
                .When("process", v => v)
                .Then("done", _ => true);
        }
    }
}
