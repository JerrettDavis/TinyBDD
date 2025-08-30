namespace TinyBDD.Tests.Common;

public class BddStepExceptionTests
{
    [Feature("Error feature")]
    private sealed class Host
    {
    }

    [Scenario("Exception wrapping")]
    [Fact]
    public async Task Wraps_Inner_Exception_And_Logs_Failed_Step()
    {
        var host = new Host();
        var ctx = Bdd.CreateContext(host);

        var boom = new InvalidOperationException("boom!");


        await Bdd.Given(ctx, "Wire-up", () => 1)
            .When("Explode", (_, _) => Task.FromException(boom))
            .Then("Unreached", _ => Task.CompletedTask);

        var last = ctx.Steps.Last(x => x.Error != null);
        Assert.Equal("When", last.Kind);
        Assert.Equal("Explode", last.Title);
        Assert.NotNull(last.Error);
    }
}