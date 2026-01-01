using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PatternKit.Core;

/// <summary>
/// Executes a queued series of workflow steps, recording timing and results
/// into the owning <see cref="WorkflowContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is a single-consumer queue that executes steps in FIFO order. Each step receives the
/// previous step's state object and may return a new state. Timing, keyword, title, and any error are
/// captured as <see cref="StepResult"/> entries in <see cref="WorkflowContext.Steps"/>.
/// </para>
/// <para>
/// Behavior is governed by <see cref="WorkflowOptions"/>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="WorkflowOptions.ContinueOnError"/> - continue executing after non-assert exceptions.</description></item>
///   <item><description><see cref="WorkflowOptions.HaltOnFailedAssertion"/> - rethrow <see cref="WorkflowAssertionException"/> failures immediately.</description></item>
///   <item><description><see cref="WorkflowOptions.StepTimeout"/> - optional per-step timeout.</description></item>
///   <item><description><see cref="WorkflowOptions.MarkRemainingAsSkippedOnFailure"/> - mark enqueued steps as skipped when aborting.</description></item>
/// </list>
/// <para>
/// Two optional hooks, <see cref="BeforeStep"/> and <see cref="AfterStep"/>, enable lightweight telemetry or logging
/// around each step. Hooks are invoked synchronously on the executing thread.
/// </para>
/// </remarks>
/// <param name="ctx">The workflow context to execute within.</param>
internal sealed class ExecutionPipeline(WorkflowContext ctx)
{
    private object? _state;
    private StepPhase _lastPhase = StepPhase.Given;
    private readonly Queue<Step> _steps = new();
    private readonly List<Func<CancellationToken, ValueTask>> _finallyHandlers = [];

    /// <summary>
    /// Gets the owning <see cref="WorkflowContext"/>.
    /// </summary>
    internal WorkflowContext Context => ctx;

    /// <summary>
    /// Optional hook invoked immediately before a step executes.
    /// </summary>
    public Action<WorkflowContext, StepMetadata>? BeforeStep { get; init; }

    /// <summary>
    /// Optional hook invoked after a step completes and its <see cref="StepResult"/> has been added to the context.
    /// </summary>
    public Action<WorkflowContext, StepResult>? AfterStep { get; init; }

    /// <summary>
    /// Represents a single enqueued step.
    /// </summary>
    private readonly struct Step(
        StepPhase phase,
        StepWord word,
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec)
    {
        public readonly StepPhase Phase = phase;
        public readonly StepWord Word = word;
        public readonly string Title = title;
        public readonly Func<object?, CancellationToken, ValueTask<object?>> Exec = exec;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string KindCached() => KindStrings.For(Phase, Word);
    }

    /// <summary>
    /// Enqueues a step with explicit phase, connective, title, and executor.
    /// </summary>
    public void Enqueue(
        StepPhase phase,
        StepWord word,
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec)
    {
        _lastPhase = phase;
        _steps.Enqueue(new Step(phase, word, title, exec));
    }

    /// <summary>
    /// Enqueues a step that inherits the previously enqueued phase.
    /// </summary>
    public void EnqueueInherit(
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec,
        StepWord word)
        => Enqueue(_lastPhase, word, title, exec);

    /// <summary>
    /// Enqueues a finally handler that captures the current state.
    /// </summary>
    public void EnqueueFinally<T>(string title, Func<T, CancellationToken, ValueTask> handler)
    {
        Enqueue(_lastPhase, StepWord.Primary, title,
            (state, _) =>
            {
                var captured = (T)state!;
                _finallyHandlers.Add(token => handler(captured, token));
                return new ValueTask<object?>(state);
            });
    }

    /// <summary>
    /// Executes all enqueued steps in FIFO order.
    /// </summary>
    /// <param name="ct">Cancellation token that aborts execution.</param>
    /// <exception cref="OperationCanceledException">Thrown when canceled.</exception>
    /// <exception cref="WorkflowStepException">Thrown when a step throws and <see cref="WorkflowOptions.ContinueOnError"/> is false.</exception>
    public async ValueTask RunAsync(CancellationToken ct)
    {
        var continueOnError = ctx.Options.ContinueOnError;
        var stepTimeout = ctx.Options.StepTimeout;
        var markRemainingAsSkipped = ctx.Options.MarkRemainingAsSkippedOnFailure;
        var haltOnFailedAssert = ctx.Options.HaltOnFailedAssertion;

        try
        {
            while (_steps.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                var step = _steps.Dequeue();
                var title = string.IsNullOrWhiteSpace(step.Title)
                    ? step.Phase.ToString()
                    : step.Title;
                var kind = step.KindCached();
                var sw = Stopwatch.StartNew();

                Exception? err = null;
                var canceled = false;
                var captured = false;
                var input = _state;

                BeforeStep?.Invoke(ctx, new StepMetadata(kind, title, step.Phase, step.Word));

                try
                {
                    if (stepTimeout is { } timeout)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(timeout);
                        _state = await step.Exec(_state, cts.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        _state = await step.Exec(_state, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    canceled = true;
                    err = null;
                }
                catch (WorkflowAssertionException ex)
                {
                    err = ex;
                    if (haltOnFailedAssert)
                        throw;
                }
                catch (Exception ex)
                {
                    err = ex;
                    if (!continueOnError)
                    {
                        CaptureStepResult();

                        if (markRemainingAsSkipped)
                            DrainAsSkipped();

                        throw new WorkflowStepException(
                            $"Step failed: {step.KindCached()} {title}", ctx, ex);
                    }
                }
                finally
                {
                    CaptureStepResult();
                }

                if (canceled)
                    ct.ThrowIfCancellationRequested();

                continue;

                void CaptureStepResult()
                {
                    sw.Stop();

                    var result = new StepResult
                    {
                        Kind = kind,
                        Title = title,
                        Elapsed = sw.Elapsed,
                        Error = err ?? (canceled ? new OperationCanceledException() : null)
                    };

                    if (captured)
                        return;

                    captured = true;

                    ctx.AddStep(result);
                    ctx.AddIO(new StepIO(kind, title, input, _state));
                    ctx.CurrentValue = _state;

                    AfterStep?.Invoke(ctx, result);
                }
            }
        }
        finally
        {
            foreach (var handler in _finallyHandlers)
            {
                try
                {
                    await handler(ct).ConfigureAwait(false);
                }
                catch
                {
                    // Suppress to allow all finally handlers to run
                }
            }
        }
    }

    /// <summary>
    /// Drains remaining steps and records each as skipped.
    /// </summary>
    private void DrainAsSkipped()
    {
        while (_steps.Count > 0)
        {
            var pending = _steps.Dequeue();
            var title = string.IsNullOrWhiteSpace(pending.Title)
                ? pending.Phase.ToString()
                : pending.Title;

            ctx.AddStep(new StepResult
            {
                Kind = KindStrings.For(pending.Phase, pending.Word),
                Title = title,
                Elapsed = TimeSpan.Zero,
                Error = new InvalidOperationException("Skipped due to previous failure.")
            });
        }
    }
}
