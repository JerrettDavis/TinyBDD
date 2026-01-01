# PatternKit Architecture Transformation Plan

## Executive Summary

Transform TinyBDD from a testing-focused BDD framework into **PatternKit** - a general-purpose, AoT-compatible, high-performance fluent DSL for expressing domain requirements as executable workflows. Testing becomes one use case of a broader pattern orchestration system.

## Current State Analysis

### Strengths
- Solid fluent builder pattern (`ScenarioChain<T>`, `ThenChain<T>`)
- Deferred execution via `Pipeline` - steps queued until awaited
- ValueTask-based async for zero-allocation fast paths
- State-passing overloads to avoid closures
- Extensive delegate normalization (`ToCT` methods)
- Framework-agnostic via abstraction layers (`ITraitBridge`, `ITestMethodResolver`)

### Limitations for General Use
1. **Testing-centric naming**: `ScenarioContext`, `ScenarioChain`, `FeatureAttribute`
2. **Reflection hotspots**: `FindByStackTrace()`, `GetCustomAttribute<T>()`
3. **No AoT source generation**: All delegate wrapping is runtime
4. **Limited extensibility**: No middleware/behavior pipeline
5. **No DI integration**: Context created via static factory
6. **Object allocations**: Delegate wrapping creates closures

---

## Architectural Vision

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          PatternKit.Core                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  WorkflowContext         - Execution state container                ││
│  │  WorkflowChain<T>        - Fluent builder (Given/When/Then)         ││
│  │  ResultChain<T>          - Terminal chain (awaitable)               ││
│  │  ExecutionPipeline       - Step execution engine                    ││
│  │  IStepHandler<TIn,TOut>  - Mediator-style step handler              ││
│  │  IBehavior<T>            - Cross-cutting concerns (middleware)      ││
│  └─────────────────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  Source Generators (AoT)                                            ││
│  │  - DelegateNormalizationGenerator                                   ││
│  │  - StepHandlerGenerator                                             ││
│  │  - WorkflowComposerGenerator                                        ││
│  └─────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         ▼                          ▼                          ▼
┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│ PatternKit.Test │    │ PatternKit.Commands │    │ PatternKit.Events   │
│ (TinyBDD compat)│    │ (CQRS workflows)    │    │ (Event processing)  │
└─────────────────┘    └─────────────────────┘    └─────────────────────┘
```

---

## Implementation Phases

### Phase 1: Core Abstraction Layer

**Goal**: Create a testing-agnostic core that can support any workflow use case.

#### 1.1 Namespace & Project Structure

```
src/
├── PatternKit.Core/              # Core abstractions (new)
│   ├── Workflows/
│   │   ├── WorkflowContext.cs    # Replaces ScenarioContext
│   │   ├── WorkflowChain.cs      # Generic fluent builder
│   │   ├── ResultChain.cs        # Terminal awaitable chain
│   │   └── ExecutionPipeline.cs  # Step execution engine
│   ├── Steps/
│   │   ├── StepPhase.cs          # Given/When/Then phases
│   │   ├── StepWord.cs           # Primary/And/But
│   │   ├── StepResult.cs         # Execution outcome
│   │   └── StepIO.cs             # Input/output tracking
│   ├── Handlers/
│   │   ├── IStepHandler.cs       # Mediator pattern for steps
│   │   ├── IStepHandlerFactory.cs
│   │   └── StepHandlerRegistry.cs
│   ├── Behaviors/
│   │   ├── IBehavior.cs          # Middleware interface
│   │   ├── BehaviorPipeline.cs   # Behavior composition
│   │   └── Built-in/
│   │       ├── TimingBehavior.cs
│   │       ├── RetryBehavior.cs
│   │       └── CircuitBreakerBehavior.cs
│   ├── Extensions/
│   │   └── IServiceCollectionExtensions.cs
│   └── Options/
│       └── WorkflowOptions.cs    # Replaces ScenarioOptions
│
├── PatternKit.Generators/        # Source generators (new)
│   ├── DelegateNormalizationGenerator.cs
│   └── Analyzers/
│
├── TinyBDD/                      # Becomes thin wrapper
│   ├── Core/
│   │   ├── Bdd.cs               # Delegates to PatternKit
│   │   ├── Flow.cs              # Ambient API (uses PatternKit)
│   │   ├── ScenarioContext.cs   # Inherits WorkflowContext
│   │   └── ...
│   └── Testing/                 # Test-specific extensions
│       ├── Assertions/
│       └── Framework adapters
```

#### 1.2 Core Type Mappings

| TinyBDD (Current)      | PatternKit.Core (New)       | Notes                           |
|------------------------|-----------------------------|---------------------------------|
| `ScenarioContext`      | `WorkflowContext`           | Generic execution context       |
| `ScenarioChain<T>`     | `WorkflowChain<T>`          | Fluent builder                  |
| `ThenChain<T>`         | `ResultChain<T>`            | Terminal awaitable              |
| `Pipeline`             | `ExecutionPipeline`         | Step execution engine           |
| `ScenarioOptions`      | `WorkflowOptions`           | Execution configuration         |
| `ITraitBridge`         | `IContextExtension`         | Generic extension point         |
| `ITestMethodResolver`  | `IContextProvider`          | Context discovery               |

#### 1.3 WorkflowContext Design

```csharp
namespace PatternKit.Core;

/// <summary>
/// Holds execution state for a single workflow run.
/// Thread-safe for concurrent access within a single execution.
/// </summary>
public class WorkflowContext : IAsyncDisposable
{
    // Core identity
    public required string WorkflowName { get; init; }
    public string? Description { get; init; }
    public string ExecutionId { get; } = Guid.NewGuid().ToString("N")[..8];

    // Execution state (internal mutation only)
    public IReadOnlyList<StepResult> Steps => _steps;
    public IReadOnlyList<StepIO> IO => _io;
    public object? CurrentValue { get; internal set; }

    // Metadata
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;
    public WorkflowOptions Options { get; init; } = WorkflowOptions.Default;

    // Extension point
    public T? GetExtension<T>() where T : class;
    public void SetExtension<T>(T extension) where T : class;

    // Cleanup
    public ValueTask DisposeAsync();
}
```

#### 1.4 WorkflowChain Design

```csharp
namespace PatternKit.Core;

/// <summary>
/// Fluent builder for constructing workflows using Given/When/Then semantics.
/// Immutable - each method returns a new chain instance.
/// </summary>
public readonly struct WorkflowChain<T>
{
    private readonly ExecutionPipeline _pipeline;

    // Phase-specific entry points
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, TOut> transform);
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, CancellationToken, ValueTask<TOut>> transform);
    public WorkflowChain<T> When(string title, Action<T> effect);

    // Connectives
    public WorkflowChain<TOut> And<TOut>(...);
    public WorkflowChain<T> And(...);
    public WorkflowChain<TOut> But<TOut>(...);
    public WorkflowChain<T> But(...);

    // Terminal
    public ResultChain<T> Then(string title, Func<T, bool> predicate);
    public ResultChain<TOut> Then<TOut>(string title, Func<T, TOut> transform);

    // Cleanup
    public WorkflowChain<T> Finally(string title, Func<T, CancellationToken, ValueTask> cleanup);

    // Behaviors
    public WorkflowChain<T> WithBehavior(IBehavior<T> behavior);
}
```

---

### Phase 2: AoT Compatibility

**Goal**: Eliminate runtime reflection and enable native AOT compilation.

#### 2.1 Reflection Points to Address

| Location | Current Code | AoT Issue | Solution |
|----------|-------------|-----------|----------|
| `Bdd.FindByStackTrace()` | `StackTrace().GetFrames()` | Stack walking trimmed | Remove entirely, use `IContextProvider` |
| `Bdd.CreateContext()` | `GetCustomAttribute<T>()` | Attribute reflection | Source generator for metadata |
| `ScenarioContextBuilder` | `GetCustomAttributes<TagAttribute>()` | Generic reflection | Pre-generate metadata |
| Delegate wrapping | `ToCT<T>()` methods | No issue (generics preserved) | Keep, optimize allocations |

#### 2.2 Source Generator: WorkflowMetadataGenerator

```csharp
// Generates compile-time metadata for workflow classes
[Generator]
public class WorkflowMetadataGenerator : IIncrementalGenerator
{
    // Scans for [Workflow], [Feature], [Scenario] attributes
    // Generates static metadata tables:

    // Generated code example:
    // internal static class MyTestsMetadata
    // {
    //     public static readonly WorkflowInfo Workflow = new()
    //     {
    //         Name = "Calculator Tests",
    //         Description = "Tests for calculator operations",
    //         Tags = ImmutableArray.Create("math", "smoke")
    //     };
    //
    //     public static readonly ImmutableDictionary<string, ScenarioInfo> Scenarios =
    //         ImmutableDictionary.CreateRange([
    //             KeyValuePair.Create("AddNumbers", new ScenarioInfo { Name = "Addition", Tags = ... }),
    //             ...
    //         ]);
    // }
}
```

#### 2.3 Source Generator: DelegateNormalizationGenerator

```csharp
// Pre-generates optimized delegate wrappers to avoid runtime allocation
[Generator]
public class DelegateNormalizationGenerator : IIncrementalGenerator
{
    // Analyzes lambda expressions in workflow chains
    // Generates cached delegate instances:

    // Generated code example:
    // [CompilerGenerated]
    // file static class WorkflowDelegates
    // {
    //     public static readonly Func<int, CancellationToken, ValueTask<int>>
    //         Transform_Int32_Int32 = (v, _) => new ValueTask<int>(v);
    //
    //     // Pooled wrapper for user delegates
    //     public static Func<T, CancellationToken, ValueTask<TOut>> Wrap<T, TOut>(Func<T, TOut> f)
    //         => CachedDelegates<T, TOut>.GetOrCreate(f);
    // }
}
```

#### 2.4 AoT-Safe Context Discovery

```csharp
namespace PatternKit.Core;

/// <summary>
/// AoT-safe context provider using CallerMemberName and explicit registration.
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Creates a workflow context using compile-time information.
    /// </summary>
    WorkflowContext CreateContext(
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null);
}

// Implementation uses source-generated metadata tables
public sealed class GeneratedMetadataProvider : IContextProvider
{
    public WorkflowContext CreateContext(string? memberName, string? filePath)
    {
        // Look up pre-generated metadata instead of reflection
        var metadata = WorkflowMetadataRegistry.Lookup(filePath, memberName);
        return new WorkflowContext { WorkflowName = metadata.Name, ... };
    }
}
```

---

### Phase 3: Performance Optimization

**Goal**: Minimize allocations, optimize hot paths, enable zero-copy where possible.

#### 3.1 Allocation Reduction Strategies

| Area | Current | Optimized | Savings |
|------|---------|-----------|---------|
| Delegate wrapping | New closure per step | Pooled/cached delegates | ~40 bytes/step |
| Step queue | `Queue<Step>` | `ArrayPool<Step>` | Variable |
| Step results | `new StepResult` per step | Pooled structs | ~100 bytes/step |
| String formatting | Interpolated strings | Cached/pooled | Variable |
| ValueTask wrapping | `new ValueTask<T>(f())` | Direct return | 0 bytes for sync |

#### 3.2 ExecutionPipeline Optimization

```csharp
namespace PatternKit.Core;

internal sealed class ExecutionPipeline
{
    // Use ArrayPool for step storage
    private Step[] _steps;
    private int _stepCount;
    private static readonly ArrayPool<Step> StepPool = ArrayPool<Step>.Shared;

    // Inline hot path
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(in Step step)
    {
        EnsureCapacity();
        _steps[_stepCount++] = step;
    }

    // Struct-based step to avoid heap allocation
    private readonly record struct Step(
        StepPhase Phase,
        StepWord Word,
        string Title,
        // Use function pointer for perf-critical scenarios
        delegate*<object?, CancellationToken, ValueTask<object?>> Exec);

    // Span-based iteration
    public async ValueTask RunAsync(CancellationToken ct)
    {
        var steps = _steps.AsSpan(0, _stepCount);
        foreach (ref readonly var step in steps)
        {
            // Hot path - no allocation for sync completion
            var task = step.Exec(_state, ct);
            _state = task.IsCompletedSuccessfully
                ? task.Result
                : await task.ConfigureAwait(false);
        }
    }
}
```

#### 3.3 Zero-Allocation Delegate Patterns

```csharp
namespace PatternKit.Core;

/// <summary>
/// Cached delegate wrappers to avoid closure allocation.
/// </summary>
internal static class DelegateCache<T, TOut>
{
    // Use ConditionalWeakTable to allow GC of unused delegates
    private static readonly ConditionalWeakTable<Delegate, Func<T, CancellationToken, ValueTask<TOut>>>
        Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<T, CancellationToken, ValueTask<TOut>> Normalize(Func<T, TOut> f)
    {
        return Cache.GetOrCreateValue(f, static f => (v, _) => new ValueTask<TOut>(((Func<T, TOut>)f)(v)));
    }
}
```

#### 3.4 Benchmarks

Add BenchmarkDotNet suite:

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class WorkflowBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task SimpleChain_Current()
    {
        // Current TinyBDD implementation
    }

    [Benchmark]
    public async Task SimpleChain_Optimized()
    {
        // PatternKit.Core implementation
    }

    [Benchmark]
    public async Task DataDriven_100Rows()
    {
        // Measure data-driven scenario performance
    }
}
```

---

### Phase 4: Extensible Mediator Pattern

**Goal**: Enable handler-based step execution with dependency injection.

#### 4.1 IStepHandler Interface

```csharp
namespace PatternKit.Core.Handlers;

/// <summary>
/// Handles a specific step type in a workflow.
/// Enables mediator-style decoupling of step definitions from implementations.
/// </summary>
public interface IStepHandler<in TRequest, TResponse>
{
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

/// <summary>
/// Marker interface for step requests.
/// </summary>
public interface IStep<out TResponse> { }

// Example usage:
public record AddNumbers(int A, int B) : IStep<int>;

public class AddNumbersHandler : IStepHandler<AddNumbers, int>
{
    public ValueTask<int> HandleAsync(AddNumbers request, CancellationToken ct)
        => new(request.A + request.B);
}
```

#### 4.2 IBehavior Interface (Middleware)

```csharp
namespace PatternKit.Core.Behaviors;

/// <summary>
/// Cross-cutting behavior that wraps step execution.
/// Implements the Chain of Responsibility pattern.
/// </summary>
public interface IBehavior<T>
{
    ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct);
}

// Built-in behaviors:
public class TimingBehavior<T> : IBehavior<T>
{
    public async ValueTask<T> WrapAsync(...)
    {
        var sw = Stopwatch.StartNew();
        try { return await next(ct); }
        finally { context.SetMetadata($"timing:{step.Title}", sw.Elapsed); }
    }
}

public class RetryBehavior<T> : IBehavior<T>
{
    private readonly int _maxRetries;

    public async ValueTask<T> WrapAsync(...)
    {
        for (int i = 0; i <= _maxRetries; i++)
        {
            try { return await next(ct); }
            catch when (i < _maxRetries) { await Task.Delay(100 * (i + 1), ct); }
        }
        throw new InvalidOperationException("Unreachable");
    }
}
```

#### 4.3 Dependency Injection Integration

```csharp
namespace PatternKit.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPatternKit(
        this IServiceCollection services,
        Action<PatternKitOptions>? configure = null)
    {
        var options = new PatternKitOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IStepHandlerFactory, ServiceProviderHandlerFactory>();
        services.AddScoped<WorkflowContext>();

        // Register built-in behaviors
        services.AddSingleton<IBehavior<object>, TimingBehavior<object>>();

        return services;
    }

    public static IServiceCollection AddStepHandler<TRequest, TResponse, THandler>(
        this IServiceCollection services)
        where TRequest : IStep<TResponse>
        where THandler : class, IStepHandler<TRequest, TResponse>
    {
        services.AddTransient<IStepHandler<TRequest, TResponse>, THandler>();
        return services;
    }
}

// Usage in ASP.NET Core:
builder.Services.AddPatternKit(opts =>
{
    opts.DefaultTimeout = TimeSpan.FromSeconds(30);
    opts.EnableMetrics = true;
});

builder.Services.AddStepHandler<ProcessOrder, OrderResult, ProcessOrderHandler>();
```

---

### Phase 5: TinyBDD Compatibility Layer

**Goal**: Maintain 100% backward compatibility while leveraging PatternKit.Core.

#### 5.1 TinyBDD as Thin Wrapper

```csharp
namespace TinyBDD;

/// <summary>
/// ScenarioContext extends WorkflowContext with testing-specific features.
/// </summary>
public sealed class ScenarioContext : WorkflowContext
{
    // Testing-specific properties
    public string FeatureName => WorkflowName;
    public string? FeatureDescription => Description;
    public string ScenarioName { get; }
    public IReadOnlyCollection<string> Tags => GetExtension<TagCollection>()?.Tags ?? [];

    // Backward-compatible factory
    public ScenarioContext(
        string featureName,
        string? featureDescription,
        string scenarioName,
        ITraitBridge traitBridge,
        ScenarioOptions options)
        : base()
    {
        WorkflowName = featureName;
        Description = featureDescription;
        ScenarioName = scenarioName;
        Options = options.ToWorkflowOptions();
        SetExtension(new TraitBridgeExtension(traitBridge));
    }
}

/// <summary>
/// ScenarioChain is a type alias for WorkflowChain with testing defaults.
/// </summary>
public sealed class ScenarioChain<T> : WorkflowChain<T>
{
    // Maintain existing API surface
}
```

#### 5.2 Migration Guide

```markdown
## Migrating from TinyBDD to PatternKit

### No Changes Required
- Existing test code continues to work unchanged
- TinyBDD namespace remains available
- All current APIs are preserved

### Optional Upgrades
1. Use `WorkflowContext` for non-test workflows
2. Add behaviors for cross-cutting concerns
3. Use DI for step handler registration
4. Leverage source generators for AoT builds

### New Capabilities
- Command/query workflow orchestration
- Retry/circuit-breaker behaviors
- Metrics and observability
- Native AOT compilation
```

---

## Implementation Roadmap

### Milestone 1: Core Abstraction (Phase 1)
- [ ] Create `PatternKit.Core` project
- [ ] Implement `WorkflowContext`, `WorkflowChain<T>`, `ResultChain<T>`
- [ ] Implement `ExecutionPipeline` (copy from Pipeline, generalize)
- [ ] Add extension point infrastructure
- [ ] Update TinyBDD to inherit from PatternKit.Core

### Milestone 2: AoT Support (Phase 2)
- [ ] Create `PatternKit.Generators` project
- [ ] Implement `WorkflowMetadataGenerator`
- [ ] Remove `FindByStackTrace()` fallback
- [ ] Add `[CallerMemberName]` based context creation
- [ ] Verify with `PublishAot=true`

### Milestone 3: Performance (Phase 3)
- [ ] Create benchmark project
- [ ] Implement pooled step storage
- [ ] Add delegate caching
- [ ] Optimize hot paths with `[MethodImpl]`
- [ ] Achieve <100 bytes allocation per simple chain

### Milestone 4: Mediator/Behaviors (Phase 4)
- [ ] Implement `IStepHandler<TRequest, TResponse>`
- [ ] Implement `IBehavior<T>` pipeline
- [ ] Add DI integration
- [ ] Create built-in behaviors (Timing, Retry, CircuitBreaker)

### Milestone 5: Polish & Compatibility (Phase 5)
- [ ] Ensure TinyBDD tests pass unchanged
- [ ] Write migration documentation
- [ ] Add example projects for non-test use cases
- [ ] Performance comparison benchmarks

---

## Delegate Support Matrix

| Delegate Type | Sync | Task | ValueTask | CT+Task | CT+ValueTask |
|--------------|------|------|-----------|---------|--------------|
| `Func<T, TOut>` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Action<T>` | ✅ | - | - | - | - |
| `Func<T, Task>` | - | ✅ | - | - | - |
| `Func<T, ValueTask>` | - | - | ✅ | - | - |
| `Func<T, CT, Task<TOut>>` | - | - | - | ✅ | - |
| `Func<T, CT, ValueTask<TOut>>` | - | - | - | - | ✅ |
| `Func<T, TState, TOut>` (state-passing) | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Func<bool>` (no-input predicate) | ✅ | ✅ | ✅ | ✅ | ✅ |

All delegate types normalized to: `Func<T, CancellationToken, ValueTask<TOut>>`

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Allocation per simple chain | <100 bytes |
| Step execution overhead | <1μs |
| AoT binary size | <2MB for simple app |
| Backward compatibility | 100% existing tests pass |
| New API surface | <50 new public types |

---

## Appendix: File Changes Summary

### New Files
```
src/PatternKit.Core/
├── Workflows/
│   ├── WorkflowContext.cs
│   ├── WorkflowChain.cs
│   ├── ResultChain.cs
│   └── ExecutionPipeline.cs
├── Steps/
│   ├── StepPhase.cs (move from Pipeline.cs)
│   ├── StepWord.cs (move from Pipeline.cs)
│   └── StepResult.cs (move)
├── Handlers/
│   ├── IStepHandler.cs
│   └── StepHandlerRegistry.cs
├── Behaviors/
│   ├── IBehavior.cs
│   └── BehaviorPipeline.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs

src/PatternKit.Generators/
├── WorkflowMetadataGenerator.cs
└── DelegateNormalizationGenerator.cs
```

### Modified Files
```
src/TinyBDD/Core/
├── Bdd.cs           → Delegates to PatternKit.Core
├── ScenarioContext.cs → Inherits WorkflowContext
├── ScenarioChain.cs → Wraps WorkflowChain
├── ThenChain.cs     → Wraps ResultChain
└── Pipeline.cs      → Replaced by ExecutionPipeline
```

### Deleted Files
```
(None - maintain backward compatibility)
```
