using System.Runtime.CompilerServices;

namespace TinyBDD.MSTest;

/// <summary>
/// MSTest adapter for TinyBDD providing a base class, trait bridge, and reporter.
/// </summary>
/// <remarks>
/// <para>Usage</para>
/// <list type="number">
/// <item><description>Inherit from <see cref="TinyBddMsTestBase"/> in your test class.</description></item>
/// <item><description>Write tests using the ambient <see cref="TinyBDD.Flow"/> API.</description></item>
/// <item><description>Tags are logged via <see cref="MsTestTraitBridge"/>; reports use <see cref="MsTestBddReporter"/>.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class CalculatorTests : TinyBddMsTestBase
/// {
///     [Scenario("Add numbers"), TestMethod]
///     public async Task Add()
///     {
///         await Flow.Given(() => 1)
///                   .When("+1", x => x + 1)
///                   .Then("== 2", v => v == 2);
///         Scenario.AssertPassed();
///     }
/// }
/// </code>
/// </example>
[CompilerGenerated]
internal static class NamespaceDoc { }

