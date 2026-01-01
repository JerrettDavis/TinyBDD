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

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => runner.RunAsync(workflow, cts.Token));
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
                .When("fails", _ => throw new InvalidOperationException("Test failure"))
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
            await Bdd.Given(context, "start", async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    return 1;
                })
                .When("process", v => v)
                .Then("done", _ => true);
        }
    }
}
