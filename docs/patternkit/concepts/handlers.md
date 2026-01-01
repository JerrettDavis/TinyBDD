# Step Handlers

Step handlers implement the Mediator pattern in PatternKit, allowing you to decouple step logic from workflows. This enables better testability, dependency injection, and separation of concerns.

## Overview

The handler pattern separates "what to do" from "how to do it":

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│    Workflow     │ ───▶ │    Request      │ ───▶ │    Handler      │
│   (defines      │      │  (what to do)   │      │  (how to do it) │
│   the steps)    │      │                 │      │                 │
└─────────────────┘      └─────────────────┘      └─────────────────┘
                                                          │
                                                          ▼
                                                  ┌─────────────────┐
                                                  │    Response     │
                                                  │   (result)      │
                                                  └─────────────────┘
```

## Core Interfaces

### IStep<TResponse>

Marker interface for step requests:

```csharp
public interface IStep<TResponse> { }
```

### IStep

For steps that return no value:

```csharp
public interface IStep : IStep<Unit> { }
```

### IStepHandler<TRequest, TResponse>

Handler interface that processes requests:

```csharp
public interface IStepHandler<in TRequest, TResponse>
    where TRequest : IStep<TResponse>
{
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
```

### Unit

A void/unit type for handlers with no return value:

```csharp
public readonly struct Unit
{
    public static Unit Value { get; } = new();
}
```

## Defining Step Requests

### Basic Request

```csharp
public record CreateOrderRequest(
    string CustomerId,
    List<OrderItem> Items
) : IStep<Order>;
```

### Request with Validation

```csharp
public record ProcessPaymentRequest(
    Order Order,
    PaymentMethod PaymentMethod
) : IStep<PaymentResult>
{
    public void Validate()
    {
        if (Order.Total <= 0)
            throw new ArgumentException("Order total must be positive");

        if (PaymentMethod is null)
            throw new ArgumentNullException(nameof(PaymentMethod));
    }
}
```

### Void Request

```csharp
public record SendNotificationRequest(
    string UserId,
    string Message
) : IStep;  // Returns Unit
```

## Implementing Handlers

### Basic Handler

```csharp
public class CreateOrderHandler : IStepHandler<CreateOrderRequest, Order>
{
    private readonly IOrderRepository _repository;

    public CreateOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Order> HandleAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            Items = request.Items,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(order, cancellationToken);

        return order;
    }
}
```

### Void Handler

```csharp
public class SendNotificationHandler : IStepHandler<SendNotificationRequest, Unit>
{
    private readonly INotificationService _notificationService;

    public SendNotificationHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async ValueTask<Unit> HandleAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendAsync(
            request.UserId,
            request.Message,
            cancellationToken);

        return Unit.Value;
    }
}
```

### Handler with Dependencies

```csharp
public class ProcessPaymentHandler : IStepHandler<ProcessPaymentRequest, PaymentResult>
{
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<ProcessPaymentHandler> _logger;
    private readonly IMetrics _metrics;

    public ProcessPaymentHandler(
        IPaymentGateway gateway,
        ILogger<ProcessPaymentHandler> logger,
        IMetrics metrics)
    {
        _gateway = gateway;
        _logger = logger;
        _metrics = metrics;
    }

    public async ValueTask<PaymentResult> HandleAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        request.Validate();

        _logger.LogInformation(
            "Processing payment for order {OrderId}",
            request.Order.Id);

        using var timer = _metrics.StartTimer("payment_processing");

        var result = await _gateway.ChargeAsync(
            request.PaymentMethod,
            request.Order.Total,
            cancellationToken);

        if (result.Success)
        {
            _metrics.Increment("payments_successful");
        }
        else
        {
            _metrics.Increment("payments_failed");
        }

        return result;
    }
}
```

## Handler Factories

### IStepHandlerFactory

Factory interface for creating handlers:

```csharp
public interface IStepHandlerFactory
{
    IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
        where TRequest : IStep<TResponse>;
}
```

### DefaultStepHandlerFactory

Manual registration factory:

```csharp
var factory = new DefaultStepHandlerFactory()
    .Register<CreateOrderRequest, Order>(() => new CreateOrderHandler(repository))
    .Register<ProcessPaymentRequest, PaymentResult>(() => new ProcessPaymentHandler(gateway));
```

### ServiceProviderStepHandlerFactory

DI-based factory (from `PatternKit.Extensions.DependencyInjection`):

```csharp
// Automatically resolves handlers from IServiceProvider
var factory = new ServiceProviderStepHandlerFactory(serviceProvider);
```

## Using Handlers in Workflows

### With Handle Extension

```csharp
await Workflow
    .Given(context, "order data", () => (customerId, items))
    .Handle("create order", factory, data =>
        new CreateOrderRequest(data.customerId, data.items))
    .Handle("process payment", factory, order =>
        new ProcessPaymentRequest(order, paymentMethod))
    .Then("order completed", order => order.Status == OrderStatus.Complete);
```

### Manual Handler Invocation

```csharp
await Workflow
    .Given(context, "order data", () => orderData)
    .When("create order", async data =>
    {
        var handler = factory.Create<CreateOrderRequest, Order>();
        if (handler is null)
            throw new InvalidOperationException("Handler not registered");

        return await handler.HandleAsync(
            new CreateOrderRequest(data.CustomerId, data.Items),
            CancellationToken.None);
    })
    .Then("order created", order => order.Id != null);
```

## Registration with DI

### Register Handlers

```csharp
services.AddPatternKit()
    // Register with type
    .AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>()

    // Register with instance
    .AddStepHandler<ProcessPaymentRequest, PaymentResult>(
        new ProcessPaymentHandler(gateway, logger, metrics))

    // Register with factory
    .AddStepHandler<SendNotificationRequest, Unit>(sp =>
        new SendNotificationHandler(sp.GetRequiredService<INotificationService>()));
```

### Handler Lifetime

Control handler lifetime:

```csharp
// Transient (default) - new instance per request
services.AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>(ServiceLifetime.Transient);

// Scoped - one instance per scope
services.AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>(ServiceLifetime.Scoped);

// Singleton - one instance for application lifetime
services.AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>(ServiceLifetime.Singleton);
```

## Testing Handlers

### Unit Testing

```csharp
[Fact]
public async Task CreateOrderHandler_CreatesOrder_WithCorrectData()
{
    // Arrange
    var repository = new InMemoryOrderRepository();
    var handler = new CreateOrderHandler(repository);
    var request = new CreateOrderRequest("customer-1", new List<OrderItem>
    {
        new("product-1", 2, 10.00m)
    });

    // Act
    var order = await handler.HandleAsync(request, CancellationToken.None);

    // Assert
    Assert.NotNull(order.Id);
    Assert.Equal("customer-1", order.CustomerId);
    Assert.Single(order.Items);
    Assert.Equal(OrderStatus.Created, order.Status);
}
```

### Integration Testing with Mock Factory

```csharp
[Fact]
public async Task OrderWorkflow_CompletesSuccessfully()
{
    // Arrange
    var mockHandler = new Mock<IStepHandler<CreateOrderRequest, Order>>();
    mockHandler
        .Setup(h => h.HandleAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Order { Id = "order-1", Status = OrderStatus.Created });

    var factory = new DefaultStepHandlerFactory()
        .Register<CreateOrderRequest, Order>(() => mockHandler.Object);

    var context = new WorkflowContext { WorkflowName = "Test" };

    // Act
    await Workflow
        .Given(context, "order data", () => ("cust-1", new List<OrderItem>()))
        .Handle("create order", factory, data =>
            new CreateOrderRequest(data.Item1, data.Item2))
        .Then("order created", order => order.Status == OrderStatus.Created);

    // Assert
    Assert.True(context.AllPassed);
    mockHandler.Verify(h => h.HandleAsync(
        It.IsAny<CreateOrderRequest>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

## Advanced Patterns

### Decorating Handlers

```csharp
public class LoggingHandlerDecorator<TRequest, TResponse> : IStepHandler<TRequest, TResponse>
    where TRequest : IStep<TResponse>
{
    private readonly IStepHandler<TRequest, TResponse> _inner;
    private readonly ILogger _logger;

    public LoggingHandlerDecorator(
        IStepHandler<TRequest, TResponse> inner,
        ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling {RequestType}",
            typeof(TRequest).Name);

        try
        {
            var response = await _inner.HandleAsync(request, cancellationToken);

            _logger.LogInformation(
                "Handled {RequestType} successfully",
                typeof(TRequest).Name);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling {RequestType}",
                typeof(TRequest).Name);
            throw;
        }
    }
}
```

### Pipeline Handlers

```csharp
public class PipelineHandler<TRequest, TResponse> : IStepHandler<TRequest, TResponse>
    where TRequest : IStep<TResponse>
{
    private readonly IStepHandler<TRequest, TResponse> _handler;
    private readonly IEnumerable<IPipelineBehavior<TRequest, TResponse>> _behaviors;

    public PipelineHandler(
        IStepHandler<TRequest, TResponse> handler,
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
    {
        _handler = handler;
        _behaviors = behaviors;
    }

    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        async ValueTask<TResponse> Handler() =>
            await _handler.HandleAsync(request, cancellationToken);

        return await _behaviors
            .Reverse()
            .Aggregate(
                (Func<ValueTask<TResponse>>)Handler,
                (next, behavior) => () => behavior.HandleAsync(request, next, cancellationToken))();
    }
}
```

### Composite Handlers

```csharp
public class CompositeOrderHandler : IStepHandler<CompleteOrderRequest, Order>
{
    private readonly IStepHandler<ValidateOrderRequest, ValidationResult> _validateHandler;
    private readonly IStepHandler<ProcessPaymentRequest, PaymentResult> _paymentHandler;
    private readonly IStepHandler<UpdateInventoryRequest, Unit> _inventoryHandler;

    public CompositeOrderHandler(
        IStepHandler<ValidateOrderRequest, ValidationResult> validateHandler,
        IStepHandler<ProcessPaymentRequest, PaymentResult> paymentHandler,
        IStepHandler<UpdateInventoryRequest, Unit> inventoryHandler)
    {
        _validateHandler = validateHandler;
        _paymentHandler = paymentHandler;
        _inventoryHandler = inventoryHandler;
    }

    public async ValueTask<Order> HandleAsync(
        CompleteOrderRequest request,
        CancellationToken cancellationToken)
    {
        // Validate
        var validation = await _validateHandler.HandleAsync(
            new ValidateOrderRequest(request.Order),
            cancellationToken);

        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Process payment
        var payment = await _paymentHandler.HandleAsync(
            new ProcessPaymentRequest(request.Order, request.PaymentMethod),
            cancellationToken);

        if (!payment.Success)
            throw new PaymentException(payment.ErrorMessage);

        // Update inventory
        await _inventoryHandler.HandleAsync(
            new UpdateInventoryRequest(request.Order.Items),
            cancellationToken);

        request.Order.Status = OrderStatus.Complete;
        return request.Order;
    }
}
```

## Best Practices

### 1. Keep Requests Immutable

```csharp
// Good - immutable record
public record CreateOrderRequest(string CustomerId, List<OrderItem> Items) : IStep<Order>;

// Bad - mutable class
public class CreateOrderRequest : IStep<Order>
{
    public string CustomerId { get; set; }  // Mutable!
}
```

### 2. Single Responsibility

Each handler should do one thing:

```csharp
// Good - focused handlers
public class CreateOrderHandler : IStepHandler<CreateOrderRequest, Order> { }
public class SendConfirmationHandler : IStepHandler<SendConfirmationRequest, Unit> { }

// Bad - handler doing too much
public class OrderHandler : IStepHandler<OrderRequest, Order>
{
    // Creates order, processes payment, sends email, updates inventory...
}
```

### 3. Use Cancellation Tokens

Always pass through cancellation tokens:

```csharp
public async ValueTask<Order> HandleAsync(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    // Check for cancellation
    cancellationToken.ThrowIfCancellationRequested();

    // Pass to async operations
    await _repository.SaveAsync(order, cancellationToken);

    return order;
}
```

### 4. Validate Early

Validate requests at the start of handling:

```csharp
public async ValueTask<Order> HandleAsync(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    // Validate first
    if (string.IsNullOrEmpty(request.CustomerId))
        throw new ArgumentException("Customer ID required");

    if (request.Items.Count == 0)
        throw new ArgumentException("Order must have items");

    // Then process
    // ...
}
```

### 5. Return Meaningful Responses

```csharp
// Good - rich response
public record PaymentResult(
    bool Success,
    string TransactionId,
    string? ErrorMessage,
    decimal AmountCharged);

// Bad - just bool
public record PaymentResult(bool Success);
```
