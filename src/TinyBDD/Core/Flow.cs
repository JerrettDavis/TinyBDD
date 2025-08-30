// namespace TinyBDD;
//
// /// <summary>
// /// Ambient, context-free entry points for building BDD chains when <see cref="Ambient.Current"/> is set.
// /// </summary>
// /// <remarks>
// /// <para>
// /// Use this API when you prefer not to pass <see cref="ScenarioContext"/> through your methods. Set
// /// <see cref="Ambient.Current"/> at the start of a test (TinyBDD test base classes do this for you).
// /// </para>
// /// <example>
// /// <code>
// /// var prev = Ambient.Current.Value; Ambient.Current.Value = Bdd.CreateContext(this);
// /// try
// /// {
// ///     await Flow.Given(() => 1)
// ///         .When("double", x => x * 2)
// ///         .Then("== 2", v => v == 2);
// /// }
// /// finally { Ambient.Current.Value = prev; }
// /// </code>
// /// </example>
// /// </remarks>
// public static class Flow
// {
//     public static ScenarioChain<T> Given<T>(string title, Func<T> setup)
//         => Bdd.Given(Require(), title, setup);
//
//     public static ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup)
//         => Bdd.Given(Require(), title, setup);
//
//     public static ScenarioChain<T> Given<T>(Func<T> setup)
//         => Bdd.Given(Require(), setup);
//
//     public static ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup)
//         => Bdd.Given(Require(), setup);
//
//     public static FromContext From(ScenarioContext ctx) => new(ctx);
//
//     private static ScenarioContext Require()
//         => Ambient.Current.Value ?? throw new InvalidOperationException(
//             "TinyBDD ambient ScenarioContext not set. Inherit from TinyBdd*Base or set Ambient.Current manually.");
// }
//
// public readonly struct FromContext(ScenarioContext ctx)
// {
//     public ScenarioChain<T> Given<T>(string title, Func<T> setup) => Bdd.Given(ctx, title, setup);
//     public ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, title, setup);
//     public ScenarioChain<T> Given<T>(Func<T> setup) => Bdd.Given(ctx, setup);
//     public ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, setup);
// }
