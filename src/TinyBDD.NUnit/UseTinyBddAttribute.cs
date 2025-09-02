using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace TinyBDD.NUnit;

/// <summary>
/// Instructs NUnit to register <see cref="AmbientTestMethodResolver"/> and track the currently
/// executing test method for TinyBDD.
/// </summary>
/// <remarks>
/// <para>
/// This attribute can be applied at the class or method level. Before each test executes, it
/// calls <see cref="Bdd.Register(ITestMethodResolver)"/> with the global
/// <see cref="AmbientTestMethodResolver.Instance"/> and sets the current <see cref="MethodInfo"/>
/// from NUnit’s <see cref="ITest"/> metadata. After the test finishes, it clears the ambient
/// method reference.
/// </para>
/// <para>
/// This makes <see cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/> capable
/// of resolving the correct test method without relying on stack walking.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [TestFixture]
/// [UseTinyBdd]
/// public class CalculatorTests
/// {
///     [Test]
///     public async Task AddsNumbers()
///     {
///         await Flow.Given(() => 1)
///                   .When("add", x => x + 1)
///                   .Then("== 2", v => v == 2);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AmbientTestMethodResolver"/>
/// <seealso cref="Bdd.Register(ITestMethodResolver)"/>
/// <seealso cref="Ambient.Current"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UseTinyBddAttribute : NUnitAttribute, ITestAction
{
    /// <inheritdoc />
    public void BeforeTest(ITest test)
    {
        Bdd.Register(AmbientTestMethodResolver.Instance);
        AmbientTestMethodResolver.Set(test.Method?.MethodInfo);
    }

    /// <inheritdoc />
    public void AfterTest(ITest test)
    {
        AmbientTestMethodResolver.Set(null);
    }

    /// <summary>
    /// Indicates that this attribute applies to tests and test cases.
    /// </summary>
    public ActionTargets Targets => ActionTargets.Test;
}