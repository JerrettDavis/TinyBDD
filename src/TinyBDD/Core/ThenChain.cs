using System.Runtime.CompilerServices;

namespace TinyBDD;

/// <summary>
/// Represents a chain of <c>Then</c> assertions that can be further extended with
/// <c>And</c>/<c>But</c> and awaited to execute the recorded scenario.
/// </summary>
/// <typeparam name="T">The value type produced by the preceding step and carried through assertions.</typeparam>
/// <remarks>
/// <para>
/// TinyBDD defers execution: steps are queued until you await the chain. Calling any of the
/// <c>And</c>/<c>But</c> methods only records additional steps; the actual execution happens when the chain is awaited.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "start", () => 2)
///           .When("double", x => x * 2)
///           .Then(">= 4", v => v >= 4)  // returns ThenChain&lt;int&gt;
///           .And("!= 5", v => v != 5);  // add another assertion, then await
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="ScenarioChain{T}"/>
public readonly partial struct ThenChain<T>
{
    private readonly Pipeline _p;
    internal ThenChain(Pipeline p) => _p = p;

    /// <summary>
    /// Gets an awaiter that executes the queued scenario steps when awaited.
    /// </summary>
    /// <returns>The awaiter for the underlying asynchronous execution.</returns>
    public ValueTaskAwaiter GetAwaiter() => _p.RunAsync(CancellationToken.None).GetAwaiter();

    /// <summary>Completes the chain and asserts that all previous steps passed.</summary>
    /// <remarks>
    /// This method is functionally equivalent to calling the extension method <see cref="ScenarioContextAsserts.AssertPassed(ScenarioContext)"/>
    /// on the underlying <see cref="ScenarioContext"/>. It is provided for convenience to
    /// allow fluent chaining at the end of a scenario.
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when at least one step failed.</exception>
    public async Task AssertPassed(CancellationToken cancellationToken = default)
    {
        await _p.RunAsync(cancellationToken).ConfigureAwait(false);
        _p.Context.AssertPassed();
    }

    /// <summary>Completes the chain and asserts that any previous steps failed.</summary>
    /// <remarks>
    /// This method is functionally equivalent to calling the extension method <see cref="ScenarioContextAsserts.AssertFailed(ScenarioContext)"/>
    /// on the underlying <see cref="ScenarioContext"/>. It is provided for convenience to
    /// allow fluent chaining at the end of a scenario.
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when no step failed.</exception>   
    public async Task AssertFailed(CancellationToken cancellationToken = default)
    {
        await _p.RunAsync(cancellationToken).ConfigureAwait(false);
        _p.Context.AssertFailed();
    }

    private ScenarioChain<TOut> WhenTransform<TOut>(
        string? title,
        Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.Enqueue(
            StepPhase.When,
            StepWord.Primary,
            title ?? "",
            async (s, ct) => await f((T)s!, ct).ConfigureAwait(false));
        return new ScenarioChain<TOut>(_p);
    }

    private ScenarioChain<T> WhenEffect(
        string? title,
        Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title ?? "",
            async (s, ct) =>
            {
                await effect((T)s!, ct).ConfigureAwait(false);
                return s;
            });
        return new ScenarioChain<T>(_p);
    }

    // ---------- Public When(...) overloads (explicit title) ----------
    public ScenarioChain<TOut> When<TOut>(string title, Func<T, TOut> f) =>
        WhenTransform(title, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(string title, Func<T, Task<TOut>> f) =>
        WhenTransform(title, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(string title, Func<T, ValueTask<TOut>> f) =>
        WhenTransform(title, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(string title, Func<T, CancellationToken, Task<TOut>> f) =>
        WhenTransform(title, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(string title, Func<T, CancellationToken, ValueTask<TOut>> f) =>
        WhenTransform(title, f);

    public ScenarioChain<T> When(string title, Action<T> effect) =>
        WhenEffect(title, ToCT(effect));

    public ScenarioChain<T> When(string title, Func<T, Task> effect) =>
        WhenEffect(title, ToCT(effect));

    public ScenarioChain<T> When(string title, Func<T, ValueTask> effect) =>
        WhenEffect(title, ToCT(effect));

    public ScenarioChain<T> When(string title, Func<T, CancellationToken, Task> effect) =>
        WhenEffect(title, ToCT(effect));

    public ScenarioChain<T> When(string title, Func<T, CancellationToken, ValueTask> effect) =>
        WhenEffect(title, effect);

    // ---------- Public When(...) overloads (default/auto title) ----------
    public ScenarioChain<TOut> When<TOut>(Func<T, TOut> f) =>
        WhenTransform(string.Empty, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(Func<T, Task<TOut>> f) =>
        WhenTransform(string.Empty, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(Func<T, ValueTask<TOut>> f) =>
        WhenTransform(string.Empty, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(Func<T, CancellationToken, Task<TOut>> f) =>
        WhenTransform(string.Empty, ToCT(f));

    public ScenarioChain<TOut> When<TOut>(Func<T, CancellationToken, ValueTask<TOut>> f) =>
        WhenTransform(string.Empty, f);

    public ScenarioChain<T> When(Action<T> effect) =>
        WhenEffect(string.Empty, ToCT(effect));

    public ScenarioChain<T> When(Func<T, Task> effect) =>
        WhenEffect(string.Empty, ToCT(effect));

    public ScenarioChain<T> When(Func<T, ValueTask> effect) =>
        WhenEffect(string.Empty, ToCT(effect));

    public ScenarioChain<T> When(Func<T, CancellationToken, Task> effect) =>
        WhenEffect(string.Empty, ToCT(effect));

    public ScenarioChain<T> When(Func<T, CancellationToken, ValueTask> effect) =>
        WhenEffect(string.Empty, effect);

    // ---------- Adapters for transforms/effects (same shapes as ScenarioChain) ----------
    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, TOut> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return new ValueTask<TOut>(f(v));
        };

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, Task<TOut>> f)
        => async (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return await f(v).ConfigureAwait(false);
        };

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, ValueTask<TOut>> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return f(v);
        };

    private static Func<T, CancellationToken, ValueTask<TOut>> ToCT<TOut>(Func<T, CancellationToken, Task<TOut>> f)
        => async (v, ct) => await f(v, ct).ConfigureAwait(false);

    private static Func<T, CancellationToken, ValueTask> ToCT(Action<T> f)
        => (v, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            f(v);
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

    private ThenChain<T> FinallyEffect(
        string? title,
        Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueFinally(title ?? "Finally", effect);
        return this;
    }

    #region Core step implementations

    private ThenChain<T> Add(
        StepWord word,
        string? title,
        Func<object?, CancellationToken, ValueTask<object?>> body)
    {
        _p.Enqueue(StepPhase.Then, word, title ?? "", body);
        return this;
    }

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Action<T> assertion)
        => (s, _) =>
        {
            assertion((T)s!);
            return new ValueTask<object?>(s);
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<Task> assertion)
        => async (s, _) =>
        {
            await assertion().ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, Task> assertion)
        => async (s, _) =>
        {
            await assertion((T)s!).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, CancellationToken, Task> assertion)
        => async (s, ct) =>
        {
            await assertion((T)s!, ct).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, ValueTask> assertion)
        => async (s, _) =>
        {
            await assertion((T)s!).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, CancellationToken, ValueTask> assertion)
        => async (s, ct) =>
        {
            await assertion((T)s!, ct).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, bool> predicate)
        => (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return new ValueTask<object?>(s);
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, Task<bool>> predicate)
        => async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!).ConfigureAwait(false), title);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, CancellationToken, Task<bool>> predicate)
        => async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct).ConfigureAwait(false), title);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, ValueTask<bool>> predicate)
        => async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!).ConfigureAwait(false), title);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, CancellationToken, ValueTask<bool>> predicate)
        => async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct).ConfigureAwait(false), title);
            return s;
        };

    #endregion
}