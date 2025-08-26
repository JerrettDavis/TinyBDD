namespace TinyBDD;

/// <summary>
/// Ambient, context-free entry points for building BDD chains when <see cref="Ambient.Current"/> is set.
/// </summary>
/// <remarks>
/// <para>
/// Use this API when you prefer not to pass <see cref="ScenarioContext"/> through your methods. Set
/// <see cref="Ambient.Current"/> at the start of a test (TinyBDD test base classes do this for you).
/// </para>
/// <example>
/// <code>
/// var prev = Ambient.Current.Value; Ambient.Current.Value = Bdd.CreateContext(this);
/// try
/// {
///     await Flow.Given(() => 1)
///         .When("double", x => x * 2)
///         .Then("== 2", v => v == 2);
/// }
/// finally { Ambient.Current.Value = prev; }
/// </code>
/// </example>
/// </remarks>
public static class Flow
{
    // Ambient start: use Ambient.Current or throw if missing
    /// <summary>Starts a <c>Given</c> using a title and synchronous setup, reading context from <see cref="Ambient.Current"/>.</summary>
    public static GivenBuilder<T> Given<T>(string title, Func<T> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a <c>Given</c> using a title and async setup, reading context from <see cref="Ambient.Current"/>.</summary>
    public static GivenBuilder<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), title, setup);

    /// <summary>Starts a <c>Given</c> with a default title using synchronous setup, reading context from <see cref="Ambient.Current"/>.</summary>
    public static GivenBuilder<T> Given<T>(Func<T> setup)
        => Bdd.Given(Require(), setup);

    /// <summary>Starts a <c>Given</c> with a default title using async setup, reading context from <see cref="Ambient.Current"/>.</summary>
    public static GivenBuilder<T> Given<T>(Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), setup);

    /// <summary>
    /// Provides an explicit context-bound entry point when you want to keep a method style but avoid ambient state.
    /// </summary>
    public static FromContext From(ScenarioContext ctx) => new(ctx);

    private static ScenarioContext Require()
        => Ambient.Current.Value ?? throw new InvalidOperationException(
            "TinyBDD ambient ScenarioContext not set. Inherit from TinyBdd*Base or set Ambient.Current manually.");
}

/// <summary>
/// A small façade for <see cref="Bdd"/> that pins a specific <see cref="ScenarioContext"/>.
/// </summary>
public readonly struct FromContext(ScenarioContext ctx)
{
    /// <summary>Starts a <c>Given</c> using a title and synchronous setup.</summary>
    public GivenBuilder<T> Given<T>(string title, Func<T> setup) => Bdd.Given(ctx, title, setup);
    /// <summary>Starts a <c>Given</c> using a title and async setup.</summary>
    public GivenBuilder<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, title, setup);
    /// <summary>Starts a <c>Given</c> with a default title using synchronous setup.</summary>
    public GivenBuilder<T> Given<T>(Func<T> setup) => Bdd.Given(ctx, setup);
    /// <summary>Starts a <c>Given</c> with a default title using async setup.</summary>
    public GivenBuilder<T> Given<T>(Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, setup);
}
