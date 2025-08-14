namespace TinyBDD.Tests.Common;

public class FluentTaskExtensionsTests
{
    [Feature("Fluent")]
    private sealed class Host { }

    [Scenario("Single await chain (typed Then)")]
    [Fact]
    public async Task SingleAwait_Typed()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 10)
            .When("add 5", (x, _) => Task.FromResult(x + 5))          // Task<WhenBuilder<T,TOut>>
            .Then("is 15", Equals15) // <- typed Func<T, Task>
            .And("still >= 10", GreaterThanOrEqual10);

        Assert.Collection(ctx.Steps,
            s => Assert.Equal("Given", s.Kind),
            s => Assert.Equal("When", s.Kind),
            s => Assert.Equal("Then", s.Kind),
            s => Assert.Equal("Then", s.Kind));
    }

    private static Task GreaterThanOrEqual10(int v)
    {
        Assert.True(v >= 10);
        return Task.CompletedTask;
    }

    private static Task Equals15(int v)
    {
        Assert.Equal(15, v);
        return Task.CompletedTask;
    }

    [Scenario("Single await chain (untyped Then)")]
    [Fact]
    public async Task SingleAwait_Untyped()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "prepare", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)               // Task<WhenBuilder<T>>
            .Then("ok", _ => Task.CompletedTask)                       // <- untyped Func<Task>
            .And("ok again", _ => Task.CompletedTask);

        Assert.Equal(4, ctx.Steps.Count);
    }

    [Scenario("But overloads compile")]
    [Fact]
    public async Task But_Overloads_Work_For_Typed_And_Untyped()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 2)
            .When("double", (x, _) => Task.FromResult(x * 2))
            .Then("is 4", Equals4)
            .But("not 5", Not5);

        Assert.Equal(4, ctx.Steps.Count);
    }

    private static Task Not5(int v)
    {
        Assert.NotEqual(5, v);
        return Task.CompletedTask;
    }

    private static Task Equals4(int v)
    {
        Assert.Equal(4, v);
        return Task.CompletedTask;
    }
}
