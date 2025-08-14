using NUnit.Framework;

namespace TinyBDD.NUnit.Tests;

[Feature("Inventory")]
public class InventoryTests : TinyBddNUnitBase
{
    [Scenario("A cool scenario with all the whistles", "Tag1", "Tag2")]
    [Test]
    public async Task TestScenario()
    {
        await Flow.Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => { Assert.That(v, Is.EqualTo(2)); return Task.CompletedTask; });

        Scenario.AssertPassed();
    }

    // If you prefer the explicit param style:
    [Scenario("Explicit from(context)")]
    [Test]
    public async Task TestScenario_With_From_Context()
    {
        var context = Scenario; // or Ambient.Current.Value!
        await Flow.From(context)
            .Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => { Assert.That(v, Is.EqualTo(2)); return Task.CompletedTask; });

        context.AssertPassed();
    }
}