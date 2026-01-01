namespace TinyBDD;

/// <summary>
/// Represents a fluent chain starting at <c>Given</c> and flowing through <c>When</c>,
/// <c>And</c>, <c>But</c>, and into <c>Then</c> where it becomes an awaitable <see cref="ThenChain{T}"/>.
/// </summary>
/// <typeparam name="T">The value type carried by the chain at the current position.</typeparam>
/// <remarks>
/// <para>
/// Steps are recorded and executed only when the terminal <see cref="ThenChain{T}"/> is awaited.
/// Overloads are provided for synchronous and asynchronous transforms and side-effects, with
/// optional titles and cancellation tokens. A "default title" is used when the provided title is empty; format may vary by step kind.
/// </para>
/// <para>
/// User code exceptions thrown inside delegates are captured as step failures and surfaced in
/// <see cref="ScenarioContext.Steps"/> and reporters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "start", () => 10)
///          .When("add 5", (x, _) => Task.FromResult(x + 5))
///          .And(v => v * 2)
///          .But("no-op", _ => Task.CompletedTask)
///          .Then(v => v == 30);
/// </code>
/// </example>
/// <seealso cref="Bdd"/>
/// <seealso cref="Flow"/>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="ThenChain{T}"/>
public sealed partial class ScenarioChain<T>
{
    private readonly Pipeline _p;
    internal ScenarioChain(Pipeline p) => _p = p;

    internal static ScenarioChain<T> Seed(ScenarioContext ctx, string title, Func<CancellationToken, ValueTask<T>> fn)
    {
        var p = new Pipeline(ctx);
        p.Enqueue(StepPhase.Given, StepWord.Primary, title, async (_, ct) => await fn(ct));
        return new ScenarioChain<T>(p);
    }

    #region Core step implementations

    private ScenarioChain<TOut> Transform<TOut>(
        StepPhase phase,
        StepWord word,
        string? title,
        Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.Enqueue(phase, word, title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false));
        return new ScenarioChain<TOut>(_p);
    }

    private ScenarioChain<TOut> TransformInherit<TOut>(
        StepWord word,
        string? title,
        Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false),
            word);
        return new ScenarioChain<TOut>(_p);
    }

    private ScenarioChain<T> Effect(
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

    private ScenarioChain<T> EffectInherit(
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

    private ScenarioChain<T> FinallyEffect(
        string? title,
        Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueFinally(title ?? "Finally", effect);
        return this;
    }

    private ThenChain<TOut> ThenAssert<TOut>(string? title, Func<T, CancellationToken, Task<TOut>> assert)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) => await assert((T)s!, ct).ConfigureAwait(false));
        return new ThenChain<TOut>(_p);
    }

    private ThenChain<T> ThenAssert(string? title, Func<T, CancellationToken, Task> assert)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                await assert((T)s!, ct).ConfigureAwait(false);
                return s;
            });
        return new ThenChain<T>(_p);
    }

    private ThenChain<T> ThenAssert(string? title, Func<T, CancellationToken, ValueTask> assert)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                await assert((T)s!, ct).ConfigureAwait(false);
                return s;
            });
        return new ThenChain<T>(_p);
    }

    private ThenChain<T> ThenPredicate(string? title, Func<T, CancellationToken, ValueTask<bool>> pred)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                AssertUtil.Ensure(await pred((T)s!, ct).ConfigureAwait(false), title ?? "Then");
                return s;
            });
        return new ThenChain<T>(_p);
    }

    private ThenChain<T> ThenPredicateNoValue(string? title, Func<CancellationToken, ValueTask<bool>> pred)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                AssertUtil.Ensure(await pred(ct).ConfigureAwait(false), title ?? "Then");
                return s;
            });
        return new ThenChain<T>(_p);
    }

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, TOut> f)
        => (v, _) => new ValueTask<TOut>(f(v));

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, Task<TOut>> f)
        => async (v, _) => await f(v).ConfigureAwait(false);

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, ValueTask<TOut>> f)
        => (v, _) => f(v);

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, CancellationToken, Task<TOut>> f)
        => async (v, ct) => await f(v, ct).ConfigureAwait(false);

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, CancellationToken, ValueTask<TOut>> f)
        => f;

    private static Func<T, CancellationToken, ValueTask> ToCT(Action<T> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f(v);
            return default;
        };
    
    
    private static Func<T, CancellationToken, ValueTask> ToCT(Action f)
        => (_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f();
            return default;
        };

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, Task> f)
        => async (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await f(v).ConfigureAwait(false);
        };

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, ValueTask> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return f(v);
        };

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, Task> f)
        => async (v, ct) => await f(v, ct).ConfigureAwait(false);

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, ValueTask> f)
        => f;

    private static Func<CancellationToken, ValueTask<bool>> ToCT(Func<bool> f)
        => _ => new ValueTask<bool>(f());

    private static Func<CancellationToken, ValueTask<bool>> ToCT(Func<Task<bool>> f)
        => async _ => await f().ConfigureAwait(false);

    #endregion

    #region State-passing ToCT overloads

    // Transform with state - sync
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, TOut> f)
        => (v, _) => new ValueTask<TOut>(f(v, state));

    // Transform with state - Task
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, Task<TOut>> f)
        => async (v, _) => await f(v, state).ConfigureAwait(false);

    // Transform with state - ValueTask
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, ValueTask<TOut>> f)
        => (v, _) => f(v, state);

    // Transform with state - Token-aware Task
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TState, TOut>(
        TState state, Func<T, TState, CancellationToken, Task<TOut>> f)
        => async (v, ct) => await f(v, state, ct).ConfigureAwait(false);

    // Effect with state - sync
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Action<T, TState> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f(v, state);
            return default;
        };

    // Effect with state - Task
    private static Func<T, CancellationToken, ValueTask> ToCT<TState>(
        TState state, Func<T, TState, Task> f)
        => async (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await f(v, state).ConfigureAwait(false);
        };

    // Predicate with state - sync
    private static Func<T, CancellationToken, ValueTask<bool>> ToCT<TState>(
        TState state, Func<T, TState, bool> f)
        => (v, _) => new ValueTask<bool>(f(v, state));

    // Predicate with state - Task
    private static Func<T, CancellationToken, ValueTask<bool>> ToCT<TState>(
        TState state, Func<T, TState, Task<bool>> f)
        => async (v, _) => await f(v, state).ConfigureAwait(false);

    #endregion

    #region State-passing step methods

    /// <summary>
    /// Adds a <c>When</c> transformation with state, avoiding closure allocation.
    /// </summary>
    /// <typeparam name="TState">The type of state to pass.</typeparam>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="state">State value to pass to the transform function.</param>
    /// <param name="f">Transformation applied to the carried value and state.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TState, TOut>(string title, TState state, Func<T, TState, TOut> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(state, f));

    /// <summary>
    /// Adds a <c>When</c> transformation with state using async Task, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<TOut> When<TState, TOut>(string title, TState state, Func<T, TState, Task<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(state, f));

    /// <summary>
    /// Adds a <c>When</c> transformation with state using async ValueTask, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<TOut> When<TState, TOut>(string title, TState state, Func<T, TState, ValueTask<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(state, f));

    /// <summary>
    /// Adds a <c>When</c> transformation with state using token-aware async Task, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<TOut> When<TState, TOut>(string title, TState state, Func<T, TState, CancellationToken, Task<TOut>> f) =>
        Transform(StepPhase.When, StepWord.Primary, title, ToCT(state, f));

    /// <summary>
    /// Adds a <c>When</c> side effect with state, avoiding closure allocation.
    /// </summary>
    /// <typeparam name="TState">The type of state to pass.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="state">State value to pass to the effect function.</param>
    /// <param name="effect">Side-effect that receives the carried value and state.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When<TState>(string title, TState state, Action<T, TState> effect) =>
        Effect(StepPhase.When, StepWord.Primary, title, ToCT(state, effect));

    /// <summary>
    /// Adds an <c>And</c> transformation with state, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<TOut> And<TState, TOut>(string title, TState state, Func<T, TState, TOut> f) =>
        TransformInherit(StepWord.And, title, ToCT(state, f));

    /// <summary>
    /// Adds an <c>And</c> transformation with state using async Task, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<TOut> And<TState, TOut>(string title, TState state, Func<T, TState, Task<TOut>> f) =>
        TransformInherit(StepWord.And, title, ToCT(state, f));

    /// <summary>
    /// Adds an <c>And</c> side effect with state, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<T> And<TState>(string title, TState state, Action<T, TState> effect) =>
        EffectInherit(StepWord.And, title, ToCT(state, effect));

    /// <summary>
    /// Adds a <c>But</c> transformation with state, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<TOut> But<TState, TOut>(string title, TState state, Func<T, TState, TOut> f) =>
        TransformInherit(StepWord.But, title, ToCT(state, f));

    /// <summary>
    /// Adds a <c>But</c> side effect with state, avoiding closure allocation.
    /// </summary>
    public ScenarioChain<T> But<TState>(string title, TState state, Action<T, TState> effect) =>
        EffectInherit(StepWord.But, title, ToCT(state, effect));

    /// <summary>
    /// Adds a <c>Then</c> predicate with state, avoiding closure allocation.
    /// </summary>
    /// <typeparam name="TState">The type of state to pass.</typeparam>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="state">State value to pass to the predicate function.</param>
    /// <param name="predicate">Predicate evaluated against the carried value and state.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then<TState>(string title, TState state, Func<T, TState, bool> predicate) =>
        ThenPredicate(title, ToCT(state, predicate));

    /// <summary>
    /// Adds a <c>Then</c> predicate with state using async Task, avoiding closure allocation.
    /// </summary>
    public ThenChain<T> Then<TState>(string title, TState state, Func<T, TState, Task<bool>> predicate) =>
        ThenPredicate(title, ToCT(state, predicate));

    /// <summary>
    /// Adds a <c>Then</c> assertion with state, avoiding closure allocation.
    /// </summary>
    public ThenChain<T> Then<TState>(string title, TState state, Action<T, TState> assertion) =>
        ThenAssert(title, ToCT(state, assertion));

    /// <summary>
    /// Registers a cleanup handler with state that executes after all steps complete.
    /// </summary>
    public ScenarioChain<T> Finally<TState>(string title, TState state, Action<T, TState> effect) =>
        FinallyEffect(title, ToCT(state, effect));

    /// <summary>
    /// Registers a cleanup handler with state using async Task.
    /// </summary>
    public ScenarioChain<T> Finally<TState>(string title, TState state, Func<T, TState, Task> effect) =>
        FinallyEffect(title, ToCT(state, effect));

    #endregion
}