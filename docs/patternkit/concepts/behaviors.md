# Behaviors

Behaviors in PatternKit provide a way to add cross-cutting concerns to workflow execution. They wrap step execution using the Chain of Responsibility pattern, enabling features like timing, retries, circuit breakers, and custom middleware.

## Overview

A behavior wraps the execution of a step, allowing code to run before, after, or around the step:

```
┌──────────────────────────────────────────────────────┐
│                    Behavior 1                         │
│  ┌────────────────────────────────────────────────┐  │
│  │                  Behavior 2                     │  │
│  │  ┌──────────────────────────────────────────┐  │  │
│  │  │                Behavior 3                 │  │  │
│  │  │  ┌────────────────────────────────────┐  │  │  │
│  │  │  │          Step Execution            │  │  │  │
│  │  │  └────────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

## IBehavior<T> Interface

```csharp
public interface IBehavior<T>
{
    ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken);
}
```

Parameters:
- `next` - The next behavior or step execution in the chain
- `context` - The workflow context for accessing/storing metadata
- `step` - Information about the step being executed
- `cancellationToken` - Cancellation token for the operation

## Built-in Behaviors

PatternKit includes three built-in behaviors:

### TimingBehavior

Records execution time for each step:

```csharp
public sealed class TimingBehavior<T> : IBehavior<T>
{
    public static TimingBehavior<T> Instance { get; } = new();

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            sw.Stop();
            context.SetMetadata($"timing:{step.Title}", sw.Elapsed);
        }
    }
}
```

Usage:
```csharp
// Register with DI
services.AddTimingBehavior<OrderResult>();

// Access timing data
var timing = context.GetMetadata<TimeSpan>("timing:process order");
Console.WriteLine($"Processing took: {timing.TotalMilliseconds}ms");
```

### RetryBehavior

Retries failed steps with exponential backoff:

```csharp
var retryBehavior = new RetryBehavior<OrderResult>(
    maxRetries: 3,
    baseDelay: TimeSpan.FromSeconds(1),
    shouldRetry: ex => ex is TransientException
);
```

Configuration:
- `maxRetries` - Maximum number of retry attempts (default: 3)
- `baseDelay` - Initial delay between retries (default: 1 second)
- `shouldRetry` - Predicate to determine if exception is retryable (default: all exceptions)

Backoff formula: `delay * 2^(attempt - 1)`
- Attempt 1: 1s delay
- Attempt 2: 2s delay
- Attempt 3: 4s delay

Metadata recorded:
- `retry:{step.Title}:attempt` - Current attempt number
- `retry:{step.Title}:delay` - Delay before this attempt

Usage:
```csharp
// Register with DI
services.AddRetryBehavior<OrderResult>(
    maxRetries: 3,
    baseDelay: TimeSpan.FromMilliseconds(100)
);

// Check retry information
var attempts = context.GetMetadata<int>("retry:call api:attempt");
Console.WriteLine($"Succeeded after {attempts} attempts");
```

### CircuitBreakerBehavior

Implements the circuit breaker pattern for fault tolerance:

```csharp
var circuitBreaker = new CircuitBreakerBehavior<ApiResponse>(
    failureThreshold: 5,
    openDuration: TimeSpan.FromSeconds(30)
);
```

States:
- **Closed** - Normal operation, all requests pass through
- **Open** - Too many failures, requests are blocked
- **Half-Open** - Testing recovery, one request allowed

State transitions:
```
        Failure ≥ threshold
Closed ─────────────────────▶ Open
   ▲                            │
   │                            │ After openDuration
   │   Success                  ▼
   └────────────────────── Half-Open
                                │
                                │ Failure
                                ▼
                              Open
```

Metadata recorded:
- `circuit:{step.Title}:state` - Current state (closed/open/half-open)
- `circuit:{step.Title}:failures` - Failure count when circuit opened

Usage:
```csharp
// Register with DI
services.AddCircuitBreakerBehavior<ApiResponse>(
    failureThreshold: 5,
    openDuration: TimeSpan.FromSeconds(30)
);

// Check circuit state
var state = context.GetMetadata<string>("circuit:call external api:state");
if (state == "open")
{
    Console.WriteLine("Circuit is open, requests blocked");
}
```

Exception thrown when circuit is open:
```csharp
try
{
    await workflow;
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine("Circuit breaker prevented request");
}
```

## Creating Custom Behaviors

### Simple Logging Behavior

```csharp
public class LoggingBehavior<T> : IBehavior<T>
{
    private readonly ILogger _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<T>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting step: {Kind} {Title}", step.Kind, step.Title);

        try
        {
            var result = await next(cancellationToken);
            _logger.LogInformation("Completed step: {Title}", step.Title);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed step: {Title}", step.Title);
            throw;
        }
    }
}
```

### Metrics Behavior

```csharp
public class MetricsBehavior<T> : IBehavior<T>
{
    private readonly IMetricsCollector _metrics;

    public MetricsBehavior(IMetricsCollector metrics)
    {
        _metrics = metrics;
    }

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        var tags = new Dictionary<string, string>
        {
            ["workflow"] = context.WorkflowName,
            ["step"] = step.Title,
            ["phase"] = step.Phase.ToString()
        };

        using var timer = _metrics.StartTimer("step_duration", tags);

        try
        {
            var result = await next(cancellationToken);
            _metrics.Increment("step_success", tags);
            return result;
        }
        catch
        {
            _metrics.Increment("step_failure", tags);
            throw;
        }
    }
}
```

### Caching Behavior

```csharp
public class CachingBehavior<T> : IBehavior<T>
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _expiration;

    public CachingBehavior(IDistributedCache cache, TimeSpan expiration)
    {
        _cache = cache;
        _expiration = expiration;
    }

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        // Generate cache key from step context
        var input = context.CurrentValue;
        var cacheKey = $"{context.WorkflowName}:{step.Title}:{input?.GetHashCode()}";

        // Try to get from cache
        var cached = await _cache.GetAsync<T>(cacheKey, cancellationToken);
        if (cached != null)
        {
            context.SetMetadata($"cache:{step.Title}", "hit");
            return cached;
        }

        // Execute step
        var result = await next(cancellationToken);

        // Store in cache
        await _cache.SetAsync(cacheKey, result, _expiration, cancellationToken);
        context.SetMetadata($"cache:{step.Title}", "miss");

        return result;
    }
}
```

### Validation Behavior

```csharp
public class ValidationBehavior<T> : IBehavior<T> where T : IValidatable
{
    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);

        var validationResult = result.Validate();
        if (!validationResult.IsValid)
        {
            context.SetMetadata($"validation:{step.Title}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }

        return result;
    }
}
```

## Registering Behaviors

### With Dependency Injection

```csharp
services.AddPatternKit()
    // Built-in behaviors
    .AddTimingBehavior<OrderResult>()
    .AddRetryBehavior<OrderResult>(maxRetries: 3)
    .AddCircuitBreakerBehavior<OrderResult>(failureThreshold: 5)

    // Custom behaviors
    .AddBehavior<OrderResult, LoggingBehavior<OrderResult>>()
    .AddBehavior<OrderResult, MetricsBehavior<OrderResult>>()

    // Instance registration
    .AddBehavior<ApiResponse>(new CachingBehavior<ApiResponse>(cache, TimeSpan.FromMinutes(5)));
```

### Behavior Order

Behaviors execute in the order they are registered:

```csharp
services
    .AddBehavior<T, LoggingBehavior<T>>()    // 1. Outer - logs start/end
    .AddBehavior<T, MetricsBehavior<T>>()    // 2. Records metrics
    .AddBehavior<T, RetryBehavior<T>>()      // 3. Retries on failure
    .AddBehavior<T, CachingBehavior<T>>();   // 4. Inner - caches result
```

Execution order:
1. LoggingBehavior starts
2. MetricsBehavior starts
3. RetryBehavior starts
4. CachingBehavior checks cache
5. Step executes (on cache miss)
6. CachingBehavior stores result
7. RetryBehavior completes (or retries)
8. MetricsBehavior records time
9. LoggingBehavior logs completion

## Non-Generic IBehavior

For behaviors that don't interact with the value type:

```csharp
public interface IBehavior
{
    ValueTask<object?> WrapAsync(
        Func<CancellationToken, ValueTask<object?>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken);
}
```

This is useful for:
- Logging (doesn't care about value type)
- Generic timing
- Authentication/authorization checks

## Best Practices

### 1. Keep Behaviors Focused

Each behavior should do one thing:

```csharp
// Good - focused behaviors
public class TimingBehavior<T> : IBehavior<T> { ... }
public class LoggingBehavior<T> : IBehavior<T> { ... }

// Bad - behavior doing too much
public class TimingAndLoggingAndRetryBehavior<T> : IBehavior<T> { ... }
```

### 2. Handle Exceptions Properly

Always rethrow after handling:

```csharp
public async ValueTask<T> WrapAsync(...)
{
    try
    {
        return await next(cancellationToken);
    }
    catch (Exception ex)
    {
        // Log, record metrics, etc.
        LogError(ex);

        // Always rethrow unless intentionally swallowing
        throw;
    }
}
```

### 3. Use Metadata for Communication

Store information in context metadata:

```csharp
// In behavior
context.SetMetadata("behavior:attempts", attemptCount);

// Later in workflow or tests
var attempts = context.GetMetadata<int>("behavior:attempts");
```

### 4. Consider Cancellation

Always pass through cancellation tokens:

```csharp
public async ValueTask<T> WrapAsync(
    Func<CancellationToken, ValueTask<T>> next,
    WorkflowContext context,
    StepMetadata step,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    // Pass cancellation token to async operations
    await SomeAsyncOperation(cancellationToken);

    return await next(cancellationToken);
}
```

### 5. Make Behaviors Reusable

Design behaviors to work across different value types:

```csharp
// Generic and reusable
public class LoggingBehavior<T> : IBehavior<T>

// Can be registered for multiple types
services.AddBehavior<OrderResult, LoggingBehavior<OrderResult>>();
services.AddBehavior<UserProfile, LoggingBehavior<UserProfile>>();
```

## Testing Behaviors

```csharp
[Fact]
public async Task RetryBehavior_RetriesOnFailure()
{
    var attempts = 0;
    var behavior = new RetryBehavior<int>(maxRetries: 3);
    var context = new WorkflowContext { WorkflowName = "Test" };
    var step = new StepMetadata { Kind = "When", Title = "test step" };

    Func<CancellationToken, ValueTask<int>> stepThatFails = ct =>
    {
        attempts++;
        if (attempts < 3)
            throw new Exception("Transient failure");
        return new ValueTask<int>(42);
    };

    var result = await behavior.WrapAsync(stepThatFails, context, step, CancellationToken.None);

    Assert.Equal(42, result);
    Assert.Equal(3, attempts);
}
```
