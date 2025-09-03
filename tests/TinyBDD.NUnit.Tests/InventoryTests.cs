using NUnit.Framework;

namespace TinyBDD.NUnit.Tests;

[Feature("Inventory")]
public class InventoryTests : TinyBddNUnitBase
{
    [Scenario("A cool scenario with all the whistles", "Tag1", "Tag2")]
    [Test]
    public async Task TestScenario()
        => await Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => { Assert.That(v, Is.EqualTo(2)); return Task.CompletedTask; })
            .AssertPassed();

    [Scenario("Explicit from(context)")]
    [Test]
    public async Task TestScenario_With_From_Context()
    {
        var context = Scenario; // or Ambient.Current.Value!
        await From(context)
            .Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => { Assert.That(v, Is.EqualTo(2)); return Task.CompletedTask; })
            .AssertPassed();
    }

    [Scenario("Explicit from(context)")]
    [Test]
    public async Task TestScenario_With_From_Context_Tasks()
    {
        var context = Scenario; // or Ambient.Current.Value!
        await From(context)
            .Given("wire", _ => Task.FromResult(1))
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => { Assert.That(v, Is.EqualTo(2)); return Task.CompletedTask; })
            .AssertPassed();
    }

    [Scenario("Use Base Scenario")]
    [Test]
    public async Task TestScenario_With_From_Tasks()
    {
        await From()
            .Given("wire", _ => Task.FromResult(1))
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => { Assert.That(v, Is.EqualTo(2)); return Task.CompletedTask; })
            .AssertPassed();
    }
}