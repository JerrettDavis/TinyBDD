namespace TinyBDD.Tests.Common;

[Feature("Tagged feature")]
[Tag("class-tag-1")]
[Tag("class-tag-2")]
public class TagAttributeTests
{
    [Scenario("Scenario with tags", "scenario-tag-1", "scenario-tag-2")]
    [Tag("method-tag")]
    [Fact]
    public async Task Aggregates_All_Tag_Sources()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd.Given(ctx, "noop", () => 0)
            .When("noop", (x, _) => Task.FromResult(x))
            .Then("ok", _ => Task.CompletedTask);

        var tags = ctx.Tags.ToHashSet();
        Assert.True(new[] { "class-tag-1", "class-tag-2", "method-tag", "scenario-tag-1", "scenario-tag-2" }
            .All(tags.Contains));
    }
}