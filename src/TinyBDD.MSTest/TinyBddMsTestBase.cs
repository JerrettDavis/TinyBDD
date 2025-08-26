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
public abstract class TinyBddMsTestBase
{
    /// <summary>Provided by MSTest; used for logging and trait bridging.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>The current scenario context for the running test.</summary>
    protected ScenarioContext Scenario => Ambient.Current.Value!;

    /// <summary>Initializes the TinyBDD ambient context and trait bridge.</summary>
    [TestInitialize]
    public void TinyBdd_Init()
    {
        MsTestTraitBridge.TestContext = TestContext;
        var traits = new MsTestTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    [TestCleanup]
    public void TinyBdd_Cleanup()
    {
        var ctx = Ambient.Current.Value;
        if (ctx is not null)
        {
            var reporter = new MsTestBddReporter();
            GherkinFormatter.Write(ctx, reporter);
        }

        Ambient.Current.Value = null;
    }
}