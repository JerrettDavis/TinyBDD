# Dependency Injection Integration

**TinyBDD.Extensions.DependencyInjection** provides seamless integration between TinyBDD and `Microsoft.Extensions.DependencyInjection`, enabling DI-aware scenario context creation and configuration.

## Installation

```bash
dotnet add package TinyBDD.Extensions.DependencyInjection
```

## Quick Start

```csharp
// In Startup.cs or Program.cs
services.AddTinyBdd();

// In your service or controller
public class OrderService
{
    private readonly IScenarioContextFactory _contextFactory;

    public OrderService(IScenarioContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<OrderResult> ProcessOrder(Order order)
    {
        var context = _contextFactory.Create("Order Processing", "Process single order");

        await Bdd.Given(context, "order received", () => order)
            .When("validated", o => ValidateOrder(o))
            .When("saved to database", o => SaveOrder(o))
            .Then("confirmation sent", o => SendConfirmation(o));

        return new OrderResult { Success = context.Steps.All(s => s.Error == null) };
    }
}
```

## Service Registration

### Basic Registration

```csharp
services.AddTinyBdd();
```

This registers:
- `IScenarioContextFactory` as scoped
- `ITraitBridge` as singleton (default `NullTraitBridge`)
- `TinyBddOptions` with defaults

### With Configuration

```csharp
services.AddTinyBdd(options =>
{
    options.DefaultScenarioOptions = new ScenarioOptions
    {
        ContinueOnError = true,
        HaltOnFailedAssertion = false,
        StepTimeout = TimeSpan.FromSeconds(30),
        MarkRemainingAsSkippedOnFailure = true
    };
    options.EnableStepTiming = true;
});
```

### Custom Trait Bridge

For test framework integration or custom tag handling:

```csharp
// Using a type
services.AddTinyBddTraitBridge<MyCustomTraitBridge>();

// Using an instance
services.AddTinyBddTraitBridge(new LoggingTraitBridge(logger));
```

## API Reference

### TinyBddOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultScenarioOptions` | `ScenarioOptions` | `new()` | Default options applied to all created contexts |
| `RegisterContextFactory` | `bool` | `true` | Whether to register `IScenarioContextFactory` |
| `EnableStepTiming` | `bool` | `false` | Enable detailed step timing through DI pipeline |

### IScenarioContextFactory

```csharp
public interface IScenarioContextFactory
{
    /// <summary>
    /// Creates a new ScenarioContext with explicit names.
    /// </summary>
    ScenarioContext Create(
        string featureName,
        string scenarioName,
        string? featureDescription = null);

    /// <summary>
    /// Creates a ScenarioContext from object type attributes.
    /// </summary>
    ScenarioContext CreateFromAttributes(
        object featureSource,
        string? scenarioName = null);
}
```

## Usage Patterns

### In ASP.NET Core Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IScenarioContextFactory _factory;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IScenarioContextFactory factory,
        ILogger<WorkflowController> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessRequest([FromBody] Request request)
    {
        var context = _factory.Create("API Processing", $"Process {request.Type}");

        try
        {
            await Bdd.Given(context, "request received", () => request)
                .When("validated", r => Validate(r))
                .When("processed", r => Process(r))
                .Then("completed", r => r.IsComplete);

            // Log step results
            foreach (var step in context.Steps)
            {
                _logger.LogInformation(
                    "{Kind} {Title}: {Status} ({Elapsed}ms)",
                    step.Kind, step.Title,
                    step.Error == null ? "OK" : "FAILED",
                    step.Elapsed.TotalMilliseconds);
            }

            return Ok(new { Steps = context.Steps.Count, Success = true });
        }
        catch (BddStepException ex)
        {
            _logger.LogError(ex, "Workflow failed at step: {Step}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
    }
}
```

### In Background Services

```csharp
public class DataSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DataSyncService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IScenarioContextFactory>();
            var context = factory.Create("Data Sync", "Synchronize external data");

            await Bdd.Given(context, "fetch pending items", () => GetPendingItems())
                .When("transform data", items => TransformItems(items))
                .When("save to database", data => SaveData(data))
                .Then("all items synced", result => result.FailedCount == 0);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### With Scoped Dependencies

```csharp
public class OrderWorkflow
{
    private readonly IScenarioContextFactory _factory;
    private readonly IOrderRepository _repository;
    private readonly IPaymentService _payments;
    private readonly INotificationService _notifications;

    public OrderWorkflow(
        IScenarioContextFactory factory,
        IOrderRepository repository,
        IPaymentService payments,
        INotificationService notifications)
    {
        _factory = factory;
        _repository = repository;
        _payments = payments;
        _notifications = notifications;
    }

    public async Task<OrderResult> FulfillOrder(Guid orderId, CancellationToken ct)
    {
        var context = _factory.Create("Order Fulfillment", $"Fulfill order {orderId}");

        await Bdd.Given(context, "order loaded", () => _repository.GetByIdAsync(orderId))
            .When("payment processed", order => _payments.ProcessAsync(order, ct))
            .And("inventory reserved", order => _repository.ReserveInventoryAsync(order, ct))
            .When("shipment created", order => CreateShipmentAsync(order, ct))
            .Then("customer notified", order => _notifications.SendOrderConfirmationAsync(order, ct));

        return new OrderResult
        {
            OrderId = orderId,
            Success = context.Steps.All(s => s.Error == null),
            Steps = context.Steps.Select(s => new StepSummary(s.Kind, s.Title, s.Error?.Message)).ToList()
        };
    }
}
```

## Testing with DI

The same workflow classes work in both production and test environments:

```csharp
public class OrderWorkflowTests
{
    [Fact]
    public async Task FulfillOrder_WithValidOrder_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTinyBdd();
        services.AddSingleton<IOrderRepository>(new FakeOrderRepository());
        services.AddSingleton<IPaymentService>(new FakePaymentService());
        services.AddSingleton<INotificationService>(new FakeNotificationService());
        services.AddScoped<OrderWorkflow>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<OrderWorkflow>();

        // Act
        var result = await workflow.FulfillOrder(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4, result.Steps.Count);
    }
}
```

## Configuration from appsettings.json

```csharp
// appsettings.json
{
  "TinyBdd": {
    "StepTimeoutSeconds": 30,
    "ContinueOnError": false
  }
}

// Program.cs
services.AddTinyBdd(options =>
{
    var config = configuration.GetSection("TinyBdd");
    options.DefaultScenarioOptions = new ScenarioOptions
    {
        StepTimeout = TimeSpan.FromSeconds(config.GetValue<int>("StepTimeoutSeconds", 30)),
        ContinueOnError = config.GetValue<bool>("ContinueOnError", false)
    };
});
```

## Best Practices

1. **Use scoped contexts**: Create contexts within request/operation scope, not as singletons
2. **Log step results**: Capture `context.Steps` for observability in production
3. **Handle exceptions gracefully**: Wrap workflows in try/catch for `BddStepException`
4. **Configure timeouts**: Use `StepTimeout` to prevent runaway operations
5. **Test with real DI**: Use the same service registration in tests for confidence

## Next Steps

- [Hosting Integration](hosting.md) - Run workflows as hosted services
- [Orchestrator Patterns](../orchestrator-patterns.md) - Advanced workflow patterns
- [Enterprise Samples](../samples-enterprise.md) - Production-ready examples

Return to: [Extensions Index](index.md) | [User Guide](../index.md)
