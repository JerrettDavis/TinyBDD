# PatternKit.Core API Reference

Complete API documentation for the `PatternKit.Core` package.

## Workflow Entry Point

### Workflow Class

```csharp
namespace PatternKit.Core;

public static class Workflow
```

Static entry point for creating workflows.

#### Methods

##### Given<T>

Starts a workflow with a value-producing function.

```csharp
public static WorkflowChain<T> Given<T>(
    WorkflowContext context,
    string title,
    Func<T> initializer)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `context` | `WorkflowContext` | The workflow execution context |
| `title` | `string` | Human-readable step description |
| `initializer` | `Func<T>` | Function producing the initial value |

**Returns:** `WorkflowChain<T>` for method chaining.

##### Given<T> (with value)

Starts a workflow with an existing value.

```csharp
public static WorkflowChain<T> Given<T>(
    WorkflowContext context,
    string title,
    T value)
```

##### Given<TState, T> (state-passing)

Starts a workflow with state-passing to avoid closures.

```csharp
public static WorkflowChain<T> Given<TState, T>(
    WorkflowContext context,
    string title,
    TState state,
    Func<TState, T> initializer)
```

##### Given<T> (auto-title)

Starts a workflow with auto-generated title from type name.

```csharp
public static WorkflowChain<T> Given<T>(
    WorkflowContext context,
    Func<T> initializer)
```

---

## Context and Options

### WorkflowContext Class

```csharp
namespace PatternKit.Core;

public sealed class WorkflowContext : IAsyncDisposable
```

Central container for workflow execution state.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `WorkflowName` | `required string` | Unique workflow identifier |
| `Description` | `string?` | Optional description |
| `ExecutionId` | `string` | Auto-generated execution ID |
| `Options` | `WorkflowOptions` | Execution configuration |
| `Steps` | `IReadOnlyList<StepResult>` | Executed step results |
| `IO` | `IReadOnlyList<StepIO>` | Input/output log |
| `CurrentValue` | `object?` | Current pipeline value |
| `Metadata` | `IReadOnlyDictionary<string, object?>` | Custom metadata |
| `AllPassed` | `bool` | True if all steps passed |
| `FirstFailure` | `StepResult?` | First failed step |

#### Methods

##### SetMetadata

```csharp
public void SetMetadata(string key, object? value)
```

Stores a value in the metadata dictionary.

##### GetMetadata<T>

```csharp
public T? GetMetadata<T>(string key)
```

Retrieves a typed value from metadata.

##### TryGetMetadata<T>

```csharp
public bool TryGetMetadata<T>(string key, out T? value)
```

Attempts to retrieve a typed value.

##### SetExtension<T>

```csharp
public void SetExtension<T>(T extension) where T : class, IWorkflowExtension
```

Attaches an extension to the context.

##### GetExtension<T>

```csharp
public T? GetExtension<T>() where T : class, IWorkflowExtension
```

Retrieves an attached extension.

##### OnDispose

```csharp
public void OnDispose(Func<ValueTask> disposeAction)
```

Registers a cleanup action to run on disposal.

##### DisposeAsync

```csharp
public async ValueTask DisposeAsync()
```

Disposes the context and runs registered cleanup actions.

---

### WorkflowOptions Record

```csharp
namespace PatternKit.Core;

public sealed record WorkflowOptions
```

Configuration for workflow execution behavior.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ContinueOnError` | `bool` | `false` | Continue after exceptions |
| `HaltOnFailedAssertion` | `bool` | `true` | Stop on assertion failure |
| `StepTimeout` | `TimeSpan?` | `null` | Per-step timeout |
| `MarkRemainingAsSkippedOnFailure` | `bool` | `false` | Mark skipped steps |

#### Static Properties

```csharp
public static WorkflowOptions Default { get; }
```

Default options instance.

---

## Fluent Chains

### WorkflowChain<T> Class

```csharp
namespace PatternKit.Core;

public sealed partial class WorkflowChain<T>
```

Fluent builder for composing workflow steps.

#### When Methods

Transform the current value:

```csharp
// Synchronous transform
public WorkflowChain<TOut> When<TOut>(string title, Func<T, TOut> transform)

// Async Task transform
public WorkflowChain<TOut> When<TOut>(string title, Func<T, Task<TOut>> transform)

// Async ValueTask transform
public WorkflowChain<TOut> When<TOut>(string title, Func<T, ValueTask<TOut>> transform)

// With cancellation token
public WorkflowChain<TOut> When<TOut>(string title, Func<T, CancellationToken, Task<TOut>> transform)

// State-passing variants
public WorkflowChain<TOut> When<TState, TOut>(string title, TState state, Func<T, TState, TOut> transform)
```

Side effect (returns same value):

```csharp
public WorkflowChain<T> When(string title, Action<T> effect)
public WorkflowChain<T> When(string title, Func<T, Task> effect)
```

#### And Methods

Continue in the same phase:

```csharp
public WorkflowChain<TOut> And<TOut>(string title, Func<T, TOut> transform)
public WorkflowChain<T> And(string title, Action<T> effect)
// ... all variants mirror When methods
```

#### But Methods

Contrasting continuation:

```csharp
public WorkflowChain<TOut> But<TOut>(string title, Func<T, TOut> transform)
public WorkflowChain<T> But(string title, Action<T> effect)
// ... all variants mirror When methods
```

#### Then Methods

Start assertion phase:

```csharp
// Predicate assertion
public ResultChain<T> Then(string title, Func<T, bool> predicate)
public ResultChain<T> Then(string title, Func<T, Task<bool>> predicate)

// Action assertion (throws on failure)
public ResultChain<T> Then(string title, Action<T> assertion)

// Transform in Then phase
public ResultChain<TOut> Then<TOut>(string title, Func<T, TOut> transform)

// State-passing
public ResultChain<T> Then<TState>(string title, TState state, Func<T, TState, bool> predicate)
```

#### Finally Methods

Cleanup handlers:

```csharp
public WorkflowChain<T> Finally(string title, Action<T> cleanup)
public WorkflowChain<T> Finally(string title, Func<T, Task> cleanup)
public WorkflowChain<T> Finally(string title, Func<T, ValueTask> cleanup)
```

---

### ResultChain<T> Struct

```csharp
namespace PatternKit.Core;

public readonly struct ResultChain<T>
```

Terminal chain that can be awaited.

#### Methods

##### GetAwaiter

```csharp
public TaskAwaiter GetAwaiter()
```

Makes the struct awaitable.

##### AssertPassed

```csharp
public async ValueTask AssertPassed(CancellationToken cancellationToken = default)
```

Executes workflow and asserts all steps passed.

**Throws:** `WorkflowAssertionException` if any step failed.

##### AssertFailed

```csharp
public async ValueTask AssertFailed(CancellationToken cancellationToken = default)
```

Executes workflow and asserts at least one step failed.

##### GetResultAsync

```csharp
public async ValueTask<T> GetResultAsync(CancellationToken cancellationToken = default)
```

Executes workflow and returns the final value.

##### And, But, When, Finally

Continuation methods for further chaining (same signatures as `WorkflowChain<T>`).

---

## Step Types

### StepResult Record

```csharp
namespace PatternKit.Core;

public readonly record struct StepResult
{
    public required string Kind { get; init; }
    public required string Title { get; init; }
    public TimeSpan Elapsed { get; init; }
    public Exception? Error { get; init; }
    public bool Passed => Error is null;
}
```

Captures individual step execution outcome.

### StepMetadata Record

```csharp
namespace PatternKit.Core;

public readonly record struct StepMetadata
{
    public string Kind { get; init; }
    public string Title { get; init; }
    public StepPhase Phase { get; init; }
    public StepWord Word { get; init; }
}
```

Describes a step for hooks.

### StepIO Record

```csharp
namespace PatternKit.Core;

public readonly record struct StepIO
{
    public string Kind { get; init; }
    public string Title { get; init; }
    public object? Input { get; init; }
    public object? Output { get; init; }
}
```

Records input/output for data flow tracking.

### StepPhase Enum

```csharp
namespace PatternKit.Core;

public enum StepPhase
{
    Given,
    When,
    Then
}
```

### StepWord Enum

```csharp
namespace PatternKit.Core;

public enum StepWord
{
    Primary,
    And,
    But
}
```

---

## Behaviors

### IBehavior<T> Interface

```csharp
namespace PatternKit.Core;

public interface IBehavior<T>
{
    ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken);
}
```

Wraps step execution for cross-cutting concerns.

### TimingBehavior<T> Class

```csharp
namespace PatternKit.Core;

public sealed class TimingBehavior<T> : IBehavior<T>
{
    public static TimingBehavior<T> Instance { get; }
}
```

Records step execution time in context metadata.

**Metadata key:** `timing:{step.Title}` → `TimeSpan`

### RetryBehavior<T> Class

```csharp
namespace PatternKit.Core;

public sealed class RetryBehavior<T> : IBehavior<T>
{
    public RetryBehavior(
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null);

    public static RetryBehavior<T> Create(
        int maxRetries = 3,
        TimeSpan? baseDelay = null);
}
```

Retries failed steps with exponential backoff.

**Metadata keys:**
- `retry:{step.Title}:attempt` → `int`
- `retry:{step.Title}:delay` → `TimeSpan`

### CircuitBreakerBehavior<T> Class

```csharp
namespace PatternKit.Core;

public sealed class CircuitBreakerBehavior<T> : IBehavior<T>
{
    public CircuitBreakerBehavior(
        int failureThreshold = 5,
        TimeSpan? openDuration = null);

    public CircuitState State { get; }

    public static CircuitBreakerBehavior<T> Create(
        int failureThreshold = 5,
        TimeSpan? openDuration = null);
}
```

**Throws:** `CircuitBreakerOpenException` when circuit is open.

**Metadata keys:**
- `circuit:{step.Title}:state` → `string`
- `circuit:{step.Title}:failures` → `int`

### CircuitState Enum

```csharp
namespace PatternKit.Core;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}
```

---

## Step Handlers

### IStep<TResponse> Interface

```csharp
namespace PatternKit.Core;

public interface IStep<TResponse> { }
```

Marker interface for step requests.

### IStep Interface

```csharp
namespace PatternKit.Core;

public interface IStep : IStep<Unit> { }
```

For steps returning no value.

### IStepHandler<TRequest, TResponse> Interface

```csharp
namespace PatternKit.Core;

public interface IStepHandler<in TRequest, TResponse>
    where TRequest : IStep<TResponse>
{
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
```

### Unit Struct

```csharp
namespace PatternKit.Core;

public readonly struct Unit
{
    public static Unit Value { get; }
}
```

Void/unit return type for handlers.

### IStepHandlerFactory Interface

```csharp
namespace PatternKit.Core;

public interface IStepHandlerFactory
{
    IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
        where TRequest : IStep<TResponse>;
}
```

### DefaultStepHandlerFactory Class

```csharp
namespace PatternKit.Core;

public sealed class DefaultStepHandlerFactory : IStepHandlerFactory
{
    public DefaultStepHandlerFactory Register<TRequest, TResponse>(
        Func<IStepHandler<TRequest, TResponse>> factory)
        where TRequest : IStep<TResponse>;

    public DefaultStepHandlerFactory Register<TRequest, TResponse>(
        IStepHandler<TRequest, TResponse> handler)
        where TRequest : IStep<TResponse>;

    public IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
        where TRequest : IStep<TResponse>;
}
```

---

## Extensions

### WorkflowExtensions Class

```csharp
namespace PatternKit.Core;

public static class WorkflowExtensions
```

#### Methods

##### Handle

```csharp
public static WorkflowChain<TResponse> Handle<T, TRequest, TResponse>(
    this WorkflowChain<T> chain,
    string title,
    IStepHandlerFactory factory,
    Func<T, TRequest> createRequest)
    where TRequest : IStep<TResponse>
```

Executes a step handler within the chain.

##### MergeSteps

```csharp
public static void MergeSteps(this WorkflowContext target, WorkflowContext source)
```

Copies steps and IO from source to target context.

##### TotalElapsed

```csharp
public static TimeSpan TotalElapsed(this WorkflowContext context)
```

Returns sum of all step durations.

##### GetFailedSteps

```csharp
public static IEnumerable<StepResult> GetFailedSteps(this WorkflowContext context)
```

Returns all failed steps.

##### AssertPassed

```csharp
public static void AssertPassed(this WorkflowContext context)
```

Throws `WorkflowAssertionException` if any step failed.

---

## Exceptions

### WorkflowStepException Class

```csharp
namespace PatternKit.Core;

public class WorkflowStepException : Exception
{
    public WorkflowContext Context { get; }

    public WorkflowStepException(
        string message,
        WorkflowContext context,
        Exception innerException);
}
```

### WorkflowAssertionException Class

```csharp
namespace PatternKit.Core;

public class WorkflowAssertionException : Exception
{
    public WorkflowAssertionException(string message);
    public WorkflowAssertionException(string message, Exception innerException);
}
```

### CircuitBreakerOpenException Class

```csharp
namespace PatternKit.Core;

public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message);
}
```

---

## Extension Marker

### IWorkflowExtension Interface

```csharp
namespace PatternKit.Core;

public interface IWorkflowExtension { }
```

Marker interface for context extensions.
