using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("Reconfiguring a context with new options with the ReconfigureContext method")]
public class BddContextReconfigurationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("We're presented with a ScenarioContext instance to reconfigure and we're missing required parameters")]
    [Fact]
    public async Task ReconfigureContext_ShouldRequireValidParameters()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd.Given(ctx, "you have a valid context", () => ctx)
            .When("the context is set to have an invalid feature name",
                (ScenarioContext Context, Func<ScenarioContext> Reconfigure) (c)
                    => (Context: c,
                        Reconfigure: () => Bdd.ReconfigureContext(c, o => o.FeatureName = null)))
            .Then("the context should throw an exception", f => Assert.Throws<ArgumentException>(f.Reconfigure))
            .When("the context is set to have an invalid scenario name",
                (ScenarioContext Context, Func<ScenarioContext> Reconfigure) (c)
                    => c with { Reconfigure = () => Bdd.ReconfigureContext(c.Context, o => o.ScenarioName = null) })
            .Then("the context should throw an exception", f => Assert.Throws<ArgumentException>(f.Reconfigure))
            .When("the context is set to have an invalid trait bridge",
                (ScenarioContext Context, Func<ScenarioContext> Reconfigure) (c)
                    => c with { Reconfigure = () => Bdd.ReconfigureContext(c.Context, o => o.TraitBridge = null) })
            .Then("the context should throw an exception", f => Assert.Throws<ArgumentException>(f.Reconfigure))
            .When("The context is ste to have an invalid options",
                (ScenarioContext Context, Func<ScenarioContext> Reconfigure) (c)
                    => c with { Reconfigure = () => Bdd.ReconfigureContext(c.Context, o => o.Options = null) })
            .Then("the context should throw an exception", f => Assert.Throws<ArgumentException>(f.Reconfigure))
            .When("The existing context is passed along", Func<ScenarioContext> (f) => () => Bdd.ReconfigureContext(f.Context, o => o.Options = f.Context.Options))
            .Then("There should not be any exception", f => f())
            .AssertPassed();
    }
}