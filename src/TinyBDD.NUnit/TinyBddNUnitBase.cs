using NUnit.Framework;

namespace TinyBDD.NUnit;

[Feature("Unnamed Feature")]
public abstract class TinyBddNUnitBase
{
    protected ScenarioContext Scenario => Ambient.Current.Value!;

    [SetUp]
    public void TinyBdd_SetUp()
    {
        var traits = new NUnitTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

    [TearDown]
    public void TinyBdd_TearDown()
    {
        var ctx = Ambient.Current.Value;
        if (ctx is not null)
        {
            var reporter = new NUnitBddReporter();
            GherkinFormatter.Write(ctx, reporter);
        }
        Ambient.Current.Value = null;
    }
}
