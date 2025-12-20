using System.Reflection;

namespace TinyBDD.MSTest;

/// <summary>
/// Base class for MSTest that initializes TinyBDD ambient context and writes a Gherkin report
/// after each test.
/// </summary>
/// <remarks>
/// In <see cref="TinyBdd_Init"/>, this class creates a <see cref="ScenarioContext"/>, sets
/// <see cref="Ambient.Current"/>, and wires an <see cref="MsTestTraitBridge"/>. In
/// <see cref="TinyBdd_Cleanup"/>, it emits a Gherkin report and clears the ambient context.
/// </remarks>
[Feature("Unnamed Feature")]
public abstract class TinyBddMsTestBase : TestBase
{
    /// <summary>Provided by MSTest; used for logging and trait bridging.</summary>
    public TestContext TestContext { get; set; } = null!;

    protected override IBddReporter Reporter => new MsTestBddReporter();

    /// <summary>Initializes the TinyBDD ambient context and trait bridge.</summary>
    [TestInitialize]
    public void TinyBdd_Init()
    {
        Bdd.Register(AmbientTestMethodResolver.Instance);
        AmbientTestMethodResolver.Set(ResolveCurrentMethod(TestContext));
        
        MsTestTraitBridge.TestContext = TestContext;
        var traits = new MsTestTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }
    
    private static MethodInfo? ResolveCurrentMethod(TestContext tc)
    {
        // FullyQualifiedTestClassName is authoritative for the test class.
        var classType = Type.GetType(tc.FullyQualifiedTestClassName) ?? null;
        if (classType is null) return null;
        
        // 1) Exact match (typical)
        var mi = classType.GetMethod(
            tc.TestName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (mi is not null) return mi;

        // 2) Data-driven / decorated name: often "MethodName (arg1, arg2, ...)"
        // Try prefix match first
        mi = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => tc.TestName.StartsWith(m.Name, StringComparison.Ordinal));

        if (mi is not null) return mi;

        // 3) Fallback: any method carrying [Scenario] (when unique per class)
        mi = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.GetCustomAttribute<ScenarioAttribute>() is not null);

        return mi; // may be null; TinyBDD will fall back to stack-walk if needed
    }

    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    [TestCleanup]
    public void TinyBdd_Cleanup()
        => CleanUp();
}