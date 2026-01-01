# Samples & Tutorials

This section provides practical examples and step-by-step tutorials for using PatternKit in real-world scenarios.

## Samples Overview

| Sample | Description | Complexity |
|--------|-------------|------------|
| [Basic Workflows](basic-workflows.md) | Core fluent API usage patterns | Beginner |
| [Testing Patterns](testing-patterns.md) | Unit and integration testing | Beginner |
| [Enterprise Integration](enterprise-integration.md) | DI, hosting, and production patterns | Intermediate |
| [Advanced Patterns](advanced-patterns.md) | Custom behaviors, composition, and optimization | Advanced |

## Quick Start Examples

### Minimal Workflow

```csharp
using PatternKit.Core;

var ctx = new WorkflowContext { WorkflowName = "QuickStart" };

var result = await Workflow
    .Given(ctx, "input", () => 42)
    .When("double", x => x * 2)
    .Then("is 84", x => x == 84)
    .GetResultAsync();

Console.WriteLine($"Result: {result}, All Passed: {ctx.AllPassed}");
```

### With Error Handling

```csharp
var ctx = new WorkflowContext
{
    WorkflowName = "ErrorHandling",
    Options = new WorkflowOptions
    {
        ContinueOnError = true,
        MarkRemainingAsSkippedOnFailure = true
    }
};

try
{
    await Workflow
        .Given(ctx, "data", () => GetData())
        .When("process", data => ProcessData(data))
        .Then("valid", result => result.IsValid)
        .AssertPassed();
}
catch (WorkflowAssertionException ex)
{
    Console.WriteLine($"Workflow failed: {ex.Message}");

    foreach (var failure in ctx.GetFailedSteps())
    {
        Console.WriteLine($"  {failure.Kind} {failure.Title}: {failure.Error?.Message}");
    }
}
```

### With Dependency Injection

```csharp
// Startup
services.AddPatternKit()
    .AddStepHandler<CreateUserRequest, User, CreateUserHandler>()
    .AddTimingBehavior<User>();

// Usage
public class UserService
{
    private readonly IWorkflowContextFactory _contextFactory;
    private readonly IStepHandlerFactory _handlerFactory;

    public UserService(
        IWorkflowContextFactory contextFactory,
        IStepHandlerFactory handlerFactory)
    {
        _contextFactory = contextFactory;
        _handlerFactory = handlerFactory;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        var context = _contextFactory.Create("CreateUser");

        return await Workflow
            .Given(context, "user data", () => dto)
            .Handle("create user", _handlerFactory, d =>
                new CreateUserRequest(d.Email, d.Name))
            .Then("created", user => user.Id != null)
            .GetResultAsync();
    }
}
```

### Background Workflow

```csharp
// Define recurring workflow
public class MetricsCollectionWorkflow : IRecurringWorkflowDefinition
{
    public string Name => "MetricsCollection";
    public string? Description => "Collects and reports system metrics";
    public TimeSpan Interval => TimeSpan.FromMinutes(1);
    public bool RunImmediately => true;

    private readonly IMetricsService _metrics;

    public MetricsCollectionWorkflow(IMetricsService metrics)
    {
        _metrics = metrics;
    }

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "timestamp", () => DateTime.UtcNow)
            .When("collect cpu", async _ =>
                await _metrics.GetCpuUsageAsync(cancellationToken))
            .And("collect memory", async cpu =>
            {
                var memory = await _metrics.GetMemoryUsageAsync(cancellationToken);
                return (cpu, memory);
            })
            .When("report metrics", async data =>
            {
                await _metrics.ReportAsync(data.cpu, data.memory, cancellationToken);
                return data;
            })
            .Then("reported", _ => true);
    }
}

// Register
services.AddPatternKitHosting()
    .AddRecurringWorkflow<MetricsCollectionWorkflow>();
```

## Common Use Cases

### E-Commerce Order Processing

```csharp
public class OrderProcessingWorkflow : IWorkflowDefinition<Order>
{
    public string Name => "ProcessOrder";

    public async ValueTask<Order> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        return await Workflow
            .Given(context, "pending order", async () =>
                await _orderRepo.GetNextPendingAsync(cancellationToken))
            .When("validate inventory", async order =>
            {
                foreach (var item in order.Items)
                {
                    var available = await _inventory.CheckAsync(item.ProductId, cancellationToken);
                    if (available < item.Quantity)
                        throw new InsufficientInventoryException(item.ProductId);
                }
                return order;
            })
            .When("process payment", async order =>
            {
                var result = await _payment.ChargeAsync(order, cancellationToken);
                if (!result.Success)
                    throw new PaymentException(result.ErrorMessage);
                order.PaymentId = result.TransactionId;
                return order;
            })
            .And("reserve inventory", async order =>
            {
                foreach (var item in order.Items)
                {
                    await _inventory.ReserveAsync(item.ProductId, item.Quantity, cancellationToken);
                }
                return order;
            })
            .When("update status", async order =>
            {
                order.Status = OrderStatus.Confirmed;
                await _orderRepo.UpdateAsync(order, cancellationToken);
                return order;
            })
            .And("send confirmation", async order =>
            {
                await _email.SendOrderConfirmationAsync(order, cancellationToken);
                return order;
            })
            .Then("processed", order => order.Status == OrderStatus.Confirmed)
            .GetResultAsync(cancellationToken);
    }
}
```

### Data Pipeline

```csharp
public async Task<ProcessingResult> ProcessDataPipelineAsync(
    DataSource source,
    CancellationToken cancellationToken)
{
    var context = _contextFactory.Create("DataPipeline", $"Processing {source.Name}");

    return await Workflow
        .Given(context, "data source", () => source)
        .When("extract data", async src =>
        {
            var rawData = await _extractor.ExtractAsync(src, cancellationToken);
            context.SetMetadata("extractedRows", rawData.Count);
            return rawData;
        })
        .When("transform data", data =>
        {
            var transformed = _transformer.Transform(data);
            context.SetMetadata("transformedRows", transformed.Count);
            return transformed;
        })
        .When("validate data", data =>
        {
            var errors = _validator.Validate(data);
            if (errors.Any())
            {
                context.SetMetadata("validationErrors", errors);
                data = data.Where(d => !errors.ContainsKey(d.Id)).ToList();
            }
            return data;
        })
        .When("load data", async data =>
        {
            await _loader.LoadAsync(data, cancellationToken);
            context.SetMetadata("loadedRows", data.Count);
            return new ProcessingResult
            {
                Extracted = context.GetMetadata<int>("extractedRows"),
                Transformed = context.GetMetadata<int>("transformedRows"),
                Loaded = context.GetMetadata<int>("loadedRows"),
                Errors = context.GetMetadata<Dictionary<string, string>>("validationErrors")
            };
        })
        .Then("pipeline complete", result => result.Loaded > 0)
        .GetResultAsync(cancellationToken);
}
```

### API Validation Workflow

```csharp
app.MapPost("/api/users", async (
    CreateUserRequest request,
    IWorkflowContextFactory contextFactory) =>
{
    var context = contextFactory.Create("CreateUser");

    try
    {
        var user = await Workflow
            .Given(context, "request", () => request)
            .When("validate email", req =>
            {
                if (!IsValidEmail(req.Email))
                    throw new ValidationException("Invalid email format");
                return req;
            })
            .And("check email uniqueness", async req =>
            {
                if (await _userRepo.EmailExistsAsync(req.Email))
                    throw new ValidationException("Email already registered");
                return req;
            })
            .And("validate password", req =>
            {
                if (!IsStrongPassword(req.Password))
                    throw new ValidationException("Password does not meet requirements");
                return req;
            })
            .When("create user", async req =>
            {
                var user = new User
                {
                    Email = req.Email,
                    PasswordHash = HashPassword(req.Password),
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepo.CreateAsync(user);
                return user;
            })
            .Then("created", user => user.Id != null)
            .GetResultAsync();

        return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
```

## Performance Optimization Example

```csharp
// High-performance workflow with state-passing to avoid closures
public async Task<ProcessingResult> ProcessHighThroughputAsync(
    BatchRequest batch,
    ProcessingConfig config,
    CancellationToken cancellationToken)
{
    var context = new WorkflowContext
    {
        WorkflowName = "HighThroughputProcessing",
        Options = new WorkflowOptions
        {
            StepTimeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
        }
    };

    // State-passing eliminates closure allocations
    return await Workflow
        .Given(context, "batch", batch, b => b.Items)
        .When("filter", config, (items, cfg) =>
            items.Where(i => i.Priority >= cfg.MinPriority).ToList())
        .When("process", config, async (items, cfg) =>
        {
            var results = new List<ItemResult>(items.Count);

            await Parallel.ForEachAsync(
                items,
                new ParallelOptions { MaxDegreeOfParallelism = cfg.MaxParallelism },
                async (item, ct) =>
                {
                    var result = await ProcessItemAsync(item, ct);
                    lock (results) results.Add(result);
                });

            return results;
        })
        .When("aggregate", config, (results, cfg) =>
            new ProcessingResult
            {
                Successful = results.Count(r => r.Success),
                Failed = results.Count(r => !r.Success),
                Duration = context.TotalElapsed()
            })
        .Then("completed", config, (result, cfg) =>
            result.Failed <= cfg.MaxAllowedFailures)
        .GetResultAsync(cancellationToken);
}
```

## Next Steps

- [Basic Workflows](basic-workflows.md) - Start with fundamentals
- [Testing Patterns](testing-patterns.md) - Learn testing strategies
- [Enterprise Integration](enterprise-integration.md) - Production-ready patterns
- [Advanced Patterns](advanced-patterns.md) - Optimization and composition
