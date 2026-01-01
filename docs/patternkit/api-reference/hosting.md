# PatternKit.Extensions.Hosting API Reference

Complete API documentation for the `PatternKit.Extensions.Hosting` package.

## Installation

```bash
dotnet add package PatternKit.Extensions.Hosting
```

**Dependencies:**
- `PatternKit.Core`
- `PatternKit.Extensions.DependencyInjection`
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`

---

## HostBuilderExtensions

```csharp
namespace PatternKit.Extensions.Hosting;

public static class HostBuilderExtensions
```

Extension methods for integrating PatternKit with hosts.

### UsePatternKit

Adds PatternKit workflow hosting to `IHostBuilder`.

```csharp
public static IHostBuilder UsePatternKit(
    this IHostBuilder hostBuilder,
    Action<PatternKitOptions>? configurePatternKit = null,
    Action<WorkflowHostingOptions>? configureHosting = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `hostBuilder` | `IHostBuilder` | The host builder |
| `configurePatternKit` | `Action<PatternKitOptions>?` | PatternKit configuration |
| `configureHosting` | `Action<WorkflowHostingOptions>?` | Hosting configuration |

**Example:**
```csharp
var host = Host.CreateDefaultBuilder(args)
    .UsePatternKit(
        configurePatternKit: options =>
        {
            options.DefaultTimeout = TimeSpan.FromMinutes(5);
        },
        configureHosting: options =>
        {
            options.StartAutomatically = true;
            options.EnableParallelExecution = true;
        })
    .Build();
```

---

### AddPatternKitHosting

Adds PatternKit hosting to `IServiceCollection`.

```csharp
public static IServiceCollection AddPatternKitHosting(
    this IServiceCollection services,
    Action<PatternKitOptions>? configurePatternKit = null,
    Action<WorkflowHostingOptions>? configureHosting = null)
```

**Registers:**
- All `AddPatternKit()` services
- `WorkflowHostingOptions` (Singleton)
- `WorkflowHostedService` (Hosted Service)
- `IWorkflowRunner` → `WorkflowRunner` (Singleton)

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPatternKitHosting(
    configureHosting: options =>
    {
        options.MaxDegreeOfParallelism = 4;
        options.ContinueOnFailure = true;
    });
```

---

### AddWorkflow (Type Registration)

Registers a workflow definition.

```csharp
public static IServiceCollection AddWorkflow<TWorkflow>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
    where TWorkflow : class, IWorkflowDefinition
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TWorkflow` | Type | - | The workflow type |
| `lifetime` | `ServiceLifetime` | `Singleton` | Service lifetime |

**Example:**
```csharp
services.AddPatternKitHosting()
    .AddWorkflow<OrderProcessingWorkflow>()
    .AddWorkflow<ReportWorkflow>(ServiceLifetime.Scoped);
```

---

### AddWorkflow (Instance Registration)

Registers a workflow instance.

```csharp
public static IServiceCollection AddWorkflow(
    this IServiceCollection services,
    IWorkflowDefinition workflow)
```

**Example:**
```csharp
var workflow = new MyWorkflow(config);
services.AddWorkflow(workflow);
```

---

### AddWorkflow (Factory Registration)

Registers a workflow using a factory.

```csharp
public static IServiceCollection AddWorkflow(
    this IServiceCollection services,
    Func<IServiceProvider, IWorkflowDefinition> factory,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
```

**Example:**
```csharp
services.AddWorkflow(sp => new MyWorkflow(
    sp.GetRequiredService<IRepository>(),
    sp.GetRequiredService<ILogger<MyWorkflow>>()));
```

---

### AddRecurringWorkflow

Registers a recurring workflow.

```csharp
public static IServiceCollection AddRecurringWorkflow<TWorkflow>(
    this IServiceCollection services)
    where TWorkflow : class, IRecurringWorkflowDefinition
```

**Example:**
```csharp
services.AddPatternKitHosting()
    .AddRecurringWorkflow<HealthCheckWorkflow>()
    .AddRecurringWorkflow<DataSyncWorkflow>();
```

---

## WorkflowHostingOptions

```csharp
namespace PatternKit.Extensions.Hosting;

public sealed class WorkflowHostingOptions
```

Configuration for workflow hosting behavior.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StartAutomatically` | `bool` | `true` | Start workflows on host start |
| `ShutdownTimeout` | `TimeSpan` | `30 seconds` | Graceful shutdown timeout |
| `EnableParallelExecution` | `bool` | `true` | Execute workflows in parallel |
| `MaxDegreeOfParallelism` | `int?` | `null` | Max parallel workflows (null = ProcessorCount) |
| `ContinueOnFailure` | `bool` | `true` | Continue after workflow failure |

### Example

```csharp
services.AddPatternKitHosting(configureHosting: options =>
{
    options.StartAutomatically = true;
    options.EnableParallelExecution = true;
    options.MaxDegreeOfParallelism = 4;
    options.ContinueOnFailure = true;
    options.ShutdownTimeout = TimeSpan.FromMinutes(1);
});
```

---

## IWorkflowDefinition

```csharp
namespace PatternKit.Extensions.Hosting;

public interface IWorkflowDefinition
{
    string Name { get; }
    string? Description { get; }
    ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);
}
```

Interface for defining reusable workflows.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Unique workflow identifier |
| `Description` | `string?` | Optional description |

### Methods

#### ExecuteAsync

```csharp
ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
```

Executes the workflow with the provided context.

### Example

```csharp
public class OrderProcessingWorkflow : IWorkflowDefinition
{
    private readonly IOrderRepository _repository;

    public string Name => "OrderProcessing";
    public string? Description => "Processes pending orders";

    public OrderProcessingWorkflow(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        var orders = await _repository.GetPendingAsync(cancellationToken);

        foreach (var order in orders)
        {
            await Workflow
                .Given(context, "order", () => order)
                .When("process", o => Process(o))
                .Then("processed", o => o.Status == "Processed");
        }
    }
}
```

---

## IWorkflowDefinition<TResult>

```csharp
namespace PatternKit.Extensions.Hosting;

public interface IWorkflowDefinition<TResult> : IWorkflowDefinition
{
    new ValueTask<TResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken);
}
```

Workflow that produces a typed result.

### Example

```csharp
public class ReportWorkflow : IWorkflowDefinition<Report>
{
    public string Name => "GenerateReport";
    public string? Description => "Generates sales report";

    public async ValueTask<Report> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        return await Workflow
            .Given(context, "date range", () => GetDateRange())
            .When("fetch data", async range =>
                await FetchDataAsync(range, cancellationToken))
            .When("generate report", data => new Report(data))
            .Then("valid", report => report.IsValid)
            .GetResultAsync(cancellationToken);
    }

    // Explicit interface implementation
    async ValueTask IWorkflowDefinition.ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, cancellationToken);
    }
}
```

---

## IRecurringWorkflowDefinition

```csharp
namespace PatternKit.Extensions.Hosting;

public interface IRecurringWorkflowDefinition : IWorkflowDefinition
{
    TimeSpan Interval { get; }
    bool RunImmediately { get; }
}
```

Workflow that runs on a schedule.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Interval` | `TimeSpan` | - | Time between executions |
| `RunImmediately` | `bool` | `false` | Run on startup |

### Example

```csharp
public class HealthCheckWorkflow : IRecurringWorkflowDefinition
{
    public string Name => "HealthCheck";
    public string? Description => "Periodic health check";
    public TimeSpan Interval => TimeSpan.FromMinutes(5);
    public bool RunImmediately => true;

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "health check", () => DateTime.UtcNow)
            .When("check services", async _ =>
                await CheckServicesAsync(cancellationToken))
            .Then("healthy", result => result.IsHealthy);
    }
}
```

---

## IWorkflowRunner

```csharp
namespace PatternKit.Extensions.Hosting;

public interface IWorkflowRunner
```

Service for running workflows on-demand.

### Methods

#### RunAsync (by name)

```csharp
ValueTask<WorkflowContext> RunAsync(
    string workflowName,
    CancellationToken cancellationToken = default)
```

Runs a registered workflow by name.

#### RunAsync (definition)

```csharp
ValueTask<WorkflowContext> RunAsync(
    IWorkflowDefinition workflow,
    CancellationToken cancellationToken = default)
```

Runs a workflow definition instance.

#### RunAsync<TResult> (typed result)

```csharp
ValueTask<TResult> RunAsync<TResult>(
    IWorkflowDefinition<TResult> workflow,
    CancellationToken cancellationToken = default)
```

Runs a workflow and returns its typed result.

#### RunAsync<T> (ad-hoc)

```csharp
ValueTask<T> RunAsync<T>(
    string workflowName,
    Func<WorkflowContext, CancellationToken, ValueTask<T>> buildWorkflow,
    CancellationToken cancellationToken = default)
```

Runs an ad-hoc workflow built inline.

### Example

```csharp
public class OrderController : ControllerBase
{
    private readonly IWorkflowRunner _runner;

    public OrderController(IWorkflowRunner runner)
    {
        _runner = runner;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessOrder()
    {
        // By name
        var context = await _runner.RunAsync("OrderProcessing");
        return Ok(context.AllPassed);
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetReport()
    {
        // With typed result
        var workflow = new ReportWorkflow();
        var report = await _runner.RunAsync(workflow);
        return Ok(report);
    }

    [HttpPost("adhoc")]
    public async Task<IActionResult> RunAdhoc(AdhocRequest request)
    {
        // Ad-hoc workflow
        var result = await _runner.RunAsync(
            "AdhocProcessing",
            async (ctx, ct) =>
            {
                return await Workflow
                    .Given(ctx, "request", () => request)
                    .When("process", r => Process(r))
                    .Then("valid", r => r.IsValid)
                    .GetResultAsync(ct);
            });

        return Ok(result);
    }
}
```

---

## WorkflowRunner

```csharp
namespace PatternKit.Extensions.Hosting;

public sealed class WorkflowRunner : IWorkflowRunner
```

Default implementation of `IWorkflowRunner`.

### Constructor

```csharp
public WorkflowRunner(
    IServiceProvider serviceProvider,
    ILogger<WorkflowRunner> logger,
    IEnumerable<IWorkflowDefinition> registeredWorkflows)
```

### Behavior

- Creates a new DI scope for each workflow execution
- Resolves `IWorkflowContextFactory` from the scope
- Logs workflow start and completion
- Disposes context after execution
- Throws `InvalidOperationException` if workflow name not found

---

## WorkflowHostedService

```csharp
namespace PatternKit.Extensions.Hosting;

public sealed class WorkflowHostedService : BackgroundService
```

Background service that hosts and executes registered workflows.

### Constructor

```csharp
public WorkflowHostedService(
    IServiceProvider serviceProvider,
    ILogger<WorkflowHostedService> logger,
    WorkflowHostingOptions options,
    IEnumerable<IWorkflowDefinition> workflows)
```

### Behavior

1. **Startup**: When `StartAutomatically = true`, executes one-time workflows
2. **Parallel Execution**: Uses `Parallel.ForEachAsync` when `EnableParallelExecution = true`
3. **Recurring Workflows**: Starts `PeriodicTimer` loops for recurring workflows
4. **Error Handling**: Logs errors and continues/stops based on `ContinueOnFailure`
5. **Shutdown**: Respects `ShutdownTimeout` for graceful cancellation

### Execution Flow

```
ExecuteAsync called
        │
        ├─────────────────────────┬─────────────────────────┐
        │                         │                         │
        ▼                         ▼                         ▼
   One-time workflows      Recurring workflow 1      Recurring workflow 2
   (parallel or sequential)     (PeriodicTimer)          (PeriodicTimer)
        │                         │                         │
        │                         ├──── Execute ────────────┤
        │                         │                         │
        │                         ├──── Wait Interval ──────┤
        │                         │                         │
        ▼                         ├──── Execute ────────────┤
   Complete                       │                         │
                                  └──── (continues) ────────┘
```

---

## Complete Example

```csharp
using PatternKit.Core;
using PatternKit.Extensions.Hosting;

// Define workflows
public class StartupWorkflow : IWorkflowDefinition
{
    public string Name => "Startup";
    public string? Description => "Application initialization";

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "application starting", () => DateTime.UtcNow)
            .When("initialize database", async _ =>
            {
                await InitializeDatabaseAsync(cancellationToken);
                return true;
            })
            .And("warm up caches", async _ =>
            {
                await WarmUpCachesAsync(cancellationToken);
                return true;
            })
            .Then("ready", success => success);
    }
}

public class DataSyncWorkflow : IRecurringWorkflowDefinition
{
    private readonly IDataService _dataService;

    public string Name => "DataSync";
    public string? Description => "Synchronizes data with external system";
    public TimeSpan Interval => TimeSpan.FromMinutes(15);
    public bool RunImmediately => false;

    public DataSyncWorkflow(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "sync started", () => DateTime.UtcNow)
            .When("fetch remote data", async _ =>
                await _dataService.FetchRemoteAsync(cancellationToken))
            .When("update local", async data =>
            {
                await _dataService.UpdateLocalAsync(data, cancellationToken);
                return data.Count;
            })
            .Then("sync complete", count => count >= 0);
    }
}

// Configure and run
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IDataService, DataService>();

        services.AddPatternKitHosting(
            configurePatternKit: options =>
            {
                options.DefaultWorkflowOptions = new WorkflowOptions
                {
                    StepTimeout = TimeSpan.FromMinutes(2)
                };
            },
            configureHosting: options =>
            {
                options.StartAutomatically = true;
                options.EnableParallelExecution = true;
                options.ContinueOnFailure = true;
            })
            .AddWorkflow<StartupWorkflow>()
            .AddRecurringWorkflow<DataSyncWorkflow>();
    })
    .Build();

// Run host (workflows execute automatically)
await host.RunAsync();
```

---

## Web API Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPatternKitHosting()
    .AddWorkflow<OrderProcessingWorkflow>()
    .AddRecurringWorkflow<CleanupWorkflow>();

var app = builder.Build();

// On-demand workflow execution via API
app.MapPost("/orders/{id}/process", async (
    string id,
    IWorkflowRunner runner) =>
{
    var context = await runner.RunAsync("OrderProcessing");

    if (!context.AllPassed)
    {
        return Results.Problem(
            detail: context.FirstFailure?.Error?.Message,
            statusCode: 500);
    }

    return Results.Ok(new { OrderId = id, Status = "Processed" });
});

// Ad-hoc workflow
app.MapPost("/compute", async (
    ComputeRequest request,
    IWorkflowRunner runner) =>
{
    var result = await runner.RunAsync(
        "Compute",
        async (ctx, ct) =>
        {
            return await Workflow
                .Given(ctx, "input", () => request.Value)
                .When("compute", v => v * 2)
                .Then("valid", v => v > 0)
                .GetResultAsync(ct);
        });

    return Results.Ok(new { Result = result });
});

app.Run();
```
