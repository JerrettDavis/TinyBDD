using System.Reflection;
using Xunit.v3;

namespace TinyBDD.Xunit.v3;

/// <summary>
/// Instructs xUnit.v3 to register <see cref="AmbientTestMethodResolver"/> and track the currently
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
    public override void Before(
        MethodInfo methodUnderTest, 
        IXunitTest test)
    {
        Bdd.Register(AmbientTestMethodResolver.Instance);
        AmbientTestMethodResolver.Set(methodUnderTest);
        
        var ctx = Ambient.Current.Value!;
        Ambient.Current.Value = Bdd.CreateContext(
            methodUnderTest.DeclaringType!,
            MethodNameResolver(methodUnderTest),
            traits: ctx.TraitBridge);
    }
    
    private static string MethodNameResolver(MethodInfo methodUnderTest)
        => methodUnderTest.GetCustomAttribute<ScenarioAttribute>()?.Name
           ?? methodUnderTest.Name;

    /// <inheritdoc />
    public override void After(
        MethodInfo methodUnderTest,
        IXunitTest test)
    {
        AmbientTestMethodResolver.Set(null);
    }
}