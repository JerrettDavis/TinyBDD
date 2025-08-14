using System.Diagnostics;
using System.Reflection;

namespace TinyBDD;

public static class Bdd
{
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
            !string.IsNullOrWhiteSpace(scenarioAttr?.Name) ? scenarioAttr!.Name! :
            method?.Name ?? "Scenario";

        var ctx = new ScenarioContext(featureName, featureDesc, resolvedScenarioName, traits ?? new NullTraitBridge());

        // tags: class (feature source)
        foreach (var tag in featureSource.GetType().GetCustomAttributes<TagAttribute>(inherit: true))
            ctx.AddTag(tag.Name);

        // tags: method
        if (method is not null)
        {
            foreach (var tag in method.GetCustomAttributes<TagAttribute>(inherit: true))
                ctx.AddTag(tag.Name);

            if (scenarioAttr?.Tags is { Length: > 0 })
                foreach (var tg in scenarioAttr.Tags)
                    ctx.AddTag(tg);
        }

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
        var nattrs = m.GetCustomAttributes(inherit: true).Select(a => a.GetType().FullName ?? "");
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


    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<T> setup)
        => new(ctx, title, _ => Task.FromResult(setup()));

    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        string title,
        Func<CancellationToken, Task<T>> setupAsync)
        => new(ctx, title, setupAsync);

    public static GivenBuilder<T> Given<T>(
        ScenarioContext ctx,
        Func<T> setup)
        => Given(ctx, $"Given {typeof(T).Name}", setup);

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
        => RunStepAsyncCore<T>(ctx, kind, title, step)!;
}