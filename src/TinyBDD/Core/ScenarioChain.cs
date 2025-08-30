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
public sealed class ScenarioChain<T>
{
    private readonly Pipeline _p;
    internal ScenarioChain(Pipeline p) => _p = p;

    internal static ScenarioChain<T> Seed(ScenarioContext ctx, string title, Func<CancellationToken, ValueTask<T>> fn)
    {
        var p = new Pipeline(ctx);
        p.Enqueue(StepPhase.Given, StepWord.Primary, title, async (_, ct) => await fn(ct));
        return new ScenarioChain<T>(p);
    }

    // explicit title
    /// <summary>
    /// Adds a <c>When</c> transformation with an explicit title using a synchronous function.
    /// </summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation function from <typeparamref name="T"/> to <typeparamref name="TOut"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(string title, Func<T, TOut> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, (s, _) => VT.From((object?)f((T)s!)));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>
    /// Adds a <c>When</c> side-effect with an explicit title using a <see cref="ValueTask"/>. Keeps the current value type.
    /// </summary>
    /// <typeparam name="TOut">The result of the side-effect action (ignored by the chain).</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect function that receives the carried value and may produce a result.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    /// <remarks>The returned <typeparamref name="TOut"/> from <paramref name="effect"/> is ignored; the chain continues with <typeparamref name="T"/>.</remarks>
    public ScenarioChain<T> When<TOut>(string title, Func<T, ValueTask<TOut>> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, _) => await effect((T)s!));
        return this;
    }

    /// <summary>
    /// Adds a <c>When</c> transformation with an explicit title using an asynchronous function.
    /// </summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Asynchronous transformation that receives the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(string title, Func<T, Task<TOut>> f) // tokenless async
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, _) => await f((T)s!));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>When</c> transformation with a default title using a synchronous function.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation function from <typeparamref name="T"/> to <typeparamref name="TOut"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(Func<T, TOut> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", (s, _) => VT.From((object?)f((T)s!)));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>When</c> transformation with a default title using an asynchronous function.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Asynchronous transformation that receives the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(Func<T, Task<TOut>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, _) => await f((T)s!));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>When</c> transformation with a default title using a <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation that returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(Func<T, ValueTask<TOut>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, _) => await f((T)s!));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>When</c> transformation with a default title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Asynchronous transformation that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(Func<T, CancellationToken, Task<TOut>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) => await f((T)s!, ct));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>When</c> transformation with a default title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) => await f((T)s!, ct));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>When</c> side-effect with an explicit title using a synchronous action. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(string title, Action<T> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, (s, _) =>
        {
            effect((T)s!);
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds a <c>When</c> side-effect with an explicit title using an asynchronous action. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(string title, Func<T, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, _) =>
        {
            await effect((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds a token-aware <c>When</c> transformation with an explicit title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous transformation that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> When<TOut>(string title, Func<T, CancellationToken, Task<TOut>> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => await effect((T)s!, ct));
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>When</c> side-effect with an explicit title. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous side-effect that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(string title, Func<T, CancellationToken, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds a token-aware <c>When</c> side-effect with an explicit title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <typeparam name="TOut">The result of the side-effect action (ignored by the chain).</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When<TOut>(string title, Func<T, CancellationToken, ValueTask<TOut>> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => await effect((T)s!, ct));
        return this;
    }

    /// <summary>Adds a token-aware <c>When</c> side-effect with an explicit title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(string title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>When</c> side-effect with a default title using a synchronous action. Keeps the current value.</summary>
    /// <param name="effect">Action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(Action<T> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", (s, _) =>
        {
            effect((T)s!);
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds a <c>When</c> side-effect with a default title using an asynchronous action. Keeps the current value.</summary>
    /// <param name="effect">Asynchronous action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(Func<T, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, _) =>
        {
            await effect((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>When</c> side-effect with a default title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="effect">Side-effect that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(Func<T, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, _) =>
        {
            await effect((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds a token-aware <c>When</c> side-effect with a default title. Keeps the current value.</summary>
    /// <param name="effect">Asynchronous action that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(Func<T, CancellationToken, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds a token-aware <c>When</c> side-effect with a default title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="effect">Side-effect that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> When(Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> transformation with an explicit title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(string title, Func<T, TOut> f)
    {
        _p.EnqueueInherit(title, (s, _) => VT.From((object?)f((T)s!)), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds an <c>And</c> transformation with an explicit title using an asynchronous function.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Asynchronous transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(string title, Func<T, Task<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, _) => await f((T)s!), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds an <c>And</c> transformation with an explicit title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation that returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(string title, Func<T, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, _) => await f((T)s!), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>And</c> transformation with an explicit title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Asynchronous transformation that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(string title, Func<T, CancellationToken, Task<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>And</c> transformation with an explicit title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(string title, Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>But</c> transformation with an explicit title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(string title, Func<T, TOut> f)
    {
        _p.EnqueueInherit(title, (s, _) => VT.From((object?)f((T)s!)), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>But</c> transformation with an explicit title using an asynchronous function.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Asynchronous transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(string title, Func<T, Task<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, _) => await f((T)s!), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>But</c> transformation with an explicit title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation that returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(string title, Func<T, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, _) => await f((T)s!), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>But</c> transformation with an explicit title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Asynchronous transformation that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(string title, Func<T, CancellationToken, Task<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>But</c> transformation with an explicit title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="title">Display title for this step.</param>
    /// <param name="f">Transformation that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(string title, Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds an <c>And</c> transformation with a default title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(Func<T, TOut> f)
    {
        _p.EnqueueInherit("", (s, _) => VT.From((object?)f((T)s!)), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds an <c>And</c> transformation with a default title using an asynchronous function.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Asynchronous transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(Func<T, Task<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, _) => await f((T)s!), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds an <c>And</c> transformation with a default title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation that returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(Func<T, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, _) => await f((T)s!), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>And</c> transformation with a default title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Asynchronous transformation that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(Func<T, CancellationToken, Task<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>And</c> transformation with a default title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> And<TOut>(Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>But</c> transformation with a default title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(Func<T, TOut> f)
    {
        _p.EnqueueInherit("", (s, _) => VT.From((object?)f((T)s!)), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>But</c> transformation with a default title using an asynchronous function.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Asynchronous transformation applied to the carried value.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(Func<T, Task<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, _) => await f((T)s!), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a <c>But</c> transformation with a default title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation that returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(Func<T, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, _) => await f((T)s!), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>But</c> transformation with a default title.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Asynchronous transformation that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(Func<T, CancellationToken, Task<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds a token-aware <c>But</c> transformation with a default title using <see cref="ValueTask"/>.</summary>
    /// <typeparam name="TOut">The result type of the transformation.</typeparam>
    /// <param name="f">Transformation that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask{TOut}"/>.</param>
    /// <returns>A new <see cref="ScenarioChain{TOut}"/> carrying the transformed value.</returns>
    public ScenarioChain<TOut> But<TOut>(Func<T, CancellationToken, ValueTask<TOut>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<TOut>(_p);
    }

    /// <summary>Adds an <c>And</c> side-effect with an explicit title using a synchronous action. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(string title, Action<T> effect)
    {
        _p.EnqueueInherit(title, (s, _) =>
        {
            effect((T)s!);
            return VT.From(s);
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds an <c>And</c> side-effect with an explicit title using an asynchronous action. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(string title, Func<T, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, _) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds an <c>And</c> side-effect with an explicit title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(string title, Func<T, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, _) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds a token-aware <c>And</c> side-effect with an explicit title. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous action that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(string title, Func<T, CancellationToken, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds a token-aware <c>And</c> side-effect with an explicit title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(string title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds a <c>But</c> side-effect with an explicit title using a synchronous action. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> But(string title, Action<T> effect)
    {
        _p.EnqueueInherit(title, (s, _) =>
        {
            effect((T)s!);
            return VT.From(s);
        }, StepWord.But);
        return this;
    }

    /// <summary>Adds a <c>But</c> side-effect with an explicit title using an asynchronous action. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> But(string title, Func<T, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, _) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.But);
        return this;
    }

    /// <summary>Adds a <c>But</c> side-effect with an explicit title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> But(string title, Func<T, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, _) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.But);
        return this;
    }

    /// <summary>Adds a token-aware <c>But</c> side-effect with an explicit title. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Asynchronous action that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> But(string title, Func<T, CancellationToken, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.But);
        return this;
    }

    /// <summary>Adds a token-aware <c>But</c> side-effect with an explicit title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="title">Display title for this step.</param>
    /// <param name="effect">Side-effect that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> But(string title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.But);
        return this;
    }

    /// <summary>Adds an <c>And</c> side-effect with a default title using a synchronous action. Keeps the current value.</summary>
    /// <param name="effect">Action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(Action<T> effect)
    {
        _p.EnqueueInherit("", (s, _) =>
        {
            effect((T)s!);
            return VT.From(s);
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds an <c>And</c> side-effect with a default title using an asynchronous action. Keeps the current value.</summary>
    /// <param name="effect">Asynchronous action that receives the carried value.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(Func<T, Task> effect)
    {
        _p.EnqueueInherit("", async (s, _) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds an <c>And</c> side-effect with a default title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="effect">Side-effect that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(Func<T, ValueTask> effect)
    {
        _p.EnqueueInherit("", async (s, _) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds a token-aware <c>And</c> side-effect with a default title. Keeps the current value.</summary>
    /// <param name="effect">Asynchronous action that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(Func<T, CancellationToken, Task> effect)
    {
        _p.EnqueueInherit("", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds a token-aware <c>And</c> side-effect with a default title using <see cref="ValueTask"/>. Keeps the current value.</summary>
    /// <param name="effect">Side-effect that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same <see cref="ScenarioChain{T}"/> for further chaining.</returns>
    public ScenarioChain<T> And(Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit("", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    /// <summary>Adds a <c>Then</c> step with an explicit title using a synchronous boolean predicate.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="predicate">Predicate to evaluate; <see langword="false"/> throws <see cref="BddAssertException"/>.</param>
    /// <returns>A <see cref="ThenChain{T}"/> to continue assertions and await execution.</returns>
    /// <exception cref="BddAssertException">Thrown when <paramref name="predicate"/> evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(string title, Func<bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, (s, _) =>
        {
            AssertUtil.Ensure(predicate(), title);
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with an explicit title using a synchronous boolean predicate over the carried value.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="predicate">Predicate evaluated against the carried <typeparamref name="T"/> value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(string title, Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }


    /// <summary>Adds a <c>Then</c> step with an explicit title using an asynchronous assertion.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(string title, Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, _) =>
        {
            await assertion();
            return s;
        });
        return new ThenChain<T>(_p);
    }


    /// <summary>Adds a <c>Then</c> transform with an explicit title that produces a value used only for assertion side-effects.</summary>
    /// <typeparam name="TOut">The result produced by the assertion delegate (ignored by the chain).</typeparam>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="assertion">Asynchronous function that receives the carried value and returns a result.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <remarks>The returned <typeparamref name="TOut"/> is ignored; use this overload when an assertion helper returns a value.</remarks>
    public ThenChain<T> Then<TOut>(string title, Func<T, Task<TOut>> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, _) => await assertion((T)s!));
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with an explicit title using an asynchronous assertion receiving the carried value.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(string title, Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> transform with an explicit title.</summary>
    /// <typeparam name="TOut">The result produced by the assertion delegate (ignored by the chain).</typeparam>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="assertion">Asynchronous function that receives the carried value and a token.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then<TOut>(string title, Func<T, CancellationToken, Task<TOut>> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) => await assertion((T)s!, ct));
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> assertion with an explicit title.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value and a token.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(string title, Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with an explicit title using an asynchronous boolean predicate over the carried value.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(string title, Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, _) =>
        {
            var result = await predicate((T)s!);
            AssertUtil.Ensure(result, title);
            return result;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> step with an explicit title using an asynchronous boolean predicate.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value and token.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(string title, Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> step with an explicit title using a <see cref="ValueTask{Boolean}"/> predicate.</summary>
    /// <param name="title">Display title for the assertion.</param>
    /// <param name="predicate">Predicate evaluated against the carried value and token.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(string title, Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using a synchronous boolean predicate over the carried value.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), "Then");
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using an asynchronous assertion.</summary>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, _) =>
        {
            await assertion();
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using an asynchronous assertion receiving the carried value.</summary>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using a <see cref="ValueTask"/> assertion receiving the carried value.</summary>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/>.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(Func<T, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> step with a default title using an asynchronous assertion.</summary>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> step with a default title using a <see cref="ValueTask"/> assertion.</summary>
    /// <param name="assertion">Assertion that observes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    public ThenChain<T> Then(Func<T, CancellationToken, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using a synchronous boolean predicate without a value.</summary>
    /// <param name="predicate">Predicate to evaluate.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(Func<bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", (s, _) =>
        {
            AssertUtil.Ensure(predicate(), "Then");
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using an asynchronous boolean predicate over the carried value.</summary>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a <c>Then</c> step with a default title using a <see cref="ValueTask{Boolean}"/> predicate over the carried value.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(Func<T, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> step with a default title using an asynchronous boolean predicate over the carried value.</summary>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value and token.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }

    /// <summary>Adds a token-aware <c>Then</c> step with a default title using a <see cref="ValueTask{Boolean}"/> predicate over the carried value.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value and token.</param>
    /// <returns>A <see cref="ThenChain{T}"/> for further chaining.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> Then(Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }
}

