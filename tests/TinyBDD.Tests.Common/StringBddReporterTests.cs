namespace TinyBDD.Tests.Common;

public class StringBddReporterTests
{
    [Feature("ReportFeature", "desc")]
    private sealed class Host
    {
    }

    [Scenario("ReportScenario")]
    [Fact]
    public async Task Emits_Compact_Report()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", Is2);

        var r = new StringBddReporter();
        r.WriteLine($"Feature: {ctx.FeatureName}");
        if (!string.IsNullOrWhiteSpace(ctx.FeatureDescription))
            r.WriteLine($"  {ctx.FeatureDescription}");
        r.WriteLine($"Scenario: {ctx.ScenarioName}");
        foreach (var s in ctx.Steps)
        {
            var status = s.Error is null ? "OK" : "FAIL";
            r.WriteLine($"  {s.Kind} {s.Title} [{status}]");
        }

        var output = r.ToString();
        Assert.Contains("Feature: ReportFeature", output);
        Assert.Contains("Scenario: ReportScenario", output);
        Assert.Contains("Given wire", output);
        Assert.Contains("When act", output);
        Assert.Contains("Then assert", output);
        return;

        Task Is2(int v)
        {
            Assert.Equal(2, v);
            return Task.CompletedTask;
        }
    }
}