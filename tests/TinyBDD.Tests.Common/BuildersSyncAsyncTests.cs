namespace TinyBDD.Tests.Common;

public class BuildersSyncAsyncTests
{
    [Feature("Overloads")]
    private sealed class Host
    {
    }

    [Scenario("Sync overloads flow")]
    [Fact]
    public async Task Sync_Overloads_Flow()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "sync given", () => 3)
            .When("sync when", (x, _) => Task.FromResult(x - 1))
            .Then("sync assert", v =>
            {
                ArgumentOutOfRangeException.ThrowIfNegative(v);
                Assert.Equal(2, v);
                return Task.CompletedTask;
            });

        Assert.Equal(3, ctx.Steps.Count);
    }
}