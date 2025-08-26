using System.Diagnostics;
using System.Reflection;

namespace TinyBDD;

/// <summary>
/// Entry point for creating scenarios and starting fluent Given/When/Then chains.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="CreateContext(object, string?, ITraitBridge?)"/> to create a <see cref="ScenarioContext"/>
/// bound to your test method. Then start a chain with <c>Bdd.Given(ctx, ...)</c> or use the
/// ambient <see cref="Flow"/> API if you prefer not to pass the context around.
/// </para>
/// <para>
/// This type is a thin façade that records each step (Given/When/Then/And/But) via
/// <see cref="ScenarioContext"/> and throws on failures using <see cref="BddStepException"/>.
/// </para>
/// </remarks>
public static class Bdd
{
    /// <summary>
    /// Creates a <see cref="ScenarioContext"/> by inspecting <paramref name="featureSource"/> and
    /// the current test method for attributes such as <see cref="FeatureAttribute"/> and
    /// <see cref="ScenarioAttribute"/>. Tags from <see cref="TagAttribute"/> on the class and
    /// method are also recorded.
    /// </summary>
    /// <param name="featureSource">An object from which the feature name and tags are inferred (typically <c>this</c> from a test class).</param>
    /// <param name="scenarioName">Optional explicit scenario name. If not supplied, the method name or <see cref="ScenarioAttribute.Name"/> is used.</param>
    /// <param name="traits">Optional bridge for test framework traits/categories.</param>
    /// <returns>A new <see cref="ScenarioContext"/> representing this scenario.</returns>
    /// <example>
    /// <code>
    /// var ctx = Bdd.CreateContext(this);
    /// await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
    ///          .When("sum", (arr, _) => Task.FromResult(arr.Sum()))
    ///          .Then("> 0", sum => sum > 0);
    /// ctx.AssertPassed();
    /// </code>
    /// </example>
    public static ScenarioContext CreateContext(
        object featureSource,
        string? scenarioName = null,
        ITraitBridge? traits = null)
    {
        var featureAttr = featureSource.GetType().GetCustomAttribute<FeatureAttribute>();
        var featureName = featureAttr?.Name ?? featureSource.GetType().Name;
        var featureDesc = featureAttr?.Description;

        var method = FindCurrentTestMethod(); // NOTE: no dependency on featureSource type
        var scenarioAttr = method?.GetCustomAttribute<ScenarioAttribute>();

        var resolvedScenarioName =
            !string.IsNullOrWhiteSpace(scenarioName) ? scenarioName :
            !string.IsNullOrWhiteSpace(scenarioAttr?.Name) ? scenarioAttr.Name! :
            method?.Name ?? "Scenario";

        var ctx = new ScenarioContext(featureName, featureDesc, resolvedScenarioName, traits ?? new NullTraitBridge());

        // tags: class (feature source)
        foreach (var tag in featureSource.GetType().GetCustomAttributes<TagAttribute>(inherit: true))
            ctx.AddTag(tag.Name);

        // tags: method
        if (method is null)
            return ctx;

        foreach (var tag in method.GetCustomAttributes<TagAttribute>(inherit: true))
            ctx.AddTag(tag.Name);

        if (scenarioAttr?.Tags is not { Length: > 0 })
            return ctx;

        foreach (var tg in scenarioAttr.Tags)
            ctx.AddTag(tg);

        return ctx;
    }

    private static MethodInfo? FindCurrentTestMethod()
    {
        var frames = new StackTrace().GetFrames();
        if (frames is null) return null;

        // Prefer [Scenario], then known test attributes
        foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
            if (mi.GetCustomAttribute<ScenarioAttribute>() is not null)
                return mi;

        foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
            if (HasAnyTestAttribute(mi))
                return mi;

        return null;
    }

    private static bool HasAnyTestAttribute(MethodInfo m)
    {
        var nattrs = m
            .GetCustomAttributes(inherit: true)
            .Select(a => a.GetType().FullName ?? "");

        foreach (var n in nattrs)
        {
            if (n is "Xunit.FactAttribute"
                or "Xunit.TheoryAttribute"
                or "NUnit.Framework.TestAttribute"
                or "NUnit.Framework.TestCaseAttribute"
                or "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Starts a <c>Given</c> step with an explicit title and synchronous setup.
    /// </summary>
    /// <typeparam name="T">Type produced by the setup delegate.</typeparam>
    /// <param name="ctx">Scenario context.</param>
    /// <param name="title">Display title for the step.</param>
    /// <param name="setup">Synchronous setup function.</param>
    /// <returns>A builder to continue with <c>When</c>.</returns>
    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<T> setup)
        => new(ctx, title, _ => Task.FromResult(setup()));

    /// <summary>
    /// Starts a <c>Given</c> step with an explicit title and asynchronous setup.
    /// </summary>
    /// <typeparam name="T">Type produced by the setup delegate.</typeparam>
    /// <param name="ctx">Scenario context.</param>
    /// <param name="title">Display title for the step.</param>
    /// <param name="setupAsync">Asynchronous setup function that receives a cancellation token.</param>
    /// <returns>A builder to continue with <c>When</c>.</returns>
    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, Task<T>> setupAsync)
        => new(ctx, title, setupAsync);

    /// <summary>
    /// Starts a <c>Given</c> step using a default title based on <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type produced by the setup delegate.</typeparam>
    /// <param name="ctx">Scenario context.</param>
    /// <param name="setup">Synchronous setup function.</param>
    /// <returns>A builder to continue with <c>When</c>.</returns>
    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        Func<T> setup)
        => Given(ctx, $"Given {typeof(T).Name}", setup);

    /// <summary>
    /// Starts a <c>Given</c> step using a default title based on <typeparamref name="T"/>, with async setup.
    /// </summary>
    /// <typeparam name="T">Type produced by the setup delegate.</typeparam>
    /// <param name="ctx">Scenario context.</param>
    /// <param name="setupAsync">Asynchronous setup function that receives a cancellation token.</param>
    /// <returns>A builder to continue with <c>When</c>.</returns>
    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        Func<CancellationToken, Task<T>> setupAsync)
        => Given(ctx, $"Given {typeof(T).Name}", setupAsync);

    private static async Task<TResult?> RunStepAsyncCore<TResult>(
        ScenarioContext ctx,
        string kind,
        string title,
        Func<Task<TResult>> step)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await step();
            sw.Stop();
            ctx.AddStep(new StepResult { Kind = kind, Title = title, Elapsed = sw.Elapsed });
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            ctx.AddStep(new StepResult { Kind = kind, Title = title, Elapsed = sw.Elapsed, Error = ex });
            throw new BddStepException($"{kind} failed: {title}", ex);
        }
    }

    internal static Task RunStepAsync(
        ScenarioContext ctx,
        string kind,
        string title,
        Func<Task> step)
        => RunStepAsyncCore<object?>(ctx, kind, title, async () =>
        {
            await step();
            return null;
        });

    internal static Task<T> RunStepAsync<T>(
        ScenarioContext ctx,
        string kind,
        string title,
        Func<Task<T>> step)
        => RunStepAsyncCore(ctx, kind, title, step)!;
}