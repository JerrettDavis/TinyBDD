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
        => (v, _) =>
        {
            f(v);
            return default;
        };

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, Task> f)
        => async (v, _) => await f(v).ConfigureAwait(false);

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, ValueTask> f)
        => (v, _) => f(v);

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, Task> f)
        => async (v, ct) => await f(v, ct).ConfigureAwait(false);

    private static Func<T, CancellationToken, ValueTask> ToCT(Func<T, CancellationToken, ValueTask> f)
        => f;

    private static Func<CancellationToken, ValueTask<bool>> ToCT(Func<bool> f)
        => _ => new ValueTask<bool>(f());

    private static Func<CancellationToken, ValueTask<bool>> ToCT(Func<Task<bool>> f)
        => async _ => await f().ConfigureAwait(false);

    #endregion
}