using NUnit.Framework;

namespace TinyBDD.NUnit.Tests;

[Feature("NUnit adapter")]
public class NUnitAdapterTests
{
    [Scenario("Trait bridge no-throw + end-to-end")]
    [Test]
    public async Task TraitBridge_AddTag_NoThrow_And_Scenario_Runs()
    {
        var traits = new NUnitTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);

        // should simply log to TestContext.Out and not throw
        ctx.AddTag("nunit-tag-1");
        ctx.AddTag("nunit-tag-2");

        await Bdd.Given(ctx, "wire", () => 10)
            .When("add 5", (x, _) => Task.FromResult(x + 5))
            .Then("is 15", v =>
            {
                Assert.That(v, Is.EqualTo(15));
                return Task.CompletedTask;
            });

        Assert.That(ctx.Steps.Count, Is.EqualTo(3));
    }

    [Scenario("Reporter no-throw")]
    [Test]
    public void Reporter_WriteLine_NoThrow()
    {
        var reporter = new NUnitBddReporter();
        Assert.DoesNotThrow(() => reporter.WriteLine("nunit hello"));
    }
}