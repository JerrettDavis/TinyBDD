using System.Reflection;
using Xunit.Sdk;

namespace TinyBDD.Xunit;

/// <summary>
/// Instructs xUnit to register <see cref="AmbientTestMethodResolver"/> and track the currently
/// executing test method for TinyBDD.
/// </summary>
/// <remarks>
/// <para>
/// This attribute can be applied at the class or method level. Before each test executes, it
/// calls <see cref="Bdd.Register(ITestMethodResolver)"/> with the global
/// <see cref="AmbientTestMethodResolver.Instance"/> and sets the current <see cref="MethodInfo"/>
/// from xUnit’s test pipeline. After the test finishes, it clears the ambient
/// method reference.
/// </para>
/// <para>
/// This allows <see cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/> to
/// resolve the correct test method reliably even in async scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [UseTinyBdd]
/// public class CalculatorTests
/// {
///     [Fact]
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
public sealed class UseTinyBddAttribute : BeforeAfterTestAttribute
{
    /// <inheritdoc />
    public override void Before(MethodInfo methodUnderTest)
    {
        Bdd.Register(AmbientTestMethodResolver.Instance);
        AmbientTestMethodResolver.Set(methodUnderTest);
    }

    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest)
    {
        AmbientTestMethodResolver.Set(null);
    }
}