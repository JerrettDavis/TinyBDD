using Xunit.Abstractions;

namespace TinyBDD.Xunit;

[Feature("Unnamed Feature")]
public abstract class TinyBddXunitBase : IDisposable
{
    private readonly ITestOutputHelper _output;

    protected TinyBddXunitBase(ITestOutputHelper output)
    {
        _output = output;
        var traits = new XunitTraitBridge(output);
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

    protected ScenarioContext Scenario => Ambient.Current.Value!;

    public void Dispose()
    {
        var ctx = Ambient.Current.Value;
        if (ctx is not null)
        {
            var reporter = new XunitBddReporter(_output);
            GherkinFormatter.Write(ctx, reporter);
        }

        Ambient.Current.Value = null; // tidy up
        
        GC.SuppressFinalize(this);
    }
}