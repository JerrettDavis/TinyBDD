using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyBDD.Assertions;

namespace TinyBDD;

/// <summary>
/// Identifies the high-level BDD phase of a step: <c>Given</c>, <c>When</c>, or <c>Then</c>.
/// </summary>
internal enum StepPhase
{
    /// <summary>Setup and preconditions.</summary>
    Given,

    /// <summary>Action or behavior under test.</summary>
    When,

    /// <summary>Verification/assertion.</summary>
    Then
}

/// <summary>
/// Identifies the BDD connective used for a step within a phase: primary keyword, <c>And</c>, or <c>But</c>.
/// </summary>
internal enum StepWord
{
    /// <summary>The primary keyword for the phase (e.g., <c>Given</c>, <c>When</c>, <c>Then</c>).</summary>
    Primary,

    /// <summary>Connective that continues the previous phase: <c>And</c>.</summary>
    And,

    /// <summary>Connective that continues the previous phase with contrast: <c>But</c>.</summary>
    But
}

/// <summary>
/// Utility for computing the human-readable keyword displayed for a step line
/// (e.g., <c>Given</c>, <c>When</c>, <c>Then</c>, <c>And</c>, <c>But</c>).
/// </summary>
internal static class KindStrings
{
    /// <summary>
    /// Gets the display keyword for a step based on its phase and connective.
    /// </summary>
    /// <param name="phase">The BDD phase.</param>
    /// <param name="word">The connective for the step.</param>
    /// <returns>
    /// <c>And</c> or <c>But</c> for connective steps; otherwise the phase name (<c>Given</c>, <c>When</c>, <c>Then</c>).
    /// </returns>
    public static string For(StepPhase phase, StepWord word)
        => word switch { StepWord.And => "And", StepWord.But => "But", _ => phase.ToString() };
}

/// <summary>
/// Minimal assertion helper used by TinyBDD step implementations.
/// </summary>
internal static class AssertUtil
{
    /// <summary>
    /// Throws <see cref="TinyBddAssertionException"/> if the provided condition is <see langword="false"/>.
    /// </summary>
    /// <param name="ok">Condition to validate.</param>
    /// <param name="title">Assertion title used for error reporting.</param>
    /// <exception cref="TinyBddAssertionException">Thrown when <paramref name="ok"/> is <see langword="false"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Ensure(bool ok, string title)
    {
        if (!ok) throw new TinyBddAssertionException($"Assertion failed: {title}");
    }
}

/// <summary>
/// Executes a queued series of BDD steps for a single scenario, recording timing and results
/// into the owning <see cref="ScenarioContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is a single-consumer queue that executes steps in FIFO order. Each step receives the
/// previous step’s state object and may return a new state. Timing, keyword, title, and any error are
/// captured as <see cref="StepResult"/> entries in <see cref="ScenarioContext.Steps"/>.
/// </para>
/// <para>
/// Behavior is governed by <see cref="ScenarioOptions"/>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ScenarioOptions.ContinueOnError"/> — continue executing after non-assert exceptions.</description></item>
///   <item><description><see cref="ScenarioOptions.HaltOnFailedAssertion"/> — rethrow <see cref="TinyBddAssertionException"/> failures immediately.</description></item>
///   <item><description><see cref="ScenarioOptions.StepTimeout"/> — optional per-step timeout.</description></item>
///   <item><description><see cref="ScenarioOptions.MarkRemainingAsSkippedOnFailure"/> — mark enqueued steps as skipped when aborting.</description></item>
/// </list>
/// <para>
/// Two optional hooks, <see cref="BeforeStep"/> and <see cref="AfterStep"/>, enable lightweight telemetry or logging
/// around each step. Hooks are invoked synchronously on the executing thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enqueue steps
/// var pipe = new Pipeline(ctx);
/// pipe.Enqueue(StepPhase.Given, StepWord.Primary, "seed", async (s, ct) => 1);
/// pipe.EnqueueInherit("double", async (s, ct) => (object?)((int?)s ?? 0 * 2), StepWord.And);
/// await pipe.RunAsync(CancellationToken.None);
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="ScenarioOptions"/>
internal sealed class Pipeline(ScenarioContext ctx)
{
    private object? _state;
    private StepPhase _lastPhase = StepPhase.Given;
    private readonly Queue<Step> _steps = new();
    
    /// <summary>
    /// Gets the owning <see cref="ScenarioContext"/>.
    /// </summary>
    internal ScenarioContext Context => ctx;

    /// <summary>
    /// Optional hook invoked immediately before a step executes.
    /// </summary>
    /// <remarks>
    /// The provided <see cref="StepMetadata"/> contains the computed keyword, title, phase, and connective for the step.
    /// </remarks>
    public Action<ScenarioContext, StepMetadata>? BeforeStep { get; init; }

    /// <summary>
    /// Optional hook invoked after a step completes and its <see cref="StepResult"/> has been added to the context.
    /// </summary>
    public Action<ScenarioContext, StepResult>? AfterStep { get; init; }

    /// <summary>
    /// Represents a single enqueued step. Each step receives the prior state and returns the next state.
    /// </summary>
    private readonly struct Step(
        StepPhase phase,
        StepWord word,
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec
    )
    {
        /// <summary>The BDD phase for the step.</summary>
        public readonly StepPhase Phase = phase;

        /// <summary>The connective keyword (<c>Primary</c>, <c>And</c>, <c>But</c>).</summary>
        public readonly StepWord Word = word;

        /// <summary>Human-friendly title displayed in reports.</summary>
        public readonly string Title = title;

        /// <summary>The delegate that executes the step and returns the next state.</summary>
        public readonly Func<object?, CancellationToken, ValueTask<object?>> Exec = exec;

        /// <summary>
        /// Computes and returns the display keyword for the step (e.g., <c>Given</c>, <c>And</c>).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string KindCached() => KindStrings.For(Phase, Word);
    }

    /// <summary>
    /// Metadata sent to <see cref="BeforeStep"/> describing a step about to execute.
    /// </summary>
    /// <param name="Kind">The display keyword for the step (e.g., <c>Given</c>, <c>And</c>).</param>
    /// <param name="Title">The step title.</param>
    /// <param name="Phase">The BDD phase.</param>
    /// <param name="Word">The connective keyword.</param>
    public readonly record struct StepMetadata(
        string Kind,
        string Title,
        StepPhase Phase,
        StepWord Word
    );

    /// <summary>
    /// Enqueues a step with explicit phase, connective, title, and executor.
    /// </summary>
    /// <param name="phase">The BDD phase (<see cref="StepPhase"/>).</param>
    /// <param name="word">The connective keyword (<see cref="StepWord"/>).</param>
    /// <param name="title">Human-friendly title for reporting.</param>
    /// <param name="exec">The function that executes the step, receiving prior state and a token, returning next state.</param>
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
    /// Enqueues a step that inherits the previously enqueued phase, specifying only title, executor, and connective.
    /// </summary>
    /// <param name="title">Human-friendly title for reporting.</param>
    /// <param name="exec">The function that executes the step.</param>
    /// <param name="word">The connective keyword.</param>
    public void EnqueueInherit(
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec,
        StepWord word)
        => Enqueue(_lastPhase, word, title, exec);

    /// <summary>
    /// Executes all enqueued steps in FIFO order, recording results to <paramref>
    ///     <name>ctx</name>
    /// </paramref>
    /// .
    /// </summary>
    /// <param name="ct">Cancellation token that aborts execution between or during steps.</param>
    /// <remarks>
    /// <para>
    /// On success or handled failure, each step produces a <see cref="StepResult"/> captured in the scenario context.
    /// When <see cref="ScenarioOptions.ContinueOnError"/> is <see langword="false"/>, non-assert exceptions
    /// cause a <see cref="BddStepException"/> to be thrown after the failing step is recorded. When
    /// <see cref="ScenarioOptions.MarkRemainingAsSkippedOnFailure"/> is enabled, remaining steps are recorded as skipped.
    /// </para>
    /// <para>
    /// If <see cref="ScenarioOptions.HaltOnFailedAssertion"/> is <see langword="true"/>, a <see cref="TinyBddAssertionException"/>
    /// will be rethrown immediately after recording, halting the pipeline. Otherwise the failure is recorded and
    /// execution continues according to <see cref="ScenarioOptions.ContinueOnError"/>.
    /// </para>
    /// <para>
    /// If <see cref="ScenarioOptions.StepTimeout"/> is set, each step runs under a linked cancellation source that
    /// cancels after the configured timeout.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is canceled.</exception>
    /// <exception cref="BddStepException">Thrown when a step throws and <see cref="ScenarioOptions.ContinueOnError"/> is <see langword="false"/>.</exception>
    public async ValueTask RunAsync(CancellationToken ct)
    {
        var continueOnError = ctx.Options.ContinueOnError;
        var stepTimeout = ctx.Options.StepTimeout;
        var markRemainingAsSkipped = ctx.Options.MarkRemainingAsSkippedOnFailure;
        var haltOnFailedAssert = ctx.Options.HaltOnFailedAssertion;

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
            var input = _state; // capture input before executing

            BeforeStep?.Invoke(ctx, new StepMetadata(kind, title, step.Phase, step.Word));

            try
            {
                if (stepTimeout is { } timeout)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(timeout);
                    _state = await step.Exec(_state, cts.Token);
                }
                else
                {
                    _state = await step.Exec(_state, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cooperative cancellation from the caller; record and rethrow after finally.
                canceled = true;
                err = null;
            }
            catch (TinyBddAssertionException ex)
            {
                // Fluent assertion failure (Expect.That/For) should be treated like an assertion failure.
                err = ex;
                if (haltOnFailedAssert)
                    throw;
            }
            catch (Exception ex)
            {
                // Non-assert failures.
                err = ex;
                if (!continueOnError)
                {
                    CaptureStepResult();

                    if (markRemainingAsSkipped)
                        DrainAsSkipped();

                    throw new BddStepException(
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

            // Local helper to ensure we add the step result exactly once, even if we throw later.
            void CaptureStepResult()
            {
                sw.Stop();

                var result = new StepResult
                {
                    Kind = kind,
                    Title = title,
                    Elapsed = sw.Elapsed,
                    Error = CaptureCancel()
                };

                if (captured)
                    return;

                captured = true;

                // Record step timing/result
                ctx.AddStep(result);
                // Record IO and update current item pointer
                ctx.AddIO(new StepIO(kind, title, input, _state));
                ctx.CurrentItem = _state;

                AfterStep?.Invoke(ctx, result);
            }

            Exception? CaptureCancel()
            {
                return err ?? (canceled
                    ? new OperationCanceledException()
                    : null);
            }
        }
    }

    /// <summary>
    /// Drains any remaining steps and records each as skipped due to a prior failure.
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