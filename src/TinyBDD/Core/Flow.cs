namespace TinyBDD;

/// <summary>
/// Ambient, context-free entry points for building BDD chains when <see cref="Ambient.Current"/> is set.
/// </summary>
/// <remarks>
/// <para>
/// Use this API when you prefer not to pass <see cref="ScenarioContext"/> through your methods. Set
/// <see cref="Ambient.Current"/> at the start of a test (TinyBDD test base classes do this for you).
/// </para>
/// <para>
/// All overloads delegate to <see cref="Bdd"/> equivalents using the current ambient context. If the
/// ambient context is not set, an <see cref="InvalidOperationException"/> is thrown.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var prev = Ambient.Current.Value; Ambient.Current.Value = Bdd.CreateContext(this);
/// try
/// {
///     await Flow.Given(() => 1)
///               .When("double", x => x * 2)
///               .Then("== 2", v => v == 2);
/// }
/// finally { Ambient.Current.Value = prev; }
/// </code>
/// </example>
/// <seealso cref="Ambient"/>
/// <seealso cref="Bdd"/>
/// <seealso cref="ScenarioContext"/>
public static class Flow
{
    /// <summary>Starts a <c>Given</c> step with an explicit title and synchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(string title, Func<T> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(string title, Func<ValueTask<T>> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(string title, Func<CancellationToken, ValueTask<T>> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(string title, Func<Task<T>> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a <c>Given</c> step with a default title and synchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(Func<T> setup)
        => Bdd.Given(Require(), setup);


    /// <summary>Starts a <c>Given</c> step with a default title and <see cref="ValueTask"/> setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">ValueTask-producing factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(Func<ValueTask<T>> setup)
        => Bdd.Given(Require(), setup);

    /// <summary>Starts a <c>Given</c> step with a default title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(Func<Task<T>> setup)
        => Bdd.Given(Require(), setup);

    /// <summary>Starts a token-aware <c>Given</c> step with a default title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), setup);
    
    /// <summary>Starts a token-aware <c>Given</c> step with a default title and <see cref="ValueTask"/> setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(Func<CancellationToken, ValueTask<T>> setup)
        => Bdd.Given(Require(), setup);
    
    /// <summary>Creates a helper for starting chains from an explicit context.</summary>
    /// <param name="ctx">The scenario context to use for subsequent calls.</param>
    /// <returns>A <see cref="FromContext"/> helper bound to <paramref name="ctx"/>.</returns>
    public static FromContext From(ScenarioContext ctx) => new(ctx);

    private static ScenarioContext Require()
        => Ambient.Current.Value ?? throw new InvalidOperationException(
            "TinyBDD ambient ScenarioContext not set. Inherit from TinyBdd*Base or set Ambient.Current manually.");
}

/// <summary>
/// Helper that allows starting <c>Given</c> chains against an explicit <see cref="ScenarioContext"/>,
/// instead of relying on <see cref="Ambient.Current"/>.
/// </summary>
/// <param name="ctx">The scenario context to use for subsequent calls.</param>
public readonly struct FromContext(ScenarioContext ctx)
{
    /// <summary>Starts a <c>Given</c> step with an explicit title and synchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public ScenarioChain<T> Given<T>(string title, Func<T> setup) => Bdd.Given(ctx, title, setup);

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, title, setup);

    /// <summary>Starts a <c>Given</c> step with a default title and synchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public ScenarioChain<T> Given<T>(Func<T> setup) => Bdd.Given(ctx, setup);

    /// <summary>Starts a token-aware <c>Given</c> step with a default title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, setup);
}