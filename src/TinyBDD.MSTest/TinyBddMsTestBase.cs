namespace TinyBDD.MSTest;

using static Flow;

[Feature("Unnamed Feature")]
public abstract class TinyBddMsTestBase
{
    public TestContext TestContext { get; set; } = null!;
    protected ScenarioContext Scenario => Ambient.Current.Value!;

    [TestInitialize]
    public void TinyBdd_Init()
    {
        MsTestTraitBridge.TestContext = TestContext;
        var traits = new MsTestTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

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