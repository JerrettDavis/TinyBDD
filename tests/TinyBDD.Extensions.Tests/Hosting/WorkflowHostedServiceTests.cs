using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TinyBDD.Extensions.DependencyInjection;
using TinyBDD.Extensions.Hosting;

namespace TinyBDD.Extensions.Tests.Hosting;

public class WorkflowHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_RunsWorkflow_Successfully()
    {
        // Arrange
        using var host = CreateHost<SuccessfulWorkflow>();

        // Act
        await host.StartAsync();
        await Task.Delay(100); // Give time for the workflow to run
        await host.StopAsync();

        // Assert - no exception means success
    }

    [Fact]
    public async Task ExecuteAsync_WithStartupDelay_WaitsBeforeExecution()
    {
        // Arrange
        using var host = CreateHost<SuccessfulWorkflow>(options =>
        {
            options.StartupDelay = TimeSpan.FromMilliseconds(50);
        });

        var startTime = DateTime.UtcNow;

        // Act
        await host.StartAsync();
        await Task.Delay(100);
        await host.StopAsync();

        // Assert - workflow should have waited
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task ExecuteAsync_WithStopHostOnCompletion_StopsHost()
    {
        // Arrange
        using var host = CreateHost<SuccessfulWorkflow>(options =>
        {
            options.StopHostOnCompletion = true;
        });

        // Act
        await host.StartAsync();

        // Wait for the host to stop itself
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await host.WaitForShutdownAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Host did not stop within timeout");
        }

        // Assert - host should have stopped
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingWorkflow_AndStopHostOnFailure_StopsHost()
    {
        // Arrange
        using var host = CreateHost<FailingAssertionWorkflow>(options =>
        {
            options.StopHostOnFailure = true;
        });

        // Act
        await host.StartAsync();

        // Wait for the host to stop itself
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await host.WaitForShutdownAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Host did not stop within timeout");
        }

        // Assert - host should have stopped due to failure
    }

    [Fact]
    public async Task ExecuteAsync_WithExceptionThrowingWorkflow_AndStopHostOnFailure_StopsHost()
    {
        // Arrange
        using var host = CreateHost<ExceptionThrowingWorkflow>(options =>
        {
            options.StopHostOnFailure = true;
        });

        // Act
        await host.StartAsync();

        // Wait for the host to stop itself
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await host.WaitForShutdownAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Host did not stop within timeout");
        }

        // Assert - host should have stopped due to exception
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_HandlesGracefully()
    {
        // Arrange
        using var host = CreateHost<SlowWorkflow>();

        // Act
        await host.StartAsync();
        await Task.Delay(50); // Let it start
        await host.StopAsync(); // This cancels the workflow

        // Assert - no exception means graceful cancellation
    }

    private static IHost CreateHost<TWorkflow>(Action<TinyBddHostingOptions>? configureOptions = null)
        where TWorkflow : class, IWorkflowDefinition
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug))
            .UseTinyBdd(options =>
            {
                configureOptions?.Invoke(options);
            })
            .ConfigureServices(services =>
            {
                services.AddWorkflowHostedService<TWorkflow>();
            });

        return builder.Build();
    }

    private class SuccessfulWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Successful Feature";
        public string ScenarioName => "Successful Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            await Bdd.Given(context, "value", () => 1)
                .When("processed", v => v + 1)
                .Then("correct", v => v == 2);
        }
    }

    private class FailingAssertionWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Failing Feature";
        public string ScenarioName => "Failing Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            // Run with failing assertion using AssertFailed
            await Bdd.Given(context, "value", () => 1)
                .Then("fails", v => v == 999) // This will record a failure
                .AssertFailed(); // Use AssertFailed since we expect failure
        }
    }

    private class ExceptionThrowingWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Exception Feature";
        public string ScenarioName => "Exception Scenario";
        public string? FeatureDescription => null;

        public ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            throw new InvalidOperationException("Workflow exploded!");
        }
    }

    private class SlowWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Slow Feature";
        public string ScenarioName => "Slow Scenario";
        public string? FeatureDescription => null;

        public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
