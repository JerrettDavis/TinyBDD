namespace TinyBDD.Tests.Common;

public class ScenarioContextGherkinExtensionsTests
{
    [Feature("Gherkin", "desc here")] private sealed class Host {}

    [Scenario("Writes formatted gherkin via extension")]
    [Fact]
    public async Task WriteGherkinTo_Writes_Expected_Lines()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", _ => Task.CompletedTask);

        var reporter = new StringBddReporter();
        ctx.WriteGherkinTo(reporter);
        var output = reporter.ToString();

        Assert.Contains("Feature: Gherkin", output);
        Assert.Contains("desc here", output);
        Assert.Contains("Scenario:", output);
        Assert.Contains("Given wire", output);
        Assert.Contains("When act", output);
        Assert.Contains("Then assert", output);
        Assert.Contains("[OK]", output);
        Assert.Contains(" ms", output);
    }
}

