namespace TinyBDD.Tests.Common;

[Feature("Orders", "As a customer I can place an order")]
[Tag("feature-tag")]
public class ScenarioNameAndTagsTests
{
    [Scenario("Explicit scenario name", "method-tag-1", "method-tag-2")]
    [Tag("method-tag-attr")]
    [Fact]
    public async Task Uses_Explicit_Scenario_Name_And_Aggregates_Tags()
    {
        // explicit name wins
        var ctx = Bdd.CreateContext(this);

        await Bdd.Given(ctx, "Wireup", () => 1)
            .When("Inc", (x, _) => Task.FromResult(x + 1))
            .Then("Is 2", Is2);

        Assert.Equal("Orders", ctx.FeatureName);
        Assert.Equal("As a customer I can place an order", ctx.FeatureDescription);
        Assert.Equal("Explicit scenario name", ctx.ScenarioName);

        // Tags: class [Tag], method [Tag], ScenarioAttribute tags
        var tags = ctx.Tags.ToArray();
        Assert.Contains("feature-tag", tags);
        Assert.Contains("method-tag-attr", tags);
        Assert.Contains("method-tag-1", tags);
        Assert.Contains("method-tag-2", tags);
        return;

        // do a tiny scenario so we have steps for reporting
        Task Is2(int res)
        {
            Assert.Equal(2, res);
            return Task.CompletedTask;
        }
    }

    [Scenario] // no explicit name => falls back to method name
    [Fact]
    public void Uses_Method_Name_When_Scenario_Name_Omitted()
    {
        var ctx = Bdd.CreateContext(this);
        Assert.Equal(nameof(Uses_Method_Name_When_Scenario_Name_Omitted), ctx.ScenarioName);
    }

    [Fact]
    public void Feature_Fallback_To_ClassName_When_Attribute_Missing()
    {
        var ctx = Bdd.CreateContext(new NoFeatureAttr(), scenarioName: "X");
        Assert.Equal(nameof(NoFeatureAttr), ctx.FeatureName);
        Assert.Null(ctx.FeatureDescription);
    }

    private sealed class NoFeatureAttr
    {
    }
}