using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Demonstrates the impact of removing boxing/unboxing from the pipeline.
/// Compares current object-based approach vs generic approach.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class BoxingOverheadBenchmark
{
    private int _value;
    
    [GlobalSetup]
    public void Setup()
    {
        _value = 42;
    }

    /// <summary>
    /// Baseline: Direct operation with no boxing.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int Baseline_NoBoxing()
    {
        var result = Transform(_value);
        return result;
    }

    /// <summary>
    /// Current TinyBDD approach: Box to object?, unbox back.
    /// Simulates current Pipeline._state pattern.
    /// </summary>
    [Benchmark]
    public int Current_WithBoxing()
    {
        object? boxed = _value; // Box
        var result = TransformBoxed(boxed); // Unbox inside
        return result;
    }

    /// <summary>
    /// Proposed approach: Use generics to avoid boxing.
    /// Simulates proposed Pipeline&lt;T&gt; pattern.
    /// </summary>
    [Benchmark]
    public int Proposed_Generic()
    {
        var result = TransformGeneric(_value);
        return result;
    }

    /// <summary>
    /// Multiple steps - Current approach with boxing.
    /// </summary>
    [Benchmark]
    public int Current_MultiStep_WithBoxing()
    {
        object? state = _value;
        state = TransformBoxed(state);
        state = TransformBoxed(state);
        state = TransformBoxed(state);
        return (int)state!;
    }

    /// <summary>
    /// Multiple steps - Proposed generic approach.
    /// </summary>
    [Benchmark]
    public int Proposed_MultiStep_Generic()
    {
        var state = _value;
        state = TransformGeneric(state);
        state = TransformGeneric(state);
        state = TransformGeneric(state);
        return state;
    }

    private static int Transform(int value) => value + 1;
    
    private static int TransformBoxed(object? boxed)
    {
        var value = (int)boxed!; // Unbox
        return value + 1;
    }

    private static T TransformGeneric<T>(T value) where T : struct
    {
        if (value is int intValue)
            return (T)(object)(intValue + 1);
        return value;
    }
}

/// <summary>
/// Demonstrates the full pipeline optimization comparing current vs proposed architecture.
/// This simulates a realistic TinyBDD scenario with multiple steps.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class PipelineOptimizationBenchmark
{
    private CurrentPipeline? _currentPipeline;
    private OptimizedPipeline<int>? _optimizedPipeline;

    [GlobalSetup]
    public void Setup()
    {
        _currentPipeline = new CurrentPipeline();
        _optimizedPipeline = new OptimizedPipeline<int>();
    }

    /// <summary>
    /// Current architecture: object-based state, allocates per step.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> Current_Pipeline()
    {
        _currentPipeline!.Enqueue(state => (int)state! + 1);
        _currentPipeline.Enqueue(state => (int)state! * 2);
        _currentPipeline.Enqueue(state => (int)state! - 3);
        return await _currentPipeline.RunAsync(42);
    }

    /// <summary>
    /// Optimized architecture: generic state, no boxing, pooled allocations.
    /// </summary>
    [Benchmark]
    public async Task<int> Optimized_Pipeline()
    {
        _optimizedPipeline!.Enqueue(state => state + 1);
        _optimizedPipeline.Enqueue(state => state * 2);
        _optimizedPipeline.Enqueue(state => state - 3);
        return await _optimizedPipeline.RunAsync(42);
    }

    /// <summary>
    /// Simulates current TinyBDD pipeline architecture.
    /// </summary>
    private class CurrentPipeline
    {
        private readonly Queue<Func<object?, object?>> _steps = new();
        private object? _state;

        public void Enqueue(Func<object?, object?> step) => _steps.Enqueue(step);

        public async ValueTask<int> RunAsync(int initialState)
        {
            _state = initialState; // Box

            while (_steps.Count > 0)
            {
                var step = _steps.Dequeue();
                _state = step(_state); // Box/unbox on each step
                await ValueTask.CompletedTask; // Simulate async
            }

            return (int)_state!; // Final unbox
        }
    }

    /// <summary>
    /// Proposed optimized pipeline using generics.
    /// </summary>
    private class OptimizedPipeline<T>
    {
        private struct Step
        {
            public Func<T, T> Transform;
        }

        private readonly List<Step> _steps = new(capacity: 8);
        private T _state = default!;

        public void Enqueue(Func<T, T> transform)
        {
            _steps.Add(new Step { Transform = transform });
        }

        public ValueTask<T> RunAsync(T initialState)
        {
            _state = initialState; // No boxing - direct assignment

            for (int i = 0; i < _steps.Count; i++)
            {
                _state = _steps[i].Transform(_state); // No boxing - direct call
            }

            _steps.Clear(); // Reuse list
            return new ValueTask<T>(_state);
        }
    }
}
