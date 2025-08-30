using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TinyBDD;

internal enum StepPhase
{
    Given,
    When,
    Then
}

internal enum StepWord
{
    Primary,
    And,
    But
}

internal static class VT
{
    public static ValueTask<T> From<T>(T v) => ValueTask.FromResult(v);

    public static ValueTask<T> From<T>(Task<T> t) =>
        t.IsCompletedSuccessfully ? ValueTask.FromResult(t.Result) : new ValueTask<T>(t);
}

internal static class KindStrings
{
    public static string For(StepPhase phase, StepWord word)
        => word switch { StepWord.And => "And", StepWord.But => "But", _ => phase.ToString() };
}

internal static class AssertUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Ensure(bool ok, string title)
    {
        if (!ok) throw new BddAssertException($"Assertion failed: {title}");
    }
}

internal sealed class Pipeline(ScenarioContext ctx)
{
    private object? _state;
    private StepPhase _lastPhase = StepPhase.Given;
    private readonly ConcurrentQueue<Step> _steps = new();

    private readonly struct Step(
        StepPhase phase,
        StepWord word,
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec
    )
    {
        public readonly StepPhase Phase = phase;
        public readonly StepWord Word = word;
        public readonly string Title = title;
        public readonly Func<object?, CancellationToken, ValueTask<object?>> Exec = exec;
    }

    public void Enqueue(
        StepPhase phase,
        StepWord word,
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec)
    {
        _lastPhase = phase;
        _steps.Enqueue(new Step(phase, word, title, exec));
    }

    public void EnqueueInherit(string title, Func<object?, CancellationToken, ValueTask<object?>> exec, StepWord word)
        => Enqueue(_lastPhase, word, title, exec);

    public async ValueTask RunAsync(CancellationToken ct)
    {
        while (_steps.TryDequeue(out var step))
        {
            var sw = Stopwatch.StartNew();
            Exception? err = null;
            try
            {
                _state = await step.Exec(_state, ct);
            }
            catch (Exception ex)
            {
                // Trap & record; DO NOT rethrow
                err = ex;
            }
            finally
            {
                sw.Stop();
                ctx.AddStep(new StepResult
                {
                    Kind = KindStrings.For(step.Phase, step.Word),
                    Title = string.IsNullOrWhiteSpace(step.Title) ? step.Phase.ToString() : step.Title,
                    Elapsed = sw.Elapsed,
                    Error = err
                });
            }
        }
    }
}