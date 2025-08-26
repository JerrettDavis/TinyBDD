using System.Runtime.CompilerServices;

namespace TinyBDD;

/// <summary>
/// Tiny, fluent BDD primitives for .NET tests.
/// </summary>
/// <remarks>
/// <para>Philosophy</para>
/// <list type="bullet">
/// <item><description>Keep it tiny: one small core with no framework lock-in.</description></item>
/// <item><description>Make it fluent: Given/When/Then chains with rich overloads.</description></item>
/// <item><description>Be pragmatic: integrate with xUnit/NUnit/MSTest without ceremony.</description></item>
/// </list>
/// <para>Two ways to use</para>
/// <list type="number">
/// <item>
/// <description>
/// Explicit: create a <see cref="ScenarioContext"/> and pass it to <see cref="Bdd.Given{T}(ScenarioContext, string, System.Func{T})"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Ambient: set <see cref="Ambient.Current"/> (or inherit a TinyBDD base class) and call <see cref="Flow.Given{T}(System.Func{T})"/>.
/// </description>
/// </item>
/// </list>
/// <para>Tags and reporting</para>
/// <list type="bullet">
/// <item><description>Add <see cref="TagAttribute"/> to classes or methods, or pass tags via <see cref="ScenarioAttribute"/>.</description></item>
/// <item><description>Write Gherkin-style output using <see cref="ScenarioContextGherkinExtensions.WriteGherkinTo(ScenarioContext, IBddReporter)"/> and <see cref="IBddReporter"/> implementations.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <para>Example: explicit usage (xUnit)</para>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
///          .When("sum", (arr, _) => Task.FromResult(arr.Sum()))
///          .Then("> 0", sum => sum > 0);
/// ctx.AssertPassed();
/// </code>
/// </example>
[CompilerGenerated]
internal static class NamespaceDoc { }

