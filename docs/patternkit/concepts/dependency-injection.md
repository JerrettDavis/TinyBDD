# Dependency Injection Integration

PatternKit integrates seamlessly with Microsoft.Extensions.DependencyInjection, enabling enterprise-grade dependency management for workflows, handlers, and behaviors.

## Installation

```bash
dotnet add package PatternKit.Extensions.DependencyInjection
```

## Basic Setup

### Registering PatternKit Services

```csharp
using PatternKit.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add PatternKit with default configuration
builder.Services.AddPatternKit();

// Or with custom configuration
builder.Services.AddPatternKit(options =>
{
    options.UseScopedContexts = true;
    options.AutoRegisterBehaviors = true;
    options.EnableMetrics = true;
    options.DefaultTimeout = TimeSpan.FromMinutes(5);
});
```

### What Gets Registered

`AddPatternKit()` registers these services:

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| `PatternKitOptions` | Configuration instance | Singleton |
| `IStepHandlerFactory` | `ServiceProviderStepHandlerFactory` | Singleton |
| `IWorkflowContextFactory` | `DefaultWorkflowContextFactory` | Singleton |
| `WorkflowContext` | Factory-created | Scoped or Transient |

## PatternKitOptions

Configure PatternKit behavior:

```csharp
public sealed class PatternKitOptions
{
    // Default options applied to new contexts
    public WorkflowOptions DefaultWorkflowOptions { get; set; } = WorkflowOptions.Default;

    // Auto-discover and register behaviors from assemblies
    public bool AutoRegisterBehaviors { get; set; } = true;

    // Enable metrics collection
    public bool EnableMetrics { get; set; } = false;

    // Default timeout for all workflows
    public TimeSpan? DefaultTimeout { get; set; }

    // Ordered list of behavior types to register
    public List<Type> BehaviorTypes { get; set; } = new();

    // Use scoped (true) or transient (false) context lifetime
    public bool UseScopedContexts { get; set; } = true;
}
```

### Configuration Examples

```csharp
// Strict timeout configuration
services.AddPatternKit(options =>
{
    options.DefaultWorkflowOptions = new WorkflowOptions
    {
        StepTimeout = TimeSpan.FromSeconds(30),
        HaltOnFailedAssertion = true,
        ContinueOnError = false
    };
    options.DefaultTimeout = TimeSpan.FromMinutes(5);
});

// Test-friendly configuration
services.AddPatternKit(options =>
{
    options.UseScopedContexts = false;  // Transient for test isolation
    options.DefaultWorkflowOptions = new WorkflowOptions
    {
        ContinueOnError = true,  // Run all steps for debugging
        MarkRemainingAsSkippedOnFailure = true
    };
});
```

## IWorkflowContextFactory

Factory for creating properly configured workflow contexts:

```csharp
public interface IWorkflowContextFactory
{
    WorkflowContext Create(string workflowName, string? description = null);
    WorkflowContext Create(string workflowName, WorkflowOptions options, string? description = null);
}
```

### Using the Factory

```csharp
public class OrderService
{
    private readonly IWorkflowContextFactory _contextFactory;

    public OrderService(IWorkflowContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        var context = _contextFactory.Create(
            "ProcessOrder",
            $"Processing order for customer {request.CustomerId}");

        try
        {
            var order = await Workflow
                .Given(context, "order request", () => request)
                .When("validate", r => ValidateRequest(r))
                .When("create order", r => CreateOrder(r))
                .Then("order valid", o => o.IsValid)
                .GetResultAsync();

            return order;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}
```

### Custom Context Options

```csharp
var context = _contextFactory.Create(
    "CriticalWorkflow",
    new WorkflowOptions
    {
        StepTimeout = TimeSpan.FromSeconds(5),
        HaltOnFailedAssertion = true
    },
    "High-priority workflow with strict timeouts");
```

## Scoped vs Transient Contexts

### Scoped Contexts (Default)

With `UseScopedContexts = true`:
- One `WorkflowContext` per DI scope
- Same context shared within a request
- Useful for web applications

```csharp
public class Controller1
{
    private readonly WorkflowContext _context;  // Injected

    public Controller1(WorkflowContext context)
    {
        _context = context;
    }
}

public class Service1
{
    private readonly WorkflowContext _context;  // Same instance within scope

    public Service1(WorkflowContext context)
    {
        _context = context;
    }
}
```

### Transient Contexts

With `UseScopedContexts = false`:
- New `WorkflowContext` per injection
- Isolated contexts
- Better for background services or tests

```csharp
services.AddPatternKit(options =>
{
    options.UseScopedContexts = false;
});

// Each injection gets a new context
public class Processor
{
    private readonly WorkflowContext _context;  // Unique instance

    public Processor(WorkflowContext context)
    {
        _context = context;
    }
}
```

## Registering Step Handlers

### Type Registration

```csharp
services.AddPatternKit()
    .AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>()
    .AddStepHandler<ProcessPaymentRequest, PaymentResult, ProcessPaymentHandler>()
    .AddStepHandler<SendEmailRequest, Unit, SendEmailHandler>();
```

### Instance Registration

```csharp
var emailHandler = new SendEmailHandler(smtpClient, templates);

services.AddPatternKit()
    .AddStepHandler<SendEmailRequest, Unit>(emailHandler);
```

### Factory Registration

```csharp
services.AddPatternKit()
    .AddStepHandler<CreateOrderRequest, Order>(sp =>
        new CreateOrderHandler(
            sp.GetRequiredService<IOrderRepository>(),
            sp.GetRequiredService<ILogger<CreateOrderHandler>>()));
```

### Handler Lifetime

```csharp
// Transient (default) - new instance per resolution
services.AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>(
    ServiceLifetime.Transient);

// Scoped - one instance per scope
services.AddStepHandler<ProcessPaymentRequest, PaymentResult, ProcessPaymentHandler>(
    ServiceLifetime.Scoped);

// Singleton - shared instance
services.AddStepHandler<SendEmailRequest, Unit, SendEmailHandler>(
    ServiceLifetime.Singleton);
```

## Registering Behaviors

### Type Registration

```csharp
services.AddPatternKit()
    .AddBehavior<Order, LoggingBehavior<Order>>()
    .AddBehavior<Order, MetricsBehavior<Order>>();
```

### Instance Registration

```csharp
services.AddPatternKit()
    .AddBehavior<Order>(new CustomBehavior<Order>(config));
```

### Built-in Behaviors

```csharp
services.AddPatternKit()
    // Timing - records step duration in metadata
    .AddTimingBehavior<Order>()

    // Retry - retries with exponential backoff
    .AddRetryBehavior<Order>(maxRetries: 3, baseDelay: TimeSpan.FromSeconds(1))

    // Circuit breaker - fault tolerance
    .AddCircuitBreakerBehavior<Order>(
        failureThreshold: 5,
        openDuration: TimeSpan.FromSeconds(30));
```

### Behavior Order

Behaviors execute in registration order:

```csharp
services.AddPatternKit()
    .AddBehavior<Order, LoggingBehavior<Order>>()     // 1st (outer)
    .AddBehavior<Order, MetricsBehavior<Order>>()     // 2nd
    .AddRetryBehavior<Order>()                        // 3rd
    .AddCircuitBreakerBehavior<Order>();              // 4th (inner)
```

## ServiceProviderExtension

Access the service provider from within workflows:

```csharp
public class WorkflowWithDependencies
{
    public async Task ExecuteAsync(WorkflowContext context)
    {
        // Get the service provider extension
        var spExt = context.GetExtension<ServiceProviderExtension>();
        var sp = spExt?.ServiceProvider;

        await Workflow
            .Given(context, "data", () => GetData())
            .When("process", async data =>
            {
                // Resolve services within step
                var processor = sp?.GetRequiredService<IDataProcessor>();
                return await processor.ProcessAsync(data);
            })
            .Then("valid", result => result.IsValid);
    }
}
```

### Automatic Extension Attachment

`DefaultWorkflowContextFactory` automatically attaches `ServiceProviderExtension`:

```csharp
public class DefaultWorkflowContextFactory : IWorkflowContextFactory
{
    public WorkflowContext Create(string workflowName, string? description = null)
    {
        var context = new WorkflowContext
        {
            WorkflowName = workflowName,
            Description = description,
            Options = _options.DefaultWorkflowOptions
        };

        // Automatically attached
        context.SetExtension(new ServiceProviderExtension(_serviceProvider));

        return context;
    }
}
```

## Using with ASP.NET Core

### Web API Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register PatternKit
builder.Services.AddPatternKit(options =>
{
    options.UseScopedContexts = true;
    options.DefaultWorkflowOptions = new WorkflowOptions
    {
        StepTimeout = TimeSpan.FromSeconds(30)
    };
});

// Register handlers
builder.Services
    .AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>()
    .AddStepHandler<ProcessPaymentRequest, PaymentResult, ProcessPaymentHandler>();

var app = builder.Build();

app.MapPost("/orders", async (
    OrderRequest request,
    IWorkflowContextFactory contextFactory,
    IStepHandlerFactory handlerFactory) =>
{
    var context = contextFactory.Create("CreateOrder", $"Order for {request.CustomerId}");

    await using (context)
    {
        var order = await Workflow
            .Given(context, "request", () => request)
            .Handle("create order", handlerFactory, r =>
                new CreateOrderRequest(r.CustomerId, r.Items))
            .Handle("process payment", handlerFactory, o =>
                new ProcessPaymentRequest(o, request.PaymentMethod))
            .Then("completed", o => o.Status == OrderStatus.Complete)
            .GetResultAsync();

        return Results.Created($"/orders/{order.Id}", order);
    }
});

app.Run();
```

### Minimal API with Scoped Context

```csharp
app.MapPost("/orders", async (
    OrderRequest request,
    WorkflowContext context,  // Scoped - same context for entire request
    IStepHandlerFactory factory) =>
{
    context.WorkflowName = "CreateOrder";

    var order = await Workflow
        .Given(context, "request", () => request)
        .Handle("create order", factory, r => new CreateOrderRequest(r))
        .Then("valid", o => o.IsValid)
        .GetResultAsync();

    return Results.Ok(order);
});
```

## Testing with DI

### Test Setup

```csharp
public class WorkflowTests
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowTests()
    {
        var services = new ServiceCollection();

        services.AddPatternKit(options =>
        {
            options.UseScopedContexts = false;  // Isolated contexts
        });

        // Register test implementations
        services.AddStepHandler<CreateOrderRequest, Order, TestOrderHandler>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateOrder_Succeeds()
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IWorkflowContextFactory>();
        var handlerFactory = scope.ServiceProvider.GetRequiredService<IStepHandlerFactory>();

        var context = contextFactory.Create("TestWorkflow");

        await Workflow
            .Given(context, "request", () => new OrderRequest())
            .Handle("create", handlerFactory, r => new CreateOrderRequest(r))
            .Then("created", o => o.Id != null)
            .AssertPassed();
    }
}
```

### Mock Handler Registration

```csharp
var mockHandler = new Mock<IStepHandler<CreateOrderRequest, Order>>();
mockHandler
    .Setup(h => h.HandleAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new Order { Id = "test-123", Status = OrderStatus.Created });

services.AddPatternKit()
    .AddStepHandler<CreateOrderRequest, Order>(mockHandler.Object);
```

## Best Practices

### 1. Use Factory for Context Creation

```csharp
// Good - use factory
public class OrderService
{
    private readonly IWorkflowContextFactory _factory;

    public async Task<Order> ProcessAsync(OrderRequest request)
    {
        var context = _factory.Create("ProcessOrder");
        // ...
    }
}

// Avoid - manual context creation (no DI benefits)
var context = new WorkflowContext { WorkflowName = "ProcessOrder" };
```

### 2. Scope Handlers Appropriately

```csharp
// Stateless handlers - Transient or Singleton
services.AddStepHandler<ValidateRequest, ValidationResult, ValidateHandler>(ServiceLifetime.Singleton);

// Handlers with scoped dependencies - Scoped
services.AddStepHandler<SaveOrderRequest, Order, SaveOrderHandler>(ServiceLifetime.Scoped);

// Handlers with per-request state - Transient
services.AddStepHandler<ProcessRequest, Result, ProcessHandler>(ServiceLifetime.Transient);
```

### 3. Dispose Contexts

```csharp
// Always dispose contexts when done
var context = _factory.Create("Workflow");
await using (context)
{
    await Workflow.Given(context, "data", () => data)
        // ...
        .Then("done", _ => true);
}
// Context and registered cleanup handlers disposed
```

### 4. Configure for Environment

```csharp
// Development
if (builder.Environment.IsDevelopment())
{
    services.AddPatternKit(options =>
    {
        options.DefaultWorkflowOptions = new WorkflowOptions
        {
            ContinueOnError = true,  // See all failures
            MarkRemainingAsSkippedOnFailure = true
        };
    });
}

// Production
if (builder.Environment.IsProduction())
{
    services.AddPatternKit(options =>
    {
        options.DefaultWorkflowOptions = new WorkflowOptions
        {
            StepTimeout = TimeSpan.FromSeconds(30),
            HaltOnFailedAssertion = true
        };
        options.DefaultTimeout = TimeSpan.FromMinutes(5);
    });
}
```
