using Xunit.Abstractions;

namespace TinyBDD.Xunit.Tests;

[Feature("Orders", "As a customer I can place an order")]
public class OrderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("A cool scenario with all the whistles", "Tag1", "Tag2")]
    [Fact]
    public async Task TestScenario()
    {
        await Flow.Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => Task.FromResult(v == 2));

        Scenario.AssertPassed();
    }
}