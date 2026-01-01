using System.Runtime.CompilerServices;

namespace PatternKit.Core;

/// <summary>
/// Represents a fluent chain starting at <c>Given</c> and flowing through <c>When</c>,
/// <c>And</c>, <c>But</c>, and into <c>Then</c> where it becomes an awaitable <see cref="ResultChain{T}"/>.
/// </summary>
/// <typeparam name="T">The value type carried by the chain at the current position.</typeparam>
/// <remarks>
/// <para>
/// Steps are recorded and executed only when the terminal <see cref="ResultChain{T}"/> is awaited.
/// Overloads are provided for synchronous and asynchronous transforms and side-effects, with
/// optional titles and cancellation tokens.
/// </para>
/// <para>
/// User code exceptions thrown inside delegates are captured as step failures and surfaced in
/// <see cref="WorkflowContext.Steps"/> and reporters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = new WorkflowContext { WorkflowName = "Math" };
/// await Workflow.Given(ctx, "start", () => 10)
///               .When("add 5", x => x + 5)
///               .And(v => v * 2)
///               .Then(v => v == 30);
/// </code>
/// </example>
public sealed partial class WorkflowChain<T>
{
    private readonly ExecutionPipeline _p;
    internal WorkflowChain(ExecutionPipeline p) => _p = p;

    internal static WorkflowChain<T> Seed(WorkflowContext ctx, string title, Func<CancellationToken, ValueTask<T>> fn)
    {
        var p = new ExecutionPipeline(ctx);
        p.Enqueue(StepPhase.Given, StepWord.Primary, title, async (_, ct) => await fn(ct).ConfigureAwait(false));
        return new WorkflowChain<T>(p);
    }

    #region Core step implementations

    private WorkflowChain<TOut> Transform<TOut>(
        StepPhase phase,
        StepWord word,
        string? title,
        Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.Enqueue(phase, word, title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false));
        return new WorkflowChain<TOut>(_p);
    }

    private WorkflowChain<TOut> TransformInherit<TOut>(
        StepWord word,
        string? title,
        Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false),
            word);
        return new WorkflowChain<TOut>(_p);
    }

    private WorkflowChain<T> Effect(
        StepPhase phase,
        StepWord word,
        string? title,
        Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(phase, word, title ?? "",
            async (s, ct) =>
            {
                await effect((T)s!, ct).ConfigureAwait(false);
                return s;
            });
        return this;
    }

    private WorkflowChain<T> EffectInherit(
        StepWord word,
        string? title,
        Func<T, CancellationToken, ValueTask> effect)
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

    private WorkflowChain<T> FinallyEffect(
        string? title,
        Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueFinally(title ?? "Finally", effect);
        return this;
    }

    private ResultChain<TOut> ThenTransform<TOut>(string? title, Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false));
        return new ResultChain<TOut>(_p);
    }

    private ResultChain<T> ThenEffect(string? title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                await effect((T)s!, ct).ConfigureAwait(false);
                return s;
            });
        return new ResultChain<T>(_p);
    }

    private ResultChain<T> ThenPredicate(string? title, Func<T, CancellationToken, ValueTask<bool>> pred)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                AssertUtil.Ensure(await pred((T)s!, ct).ConfigureAwait(false), title ?? "Then");
                return s;
            });
        return new ResultChain<T>(_p);
    }

    private ResultChain<T> ThenPredicateNoValue(string? title, Func<CancellationToken, ValueTask<bool>> pred)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                AssertUtil.Ensure(await pred(ct).ConfigureAwait(false), title ?? "Then");
                return s;
            });
        return new ResultChain<T>(_p);
    }

    #endregion

    #region Delegate normalization - ToCT methods

    // Transform shapes
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, TOut> f)
        => (v, _) => new ValueTask<TOut>(f(v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, Task<TOut>> f)
        => async (v, _) => await f(v).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, ValueTask<TOut>> f)
        => (v, _) => f(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, CancellationToken, Task<TOut>> f)
        => async (v, ct) => await f(v, ct).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, CancellationToken, ValueTask<TOut>> f)
        => f;

    // Effect shapes
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Action<T> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f(v);
            return default;
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Action f)
        => (_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f();
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
    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, Task> f)
        => async (v, ct) => await f(v, ct).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, ValueTask> f)
        => f;

    // Predicate shapes
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred(Func<T, bool> f)
        => (v, _) => new ValueTask<bool>(f(v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred(Func<T, Task<bool>> f)
        => async (v, _) => await f(v).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred(Func<T, ValueTask<bool>> f)
        => (v, _) => f(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<bool>> ToCTPred(Func<bool> f)
        => _ => new ValueTask<bool>(f());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<bool>> ToCTPred(Func<Task<bool>> f)
        => async _ => await f().ConfigureAwait(false);

    #endregion

    #region State-passing ToCT overloads

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, TOut> f)
        => (v, _) => new ValueTask<TOut>(f(v, state));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, Task<TOut>> f)
        => async (v, _) => await f(v, state).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, ValueTask<TOut>> f)
        => (v, _) => f(v, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, CancellationToken, Task<TOut>> f)
        => async (v, ct) => await f(v, state, ct).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, CancellationToken, ValueTask<TOut>> f)
        => (v, ct) => f(v, state, ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Action<T, TState> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f(v, state);
            return default;
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Func<T, TState, Task> f)
        => async (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await f(v, state).ConfigureAwait(false);
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Func<T, TState, ValueTask> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return f(v, state);
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Func<T, TState, CancellationToken, Task> f)
        => async (v, ct) => await f(v, state, ct).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Func<T, TState, CancellationToken, ValueTask> f)
        => (v, ct) => f(v, state, ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred<TState>(
        TState state, Func<T, TState, bool> f)
        => (v, _) => new ValueTask<bool>(f(v, state));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, CancellationToken, ValueTask<bool>> ToCTPred<TState>(
        TState state, Func<T, TState, Task<bool>> f)
        => async (v, _) => await f(v, state).ConfigureAwait(false);

    #endregion

    #region When overloads

    /// <summary>Adds a <c>When</c> transformation step.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, TOut> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(f));

    /// <summary>Adds a <c>When</c> transformation step with async Task.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, Task<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(f));

    /// <summary>Adds a <c>When</c> transformation step with async ValueTask.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, ValueTask<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(f));

    /// <summary>Adds a <c>When</c> transformation step with cancellation token.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, CancellationToken, Task<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(f));

    /// <summary>Adds a <c>When</c> transformation step with cancellation token.</summary>
    public WorkflowChain<TOut> When<TOut>(string title, Func<T, CancellationToken, ValueTask<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, f);

    /// <summary>Adds a <c>When</c> side effect step.</summary>
    public WorkflowChain<T> When(string title, Action<T> effect) =>
        Effect(StepPhase.When, StepWord.Primary, title, ToCT(effect));

    /// <summary>Adds a <c>When</c> side effect step with async Task.</summary>
    public WorkflowChain<T> When(string title, Func<T, Task> effect) =>
        Effect(StepPhase.When, StepWord.Primary, title, ToCT(effect));

    /// <summary>Adds a <c>When</c> side effect step with async ValueTask.</summary>
    public WorkflowChain<T> When(string title, Func<T, ValueTask> effect) =>
        Effect(StepPhase.When, StepWord.Primary, title, ToCT(effect));

    /// <summary>Adds a <c>When</c> transformation with state, avoiding closure allocation.</summary>
    public WorkflowChain<TOut> When<TState, TOut>(string title, TState state, Func<T, TState, TOut> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(state, f));

    /// <summary>Adds a <c>When</c> side effect with state, avoiding closure allocation.</summary>
    public WorkflowChain<T> When<TState>(string title, TState state, Action<T, TState> effect) =>
        Effect(StepPhase.When, StepWord.Primary, title, ToCT(state, effect));

    #endregion

    #region And overloads

    /// <summary>Adds an <c>And</c> transformation step.</summary>
    public WorkflowChain<TOut> And<TOut>(string title, Func<T, TOut> f) =>
        TransformInherit(StepWord.And, title, ToCT(f));

    /// <summary>Adds an <c>And</c> transformation step with async Task.</summary>
    public WorkflowChain<TOut> And<TOut>(string title, Func<T, Task<TOut>> f) =>
        TransformInherit(StepWord.And, title, ToCT(f));

    /// <summary>Adds an <c>And</c> transformation step with async ValueTask.</summary>
    public WorkflowChain<TOut> And<TOut>(string title, Func<T, ValueTask<TOut>> f) =>
        TransformInherit(StepWord.And, title, ToCT(f));

    /// <summary>Adds an <c>And</c> side effect step.</summary>
    public WorkflowChain<T> And(string title, Action<T> effect) =>
        EffectInherit(StepWord.And, title, ToCT(effect));

    /// <summary>Adds an <c>And</c> side effect step with async Task.</summary>
    public WorkflowChain<T> And(string title, Func<T, Task> effect) =>
        EffectInherit(StepWord.And, title, ToCT(effect));

    /// <summary>Adds an <c>And</c> transformation with state.</summary>
    public WorkflowChain<TOut> And<TState, TOut>(string title, TState state, Func<T, TState, TOut> f) =>
        TransformInherit(StepWord.And, title, ToCT(state, f));

    /// <summary>Adds an <c>And</c> side effect with state.</summary>
    public WorkflowChain<T> And<TState>(string title, TState state, Action<T, TState> effect) =>
        EffectInherit(StepWord.And, title, ToCT(state, effect));

    /// <summary>Adds an <c>And</c> transformation without title.</summary>
    public WorkflowChain<TOut> And<TOut>(Func<T, TOut> f) =>
        TransformInherit(StepWord.And, null, ToCT(f));

    /// <summary>Adds an <c>And</c> side effect without title.</summary>
    public WorkflowChain<T> And(Action<T> effect) =>
        EffectInherit(StepWord.And, null, ToCT(effect));

    #endregion

    #region But overloads

    /// <summary>Adds a <c>But</c> transformation step.</summary>
    public WorkflowChain<TOut> But<TOut>(string title, Func<T, TOut> f) =>
        TransformInherit(StepWord.But, title, ToCT(f));

    /// <summary>Adds a <c>But</c> transformation step with async Task.</summary>
    public WorkflowChain<TOut> But<TOut>(string title, Func<T, Task<TOut>> f) =>
        TransformInherit(StepWord.But, title, ToCT(f));

    /// <summary>Adds a <c>But</c> side effect step.</summary>
    public WorkflowChain<T> But(string title, Action<T> effect) =>
        EffectInherit(StepWord.But, title, ToCT(effect));

    /// <summary>Adds a <c>But</c> side effect step with async Task.</summary>
    public WorkflowChain<T> But(string title, Func<T, Task> effect) =>
        EffectInherit(StepWord.But, title, ToCT(effect));

    /// <summary>Adds a <c>But</c> transformation with state.</summary>
    public WorkflowChain<TOut> But<TState, TOut>(string title, TState state, Func<T, TState, TOut> f) =>
        TransformInherit(StepWord.But, title, ToCT(state, f));

    /// <summary>Adds a <c>But</c> side effect with state.</summary>
    public WorkflowChain<T> But<TState>(string title, TState state, Action<T, TState> effect) =>
        EffectInherit(StepWord.But, title, ToCT(state, effect));

    #endregion

    #region Then overloads

    /// <summary>Adds a <c>Then</c> predicate assertion step.</summary>
    public ResultChain<T> Then(string title, Func<T, bool> predicate) =>
        ThenPredicate(title, ToCTPred(predicate));

    /// <summary>Adds a <c>Then</c> predicate assertion step with async Task.</summary>
    public ResultChain<T> Then(string title, Func<T, Task<bool>> predicate) =>
        ThenPredicate(title, ToCTPred(predicate));

    /// <summary>Adds a <c>Then</c> predicate assertion step with async ValueTask.</summary>
    public ResultChain<T> Then(string title, Func<T, ValueTask<bool>> predicate) =>
        ThenPredicate(title, predicate);

    /// <summary>Adds a <c>Then</c> action assertion step.</summary>
    public ResultChain<T> Then(string title, Action<T> assertion) =>
        ThenEffect(title, ToCT(assertion));

    /// <summary>Adds a <c>Then</c> action assertion step with async Task.</summary>
    public ResultChain<T> Then(string title, Func<T, Task> assertion) =>
        ThenEffect(title, ToCT(assertion));

    /// <summary>Adds a <c>Then</c> transformation step.</summary>
    public ResultChain<TOut> Then<TOut>(string title, Func<T, TOut> f) =>
        ThenTransform(title, ToCT(f));

    /// <summary>Adds a <c>Then</c> transformation step with async Task.</summary>
    public ResultChain<TOut> Then<TOut>(string title, Func<T, Task<TOut>> f) =>
        ThenTransform(title, ToCT(f));

    /// <summary>Adds a <c>Then</c> predicate with state.</summary>
    public ResultChain<T> Then<TState>(string title, TState state, Func<T, TState, bool> predicate) =>
        ThenPredicate(title, ToCTPred(state, predicate));

    /// <summary>Adds a <c>Then</c> action with state.</summary>
    public ResultChain<T> Then<TState>(string title, TState state, Action<T, TState> assertion) =>
        ThenEffect(title, ToCT(state, assertion));

    /// <summary>Adds a <c>Then</c> no-input predicate assertion.</summary>
    public ResultChain<T> Then(string title, Func<bool> predicate) =>
        ThenPredicateNoValue(title, ToCTPred(predicate));

    /// <summary>Adds a <c>Then</c> no-input predicate assertion with async Task.</summary>
    public ResultChain<T> Then(string title, Func<Task<bool>> predicate) =>
        ThenPredicateNoValue(title, ToCTPred(predicate));

    #endregion

    #region Finally overloads

    /// <summary>Registers a cleanup handler.</summary>
    public WorkflowChain<T> Finally(string title, Action<T> cleanup) =>
        FinallyEffect(title, ToCT(cleanup));

    /// <summary>Registers a cleanup handler with async Task.</summary>
    public WorkflowChain<T> Finally(string title, Func<T, Task> cleanup) =>
        FinallyEffect(title, ToCT(cleanup));

    /// <summary>Registers a cleanup handler with async ValueTask.</summary>
    public WorkflowChain<T> Finally(string title, Func<T, ValueTask> cleanup) =>
        FinallyEffect(title, ToCT(cleanup));

    /// <summary>Registers a cleanup handler with cancellation token.</summary>
    public WorkflowChain<T> Finally(string title, Func<T, CancellationToken, ValueTask> cleanup) =>
        FinallyEffect(title, cleanup);

    /// <summary>Registers a cleanup handler with state.</summary>
    public WorkflowChain<T> Finally<TState>(string title, TState state, Action<T, TState> cleanup) =>
        FinallyEffect(title, ToCT(state, cleanup));

    /// <summary>Registers a cleanup handler with state using async Task.</summary>
    public WorkflowChain<T> Finally<TState>(string title, TState state, Func<T, TState, Task> cleanup) =>
        FinallyEffect(title, ToCT(state, cleanup));

    #endregion
}

/// <summary>
/// Minimal assertion helper used by workflow step implementations.
/// </summary>
internal static class AssertUtil
{
    /// <summary>
    /// Throws <see cref="WorkflowAssertionException"/> if the condition is false.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Ensure(bool ok, string title)
    {
        if (!ok) throw new WorkflowAssertionException($"Assertion failed: {title}");
    }
}
