# Orchestrator Patterns

TinyBDD's Given/When/Then pattern provides a powerful foundation for application orchestration. This guide covers advanced patterns for building production-grade workflows.

## Core Concepts

### Workflow as Documentation

BDD workflows serve as living documentation. Each step describes **what** the system does in business terms:

```csharp
await Bdd.Given(context, "customer places order", () => CreateOrder(customer, items))
    .When("payment is processed", order => ProcessPayment(order))
    .And("inventory is reserved", order => ReserveInventory(order))
    .When("shipment is created", order => CreateShipment(order))
    .Then("customer receives confirmation", order => NotifyCustomer(order));
```

### Traceability

Every step is recorded with:
- **Timing**: Exact duration of each step
- **Input/Output**: State flowing through the workflow
- **Errors**: Any exceptions with full context

```csharp
// After execution
foreach (var step in context.Steps)
{
    Console.WriteLine($"{step.Kind} {step.Title}: {step.Elapsed.TotalMilliseconds}ms");
}

foreach (var io in context.IO)
{
    Console.WriteLine($"{io.Title}: {io.Input?.GetType().Name} -> {io.Output?.GetType().Name}");
}
```

## Workflow Patterns

### Pipeline Pattern

Chain transformations through sequential steps:

```csharp
public async Task<ProcessedData> ProcessDataPipeline(RawData input, CancellationToken ct)
{
    var context = _factory.Create("Data Pipeline", "ETL Process");

    await Bdd.Given(context, "raw data received", () => input)
        .When("validated", data => Validate(data))
        .When("normalized", data => Normalize(data))
        .When("enriched", data => Enrich(data, ct))
        .When("transformed", data => Transform(data))
        .Then("ready for storage", data => data.IsValid);

    return (ProcessedData)context.CurrentItem!;
}
```

### Saga Pattern

Coordinate distributed transactions with compensation:

```csharp
public class OrderSaga
{
    public async Task<SagaResult> ExecuteAsync(Order order, CancellationToken ct)
    {
        var context = _factory.Create("Order Saga", $"Process order {order.Id}");
        var compensations = new Stack<Func<Task>>();

        try
        {
            await Bdd.Given(context, "order received", () => order)
                .When("payment reserved", async o =>
                {
                    var reservation = await _payments.ReserveAsync(o, ct);
                    compensations.Push(() => _payments.ReleaseAsync(reservation));
                    return o with { PaymentReservation = reservation };
                })
                .When("inventory locked", async o =>
                {
                    var lock = await _inventory.LockAsync(o.Items, ct);
                    compensations.Push(() => _inventory.UnlockAsync(lock));
                    return o with { InventoryLock = lock };
                })
                .When("payment captured", async o =>
                {
                    await _payments.CaptureAsync(o.PaymentReservation!, ct);
                    compensations.Pop(); // Payment no longer needs compensation
                    return o;
                })
                .When("inventory committed", async o =>
                {
                    await _inventory.CommitAsync(o.InventoryLock!, ct);
                    compensations.Pop(); // Inventory no longer needs compensation
                    return o;
                })
                .Then("order completed", o => o.PaymentReservation != null);

            return SagaResult.Success(context);
        }
        catch (Exception ex)
        {
            // Compensate in reverse order
            while (compensations.Count > 0)
            {
                try { await compensations.Pop()(); }
                catch { /* Log compensation failure */ }
            }
            return SagaResult.Failed(context, ex);
        }
    }
}
```

### Circuit Breaker Pattern

Protect against cascading failures:

```csharp
public class ResilientWorkflow
{
    private readonly CircuitBreaker _circuitBreaker;

    public async Task<Result> ExecuteWithCircuitBreaker(Request request, CancellationToken ct)
    {
        var context = _factory.Create("Resilient Workflow", "With circuit breaker");

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            await Bdd.Given(context, "request prepared", () => request)
                .When("external service called", async r =>
                {
                    // This call is protected by the circuit breaker
                    return await _externalService.CallAsync(r, ct);
                })
                .Then("response valid", response => response.IsSuccess);

            return new Result(context);
        });
    }
}
```

### Retry Pattern

Automatic retry with exponential backoff:

```csharp
public async Task<Result> ExecuteWithRetry(Request request, CancellationToken ct)
{
    var context = _factory.Create("Retry Workflow", "With automatic retry");
    var maxRetries = 3;
    var delay = TimeSpan.FromSeconds(1);

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await Bdd.Given(context, $"attempt {attempt}", () => request)
                .When("processed", r => Process(r, ct))
                .Then("successful", result => result.Success);

            return new Result(context);
        }
        catch (TransientException) when (attempt < maxRetries)
        {
            await Task.Delay(delay * Math.Pow(2, attempt - 1), ct);
        }
    }

    throw new MaxRetriesExceededException();
}
```

### Fan-Out/Fan-In Pattern

Parallel processing with aggregation:

```csharp
public async Task<AggregatedResult> ProcessInParallel(IList<Item> items, CancellationToken ct)
{
    var context = _factory.Create("Parallel Processing", "Fan-out/Fan-in");

    await Bdd.Given(context, "items to process", () => items)
        .When("processed in parallel", async items =>
        {
            var tasks = items.Select(item => ProcessItemAsync(item, ct));
            return await Task.WhenAll(tasks);
        })
        .When("results aggregated", results => Aggregate(results))
        .Then("all items processed", agg => agg.SuccessCount == items.Count);

    return (AggregatedResult)context.CurrentItem!;
}
```

### State Machine Pattern

Express workflows as state transitions:

```csharp
public class OrderStateMachine
{
    public async Task<Order> TransitionAsync(Order order, OrderEvent evt, CancellationToken ct)
    {
        var context = _factory.Create("Order State Machine", $"{order.State} -> ?");

        await Bdd.Given(context, $"order in {order.State} state", () => order)
            .When($"event {evt} received", o => ValidateTransition(o, evt))
            .When("transition executed", o => ExecuteTransition(o, evt, ct))
            .And("side effects applied", o => ApplySideEffects(o, evt, ct))
            .Then("new state reached", o => o.State != order.State || evt == OrderEvent.NoOp);

        return (Order)context.CurrentItem!;
    }

    private Order ExecuteTransition(Order order, OrderEvent evt, CancellationToken ct)
    {
        return (order.State, evt) switch
        {
            (OrderState.Created, OrderEvent.Submit) => order with { State = OrderState.Pending },
            (OrderState.Pending, OrderEvent.Pay) => order with { State = OrderState.Paid },
            (OrderState.Paid, OrderEvent.Ship) => order with { State = OrderState.Shipped },
            (OrderState.Shipped, OrderEvent.Deliver) => order with { State = OrderState.Delivered },
            _ => throw new InvalidOperationException($"Invalid transition: {order.State} + {evt}")
        };
    }
}
```

## Composition Patterns

### Workflow Composition

Compose complex workflows from simpler ones:

```csharp
public class CompositeWorkflow
{
    private readonly PaymentWorkflow _payment;
    private readonly InventoryWorkflow _inventory;
    private readonly ShippingWorkflow _shipping;

    public async Task<OrderResult> ProcessOrder(Order order, CancellationToken ct)
    {
        var context = _factory.Create("Order Processing", "Composite workflow");

        await Bdd.Given(context, "order validated", () => ValidateOrder(order))
            .When("payment processed", async o =>
            {
                var paymentResult = await _payment.ProcessAsync(o, ct);
                return o with { Payment = paymentResult };
            })
            .When("inventory reserved", async o =>
            {
                var inventoryResult = await _inventory.ReserveAsync(o, ct);
                return o with { Inventory = inventoryResult };
            })
            .When("shipment created", async o =>
            {
                var shippingResult = await _shipping.CreateAsync(o, ct);
                return o with { Shipping = shippingResult };
            })
            .Then("order complete", o => o.IsComplete);

        return new OrderResult(context);
    }
}
```

### Decorator Pattern

Add cross-cutting concerns:

```csharp
public class LoggingWorkflowDecorator : IWorkflowRunner
{
    private readonly IWorkflowRunner _inner;
    private readonly ILogger _logger;

    public async Task<ScenarioContext> RunAsync(IWorkflowDefinition workflow, CancellationToken ct)
    {
        _logger.LogInformation("Starting workflow: {Feature} - {Scenario}",
            workflow.FeatureName, workflow.ScenarioName);

        var sw = Stopwatch.StartNew();
        try
        {
            var context = await _inner.RunAsync(workflow, ct);

            _logger.LogInformation(
                "Workflow completed: {Feature} - {Scenario} in {Duration}ms, {StepCount} steps",
                workflow.FeatureName, workflow.ScenarioName,
                sw.ElapsedMilliseconds, context.Steps.Count);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Workflow failed: {Feature} - {Scenario} after {Duration}ms",
                workflow.FeatureName, workflow.ScenarioName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## Error Handling Patterns

### Graceful Degradation

Continue with reduced functionality:

```csharp
public async Task<Response> ProcessWithFallback(Request request, CancellationToken ct)
{
    var context = _factory.Create("Resilient Service", "With fallback",
        options: new ScenarioOptions { ContinueOnError = true });

    await Bdd.Given(context, "request received", () => request)
        .When("primary service called", async r =>
        {
            try { return await _primaryService.ProcessAsync(r, ct); }
            catch { return null; }
        })
        .When("fallback if needed", async result =>
        {
            if (result == null)
                return await _fallbackService.ProcessAsync(request, ct);
            return result;
        })
        .Then("response available", response => response != null);

    return (Response)context.CurrentItem!;
}
```

### Partial Failure Handling

Collect all failures before reporting:

```csharp
public async Task<BatchResult> ProcessBatch(IList<Item> items, CancellationToken ct)
{
    var context = _factory.Create("Batch Processing", "With partial failure handling",
        options: new ScenarioOptions
        {
            ContinueOnError = true,
            MarkRemainingAsSkippedOnFailure = false
        });

    var results = new List<ItemResult>();

    foreach (var item in items)
    {
        try
        {
            await Bdd.Given(context, $"process item {item.Id}", () => item)
                .When("validated", i => Validate(i))
                .When("transformed", i => Transform(i))
                .Then("saved", i => Save(i, ct));

            results.Add(new ItemResult(item.Id, true));
        }
        catch (Exception ex)
        {
            results.Add(new ItemResult(item.Id, false, ex.Message));
        }
    }

    return new BatchResult(results, context);
}
```

## Performance Patterns

### Lazy Initialization

Defer expensive operations:

```csharp
public async Task<Result> ProcessLazy(Request request, CancellationToken ct)
{
    var context = _factory.Create("Lazy Workflow", "Deferred initialization");

    Lazy<Task<ExpensiveResource>> lazyResource = new(() => LoadExpensiveResource(ct));

    await Bdd.Given(context, "request analyzed", () => AnalyzeRequest(request))
        .When("resource loaded if needed", async analysis =>
        {
            if (analysis.NeedsResource)
                return await lazyResource.Value;
            return null;
        })
        .When("processed", (resource, analysis) => Process(resource, analysis, ct))
        .Then("complete", result => result.Success);

    return new Result(context);
}
```

### Caching

Cache expensive step results:

```csharp
public class CachingWorkflow
{
    private readonly IDistributedCache _cache;

    public async Task<Result> ProcessWithCache(Request request, CancellationToken ct)
    {
        var context = _factory.Create("Cached Workflow", "With distributed cache");
        var cacheKey = $"workflow:{request.Id}";

        await Bdd.Given(context, "check cache", async () =>
            {
                var cached = await _cache.GetAsync<IntermediateResult>(cacheKey, ct);
                return (Request: request, Cached: cached);
            })
            .When("process or use cached", async state =>
            {
                if (state.Cached != null)
                    return state.Cached;

                var result = await ExpensiveProcess(state.Request, ct);
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1), ct);
                return result;
            })
            .Then("result available", result => result != null);

        return new Result(context);
    }
}
```

## Testing Patterns

### Workflow Testing

```csharp
public class WorkflowTests
{
    [Fact]
    public async Task OrderWorkflow_WithValidOrder_CompletesAllSteps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTinyBdd();
        services.AddSingleton<IPaymentService>(new FakePaymentService());
        services.AddSingleton<IInventoryService>(new FakeInventoryService());

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IScenarioContextFactory>();
        var workflow = new OrderWorkflow(factory, /* dependencies */);

        // Act
        var result = await workflow.ProcessAsync(new Order { /* ... */ });

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.Context.Steps.Count);
        Assert.All(result.Context.Steps, step => Assert.Null(step.Error));
    }

    [Fact]
    public async Task OrderWorkflow_WithPaymentFailure_RecordsError()
    {
        // Arrange
        var failingPayment = new FakePaymentService { ShouldFail = true };
        // ... setup

        // Act
        var result = await workflow.ProcessAsync(new Order { /* ... */ });

        // Assert
        Assert.False(result.Success);
        var failedStep = result.Context.Steps.First(s => s.Error != null);
        Assert.Contains("payment", failedStep.Title.ToLower());
    }
}
```

## Best Practices

1. **Name steps clearly**: Use business language, not technical jargon
2. **Keep steps focused**: Each step should do one thing well
3. **Handle errors explicitly**: Use `ContinueOnError` or try/catch as appropriate
4. **Log step results**: Capture execution details for debugging
5. **Test workflows**: Use fake dependencies to test all paths
6. **Monitor performance**: Track step durations in production
7. **Use composition**: Build complex workflows from simpler ones

## Next Steps

- [Enterprise Samples](samples-enterprise.md) - Production-ready examples
- [Dependency Injection](extensions/dependency-injection.md) - DI integration
- [Hosting](extensions/hosting.md) - Background service patterns
- [Reporting Extension](extensions/reporting.md) - JSON reporting and observer pattern

Return to: [User Guide](index.md)
