using TinyBDD.Assertions;

namespace TinyBDD.Tests.Common;

public class ExpectationReportingTests
{
    [Feature("ExpectationReporting")] private sealed class Host {}

    [Scenario("Failing expectation includes structured fields in report")]
    [Fact]
    public async Task FailingExpectation_Includes_StructuredInfo_In_Gherkin()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "seed", () => 1)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("failing", _ =>
                Expect.That(1, "count").Because("we expect two").With("hint").ToBe(2)
            )
            .AssertFailed();

        var reporter = new StringBddReporter();
        ctx.WriteGherkinTo(reporter);
        var output = reporter.ToString();

        Assert.Contains("Subject: count", output);
        Assert.Contains("Expected:", output);
        Assert.Contains("Actual:", output);
        Assert.Contains("Because: we expect two", output);
        Assert.Contains("Hint: hint", output);
    }
    
    
    [Scenario("Failing expectation includes structured fields in report with FluentAs")]
    [Fact]
    public async Task FailingExpectation_Includes_StructuredInfo_In_Gherkin_FluentAs()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "seed", () => 1)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("failing", _ =>
                Expect.That(1).As("count").Because("we expect two").With("hint").ToBe(2)
            )
            .AssertFailed();

        var reporter = new StringBddReporter();
        ctx.WriteGherkinTo(reporter);
        var output = reporter.ToString();

        Assert.Contains("Subject: count", output);
        Assert.Contains("Expected:", output);
        Assert.Contains("Actual:", output);
        Assert.Contains("Because: we expect two", output);
        Assert.Contains("Hint: hint", output);
    }
}
