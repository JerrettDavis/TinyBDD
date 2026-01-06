# TinyBDD Performance Optimizations

This document describes the performance optimizations applied to TinyBDD to minimize framework overhead.

## Overview

TinyBDD aims to introduce minimal overhead over direct test execution. Through careful optimization of hot paths and strategic use of modern C# features, we've reduced allocations and improved throughput.

## Optimization Categories

### 1. String Allocation Reduction

**Problem**: Repeated calls to `enum.ToString()` and string concatenation allocate on every step execution.

**Solution**:
- Cached common phase strings as `const` in `KindStrings` class
- Reuse cached strings instead of calling `ToString()` on enums
- Optimized string null/whitespace checks to avoid unnecessary allocations

**Impact**: Eliminates hundreds of string allocations per scenario

**Files Modified**:
- `src/TinyBDD/Core/Pipeline.cs` - KindStrings class optimization

```csharp
// Before: ~3 allocations per step
var kind = phase.ToString(); 

// After: 0 allocations
private const string GivenStr = "Given";
var kind = phase == StepPhase.Given ? GivenStr : ...;
```

### 2. Collection Pre-allocation

**Problem**: Default collection constructors start with capacity 0, causing multiple reallocations as steps are added.

**Solution**:
- Pre-allocate collections with typical scenario sizes
- `_steps` Queue: capacity 8 (typical scenario has 3-8 steps)
- `_finallyHandlers` List: capacity 2 (most scenarios have 0-2 cleanup handlers)
- `_steps` in ScenarioContext: capacity 8
- `_io` in ScenarioContext: capacity 8

**Impact**: Eliminates 2-3 collection reallocations per scenario

**Files Modified**:
- `src/TinyBDD/Core/Pipeline.cs` - Pipeline constructor
- `src/TinyBDD/Core/ScenarioContext.cs` - ScenarioContext collections

```csharp
// Before: Starts at 0, reallocates at 1, 2, 4, 8
private readonly Queue<Step> _steps = new();

// After: Starts at 8, no reallocations for typical scenarios
private readonly Queue<Step> _steps = new(capacity: 8);
```

### 3. Method Inlining

**Problem**: Frequent method calls have overhead even when methods are small.

**Solution**:
- Added `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to hot path methods
- Inlined methods:
  - `KindStrings.For()` - Called for every step
  - `AssertUtil.Ensure()` - Called for every Then/assertion
  - Pipeline observer notification methods
  - ScenarioChain Transform/Effect methods  
  - ScenarioChain ThenAssert methods

**Impact**: Reduces method call overhead by enabling JIT to inline small methods

**Files Modified**:
- `src/TinyBDD/Core/Pipeline.cs` - KindStrings, AssertUtil, observer methods
- `src/TinyBDD/Core/ScenarioChain.cs` - Transform, Effect, ThenAssert methods

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static void Ensure(bool ok, string title)
{
    if (!ok) throw new TinyBddAssertionException($"Assertion failed: {title}");
}
```

### 4. Observer Optimization

**Problem**: Observer notification infrastructure runs even when no observers are registered.

**Solution**:
- Cache observer count check results at scenario start
- Skip all observer notification when count is 0
- Early return in notification methods before iteration
- Added `hasStepObservers` flag to avoid repeated null checks

**Impact**: Eliminates unnecessary async operations when observers not present (common case)

**Files Modified**:
- `src/TinyBDD/Core/Pipeline.cs` - RunAsync method and observer notification methods

```csharp
// Cache at scenario start
var hasStepObservers = ctx.Options.ExtensibilityOptions?.StepObservers is { Count: > 0 };

// Skip notification entirely when no observers
if (hasStepObservers)
    await NotifyStepStarting(ctx, stepInfo);
```

### 5. StringComparer Optimization

**Problem**: Default HashSet string comparisons use culture-aware comparison which is slower.

**Solution**:
- Use `StringComparer.Ordinal` for tag HashSet
- Tags are identifiers, not user-facing text, so ordinal comparison is appropriate and faster

**Impact**: Faster tag lookups and additions

**Files Modified**:
- `src/TinyBDD/Core/ScenarioContext.cs` - Tags HashSet

```csharp
// Before: Uses default culture-aware comparison
private readonly HashSet<string> _tags = new();

// After: Uses fast ordinal comparison
private readonly HashSet<string> _tags = new(StringComparer.Ordinal);
```

## Performance Guidelines

When contributing to TinyBDD, follow these guidelines to maintain performance:

### DO:
- Use `const string` for frequently used strings
- Pre-allocate collections when typical size is known
- Add `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to small, frequently called methods
- Cache repeated calculations or property accesses in hot paths
- Use `ConfigureAwait(false)` for async operations not requiring sync context
- Prefer struct over class for small, short-lived data
- Use `StringComparer.Ordinal` for internal identifier comparisons

### DON'T:
- Call `ToString()` on enums in hot paths - cache the strings instead
- Allocate collections without considering capacity
- Add virtual method calls in hot paths
- Use LINQ in hot paths (prefer for/foreach loops)
- Create unnecessary intermediate objects
- Use string interpolation in hot paths when concatenation would suffice

## Future Optimization Opportunities

These optimizations have been identified but not yet implemented:

1. **Object Pooling**: Pool `StepResult` objects using `ArrayPool<T>` or similar
2. **Sync Fast Path**: Add optimized path for purely synchronous operations
3. **Delegate Caching**: Cache delegate instances where safe to avoid repeated allocations
4. **Span-based Operations**: Use `Span<char>` for string operations where beneficial
5. **ValueTask Optimization**: Use `ValueTask<T>` more aggressively to avoid Task allocations

## Measurement

Performance can be measured using the benchmarks in `benchmarks/TinyBDD.Benchmarks/`:

```bash
cd benchmarks/TinyBDD.Benchmarks
dotnet run -c Release --filter '*StepPipeline*'
```

Key benchmarks:
- `SingleAssertionBenchmark` - Measures overhead for simplest scenario
- `StepPipelineBenchmark` - Measures step dispatch overhead
- `ScenarioContextBenchmark` - Measures state management overhead

## References

- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [AggressiveInlining Attribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.methodimploptions)
- [StringComparer Class](https://docs.microsoft.com/en-us/dotnet/api/system.stringcomparer)
