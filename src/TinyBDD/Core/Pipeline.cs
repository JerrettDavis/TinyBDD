using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyBDD.Assertions;

namespace TinyBDD;

/// <summary>
/// Identifies the high-level BDD phase of a step: <c>Given</c>, <c>When</c>, or <c>Then</c>.
/// </summary>
public enum StepPhase
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
public enum StepWord
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
    // Cache common phase strings to avoid repeated ToString() calls
    private const string GivenStr = "Given";
    private const string WhenStr = "When";
    private const string ThenStr = "Then";
    private const string AndStr = "And";
    private const string ButStr = "But";
    
    /// <summary>
    /// Gets the display keyword for a step based on its phase and connective.
    /// </summary>
    /// <param name="phase">The BDD phase.</param>
    /// <param name="word">The connective for the step.</param>
    /// <returns>
    /// <c>And</c> or <c>But</c> for connective steps; otherwise the phase name (<c>Given</c>, <c>When</c>, <c>Then</c>).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string For(StepPhase phase, StepWord word)
    {
        if (word == StepWord.And) return AndStr;
        if (word == StepWord.But) return ButStr;
        
        return phase switch
        {
            StepPhase.Given => GivenStr,
            StepPhase.When => WhenStr,
            StepPhase.Then => ThenStr,
            _ => phase.ToString()
        };
    }
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
    private readonly Queue<Step> _steps = new(capacity: 8); // Pre-allocate for typical scenario
    private readonly List<Func<CancellationToken, ValueTask>> _finallyHandlers = new(capacity: 2);
    
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
    /// Enqueues a finally handler that captures the current state and executes after all steps complete.
    /// </summary>
    /// <typeparam name="T">The type of state to capture.</typeparam>
    /// <param name="title">Human-friendly title for reporting.</param>
    /// <param name="handler">The cleanup action to execute with the captured state.</param>
    /// <remarks>
    /// Finally handlers are executed in the order they are registered, after all normal steps complete.
    /// They execute even if previous steps throw exceptions. The handler receives the state value
    /// that existed at the point where Finally was called in the chain.
    /// </remarks>
    public void EnqueueFinally<T>(string title, Func<T, CancellationToken, ValueTask> handler)
    {
        // Enqueue a step that captures state and registers the cleanup handler
        Enqueue(_lastPhase, StepWord.Primary, title,
            (state, _) =>
            {
                var captured = (T)state!;
                _finallyHandlers.Add(token => handler(captured, token));
                return new ValueTask<object?>(state); // pass through unchanged
            });
    }

    /// <summary>
    /// Executes all enqueued steps in FIFO order, recording results to the scenario context.
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
    /// will be rethrown immediately after recording, halting the pipeline. Otherwise, the failure is recorded and
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
        var hasStepObservers = ctx.Options.ExtensibilityOptions?.StepObservers is { Count: > 0 };

        // Notify scenario observers
        await NotifyScenarioStarting(ctx);

        try
        {
            while (_steps.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                var step = _steps.Dequeue();
                var title = !string.IsNullOrWhiteSpace(step.Title)
                    ? step.Title
                    : KindStrings.For(step.Phase, StepWord.Primary); // Reuse cached phase strings
                var kind = step.KindCached();
                var sw = Stopwatch.StartNew();

                Exception? err = null;
                var canceled = false;
                var captured = false;
                var input = _state; // capture input before executing
                
                // Only create StepInfo if we have observers
                var stepInfo = new StepInfo(kind, title, step.Phase, step.Word);

                BeforeStep?.Invoke(ctx, new StepMetadata(kind, title, step.Phase, step.Word));

                // Notify step observers only if we have any
                if (hasStepObservers)
                    await NotifyStepStarting(ctx, stepInfo);

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
                        await CaptureStepResultAsync();

                        if (markRemainingAsSkipped)
                            DrainAsSkipped();

                        throw new BddStepException(
                            $"Step failed: {step.KindCached()} {title}", ctx, ex);
                    }
                }
                finally
                {
                    await CaptureStepResultAsync();
                }

                if (canceled)
                    ct.ThrowIfCancellationRequested();

                continue;

                // Local helper to ensure we add the step result exactly once, even if we throw later.
                async ValueTask CaptureStepResultAsync()
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
                    // Record IO and update the current item pointer
                    var io = new StepIO(kind, title, input, _state);
                    ctx.AddIO(io);
                    ctx.CurrentItem = _state;

                    AfterStep?.Invoke(ctx, result);

                    // Notify step observers - await to ensure they complete
                    // Exceptions are suppressed inside NotifyStepFinished to prevent masking test failures
                    if (hasStepObservers)
                        await NotifyStepFinished(ctx, stepInfo, result, io);
                }

                Exception? CaptureCancel()
                {
                    return err ?? (canceled
                        ? new OperationCanceledException()
                        : null);
                }
            }
        }
        finally
        {
            // Notify scenario observers
            await NotifyScenarioFinished(ctx);

            // Execute all finally handlers in order, even if steps threw exceptions
            foreach (var handler in _finallyHandlers)
            {
                try
                {
                    await handler(ct).ConfigureAwait(false);
                }
                catch
                {
                    // Suppress exceptions from finally handlers to prevent masking original exceptions
                    // and to allow all finally handlers to execute
                }
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
            var title = !string.IsNullOrWhiteSpace(pending.Title)
                ? pending.Title
                : KindStrings.For(pending.Phase, StepWord.Primary); // Reuse cached phase strings

            ctx.AddStep(new StepResult
            {
                Kind = KindStrings.For(pending.Phase, pending.Word),
                Title = title,
                Elapsed = TimeSpan.Zero,
                Error = new InvalidOperationException("Skipped due to previous failure.")
            });
        }
    }

    /// <summary>
    /// Notifies scenario observers that a scenario is starting.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask NotifyScenarioStarting(ScenarioContext context)
    {
        var observers = context.Options.ExtensibilityOptions?.ScenarioObservers;
        if (observers is null or { Count: 0 })
            return;

        foreach (var observer in observers)
        {
            try
            {
                await observer.OnScenarioStarting(context).ConfigureAwait(false);
            }
            catch
            {
                // Suppress observer exceptions to prevent masking test failures
            }
        }
    }

    /// <summary>
    /// Notifies scenario observers that a scenario has finished.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask NotifyScenarioFinished(ScenarioContext context)
    {
        var observers = context.Options.ExtensibilityOptions?.ScenarioObservers;
        if (observers is null or { Count: 0 })
            return;

        foreach (var observer in observers)
        {
            try
            {
                await observer.OnScenarioFinished(context).ConfigureAwait(false);
            }
            catch
            {
                // Suppress observer exceptions to prevent masking test failures
            }
        }
    }

    /// <summary>
    /// Notifies step observers that a step is starting.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask NotifyStepStarting(ScenarioContext context, StepInfo step)
    {
        var observers = context.Options.ExtensibilityOptions?.StepObservers;
        if (observers is null or { Count: 0 })
            return;

        foreach (var observer in observers)
        {
            try
            {
                await observer.OnStepStarting(context, step).ConfigureAwait(false);
            }
            catch
            {
                // Suppress observer exceptions to prevent masking test failures
            }
        }
    }

    /// <summary>
    /// Notifies step observers that a step has finished.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask NotifyStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io)
    {
        var observers = context.Options.ExtensibilityOptions?.StepObservers;
        if (observers is null or { Count: 0 })
            return;

        foreach (var observer in observers)
        {
            try
            {
                await observer.OnStepFinished(context, step, result, io).ConfigureAwait(false);
            }
            catch
            {
                // Suppress observer exceptions to prevent masking test failures
            }
        }
    }
}