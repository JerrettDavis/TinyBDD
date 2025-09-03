namespace TinyBDD.Tests.Common;

public class BddGivenOverloadsTests
{
    [Feature("GivenOverloads")]
    private sealed class Host
    {
    }

    [Scenario("Given(ctx, Func<T>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_Sync()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, () => 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();

        Assert.Equal("Given Int32", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Func<CancellationToken,Task<T>>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_Async()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, _ => Task.FromResult("abc"))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();

        Assert.Equal("Given String", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, string, Func<T>) with title executes")]
    [Fact]
    public async Task Given_WithTitle_Sync()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () => 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();

        Assert.Equal("start", ctx.Steps[0].Title);
    }
    
    [Scenario("Given(ctx, Func<CancellationToken, ValueTask<T>>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_ValueTask()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, _ => new ValueTask<string>("abc"))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();
        
        Assert.Equal("Given String", ctx.Steps[0].Title);
    }
}