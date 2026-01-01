# Hosting Integration

PatternKit integrates with Microsoft.Extensions.Hosting to run workflows as background services, enabling scheduled, recurring, and on-demand workflow execution in hosted applications.

## Installation

```bash
dotnet add package PatternKit.Extensions.Hosting
```

This package includes `PatternKit.Extensions.DependencyInjection` as a dependency.

## Basic Setup

### IHostBuilder Integration

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

await host.RunAsync();
```

### IServiceCollection Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPatternKitHosting(
    configurePatternKit: options =>
    {
        options.DefaultWorkflowOptions = new WorkflowOptions
        {
            StepTimeout = TimeSpan.FromSeconds(30)
        };
    },
    configureHosting: options =>
    {
        options.MaxDegreeOfParallelism = 4;
        options.ContinueOnFailure = true;
    });
```

### What Gets Registered

`AddPatternKitHosting()` registers:

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| All PatternKit core services | Via `AddPatternKit()` | Various |
| `WorkflowHostingOptions` | Configuration instance | Singleton |
| `WorkflowHostedService` | Background service | Singleton |
| `IWorkflowRunner` | `WorkflowRunner` | Singleton |

## WorkflowHostingOptions

Configure hosting behavior:

```csharp
public sealed class WorkflowHostingOptions
{
    // Start registered workflows when host starts
    public bool StartAutomatically { get; set; } = true;

    // Maximum time to wait during graceful shutdown
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // Execute workflows in parallel
    public bool EnableParallelExecution { get; set; } = true;

    // Maximum parallel workflow executions (null = ProcessorCount)
    public int? MaxDegreeOfParallelism { get; set; }

    // Continue executing remaining workflows after one fails
    public bool ContinueOnFailure { get; set; } = true;
}
```

### Configuration Examples

```csharp
// High-throughput processing
builder.Services.AddPatternKitHosting(configureHosting: options =>
{
    options.EnableParallelExecution = true;
    options.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
    options.ContinueOnFailure = true;
});

// Sequential, fail-fast processing
builder.Services.AddPatternKitHosting(configureHosting: options =>
{
    options.EnableParallelExecution = false;
    options.ContinueOnFailure = false;
    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});
```

## Workflow Definitions

### IWorkflowDefinition

Interface for defining reusable workflows:

```csharp
public interface IWorkflowDefinition
{
    string Name { get; }
    string? Description { get; }
    ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);
}
```

### Creating a Workflow Definition

```csharp
public class OrderProcessingWorkflow : IWorkflowDefinition
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderProcessingWorkflow> _logger;

    public string Name => "OrderProcessing";
    public string? Description => "Processes pending orders";

    public OrderProcessingWorkflow(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        ILogger<OrderProcessingWorkflow> logger)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        var pendingOrders = await _orderRepository.GetPendingAsync(cancellationToken);

        foreach (var order in pendingOrders)
        {
            await Workflow
                .Given(context, "pending order", () => order)
                .When("process payment", async o =>
                {
                    var result = await _paymentService.ProcessAsync(o, cancellationToken);
                    o.PaymentStatus = result.Success ? "Completed" : "Failed";
                    return o;
                })
                .And("update order", async o =>
                {
                    await _orderRepository.UpdateAsync(o, cancellationToken);
                    return o;
                })
                .Then("processed", o => o.PaymentStatus == "Completed");

            _logger.LogInformation("Processed order {OrderId}", order.Id);
        }
    }
}
```

### IWorkflowDefinition<TResult>

For workflows that produce a typed result:

```csharp
public interface IWorkflowDefinition<TResult> : IWorkflowDefinition
{
    new ValueTask<TResult> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);
}
```

Example:

```csharp
public class ReportGenerationWorkflow : IWorkflowDefinition<Report>
{
    public string Name => "GenerateReport";
    public string? Description => "Generates daily sales report";

    public async ValueTask<Report> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        return await Workflow
            .Given(context, "date range", () => GetDateRange())
            .When("fetch sales data", async range =>
                await _salesService.GetSalesAsync(range, cancellationToken))
            .When("aggregate data", data => AggregateData(data))
            .When("generate report", data => new Report(data))
            .Then("report valid", report => report.IsValid)
            .GetResultAsync(cancellationToken);
    }

    // Explicit interface implementation for base interface
    async ValueTask IWorkflowDefinition.ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, cancellationToken);
    }
}
```

### IRecurringWorkflowDefinition

For workflows that run on a schedule:

```csharp
public interface IRecurringWorkflowDefinition : IWorkflowDefinition
{
    TimeSpan Interval { get; }
    bool RunImmediately { get; }
}
```

Example:

```csharp
public class InventorySyncWorkflow : IRecurringWorkflowDefinition
{
    public string Name => "InventorySync";
    public string? Description => "Synchronizes inventory with supplier";

    // Run every 5 minutes
    public TimeSpan Interval => TimeSpan.FromMinutes(5);

    // Run immediately on startup, then every 5 minutes
    public bool RunImmediately => true;

    private readonly IInventoryService _inventoryService;
    private readonly ISupplierApi _supplierApi;

    public InventorySyncWorkflow(
        IInventoryService inventoryService,
        ISupplierApi supplierApi)
    {
        _inventoryService = inventoryService;
        _supplierApi = supplierApi;
    }

    public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "current inventory", async () =>
                await _inventoryService.GetAllAsync(cancellationToken))
            .When("fetch supplier inventory", async inventory =>
            {
                var supplierInventory = await _supplierApi.GetInventoryAsync(cancellationToken);
                return (local: inventory, supplier: supplierInventory);
            })
            .When("calculate differences", data =>
                CalculateDifferences(data.local, data.supplier))
            .When("apply updates", async diffs =>
            {
                foreach (var diff in diffs)
                {
                    await _inventoryService.UpdateAsync(diff, cancellationToken);
                }
                return diffs.Count;
            })
            .Then("sync complete", count => count >= 0);
    }
}
```

## Registering Workflows

### Type Registration

```csharp
builder.Services.AddPatternKitHosting()
    .AddWorkflow<OrderProcessingWorkflow>()
    .AddWorkflow<ReportGenerationWorkflow>()
    .AddRecurringWorkflow<InventorySyncWorkflow>();
```

### Instance Registration

```csharp
var workflow = new OrderProcessingWorkflow(repository, paymentService, logger);

builder.Services.AddPatternKitHosting()
    .AddWorkflow(workflow);
```

### Factory Registration

```csharp
builder.Services.AddPatternKitHosting()
    .AddWorkflow(sp => new OrderProcessingWorkflow(
        sp.GetRequiredService<IOrderRepository>(),
        sp.GetRequiredService<IPaymentService>(),
        sp.GetRequiredService<ILogger<OrderProcessingWorkflow>>()));
```

### Workflow Lifetime

```csharp
// Singleton (default) - one instance for entire application
builder.Services.AddWorkflow<OrderProcessingWorkflow>(ServiceLifetime.Singleton);

// Scoped - new instance per execution scope
builder.Services.AddWorkflow<OrderProcessingWorkflow>(ServiceLifetime.Scoped);

// Transient - new instance per resolution
builder.Services.AddWorkflow<OrderProcessingWorkflow>(ServiceLifetime.Transient);
```

## WorkflowHostedService

The `WorkflowHostedService` is a `BackgroundService` that:

1. Starts registered workflows when the host starts
2. Executes one-time workflows immediately (if `StartAutomatically = true`)
3. Starts recurring workflows with their configured intervals
4. Handles cancellation gracefully during shutdown
5. Logs workflow progress and failures

### Execution Flow

```
Host Starts
     │
     ▼
┌──────────────────┐
│ ExecuteAsync()   │
│   called         │
└────────┬─────────┘
         │
         ├─────────────────────────────┐
         │                             │
         ▼                             ▼
┌─────────────────┐          ┌─────────────────────┐
│  One-Time       │          │  Recurring          │
│  Workflows      │          │  Workflows          │
├─────────────────┤          ├─────────────────────┤
│ Execute once    │          │ Loop with           │
│ (parallel or    │          │ PeriodicTimer       │
│  sequential)    │          │                     │
└─────────────────┘          └─────────────────────┘
```

### Parallel Execution

With `EnableParallelExecution = true`:

```csharp
// Executes workflows in parallel using Parallel.ForEachAsync
await Parallel.ForEachAsync(
    workflows,
    new ParallelOptions
    {
        MaxDegreeOfParallelism = options.MaxDegreeOfParallelism ?? Environment.ProcessorCount,
        CancellationToken = stoppingToken
    },
    async (workflow, ct) =>
    {
        var context = _contextFactory.Create(workflow.Name);
        await workflow.ExecuteAsync(context, ct);
    });
```

### Error Handling

```csharp
// With ContinueOnFailure = true (default)
try
{
    await workflow.ExecuteAsync(context, cancellationToken);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Workflow {Name} failed", workflow.Name);
    // Continues with next workflow
}

// With ContinueOnFailure = false
try
{
    await workflow.ExecuteAsync(context, cancellationToken);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Workflow {Name} failed, stopping execution", workflow.Name);
    throw; // Stops all workflows
}
```

## IWorkflowRunner

Service for running workflows on-demand:

```csharp
public interface IWorkflowRunner
{
    // Run by name
    ValueTask<WorkflowContext> RunAsync(string workflowName, CancellationToken cancellationToken = default);

    // Run definition instance
    ValueTask<WorkflowContext> RunAsync(IWorkflowDefinition workflow, CancellationToken cancellationToken = default);

    // Run with typed result
    ValueTask<TResult> RunAsync<TResult>(IWorkflowDefinition<TResult> workflow, CancellationToken cancellationToken = default);

    // Run ad-hoc workflow
    ValueTask<T> RunAsync<T>(
        string workflowName,
        Func<WorkflowContext, CancellationToken, ValueTask<T>> buildWorkflow,
        CancellationToken cancellationToken = default);
}
```

### Using IWorkflowRunner

```csharp
public class OrderController : ControllerBase
{
    private readonly IWorkflowRunner _runner;

    public OrderController(IWorkflowRunner runner)
    {
        _runner = runner;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessOrder(OrderRequest request)
    {
        // Run by name (workflow must be registered)
        var context = await _runner.RunAsync("OrderProcessing");

        if (!context.AllPassed)
        {
            return BadRequest(context.FirstFailure?.Error?.Message);
        }

        return Ok();
    }

    [HttpGet("report")]
    public async Task<IActionResult> GenerateReport()
    {
        // Run with typed result
        var workflow = new ReportGenerationWorkflow(_salesService);
        var report = await _runner.RunAsync(workflow);

        return Ok(report);
    }

    [HttpPost("adhoc")]
    public async Task<IActionResult> RunAdhocWorkflow(AdhocRequest request)
    {
        // Run ad-hoc workflow
        var result = await _runner.RunAsync(
            "AdhocProcessing",
            async (context, ct) =>
            {
                return await Workflow
                    .Given(context, "request", () => request)
                    .When("process", r => Process(r))
                    .Then("valid", r => r.IsValid)
                    .GetResultAsync(ct);
            });

        return Ok(result);
    }
}
```

### WorkflowRunner Implementation

```csharp
public sealed class WorkflowRunner : IWorkflowRunner
{
    public async ValueTask<WorkflowContext> RunAsync(
        IWorkflowDefinition workflow,
        CancellationToken cancellationToken = default)
    {
        // Create a new scope for isolation
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IWorkflowContextFactory>();

        var context = factory.Create(workflow.Name, workflow.Description);

        try
        {
            _logger.LogDebug("Starting workflow {Name}", workflow.Name);
            await workflow.ExecuteAsync(context, cancellationToken);
            _logger.LogDebug("Completed workflow {Name}", workflow.Name);
        }
        finally
        {
            await context.DisposeAsync();
        }

        return context;
    }
}
```

## Complete Example

### Console Application with Background Workflows

```csharp
// Program.cs
using PatternKit.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register dependencies
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<IPaymentService, PaymentService>();
        services.AddSingleton<IInventoryService, InventoryService>();

        // Register PatternKit with hosting
        services.AddPatternKitHosting(
            configureHosting: options =>
            {
                options.StartAutomatically = true;
                options.EnableParallelExecution = true;
            })
            .AddWorkflow<StartupWorkflow>()
            .AddRecurringWorkflow<HealthCheckWorkflow>()
            .AddRecurringWorkflow<DataSyncWorkflow>();
    })
    .Build();

await host.RunAsync();

// Workflows
public class StartupWorkflow : IWorkflowDefinition
{
    public string Name => "Startup";
    public string? Description => "Initializes application on startup";

    public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "application starting", () => DateTime.UtcNow)
            .When("initialize services", async _ =>
            {
                await InitializeAsync(cancellationToken);
                return true;
            })
            .Then("ready", success => success);
    }
}

public class HealthCheckWorkflow : IRecurringWorkflowDefinition
{
    public string Name => "HealthCheck";
    public string? Description => "Periodic health check";
    public TimeSpan Interval => TimeSpan.FromMinutes(1);
    public bool RunImmediately => true;

    public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "health check started", () => DateTime.UtcNow)
            .When("check database", async _ =>
                await CheckDatabaseAsync(cancellationToken))
            .And("check external services", async _ =>
                await CheckServicesAsync(cancellationToken))
            .Then("all healthy", result => result.IsHealthy);
    }
}
```

### ASP.NET Core with On-Demand Workflows

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPatternKitHosting()
    .AddWorkflow<OrderProcessingWorkflow>()
    .AddRecurringWorkflow<CleanupWorkflow>();

var app = builder.Build();

app.MapPost("/orders/{id}/process", async (
    string id,
    IWorkflowRunner runner) =>
{
    var context = await runner.RunAsync("OrderProcessing");

    return context.AllPassed
        ? Results.Ok(new { orderId = id, status = "processed" })
        : Results.Problem(context.FirstFailure?.Error?.Message);
});

app.MapPost("/reports/generate", async (
    ReportRequest request,
    IWorkflowRunner runner) =>
{
    var report = await runner.RunAsync(
        "GenerateReport",
        async (ctx, ct) =>
        {
            return await Workflow
                .Given(ctx, "request", () => request)
                .When("generate", r => GenerateReport(r))
                .Then("valid", r => r.IsValid)
                .GetResultAsync(ct);
        });

    return Results.Ok(report);
});

app.Run();
```

## Best Practices

### 1. Use Scoped Services in Workflows

```csharp
public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
{
    // Get service provider from extension
    var sp = context.GetExtension<ServiceProviderExtension>()?.ServiceProvider;

    using var scope = sp?.CreateScope();
    var dbContext = scope?.ServiceProvider.GetRequiredService<AppDbContext>();

    await Workflow
        .Given(context, "data", async () => await dbContext.GetDataAsync())
        // ...
}
```

### 2. Handle Cancellation Gracefully

```csharp
public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
{
    await Workflow
        .Given(context, "items", async () =>
            await GetItemsAsync(cancellationToken))
        .When("process", async items =>
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessItemAsync(item, cancellationToken);
            }
            return items;
        })
        .Then("complete", _ => true);
}
```

### 3. Log Workflow Progress

```csharp
public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
{
    _logger.LogInformation("Starting workflow {Name}", Name);

    try
    {
        await Workflow.Given(context, "data", () => data)
            // ...
            .Then("success", _ => true);

        _logger.LogInformation(
            "Workflow {Name} completed in {Elapsed}ms",
            Name,
            context.TotalElapsed().TotalMilliseconds);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Workflow {Name} failed", Name);
        throw;
    }
}
```

### 4. Configure Appropriate Timeouts

```csharp
services.AddPatternKitHosting(
    configurePatternKit: options =>
    {
        options.DefaultWorkflowOptions = new WorkflowOptions
        {
            StepTimeout = TimeSpan.FromMinutes(1)
        };
        options.DefaultTimeout = TimeSpan.FromMinutes(10);
    },
    configureHosting: options =>
    {
        options.ShutdownTimeout = TimeSpan.FromMinutes(2);
    });
```
