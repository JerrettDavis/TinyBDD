using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("ScenarioContext asserts invalid operations", "Verify AssertPassed/AssertFailed throw InvalidOperationException appropriately")]
public class ScenarioContextAssertsInvalidOperationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AssertPassed throws when any step failed")]
    [Fact]
    public async Task AssertPassed_Throws_When_Any_Step_Failed()
    {
        await Flow.Given("wire", () => 1)
            .When("noop", _ => Task.CompletedTask)
            .Then("should fail", v => v == 999); // false -> recorded failure

        Assert.Throws<InvalidOperationException>(() => Scenario.AssertPassed());
    }

    [Scenario("AssertFailed throws when all steps passed")]
    [Fact]
    public async Task AssertFailed_Throws_When_All_Steps_Passed()
    {
        await Flow.Given("wire", () => 1)
            .When("noop", _ => Task.CompletedTask)
            .Then("ok", v => v == 1);

        Assert.Throws<InvalidOperationException>(() => Scenario.AssertFailed());
    }
}

