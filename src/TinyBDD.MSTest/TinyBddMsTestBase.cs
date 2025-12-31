using System.Reflection;

namespace TinyBDD.MSTest;

/// <summary>
/// Base class for MSTest that initializes TinyBDD ambient context and writes a Gherkin report
/// after each test.
/// </summary>
/// <remarks>
/// <para>
/// In <see cref="TinyBdd_Init"/>, this class creates a <see cref="ScenarioContext"/>, sets
/// <see cref="Ambient.Current"/>, and wires an <see cref="MsTestTraitBridge"/>. In
/// <see cref="TinyBdd_Cleanup"/>, it emits a Gherkin report and clears the ambient context.
/// </para>
/// <para>
/// Feature-level setup/teardown is supported via <see cref="ConfigureFeatureSetup"/> and
/// <see cref="ConfigureFeatureTeardown"/>. These run once per test class via
/// <see cref="TinyBdd_FeatureSetup"/> and <see cref="TinyBdd_FeatureTeardown"/>.
/// </para>
/// </remarks>
[Feature("Unnamed Feature")]
public abstract class TinyBddMsTestBase : TestBase
{
    /// <summary>Provided by MSTest; used for logging and trait bridging.</summary>
    public TestContext TestContext { get; set; } = null!;

    protected override IBddReporter Reporter => new MsTestBddReporter();

    /// <summary>Executes feature setup once before any tests in the class.</summary>
    /// <remarks>
    /// This method is called by MSTest before any test methods run in the class.
    /// Override <see cref="TestBase.ConfigureFeatureSetup"/> to define feature-level setup steps.
    /// </remarks>
    [ClassInitialize]
    public static async Task TinyBdd_FeatureSetup(TestContext context)
    {
        // Note: ClassInitialize is static, so we need to handle state carefully
        // The actual execution will happen in TinyBdd_Init when the first test runs
        await Task.CompletedTask;
    }

    /// <summary>Executes feature teardown once after all tests in the class complete.</summary>
    /// <remarks>
    /// This method is called by MSTest after all test methods in the class complete.
    /// Override <see cref="TestBase.ConfigureFeatureTeardown"/> to define feature-level teardown steps.
    /// </remarks>
    [ClassCleanup]
    public static async Task TinyBdd_FeatureTeardown()
    {
        // Note: ClassCleanup is static, so we need to handle state carefully
        // This is a limitation of MSTest's static class lifecycle hooks
        await Task.CompletedTask;
    }

    private static readonly object _featureSetupLock = new();
    private static bool _featureSetupExecuted;

    /// <summary>Initializes the TinyBDD ambient context and trait bridge.</summary>
    /// <remarks>
    /// If you override <see cref="TestBase.ConfigureBackground"/>, call <see cref="TestBase.ExecuteBackgroundAsync"/>
    /// at the start of your test or in a derived <c>[TestInitialize]</c> method.
    /// </remarks>
    [TestInitialize]
    public async Task TinyBdd_Init()
    {
        Bdd.Register(AmbientTestMethodResolver.Instance);
        AmbientTestMethodResolver.Set(ResolveCurrentMethod(TestContext));

        MsTestTraitBridge.TestContext = TestContext;
        var traits = new MsTestTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;

        // Execute feature setup once per class
        if (!_featureSetupExecuted)
        {
            lock (_featureSetupLock)
            {
                if (!_featureSetupExecuted)
                {
                    await ExecuteFeatureSetupAsync();
                    _featureSetupExecuted = true;
                }
            }
        }

        // Execute background for each test
        await ExecuteBackgroundAsync();
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