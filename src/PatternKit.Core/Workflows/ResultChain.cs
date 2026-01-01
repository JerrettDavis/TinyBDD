using System.Runtime.CompilerServices;

namespace PatternKit.Core;

/// <summary>
/// Represents the terminal chain of a workflow, which can be awaited to execute all steps.
/// </summary>
/// <typeparam name="T">The value type carried by the chain.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="ResultChain{T}"/> is obtained by calling a <c>Then</c> method on a
/// <see cref="WorkflowChain{T}"/>. Awaiting this chain executes all queued steps
/// in the underlying <see cref="ExecutionPipeline"/>.
/// </para>
/// <para>
/// Additional <c>And</c>/<c>But</c> steps can be chained after <c>Then</c>, and
/// execution can be explicitly triggered via <see cref="AssertPassed"/> or
/// <see cref="AssertFailed"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Awaiting executes the workflow
/// await Workflow.Given(ctx, "start", () => 1)
///               .When("add", x => x + 1)
///               .Then("== 2", v => v == 2);
///
/// // Or explicitly assert
/// await Workflow.Given(ctx, "start", () => 1)
///               .When("add", x => x + 1)
///               .Then("== 2", v => v == 2)
///               .AssertPassed();
/// </code>
/// </example>
public readonly struct ResultChain<T>
{
    private readonly ExecutionPipeline _p;

    internal ResultChain(ExecutionPipeline p) => _p = p;

    /// <summary>
    /// Gets an awaiter that executes the workflow when awaited.
    /// </summary>
    /// <returns>A task awaiter for the workflow execution.</returns>
    public TaskAwaiter GetAwaiter() => _p.RunAsync(CancellationToken.None).AsTask().GetAwaiter();

    /// <summary>
    /// Executes the workflow and asserts that all steps passed.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public async ValueTask AssertPassed(CancellationToken ct = default)
    {
        await _p.RunAsync(ct).ConfigureAwait(false);

        if (!_p.Context.AllPassed)
        {
            var first = _p.Context.FirstFailure;
            throw new WorkflowAssertionException(
                $"Workflow failed at step: {first?.Kind} {first?.Title}");
        }
    }

    /// <summary>
    /// Executes the workflow and asserts that at least one step failed.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    public async ValueTask AssertFailed(CancellationToken ct = default)
    {
        try
        {
            await _p.RunAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // Expected - a step threw
            return;
        }

        if (_p.Context.AllPassed)
        {
            throw new WorkflowAssertionException("Expected workflow to fail, but all steps passed.");
        }
    }

    /// <summary>
    /// Executes the workflow and returns the final value.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>The final value of type T.</returns>
    public async ValueTask<T> GetResultAsync(CancellationToken ct = default)
    {
        await _p.RunAsync(ct).ConfigureAwait(false);
        return (T)_p.Context.CurrentValue!;
    }

    #region Delegate normalization

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Action<T> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f(v);
            return default;
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, Task> f)
        => async (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await f(v).ConfigureAwait(false);
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, ValueTask> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return f(v);
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, ValueTask> f)
        => f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, TOut> f)
        => (v, _) => new ValueTask<TOut>(f(v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, Task<TOut>> f)
        => async (v, _) => await f(v).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred(Func<T, bool> f)
        => (v, _) => new ValueTask<bool>(f(v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred(Func<T, Task<bool>> f)
        => async (v, _) => await f(v).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred(Func<T, ValueTask<bool>> f)
        => (v, _) => f(v);

    #endregion

    #region Core implementations

    private ResultChain<T> EffectInherit(StepWord word, string? title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit(title ?? "",
            async (s, ct) =>
            {
                await effect((T)s!, ct).ConfigureAwait(false);
                return s;
            },
            word);
        return this;
    }

    private ResultChain<TOut> TransformInherit<TOut>(StepWord word, string? title, Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false),
            word);
        return new ResultChain<TOut>(_p);
    }

    private ResultChain<T> PredicateInherit(StepWord word, string? title, Func<T, CancellationToken, ValueTask<bool>> pred)
    {
        _p.EnqueueInherit(title ?? "",
            async (s, ct) =>
            {
                AssertUtil.Ensure(await pred((T)s!, ct).ConfigureAwait(false), title ?? "And");
                return s;
            },
            word);
        return this;
    }

    private ResultChain<T> FinallyEffect(string? title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueFinally(title ?? "Finally", effect);
        return this;
    }

    #endregion

    #region And overloads

    /// <summary>Adds an <c>And</c> predicate assertion step.</summary>
    public ResultChain<T> And(string title, Func<T, bool> predicate) =>
        PredicateInherit(StepWord.And, title, ToCTPred(predicate));

    /// <summary>Adds an <c>And</c> predicate assertion step with async Task.</summary>
    public ResultChain<T> And(string title, Func<T, Task<bool>> predicate) =>
        PredicateInherit(StepWord.And, title, ToCTPred(predicate));

    /// <summary>Adds an <c>And</c> predicate assertion step with async ValueTask.</summary>
    public ResultChain<T> And(string title, Func<T, ValueTask<bool>> predicate) =>
        PredicateInherit(StepWord.And, title, predicate);

    /// <summary>Adds an <c>And</c> action assertion step.</summary>
    public ResultChain<T> And(string title, Action<T> assertion) =>
        EffectInherit(StepWord.And, title, ToCT(assertion));

    /// <summary>Adds an <c>And</c> action assertion step with async Task.</summary>
    public ResultChain<T> And(string title, Func<T, Task> assertion) =>
        EffectInherit(StepWord.And, title, ToCT(assertion));

    /// <summary>Adds an <c>And</c> transformation step.</summary>
    public ResultChain<TOut> And<TOut>(string title, Func<T, TOut> f) =>
        TransformInherit(StepWord.And, title, ToCT(f));

    /// <summary>Adds an <c>And</c> transformation step with async Task.</summary>
    public ResultChain<TOut> And<TOut>(string title, Func<T, Task<TOut>> f) =>
        TransformInherit(StepWord.And, title, ToCT(f));

    /// <summary>Adds an <c>And</c> predicate without title.</summary>
    public ResultChain<T> And(Func<T, bool> predicate) =>
        PredicateInherit(StepWord.And, null, ToCTPred(predicate));

    /// <summary>Adds an <c>And</c> action without title.</summary>
    public ResultChain<T> And(Action<T> assertion) =>
        EffectInherit(StepWord.And, null, ToCT(assertion));

    #endregion

    #region But overloads

    /// <summary>Adds a <c>But</c> predicate assertion step.</summary>
    public ResultChain<T> But(string title, Func<T, bool> predicate) =>
        PredicateInherit(StepWord.But, title, ToCTPred(predicate));

    /// <summary>Adds a <c>But</c> predicate assertion step with async Task.</summary>
    public ResultChain<T> But(string title, Func<T, Task<bool>> predicate) =>
        PredicateInherit(StepWord.But, title, ToCTPred(predicate));

    /// <summary>Adds a <c>But</c> action assertion step.</summary>
    public ResultChain<T> But(string title, Action<T> assertion) =>
        EffectInherit(StepWord.But, title, ToCT(assertion));

    /// <summary>Adds a <c>But</c> action assertion step with async Task.</summary>
    public ResultChain<T> But(string title, Func<T, Task> assertion) =>
        EffectInherit(StepWord.But, title, ToCT(assertion));

    /// <summary>Adds a <c>But</c> transformation step.</summary>
    public ResultChain<TOut> But<TOut>(string title, Func<T, TOut> f) =>
        TransformInherit(StepWord.But, title, ToCT(f));

    #endregion

    #region Finally overloads

    /// <summary>Registers a cleanup handler.</summary>
    public ResultChain<T> Finally(string title, Action<T> cleanup) =>
        FinallyEffect(title, ToCT(cleanup));

    /// <summary>Registers a cleanup handler with async Task.</summary>
    public ResultChain<T> Finally(string title, Func<T, Task> cleanup) =>
        FinallyEffect(title, ToCT(cleanup));

    /// <summary>Registers a cleanup handler with async ValueTask.</summary>
    public ResultChain<T> Finally(string title, Func<T, ValueTask> cleanup) =>
        FinallyEffect(title, ToCT(cleanup));

    /// <summary>Registers a cleanup handler with cancellation token.</summary>
    public ResultChain<T> Finally(string title, Func<T, CancellationToken, ValueTask> cleanup) =>
        FinallyEffect(title, cleanup);

    #endregion

    #region When overloads (continue from Then)

    /// <summary>Continues with a <c>When</c> transformation step.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, TOut> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title,
            async (s, ct) => await ToCT(f)((T)s!, ct).ConfigureAwait(false));
        return new WorkflowChain<TOut>(_p);
    }

    /// <summary>Continues with a <c>When</c> transformation step with async Task.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, Task<TOut>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title,
            async (s, ct) => await ToCT(f)((T)s!, ct).ConfigureAwait(false));
        return new WorkflowChain<TOut>(_p);
    }

    /// <summary>Continues with a <c>When</c> side effect step.</summary>
    public WorkflowChain<T> When(string title, Action<T> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title,
            async (s, ct) =>
            {
                await ToCT(effect)((T)s!, ct).ConfigureAwait(false);
                return s;
            });
        return new WorkflowChain<T>(_p);
    }

    #endregion
}
