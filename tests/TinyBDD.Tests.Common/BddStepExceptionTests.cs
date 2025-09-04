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

        var exception = await Assert.ThrowsAsync<BddStepException>(async () =>
            await Bdd.Given(ctx, "Wire-up", () => 1)
                .When("Explode", (_, _) => Task.FromException(boom))
                .Then("Unreached", _ => Task.CompletedTask));
        
        Assert.Same(boom, exception.InnerException);
        Assert.Same(exception.Context, ctx);

        var lastUnerrored = ctx.Steps.Last(x => x.Error != null);
        Assert.Equal("When", lastUnerrored.Kind);
        Assert.Equal("Explode", lastUnerrored.Title);
        Assert.NotNull(lastUnerrored.Error);
        
        var lastItem = ctx.Steps.Last();
        Assert.Equal("When", lastItem.Kind);
        Assert.Equal("Explode", lastItem.Title);
        Assert.NotNull(lastItem.Error);
    }

    [Scenario("Exception wrapping with continue on error")]
    [Fact]
    public async Task Wraps_Inner_Exception_And_Logs_Failed_Step_On_ContinueOnError()
    {
        var host = new Host();
        var ctx = Bdd.CreateContext(host, options: new ScenarioOptions
        {
            ContinueOnError = true
        });

        var boom = new InvalidOperationException("boom!");

        await Bdd.Given(ctx, "Wire-up", () => 1)
            .When("Explode", (_, _) => Task.FromException(boom))
            .Then("Reached", _ => Task.CompletedTask);

        var lastUnerrored = ctx.Steps.Last(x => x.Error != null);
        Assert.Equal("When", lastUnerrored.Kind);
        Assert.Equal("Explode", lastUnerrored.Title);
        Assert.NotNull(lastUnerrored.Error);
        
        var lastItem = ctx.Steps.Last();
        Assert.Equal("Then", lastItem.Kind);
        Assert.Equal("Reached", lastItem.Title);
        Assert.Null(lastItem.Error);
    }
}