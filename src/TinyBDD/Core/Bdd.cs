using System.Diagnostics;
using System.Reflection;

namespace TinyBDD;

/// <summary>
/// Entry points for creating a <see cref="ScenarioContext"/> and starting fluent Given/When/Then chains.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="CreateContext(object, string?, ITraitBridge?)"/> to construct a <see cref="ScenarioContext"/>
/// from attributes on your test class and method (see <see cref="FeatureAttribute"/>, <see cref="ScenarioAttribute"/>,
/// and <see cref="TagAttribute"/>). Then begin a scenario with one of the <c>Given</c> overloads.
/// </para>
/// <para>
/// If you prefer not to pass a context around, use the ambient <see cref="Flow"/> API by setting
/// <see cref="Ambient.Current"/> at the start of a test.
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
public static class Bdd
{
    /// <summary>
    /// Creates a <see cref="ScenarioContext"/> by inspecting <paramref name="featureSource"/> and the current test method
    /// for <see cref="FeatureAttribute"/>, <see cref="ScenarioAttribute"/>, and <see cref="TagAttribute"/>.
    /// </summary>
    /// <param name="featureSource">Any object from the test class; only its type is used to read attributes.</param>
    /// <param name="scenarioName">Optional scenario name override; defaults to the method name or <see cref="ScenarioAttribute.Name"/>.</param>
    /// <param name="traits">Optional trait bridge for propagating tags to the host framework.</param>
    /// <returns>A new <see cref="ScenarioContext"/> populated with feature/scenario metadata and tags.</returns>
    public static ScenarioContext CreateContext(
        object featureSource, 
        string? scenarioName = null, 
        ITraitBridge? traits = null)
    {
        var featureAttr = featureSource.GetType().GetCustomAttribute<FeatureAttribute>();
        var featureName = featureAttr?.Name ?? featureSource.GetType().Name;
        var featureDesc = featureAttr?.Description;

        var method = FindCurrentTestMethod();
        var scenarioAttr = method?.GetCustomAttribute<ScenarioAttribute>();

        var resolvedScenarioName =
            !string.IsNullOrWhiteSpace(scenarioName) ? scenarioName :
            !string.IsNullOrWhiteSpace(scenarioAttr?.Name) ? scenarioAttr.Name! :
            method?.Name ?? "Scenario";

        var ctx = new ScenarioContext(featureName, featureDesc, resolvedScenarioName, traits ?? new NullTraitBridge());

        foreach (var tag in featureSource.GetType().GetCustomAttributes<TagAttribute>(inherit: true))
            ctx.AddTag(tag.Name);

        if (method is not null)
        {
            foreach (var tag in method.GetCustomAttributes<TagAttribute>(inherit: true))
                ctx.AddTag(tag.Name);
            if (scenarioAttr?.Tags is not { Length: > 0 })
                return ctx;
            foreach (var t in scenarioAttr.Tags)
                ctx.AddTag(t);
        }

        return ctx;
    }

    private static MethodInfo? FindCurrentTestMethod()
    {
        var frames = new StackTrace().GetFrames();
        if (frames is null) return null;

        foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
            if (mi.GetCustomAttribute<ScenarioAttribute>() is not null)
                return mi;

        return frames
            .Select(f => f.GetMethod())
            .OfType<MethodInfo>()
            .FirstOrDefault(HasAnyTestAttribute);
    }

    private static bool HasAnyTestAttribute(MethodInfo m)
    {
        var names = m.GetCustomAttributes(inherit: true).Select(a => a.GetType().FullName ?? "");
        foreach (var n in names)
            if (n is "Xunit.FactAttribute"
                or "Xunit.TheoryAttribute"
                or "NUnit.Framework.TestAttribute"
                or "NUnit.Framework.TestCaseAttribute"
                or "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                return true;
        return false;
    }

    /// <summary>Starts a <c>Given</c> step with an explicit title and synchronous setup.</summary>
    /// <param name="ctx">Scenario context created by <see cref="CreateContext(object, string?, ITraitBridge?)"/>.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <typeparam name="T">The type produced by the setup function.</typeparam>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<T> setup)
        => ScenarioChain<T>.Seed(ctx, title, _ => VT.From(setup()));

    /// <summary>Starts a <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, _ => VT.From(setup()));

    /// <summary>Starts a <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, _ => setup());

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<CancellationToken, Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, ct => VT.From(setup(ct)));

    /// <summary>Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask"/> setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<CancellationToken, ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, setup);

    /// <summary>Starts a <c>Given</c> step using a default title derived from <typeparamref name="T"/>.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<T> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", _ => VT.From(setup()));

    /// <summary>Starts a <c>Given</c> step with a default title and <see cref="ValueTask"/> setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", _ => setup());

    /// <summary>Starts a <c>Given</c> step with a default title and asynchronous setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", _ => VT.From(setup()));

    /// <summary>Starts a token-aware <c>Given</c> step with a default title and asynchronous setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<CancellationToken, Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", ct => VT.From(setup(ct)));

    /// <summary>Starts a token-aware <c>Given</c> step with a default title and <see cref="ValueTask"/> setup.</summary>
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<CancellationToken, ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", setup);
}