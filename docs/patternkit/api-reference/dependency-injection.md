# PatternKit.Extensions.DependencyInjection API Reference

Complete API documentation for the `PatternKit.Extensions.DependencyInjection` package.

## Installation

```bash
dotnet add package PatternKit.Extensions.DependencyInjection
```

**Dependencies:**
- `PatternKit.Core`
- `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## ServiceCollectionExtensions

```csharp
namespace PatternKit.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
```

Extension methods for registering PatternKit with dependency injection.

### AddPatternKit

Registers PatternKit core services.

```csharp
public static IServiceCollection AddPatternKit(
    this IServiceCollection services,
    Action<PatternKitOptions>? configure = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `services` | `IServiceCollection` | The service collection |
| `configure` | `Action<PatternKitOptions>?` | Optional configuration |

**Registers:**
- `PatternKitOptions` (Singleton)
- `IStepHandlerFactory` → `ServiceProviderStepHandlerFactory` (Singleton)
- `IWorkflowContextFactory` → `DefaultWorkflowContextFactory` (Singleton)
- `WorkflowContext` (Scoped or Transient based on options)

**Example:**
```csharp
services.AddPatternKit(options =>
{
    options.UseScopedContexts = true;
    options.DefaultTimeout = TimeSpan.FromMinutes(5);
});
```

---

### AddStepHandler (Type Registration)

Registers a step handler implementation.

```csharp
public static IServiceCollection AddStepHandler<TRequest, TResponse, THandler>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Transient)
    where TRequest : IStep<TResponse>
    where THandler : class, IStepHandler<TRequest, TResponse>
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `TRequest` | Type | The request type |
| `TResponse` | Type | The response type |
| `THandler` | Type | The handler implementation |
| `lifetime` | `ServiceLifetime` | Service lifetime (default: Transient) |

**Example:**
```csharp
services.AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>();
services.AddStepHandler<ProcessPaymentRequest, PaymentResult, ProcessPaymentHandler>(
    ServiceLifetime.Scoped);
```

---

### AddStepHandler (Instance Registration)

Registers a step handler instance.

```csharp
public static IServiceCollection AddStepHandler<TRequest, TResponse>(
    this IServiceCollection services,
    IStepHandler<TRequest, TResponse> handler)
    where TRequest : IStep<TResponse>
```

**Example:**
```csharp
var handler = new MyHandler(config);
services.AddStepHandler<MyRequest, MyResponse>(handler);
```

---

### AddStepHandler (Factory Registration)

Registers a step handler using a factory function.

```csharp
public static IServiceCollection AddStepHandler<TRequest, TResponse>(
    this IServiceCollection services,
    Func<IServiceProvider, IStepHandler<TRequest, TResponse>> factory,
    ServiceLifetime lifetime = ServiceLifetime.Transient)
    where TRequest : IStep<TResponse>
```

**Example:**
```csharp
services.AddStepHandler<MyRequest, MyResponse>(sp =>
    new MyHandler(sp.GetRequiredService<ILogger<MyHandler>>()));
```

---

### AddBehavior (Type Registration)

Registers a behavior implementation.

```csharp
public static IServiceCollection AddBehavior<T, TBehavior>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
    where TBehavior : class, IBehavior<T>
```

**Example:**
```csharp
services.AddBehavior<Order, LoggingBehavior<Order>>();
services.AddBehavior<Order, MetricsBehavior<Order>>(ServiceLifetime.Scoped);
```

---

### AddBehavior (Instance Registration)

Registers a behavior instance.

```csharp
public static IServiceCollection AddBehavior<T>(
    this IServiceCollection services,
    IBehavior<T> behavior)
```

**Example:**
```csharp
services.AddBehavior<Order>(new CachingBehavior<Order>(cache));
```

---

### AddTimingBehavior

Registers the timing behavior.

```csharp
public static IServiceCollection AddTimingBehavior<T>(
    this IServiceCollection services)
```

**Example:**
```csharp
services.AddTimingBehavior<Order>();
```

---

### AddRetryBehavior

Registers a retry behavior with configuration.

```csharp
public static IServiceCollection AddRetryBehavior<T>(
    this IServiceCollection services,
    int maxRetries = 3,
    TimeSpan? baseDelay = null)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maxRetries` | `int` | `3` | Maximum retry attempts |
| `baseDelay` | `TimeSpan?` | `null` (1s) | Base delay between retries |

**Example:**
```csharp
services.AddRetryBehavior<Order>(
    maxRetries: 5,
    baseDelay: TimeSpan.FromMilliseconds(100));
```

---

### AddCircuitBreakerBehavior

Registers a circuit breaker behavior.

```csharp
public static IServiceCollection AddCircuitBreakerBehavior<T>(
    this IServiceCollection services,
    int failureThreshold = 5,
    TimeSpan? openDuration = null)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `failureThreshold` | `int` | `5` | Failures before opening |
| `openDuration` | `TimeSpan?` | `null` (30s) | Duration circuit stays open |

**Example:**
```csharp
services.AddCircuitBreakerBehavior<Order>(
    failureThreshold: 10,
    openDuration: TimeSpan.FromMinutes(1));
```

---

## PatternKitOptions

```csharp
namespace PatternKit.Extensions.DependencyInjection;

public sealed class PatternKitOptions
```

Configuration options for PatternKit in DI scenarios.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultWorkflowOptions` | `WorkflowOptions` | `WorkflowOptions.Default` | Default options for contexts |
| `AutoRegisterBehaviors` | `bool` | `true` | Auto-discover behaviors |
| `EnableMetrics` | `bool` | `false` | Enable metrics collection |
| `DefaultTimeout` | `TimeSpan?` | `null` | Default workflow timeout |
| `BehaviorTypes` | `List<Type>` | `new()` | Ordered behavior types |
| `UseScopedContexts` | `bool` | `true` | Use scoped context lifetime |

### Example

```csharp
services.AddPatternKit(options =>
{
    options.DefaultWorkflowOptions = new WorkflowOptions
    {
        StepTimeout = TimeSpan.FromSeconds(30),
        ContinueOnError = false
    };
    options.UseScopedContexts = true;
    options.EnableMetrics = true;
    options.DefaultTimeout = TimeSpan.FromMinutes(5);
});
```

---

## IWorkflowContextFactory

```csharp
namespace PatternKit.Extensions.DependencyInjection;

public interface IWorkflowContextFactory
```

Factory for creating workflow contexts.

### Methods

#### Create (with defaults)

```csharp
WorkflowContext Create(string workflowName, string? description = null)
```

Creates a context with default options from `PatternKitOptions`.

#### Create (with options)

```csharp
WorkflowContext Create(
    string workflowName,
    WorkflowOptions options,
    string? description = null)
```

Creates a context with explicit options.

### Example

```csharp
public class MyService
{
    private readonly IWorkflowContextFactory _factory;

    public MyService(IWorkflowContextFactory factory)
    {
        _factory = factory;
    }

    public async Task ExecuteAsync()
    {
        // Default options
        var ctx1 = _factory.Create("Workflow1", "Description");

        // Custom options
        var ctx2 = _factory.Create("Workflow2", new WorkflowOptions
        {
            StepTimeout = TimeSpan.FromSeconds(5)
        });
    }
}
```

---

## DefaultWorkflowContextFactory

```csharp
namespace PatternKit.Extensions.DependencyInjection;

internal sealed class DefaultWorkflowContextFactory : IWorkflowContextFactory
```

Default implementation of `IWorkflowContextFactory`.

**Behavior:**
- Uses `PatternKitOptions.DefaultWorkflowOptions` when options not specified
- Automatically attaches `ServiceProviderExtension` to created contexts

---

## ServiceProviderExtension

```csharp
namespace PatternKit.Extensions.DependencyInjection;

public sealed class ServiceProviderExtension : IWorkflowExtension
{
    public IServiceProvider ServiceProvider { get; }
}
```

Workflow extension providing access to the service provider.

### Usage

```csharp
public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken ct)
{
    var ext = context.GetExtension<ServiceProviderExtension>();
    var sp = ext?.ServiceProvider;

    await Workflow
        .Given(context, "data", () => GetData())
        .When("process", async data =>
        {
            var processor = sp?.GetRequiredService<IProcessor>();
            return await processor.ProcessAsync(data, ct);
        })
        .Then("valid", result => result.IsValid);
}
```

---

## ServiceProviderStepHandlerFactory

```csharp
namespace PatternKit.Extensions.DependencyInjection;

public sealed class ServiceProviderStepHandlerFactory : IStepHandlerFactory
```

Step handler factory that resolves handlers from the DI container.

### Constructor

```csharp
public ServiceProviderStepHandlerFactory(IServiceProvider serviceProvider)
```

### Methods

#### Create

```csharp
public IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
    where TRequest : IStep<TResponse>
```

Resolves handler from `IServiceProvider.GetService<IStepHandler<TRequest, TResponse>>()`.

Returns `null` if handler not registered.

---

## Complete Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using PatternKit.Core;
using PatternKit.Extensions.DependencyInjection;

// Define request and handler
public record CreateUserRequest(string Email, string Name) : IStep<User>;

public class CreateUserHandler : IStepHandler<CreateUserRequest, User>
{
    private readonly IUserRepository _repository;

    public CreateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<User> HandleAsync(
        CreateUserRequest request,
        CancellationToken ct)
    {
        var user = new User { Email = request.Email, Name = request.Name };
        await _repository.SaveAsync(user, ct);
        return user;
    }
}

// Configure services
var services = new ServiceCollection();

services.AddSingleton<IUserRepository, UserRepository>();

services.AddPatternKit(options =>
{
    options.UseScopedContexts = false;
    options.DefaultWorkflowOptions = new WorkflowOptions
    {
        StepTimeout = TimeSpan.FromSeconds(30)
    };
})
.AddStepHandler<CreateUserRequest, User, CreateUserHandler>()
.AddTimingBehavior<User>()
.AddRetryBehavior<User>(maxRetries: 3);

var provider = services.BuildServiceProvider();

// Use in application
var factory = provider.GetRequiredService<IWorkflowContextFactory>();
var handlerFactory = provider.GetRequiredService<IStepHandlerFactory>();

var context = factory.Create("CreateUser", "Creates a new user account");

await using (context)
{
    var user = await Workflow
        .Given(context, "user data", () => ("alice@example.com", "Alice"))
        .Handle("create user", handlerFactory, data =>
            new CreateUserRequest(data.Item1, data.Item2))
        .Then("user created", user => user.Id != null)
        .GetResultAsync();

    Console.WriteLine($"Created user: {user.Email}");
}
```
