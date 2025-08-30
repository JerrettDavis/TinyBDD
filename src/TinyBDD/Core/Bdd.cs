//
// using System.Diagnostics;
// using System.Reflection;
//
// namespace TinyBDD;
//
// /// <summary>
// /// Entry point for creating scenarios and starting fluent Given/When/Then chains.
// /// </summary>
// /// <remarks>
// /// <para>
// /// Use <see cref="CreateContext(object, string?, ITraitBridge?)"/> to create a <see cref="ScenarioContext"/>
// /// bound to your test method. Then start a chain with <c>Bdd.Given(ctx, ...)</c> or use the
// /// ambient <see cref="Flow"/> API if you prefer not to pass the context around.
// /// </para>
// /// <para>
// /// This façade now routes fluent chains through <see cref="ScenarioBuilder"/>, a single FIFO builder
// /// that defers execution until the terminal <c>Then(...)</c> is awaited. Each step is recorded as a
// /// <see cref="StepResult"/> on <see cref="ScenarioContext"/>.
// /// </para>
// /// </remarks>
// public static class Bdd
// {
//     /// <summary>
//     /// Creates a <see cref="ScenarioContext"/> by inspecting <paramref name="featureSource"/> and
//     /// the current test method for attributes such as <see cref="FeatureAttribute"/> and
//     /// <see cref="ScenarioAttribute"/>. Tags from <see cref="TagAttribute"/> on the class and
//     /// method are also recorded.
//     /// </summary>
//     public static ScenarioContext CreateContext(
//         object featureSource,
//         string? scenarioName = null,
//         ITraitBridge? traits = null)
//     {
//         var featureAttr = featureSource.GetType().GetCustomAttribute<FeatureAttribute>();
//         var featureName = featureAttr?.Name ?? featureSource.GetType().Name;
//         var featureDesc = featureAttr?.Description;
//
//         var method = FindCurrentTestMethod(); // NOTE: no dependency on featureSource type
//         var scenarioAttr = method?.GetCustomAttribute<ScenarioAttribute>();
//
//         var resolvedScenarioName =
//             !string.IsNullOrWhiteSpace(scenarioName) ? scenarioName :
//             !string.IsNullOrWhiteSpace(scenarioAttr?.Name) ? scenarioAttr!.Name! :
//             method?.Name ?? "Scenario";
//
//         var ctx = new ScenarioContext(featureName, featureDesc, resolvedScenarioName, traits ?? new NullTraitBridge());
//
//         // tags: class (feature source)
//         foreach (var tag in featureSource.GetType().GetCustomAttributes<TagAttribute>(inherit: true))
//             ctx.AddTag(tag.Name);
//
//         // tags: method
//         if (method is null)
//             return ctx;
//
//         foreach (var tag in method.GetCustomAttributes<TagAttribute>(inherit: true))
//             ctx.AddTag(tag.Name);
//
//         if (scenarioAttr?.Tags is { Length: > 0 })
//             foreach (var tg in scenarioAttr.Tags)
//                 ctx.AddTag(tg);
//
//         return ctx;
//     }
//
//     private static MethodInfo? FindCurrentTestMethod()
//     {
//         var frames = new StackTrace().GetFrames();
//         if (frames is null) return null;
//
//         // Prefer [Scenario], then known test attributes
//         foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
//             if (mi.GetCustomAttribute<ScenarioAttribute>() is not null)
//                 return mi;
//
//         foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
//             if (HasAnyTestAttribute(mi))
//                 return mi;
//
//         return null;
//     }
//
//     private static bool HasAnyTestAttribute(MethodInfo m)
//     {
//         var nattrs = m
//             .GetCustomAttributes(inherit: true)
//             .Select(a => a.GetType().FullName ?? "");
//
//         foreach (var n in nattrs)
//         {
//             if (n is "Xunit.FactAttribute"
//                 or "Xunit.TheoryAttribute"
//                 or "NUnit.Framework.TestAttribute"
//                 or "NUnit.Framework.TestCaseAttribute"
//                 or "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
//                 return true;
//         }
//
//         return false;
//     }
//
//     // ----------------------------------------------------------------
//     // New unified entrypoints: return ScenarioBuilder instead of GivenBuilder<T>
//     // ----------------------------------------------------------------
//
//     /// <summary>
//     /// Starts a <c>Given</c> step with an explicit title and synchronous setup.
//     /// </summary>
//     public static ScenarioBuilder Given<T>(
//         ScenarioContext ctx,
//         string title,
//         Func<T> setup)
//         => ScenarioBuilder.Start(ctx).Given(title, setup);
//
//     /// <summary>
//     /// Starts a <c>Given</c> step with an explicit title and asynchronous setup.
//     /// </summary>
//     public static ScenarioBuilder Given<T>(
//         ScenarioContext ctx,
//         string title,
//         Func<CancellationToken, Task<T>> setupAsync)
//         => ScenarioBuilder.Start(ctx).Given(title, setupAsync);
//
//     /// <summary>
//     /// Starts a <c>Given</c> step with an explicit title and asynchronous (ValueTask) setup.
//     /// </summary>
//     public static ScenarioBuilder Given<T>(
//         ScenarioContext ctx,
//         string title,
//         Func<CancellationToken, ValueTask<T>> setupAsync)
//         => ScenarioBuilder.Start(ctx).Given(title, setupAsync);
//
//     /// <summary>
//     /// Starts a <c>Given</c> step using a default title based on <typeparamref name="T"/>.
//     /// </summary>
//     public static ScenarioBuilder Given<T>(
//         ScenarioContext ctx,
//         Func<T> setup)
//         => ScenarioBuilder.Start(ctx).Given(setup);
//
//     /// <summary>
//     /// Starts a <c>Given</c> step using a default title based on <typeparamref name="T"/>, with async setup.
//     /// </summary>
//     public static ScenarioBuilder Given<T>(
//         ScenarioContext ctx,
//         Func<CancellationToken, Task<T>> setupAsync)
//         => ScenarioBuilder.Start(ctx).Given(setupAsync);
//
//     /// <summary>
//     /// Starts a <c>Given</c> step using a default title based on <typeparamref name="T"/>, with async (ValueTask) setup.
//     /// </summary>
//     public static ScenarioBuilder Given<T>(
//         ScenarioContext ctx,
//         Func<CancellationToken, ValueTask<T>> setupAsync)
//         => ScenarioBuilder.Start(ctx).Given(setupAsync);
//
//     // ----------------------------------------------------------------
//     // NOTE: The old RunStep*/core helpers are no longer needed.
//     // Step timing, error capture, and StepResult recording happen inside ScenarioBuilder.
//     // ----------------------------------------------------------------
// }
