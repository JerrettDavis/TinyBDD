using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TinyBDD;

/// <summary>
/// Entry points for creating a <see cref="ScenarioContext"/> and starting fluent Given/When/Then chains.
/// </summary>
/// <remarks>
/// <para>
/// Use this static API when you want to work with an explicit <see cref="ScenarioContext"/>.
/// Call <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/> to construct a context from
/// attributes on your test class and method (see <see cref="FeatureAttribute"/>,
/// <see cref="ScenarioAttribute"/>, and <see cref="TagAttribute"/>). Then begin your scenario
/// with one of the <c>Given</c> overloads.
/// </para>
/// <para>
/// If you prefer not to pass a context around, use the ambient <see cref="Flow"/> API by
/// setting <see cref="Ambient.Current"/> at the start of a test (TinyBDD base classes do this for you).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
///          .When("sum", arr => arr.Sum())
///          .Then("> 0", total => total > 0);
/// ctx.AssertPassed();
/// </code>
/// </example>
/// <seealso cref="Flow"/>
/// <seealso cref="Ambient"/>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="FeatureAttribute"/>
/// <seealso cref="ScenarioAttribute"/>
/// <seealso cref="TagAttribute"/>
public static class Bdd
{
    private static readonly ScenarioOptions DefaultOptions = new();
    private static ITestMethodResolver? _resolver;

    /// <summary>
    /// Registers an <see cref="ITestMethodResolver"/> used to discover the current test method
    /// when creating a <see cref="ScenarioContext"/>.
    /// </summary>
    public static void Register(ITestMethodResolver resolver) => _resolver = resolver;

    /// <summary>
    /// Creates a <see cref="ScenarioContext"/> by inspecting <paramref name="featureSource"/> and the current test method
    /// for <see cref="FeatureAttribute"/>, <see cref="ScenarioAttribute"/>, and <see cref="TagAttribute"/>.
    /// </summary>
    /// <param name="featureSource">Any object from the test class; only its type is used to read attributes.</param>
    /// <param name="scenarioName">Optional scenario name override; defaults to the method name or <see cref="ScenarioAttribute.Name"/>.</param>
    /// <param name="traits">Optional trait bridge for propagating tags to the host framework.</param>
    /// <param name="options">Optional <see cref="ScenarioOptions"/> that customizes scenario behavior.</param>
    /// <returns>A new <see cref="ScenarioContext"/> populated with feature/scenario metadata and tags.</returns>
    public static ScenarioContext CreateContext(
        object featureSource,
        string? scenarioName = null,
        ITraitBridge? traits = null,
        ScenarioOptions? options = null)
    {
        options ??= DefaultOptions with { };

        var featureType = featureSource.GetType();
        var featureAttr = featureType.GetCustomAttribute<FeatureAttribute>();
        var featureName = featureAttr?.Name ?? featureType.Name;
        var featureDesc = featureAttr?.Description;

        var method = FindCurrentTestMethod();
        var scenarioAttr = method?.GetCustomAttribute<ScenarioAttribute>();

        return ScenarioContextBuilder.Create(
                featureName,
                featureDesc,
                ResolveScenarioName(scenarioName, scenarioAttr, method),
                traits ?? new NullTraitBridge(),
                options)
            .WithFeature(featureType)
            .WithMethod(method)
            .WithScenario(scenarioAttr)
            .Build();
    }

    private static string ResolveScenarioName(
        string? scenarioName,
        ScenarioAttribute? scenarioAttr,
        MethodInfo? method) =>
        !string.IsNullOrWhiteSpace(scenarioName) ? scenarioName :
        !string.IsNullOrWhiteSpace(scenarioAttr?.Name) ? scenarioAttr.Name! :
        method?.Name ?? "Scenario";

    private static MethodInfo? FindCurrentTestMethod() =>
        _resolver?.GetCurrentTestMethod() ?? FindByStackTrace();

    [RequiresUnreferencedCode("Uses reflection/stack walking to find the current test method.")]
    private static MethodInfo? FindByStackTrace()
    {
        var frames = new StackTrace().GetFrames();

        foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
            if (mi.GetCustomAttribute<ScenarioAttribute>() is not null)
                return mi;

        return frames
            .Select(f => f.GetMethod())
            .OfType<MethodInfo>()
            .FirstOrDefault(HasAnyTestAttribute);
    }

    private static readonly HashSet<string> TestAttributeNames = new(StringComparer.Ordinal)
    {
        "Xunit.FactAttribute",
        "Xunit.TheoryAttribute",
        "NUnit.Framework.TestAttribute",
        "NUnit.Framework.TestCaseAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute"
    };

    private static bool HasAnyTestAttribute(MethodInfo m) =>
        m.GetCustomAttributes(inherit: true)
            .Any(a => TestAttributeNames.Contains(a.GetType().FullName ?? ""));

    // Helpers: normalize a variety of factory shapes into Func<CancellationToken, ValueTask<T>>
    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Action setup, T seed)
        => _ =>
        {
            setup();
            return new ValueTask<T>(seed);
        };

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<TIn, TOut> setup,
        TIn seed)
        => _ => new ValueTask<TOut>(setup(seed));

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<Task<TIn>, TOut> setup,
        Task<TIn> seed)
        => _ => new ValueTask<TOut>(setup(seed));

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<Task<TIn>, CancellationToken, TOut> setup,
        Task<TIn> seed)
        => ct => new ValueTask<TOut>(setup(seed, ct));

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<Task<T>, CancellationToken, ValueTask> setup,
        Task<T> seed)
        => async ct =>
        {
            await setup(seed, ct);
            return await seed;
        };


    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<Task<T>, CancellationToken, Task> setup,
        Task<T> seed)
        => async ct =>
        {
            await setup(seed, ct);
            return await seed;
        };

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<TIn, CancellationToken, Task<TOut>> setup,
        TIn seed)
        => ct => new ValueTask<TOut>(setup(seed, ct));

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<Task<TIn>, CancellationToken, Task<TOut>> setup,
        Task<TIn> seed)
        => ct => new ValueTask<TOut>(setup(seed, ct));

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<Task<TIn>, CancellationToken, ValueTask<TOut>> setup,
        Task<TIn> seed)
        => ct => setup(seed, ct);

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<ValueTask<TIn>, TOut> setup,
        ValueTask<TIn> seed)
        => _ => new ValueTask<TOut>(setup(seed));

    private static Func<CancellationToken, ValueTask<TOut>> Wrap<TIn, TOut>(
        Func<ValueTask<TIn>, CancellationToken, TOut> setup,
        ValueTask<TIn> seed)
        => ct => new ValueTask<TOut>(setup(seed, ct));

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<T, CancellationToken, ValueTask<T>> setup,
        T seed)
        => ct => setup(seed, ct);

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<T, CancellationToken, ValueTask> setup,
        T seed)
        => ct =>
        {
            setup(seed, ct);
            return new ValueTask<T>(seed);
        };

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<CancellationToken, ValueTask> setup,
        T seed)
        => ct =>
        {
            setup(ct);
            return new ValueTask<T>(seed);
        };

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<ValueTask<T>, ValueTask> setup,
        ValueTask<T> seed)
        => _ =>
        {
            setup(seed);
            return seed;
        };

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(
        Func<CancellationToken, Task> setup,
        T seed)
        => ct =>
        {
            setup(ct);
            return new ValueTask<T>(seed);
        };

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<T> f)
        => _ => new ValueTask<T>(f());

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<Task<T>> f)
        => _ => new ValueTask<T>(f());

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<ValueTask<T>> f)
        => _ => f();

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<CancellationToken, Task<T>> f)
        => ct => new ValueTask<T>(f(ct));

    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<CancellationToken, ValueTask<T>> f)
        => f;

    private static ScenarioChain<T> Seed<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, ValueTask<T>> setup) =>
        ScenarioChain<T>.Seed(ctx, title, setup);

    private static string AutoTitle<T>() => $"Given {typeof(T).Name}";

    // --- Given overloads (explicit-title) ---
    /// <summary>Starts a <c>Given</c> step with an explicit title and synchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<T> setup) =>
        Seed(ctx, title, Wrap(setup));

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<Task<T>> setup) =>
        Seed(ctx, title, Wrap(setup));

    /// <summary>Starts a <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<ValueTask<T>> setup) =>
        Seed(ctx, title, Wrap(setup));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, Task<T>> setup) =>
        Seed(ctx, title, Wrap(setup));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, ValueTask<T>> setup) =>
        Seed(ctx, title, Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with an explicit title and a synchronous action
    /// that performs setup and yields the provided seed value.
    /// </summary>
    /// <typeparam name="T">The type produced by the seed value.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous action that performs setup side-effects.</param>
    /// <param name="seed">The value to seed into the scenario chain after performing <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Action setup,
        T seed) =>
        Seed(ctx, title, Wrap(setup, seed));


    /// <summary>Starts a <c>Given</c> step with an explicit title and synchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<TIn, TOut> setup,
        TIn seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<Task<TIn>, TOut> setup,
        Task<TIn> seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<ValueTask<TIn>, TOut> setup,
        ValueTask<TIn> seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<Task<TIn>, CancellationToken, TOut> setup,
        Task<TIn> seed) =>
        Seed(ctx, title, Wrap(setup, seed));


    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<Task<T>, CancellationToken, ValueTask> setup,
        Task<T> seed) =>
        Seed(ctx, title, Wrap(setup, seed));


    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<Task<T>, CancellationToken, Task> setup,
        Task<T> seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<Task<TIn>, CancellationToken, Task<TOut>> setup,
        Task<TIn> seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<Task<TIn>, CancellationToken, ValueTask<TOut>> setup,
        Task<TIn> seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<TIn, CancellationToken, Task<TOut>> setup,
        TIn seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type of the seed value</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, Task> setup,
        T seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam> 
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>1
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<ValueTask<TIn>, CancellationToken, TOut> setup,
        ValueTask<TIn> seed) =>
        Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam> 
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>1
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<ValueTask<TIn>, CancellationToken, ValueTask<TOut>> setup,
        ValueTask<TIn> seed) =>
        Seed(ctx, title, ct => setup(seed, ct));

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        string title,
        Func<TIn, CancellationToken, ValueTask<TOut>> setup,
        TIn seed) =>
        Seed(ctx, title, ct => setup(seed, ct));

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type of the seed value</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<T, CancellationToken, ValueTask> setup,
        T seed)
        => Seed(ctx, title, Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type of the seed value</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, ValueTask> setup,
        T seed)
        => Seed(ctx, title, Wrap(setup, seed));

    // --- Given overloads (auto-title) ---

    /// <summary>
    /// Starts a <c>Given</c> step with an automatically generated title and a synchronous factory.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<T> setup) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with an automatically generated title and a <see cref="ValueTask"/>-producing factory.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<ValueTask<T>> setup) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with an automatically generated title and an asynchronous Task-producing factory.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous Task-producing factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<Task<T>> setup) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup));

    /// <summary>
    /// Starts a token-aware <c>Given</c> step with an automatically generated title and asynchronous setup that observes a <see cref="CancellationToken"/>.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/> and returns the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<CancellationToken, Task<T>> setup) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup));

    /// <summary>
    /// Starts a token-aware <c>Given</c> step with an automatically generated title and a <see cref="ValueTask"/>-producing factory that observes a <see cref="CancellationToken"/>.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory that accepts a <see cref="CancellationToken"/> and returns the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<CancellationToken, ValueTask<T>> setup) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup));


    /// <summary>
    /// Starts a <c>Given</c> step with an automatically generated title and a synchronous action
    /// that performs setup and yields the provided seed value.
    /// </summary>
    /// <typeparam name="T">The type produced by the seed value.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Synchronous action that performs setup side-effects.</param>
    /// <param name="seed">The value to seed into the scenario chain after performing <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Action setup,
        T seed) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));


    /// <summary>Starts a <c>Given</c> step with an automatically generated title and synchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<TIn, TOut> setup,
        TIn seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an automatically generated title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<Task<TIn>, TOut> setup,
        Task<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an automatically generated title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<ValueTask<TIn>, TOut> setup,
        ValueTask<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an automatically generated title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<Task<TIn>, CancellationToken, TOut> setup,
        Task<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an automatically generated title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<Task<TIn>, CancellationToken, Task<TOut>> setup,
        Task<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an automatically generated title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam> 
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>1
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<ValueTask<TIn>, CancellationToken, TOut> setup,
        ValueTask<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an automatically generated title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam> 
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>1
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<ValueTask<TIn>, CancellationToken, ValueTask<TOut>> setup,
        ValueTask<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), ct => setup(seed, ct));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<Task<T>, CancellationToken, ValueTask> setup,
        Task<T> seed) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<Task<T>, CancellationToken, Task> setup,
        Task<T> seed) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an auto-generated title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<Task<TIn>, CancellationToken, ValueTask<TOut>> setup,
        Task<TIn> seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an auto-generated title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="TIn">The type of the seed value.</typeparam>
    /// <typeparam name="TOut">The type produced by the setup function.</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<TOut> Given<TIn, TOut>(
        ScenarioContext ctx,
        Func<TIn, CancellationToken, Task<TOut>> setup,
        TIn seed) =>
        Seed(ctx, AutoTitle<TOut>(), Wrap(setup, seed));

    /// <summary>Starts a token-aware <c>Given</c> step with an auto-generated title and <see cref="ValueTask"/> setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type of the seed value</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">ValueTask-producing factory that observes a <see cref="CancellationToken"/>.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<CancellationToken, Task> setup,
        T seed) =>
        Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an auto-generated title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type of the seed value</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<T, CancellationToken, ValueTask> setup,
        T seed)
        => Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));


    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<T, CancellationToken, ValueTask<T>> setup,
        T seed)
        => Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));

    /// <summary>Starts a <c>Given</c> step with an auto-generated title and asynchronous setup that accepts a seed value.</summary>
    /// <typeparam name="T">The type of the seed value</typeparam>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>.</param>
    /// <param name="setup">Asynchronous factory for the initial value.</param>
    /// <param name="seed">The seed value to pass to <paramref name="setup"/>.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> that can be continued with <c>When</c>/<c>Then</c>.</returns>
    public static ScenarioChain<T> Given<T>(
        ScenarioContext ctx,
        Func<CancellationToken, ValueTask> setup,
        T seed)
        => Seed(ctx, AutoTitle<T>(), Wrap(setup, seed));

    // --- ScenarioContextBuilder ---
    /// <summary>
    /// A fluent builder for constructing a <see cref="ScenarioContext"/> with feature, method,
    /// and scenario metadata and tags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This internal class is used by <see cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>
    /// to populate a <see cref="ScenarioContext"/> with information discovered from attributes
    /// on the test class (<see cref="FeatureAttribute"/>, <see cref="TagAttribute"/>), the test
    /// method (<see cref="ScenarioAttribute"/>, <see cref="TagAttribute"/>), and from
    /// <see cref="ScenarioOptions"/> provided by the caller.
    /// </para>
    /// <para>
    /// The builder pattern allows chaining multiple enrichment steps before returning the
    /// fully populated context via <see cref="Build"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var ctx = ScenarioContextBuilder.Create("Math", "Basic math operations", "Adds numbers",
    ///     new NullTraitBridge(), new ScenarioOptions())
    ///     .WithFeature(typeof(MyTests))
    ///     .WithMethod(typeof(MyTests).GetMethod(nameof(MyTests.MyTestMethod)))
    ///     .WithScenario(new ScenarioAttribute("Adds numbers", Tags = new[] { "math", "smoke" }))
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="ScenarioContext"/>
    /// <seealso cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>
    /// <seealso cref="FeatureAttribute"/>
    /// <seealso cref="ScenarioAttribute"/>
    /// <seealso cref="TagAttribute"/>
    internal sealed class ScenarioContextBuilder
    {
        private readonly ScenarioContext _ctx;

        /// <summary>
        /// Initializes a new builder instance with the specified feature, scenario name,
        /// trait bridge, and options.
        /// </summary>
        /// <param name="featureName">The logical name of the feature under test.</param>
        /// <param name="featureDescription">Optional human-readable feature description.</param>
        /// <param name="scenarioName">The name of the scenario, typically the test method name or <see cref="ScenarioAttribute.Name"/>.</param>
        /// <param name="traitBridge">The trait bridge used to register tags/categories with the underlying test framework.</param>
        /// <param name="options">Scenario-level options controlling behavior such as <c>ContinueOnError</c>.</param>
        private ScenarioContextBuilder(
            string featureName,
            string? featureDescription,
            string scenarioName,
            ITraitBridge traitBridge,
            ScenarioOptions options)
            => _ctx = new ScenarioContext(
                featureName,
                featureDescription,
                scenarioName,
                traitBridge,
                options);

        /// <summary>
        /// Creates a new <see cref="ScenarioContextBuilder"/> pre-initialized with basic feature,
        /// scenario, trait bridge, and option metadata.
        /// </summary>
        /// <param name="featureName">The logical name of the feature under test.</param>
        /// <param name="featureDescription">Optional human-readable feature description.</param>
        /// <param name="scenarioName">The name of the scenario.</param>
        /// <param name="traitBridge">The trait bridge for propagating tags.</param>
        /// <param name="options">Scenario-level options.</param>
        /// <returns>A new <see cref="ScenarioContextBuilder"/> that can be further enriched with feature/method/scenario tags.</returns>
        public static ScenarioContextBuilder Create(
            string featureName,
            string? featureDescription,
            string scenarioName,
            ITraitBridge traitBridge,
            ScenarioOptions options)
            => new(featureName, featureDescription, scenarioName, traitBridge, options);

        /// <summary>
        /// Adds any <see cref="TagAttribute"/> values declared on the specified feature type.
        /// </summary>
        /// <param name="feature">
        /// The <see cref="System.Type"/> representing the test class (feature) under test,
        /// or <see langword="null"/> to skip feature-level tags.
        /// </param>
        /// <returns>The same <see cref="ScenarioContextBuilder"/> for fluent chaining.</returns>
        /// <remarks>
        /// This method uses <see cref="System.Reflection.CustomAttributeExtensions.GetCustomAttributes(System.Reflection.MemberInfo,System.Boolean)"/> to retrieve all
        /// <see cref="TagAttribute"/> instances and adds their names to the context.
        /// </remarks>
        public ScenarioContextBuilder WithFeature(Type? feature)
        {
            var featureTags = feature?
                .GetCustomAttributes<TagAttribute>(inherit: true)
                .Select(t => t.Name) ?? [];

            _ctx.AddTags(featureTags);
            return this;
        }

        /// <summary>
        /// Adds any <see cref="TagAttribute"/> values declared on the specified test method.
        /// </summary>
        /// <param name="method">
        /// The <see cref="MethodInfo"/> representing the test method, or <see langword="null"/> to skip method-level tags.
        /// </param>
        /// <returns>The same <see cref="ScenarioContextBuilder"/> for fluent chaining.</returns>
        /// <remarks>
        /// This method uses <see cref="System.Reflection.CustomAttributeExtensions.GetCustomAttributes(System.Reflection.MemberInfo,System.Boolean)"/> to retrieve
        /// all <see cref="TagAttribute"/> instances from the method and adds their names to the context.
        /// </remarks>
        public ScenarioContextBuilder WithMethod(MethodInfo? method)
        {
            var methodTags = method?
                .GetCustomAttributes<TagAttribute>(inherit: true)
                .Select(t => t.Name) ?? [];

            _ctx.AddTags(methodTags);
            return this;
        }

        /// <summary>
        /// Adds any tags defined in the provided <see cref="ScenarioAttribute"/>.
        /// </summary>
        /// <param name="scenarioAttr">
        /// The <see cref="ScenarioAttribute"/> describing the scenario, or <see langword="null"/>
        /// to skip scenario-level tags.
        /// </param>
        /// <returns>The same <see cref="ScenarioContextBuilder"/> for fluent chaining.</returns>
        /// <remarks>
        /// Tags defined here are appended to any feature- or method-level tags already added.
        /// </remarks>
        public ScenarioContextBuilder WithScenario(ScenarioAttribute? scenarioAttr)
        {
            if (scenarioAttr?.Tags is { Length: > 0 })
                _ctx.AddTags(scenarioAttr.Tags);

            return this;
        }

        /// <summary>
        /// Finalizes the builder and returns the constructed <see cref="ScenarioContext"/>.
        /// </summary>
        /// <returns>A fully populated <see cref="ScenarioContext"/> with feature, scenario, and tag metadata.</returns>
        public ScenarioContext Build() => _ctx;
    }
}