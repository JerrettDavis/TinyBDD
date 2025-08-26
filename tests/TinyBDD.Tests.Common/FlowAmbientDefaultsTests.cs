namespace TinyBDD.Tests.Common;

public class FlowAmbientDefaultsTests
{
    [Feature("FlowAmbient")]
    private sealed class Host { }

    [Scenario("Ambient Flow.Given(Func<T>) without title")]
    [Fact]
    public async Task Ambient_Given_NoTitle_Sync()
    {
        var ctx = Bdd.CreateContext(new Host());
        var prev = Ambient.Current.Value;
        Ambient.Current.Value = ctx;
        try
        {
            await Flow.Given(() => 1)
                .When("id", x => x)
                .Then("== 1", v => v == 1);
            ctx.AssertPassed();
        }
        finally { Ambient.Current.Value = prev; }
    }

    [Scenario("Ambient Flow.Given(Func<CancellationToken,Task<T>>) without title")]
    [Fact]
    public async Task Ambient_Given_NoTitle_Async()
    {
        var ctx = Bdd.CreateContext(new Host());
        var prev = Ambient.Current.Value;
        Ambient.Current.Value = ctx;
        try
        {
            await Flow.Given(_ => Task.FromResult(2))
                .When("id", x => x)
                .Then("== 2", v => v == 2);
            ctx.AssertPassed();
        }
        finally { Ambient.Current.Value = prev; }
    }

    [Scenario("From(ctx).Given(Func<T>) without title")]
    [Fact]
    public async Task FromCtx_Given_NoTitle_Sync()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Flow.From(ctx)
            .Given(() => 3)
            .When("id", x => x)
            .Then("== 3", v => v == 3);

        ctx.AssertPassed();
    }

    [Scenario("From(ctx).Given(Func<CancellationToken,Task<T>>) without title")]
    [Fact]
    public async Task FromCtx_Given_NoTitle_Async()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Flow.From(ctx)
            .Given(_ => Task.FromResult(4))
            .When("id", x => x)
            .Then("== 4", v => v == 4);

        ctx.AssertPassed();
    }
}

