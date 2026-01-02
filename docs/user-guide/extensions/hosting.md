# Hosting Integration

**TinyBDD.Extensions.Hosting** integrates TinyBDD with `Microsoft.Extensions.Hosting`, enabling BDD workflows to run as background services, startup tasks, or scheduled jobs within .NET Generic Host applications.

## Installation

```bash
dotnet add package TinyBDD.Extensions.Hosting
```

This package includes `TinyBDD.Extensions.DependencyInjection` as a dependency.

## Quick Start

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTinyBddHosting();
builder.Services.AddWorkflowHostedService<StartupWorkflow>();

var host = builder.Build();
await host.RunAsync();

// StartupWorkflow.cs
public class StartupWorkflow : IWorkflowDefinition
{
    public string FeatureName => "Application Startup";
    public string ScenarioName => "Initialize all services";
    public string? FeatureDescription => "Ensures all dependencies are ready";

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "configuration loaded", () => LoadConfig())
            .When("database migrated", cfg => MigrateDatabase(cfg, ct))
            .And("cache initialized", cfg => InitializeCache(cfg, ct))
            .Then("system ready", _ => true);
    }
}
```

## Service Registration

### Basic Registration

```csharp
// Using IHostBuilder
builder.UseTinyBdd();

// Using IServiceCollection
services.AddTinyBddHosting();
```

### With Configuration

```csharp
builder.UseTinyBdd(options =>
{
    options.StopHostOnCompletion = true;    // Stop host when workflow completes
    options.StopHostOnFailure = true;       // Stop host if workflow fails
    options.StartupDelay = TimeSpan.Zero;   // Delay before starting
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

### Adding Workflow Hosted Services

```csharp
// Register workflow type (resolved from DI)
services.AddWorkflowHostedService<MyWorkflow>();

// Register workflow instance
services.AddWorkflowHostedService(new MyWorkflow());
```

## API Reference

### TinyBddHostingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StopHostOnCompletion` | `bool` | `false` | Stop host when workflow completes successfully |
| `StopHostOnFailure` | `bool` | `true` | Stop host when workflow fails |
| `StartupDelay` | `TimeSpan` | `Zero` | Delay before workflow execution begins |
| `ShutdownTimeout` | `TimeSpan` | `30s` | Graceful shutdown timeout for running workflows |

### IWorkflowDefinition

```csharp
public interface IWorkflowDefinition
{
    /// <summary>Feature name for this workflow.</summary>
    string FeatureName { get; }

    /// <summary>Scenario name for this workflow.</summary>
    string ScenarioName { get; }

    /// <summary>Optional feature description. Return null if not needed.</summary>
    string? FeatureDescription { get; }

    /// <summary>Execute the workflow.</summary>
    ValueTask ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken);
}
```

### IWorkflowRunner

```csharp
public interface IWorkflowRunner
{
    /// <summary>Run a workflow definition.</summary>
    Task<ScenarioContext> RunAsync(
        IWorkflowDefinition workflow,
        CancellationToken cancellationToken = default);

    /// <summary>Run a workflow delegate.</summary>
    Task<ScenarioContext> RunAsync(
        string featureName,
        string scenarioName,
        Func<ScenarioContext, CancellationToken, ValueTask> workflow,
        CancellationToken cancellationToken = default);
}
```

## Workflow Patterns

### One-Shot Startup Workflow

Execute once at application startup, then stop:

```csharp
builder.Services.AddTinyBddHosting(options =>
{
    options.StopHostOnCompletion = true;
});
builder.Services.AddWorkflowHostedService<DatabaseMigrationWorkflow>();

public class DatabaseMigrationWorkflow : IWorkflowDefinition
{
    private readonly IDbContext _db;

    public DatabaseMigrationWorkflow(IDbContext db) => _db = db;

    public string FeatureName => "Database Migration";
    public string ScenarioName => "Apply pending migrations";
    public string? FeatureDescription => null;

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "pending migrations", () => _db.GetPendingMigrations())
            .When("applied", migrations => ApplyMigrations(migrations, ct))
            .Then("database up to date", result => result.Applied == result.Total);
    }
}
```

### Continuous Background Workflow

Run indefinitely, repeating the workflow:

```csharp
public class HealthMonitorWorkflow : BackgroundService
{
    private readonly IWorkflowRunner _runner;

    public HealthMonitorWorkflow(IWorkflowRunner runner) => _runner = runner;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var context = await _runner.RunAsync(
                "Health Monitoring",
                "Check all services",
                async (ctx, ct) =>
                {
                    await Bdd.Given(ctx, "services to check", () => GetServiceEndpoints())
                        .When("health checked", endpoints => CheckHealthAsync(endpoints, ct))
                        .Then("all healthy", results => results.All(r => r.IsHealthy));
                },
                stoppingToken);

            LogResults(context);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### Workflow with Dependencies

Workflows can inject services:

```csharp
public class OrderProcessingWorkflow : IWorkflowDefinition
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _payments;
    private readonly IInventoryService _inventory;
    private readonly INotificationService _notifications;

    public OrderProcessingWorkflow(
        IOrderRepository orders,
        IPaymentGateway payments,
        IInventoryService inventory,
        INotificationService notifications)
    {
        _orders = orders;
        _payments = payments;
        _inventory = inventory;
        _notifications = notifications;
    }

    public string FeatureName => "Order Processing";
    public string ScenarioName => "Process pending orders";
    public string? FeatureDescription => null;

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "pending orders", () => _orders.GetPendingAsync(ct))
            .When("payments processed", orders => ProcessPaymentsAsync(orders, ct))
            .And("inventory reserved", orders => ReserveInventoryAsync(orders, ct))
            .When("shipped", orders => CreateShipmentsAsync(orders, ct))
            .Then("customers notified", orders => NotifyCustomersAsync(orders, ct));
    }

    private async Task<IList<Order>> ProcessPaymentsAsync(IList<Order> orders, CancellationToken ct)
    {
        foreach (var order in orders)
        {
            await _payments.ChargeAsync(order.PaymentDetails, order.Total, ct);
            order.PaymentStatus = PaymentStatus.Charged;
        }
        return orders;
    }

    // ... other methods
}
```

### Programmatic Workflow Execution

Use `IWorkflowRunner` directly for on-demand execution:

```csharp
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IWorkflowRunner _runner;

    public JobsController(IWorkflowRunner runner) => _runner = runner;

    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync(CancellationToken ct)
    {
        var context = await _runner.RunAsync(
            "Data Sync",
            "Manual sync triggered",
            async (ctx, token) =>
            {
                await Bdd.Given(ctx, "external data fetched", () => FetchExternalData(token))
                    .When("transformed", data => TransformData(data))
                    .When("saved", data => SaveData(data, token))
                    .Then("sync complete", result => result.Success);
            },
            ct);

        return Ok(new
        {
            Success = context.Steps.All(s => s.Error == null),
            Steps = context.Steps.Select(s => new
            {
                s.Kind,
                s.Title,
                DurationMs = s.Elapsed.TotalMilliseconds,
                Error = s.Error?.Message
            })
        });
    }
}
```

## Error Handling

### Workflow Failure Behavior

```csharp
builder.Services.AddTinyBddHosting(options =>
{
    // Stop the entire application on workflow failure
    options.StopHostOnFailure = true;
});
```

### Step-Level Error Handling

Configure scenario options for fine-grained control:

```csharp
builder.Services.AddTinyBdd(options =>
{
    options.DefaultScenarioOptions = new ScenarioOptions
    {
        ContinueOnError = true,              // Continue after failures
        MarkRemainingAsSkippedOnFailure = true,  // Mark skipped steps
        StepTimeout = TimeSpan.FromSeconds(30)   // Per-step timeout
    };
});
```

### Graceful Shutdown

```csharp
builder.Services.AddTinyBddHosting(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(60);  // Wait for workflow to complete
});
```

When the host receives a shutdown signal:
1. The `CancellationToken` passed to `ExecuteAsync` is cancelled
2. The workflow has `ShutdownTimeout` to complete gracefully
3. After timeout, the workflow is forcibly terminated

## Observability

### Logging

`WorkflowRunner` logs workflow execution:

```
info: TinyBDD.Extensions.Hosting.WorkflowRunner[0]
      Starting workflow: Order Processing - Process pending orders
info: TinyBDD.Extensions.Hosting.WorkflowRunner[0]
      Workflow completed successfully: Order Processing - Process pending orders, 5 steps
```

### Custom Step Logging

Access step results for detailed logging:

```csharp
public class ObservableWorkflow : IWorkflowDefinition
{
    private readonly ILogger<ObservableWorkflow> _logger;

    public ObservableWorkflow(ILogger<ObservableWorkflow> logger) => _logger = logger;

    public string FeatureName => "Observable Workflow";
    public string ScenarioName => "With detailed logging";
    public string? FeatureDescription => null;

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "setup", () => Setup())
            .When("execute", data => Execute(data, ct))
            .Then("verify", result => Verify(result));

        // Log all steps after execution
        foreach (var step in context.Steps)
        {
            _logger.LogInformation(
                "Step {Kind} '{Title}': {Status} in {Duration}ms",
                step.Kind,
                step.Title,
                step.Error == null ? "OK" : $"FAILED: {step.Error.Message}",
                step.Elapsed.TotalMilliseconds);
        }

        // Log step I/O for debugging
        foreach (var io in context.IO)
        {
            _logger.LogDebug(
                "Step '{Title}': Input={InputType}, Output={OutputType}",
                io.Title,
                io.Input?.GetType().Name ?? "null",
                io.Output?.GetType().Name ?? "null");
        }
    }
}
```

### Metrics Integration

```csharp
public class MetricsWorkflowRunner : IWorkflowRunner
{
    private readonly IWorkflowRunner _inner;
    private readonly IMetricsCollector _metrics;

    public MetricsWorkflowRunner(WorkflowRunner inner, IMetricsCollector metrics)
    {
        _inner = inner;
        _metrics = metrics;
    }

    public async Task<ScenarioContext> RunAsync(IWorkflowDefinition workflow, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var context = await _inner.RunAsync(workflow, ct);
            _metrics.RecordWorkflowDuration(workflow.FeatureName, sw.Elapsed);
            _metrics.RecordWorkflowResult(workflow.FeatureName, context.Steps.All(s => s.Error == null));
            return context;
        }
        catch (Exception ex)
        {
            _metrics.RecordWorkflowError(workflow.FeatureName, ex);
            throw;
        }
    }
}
```

## Testing Hosted Workflows

```csharp
public class WorkflowTests
{
    [Fact]
    public async Task OrderProcessingWorkflow_ProcessesPendingOrders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTinyBddHosting();
        services.AddSingleton<IOrderRepository>(new FakeOrderRepository(pendingOrders: 3));
        services.AddSingleton<IPaymentGateway>(new FakePaymentGateway());
        services.AddSingleton<IInventoryService>(new FakeInventoryService());
        services.AddSingleton<INotificationService>(new FakeNotificationService());
        services.AddScoped<OrderProcessingWorkflow>();

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IWorkflowRunner>();
        var workflow = provider.GetRequiredService<OrderProcessingWorkflow>();

        // Act
        var context = await runner.RunAsync(workflow);

        // Assert
        Assert.True(context.Steps.All(s => s.Error == null));
        Assert.Equal(5, context.Steps.Count);
    }
}
```

## Best Practices

1. **Single responsibility**: Each workflow should do one thing well
2. **Inject dependencies**: Use constructor injection for testability
3. **Handle cancellation**: Always respect the `CancellationToken`
4. **Log step results**: Capture execution details for debugging
5. **Configure timeouts**: Prevent runaway workflows with `StepTimeout`
6. **Test workflows**: Use the same DI setup in tests

## Next Steps

- [Dependency Injection Guide](dependency-injection.md) - DI integration details
- [Reporting Extension](reporting.md) - JSON reporting and observer pattern
- [Orchestrator Patterns](../orchestrator-patterns.md) - Advanced workflow patterns
- [Enterprise Samples](../samples-enterprise.md) - Production-ready examples

Return to: [Extensions Index](index.md) | [User Guide](../index.md)
