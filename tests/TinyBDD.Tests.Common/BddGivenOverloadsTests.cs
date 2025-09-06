using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

public class BddGivenOverloadsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Feature("GivenOverloads")]
    private sealed class Host
    {
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



    [Scenario("Given(ctx, string, Action, T) with title executes and returns seed")]
    [Fact]
    public async Task Given_Title_Action_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () =>
            {
                /* side effect */
            }, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();

        Assert.Equal("start", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, string, Func<TIn, TOut>, TIn) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_Transform_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", x => x * 2, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<TIn, CancellationToken, Task<TOut>>, TIn) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (x, _) => Task.FromResult(x * 2), 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<TIn, CancellationToken, ValueTask<TOut>>, TIn) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (x, _) => new ValueTask<int>(x * 2), 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<TIn, CancellationToken, ValueTask>, TIn) with title executes")]
    [Fact]
    public async Task Given_Title_ActionValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (_, _) => new ValueTask(), 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<CancellationToken, ValueTask>, TIn) with title executes")]
    [Fact]
    public async Task Given_Title_ActionValueTask_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, "start", _ => new ValueTask(), 10)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<CancellationToken, Task>, TIn) with title executes")]
    [Fact]
    public async Task Given_Title_ActionAsync_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, "start", _ => Task.CompletedTask, 10)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, TOut>, Task<TIn>) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", t => t.Result * 2, Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, Task<TOut>>, Task<TIn>) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (t, _) => Task.FromResult(t.Result * 2), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, ValueTask<TOut>>, Task<TIn>) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (t, _) => new ValueTask<int>(t.Result * 2), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, ValueTask>, Task<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_ActionTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (_, _) => new ValueTask(), Task.FromResult(5))
            .When("noop", _ => Task.CompletedTask)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, Task>, Task<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_ActionTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (_, _) => Task.CompletedTask, Task.FromResult(5))
            .When("noop", _ => Task.CompletedTask)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }
    
    [Scenario("Given(ctx, string, Func<TIn, TOut>, TIn) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_Transform_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", x => x * 2, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }
    
    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, TOut>, ValueTask<TIn>) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformTaskValueTask_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (_, _) => new ValueTask<int>(5), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();   
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, TOut>, Task<TIn>) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformTaskAsync_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (_, _) => Task.FromResult(5), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();   
    }

    [Scenario("Given(ctx, string, Func<ValueTask<TIn>, TOut>, ValueTask<TIn>) with title executes and returns Func result")]
    [Fact]
    public async Task Given_Title_TransformValueTaskToNon_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", t => t.Result * 2, new ValueTask<int>(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }
    
    
    [Scenario("Given(ctx, string, Action, t) with title executes")]
    [Fact]
    public async Task Given_Action_NoTitle_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, () => {}, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();
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

    [Scenario("Given(ctx, Action, T) with no title executes and returns seed")]
    [Fact]
    public async Task Given_NoTitle_Action_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () =>
            {
                /* side effect */
            }, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();

        Assert.Equal("start", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Func<TIn, TOut>, TIn) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_Transform_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, x => x * 2, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn, CancellationToken, Task<TOut>>, TIn) with title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (x, _) => Task.FromResult(x * 2), 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn, CancellationToken, ValueTask<TOut>>, TIn) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (x, _) => new ValueTask<int>(x * 2), 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn, CancellationToken, ValueTask>, TIn) with no title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (_, _) => new ValueTask(), 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<CancellationToken, ValueTask>, TIn) with no title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionValueTask_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, _ => new ValueTask(), 10)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<CancellationToken, Task>, TIn) with no title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionAsync_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, _ => Task.CompletedTask, 10)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>, TOut>, Task<TIn>) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, t => t.Result * 2, Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>, CancellationToken, Task<TOut>>, Task<TIn>) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (t, _) => Task.FromResult(t.Result * 2), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>, CancellationToken, ValueTask<TOut>>, Task<TIn>) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (t, _) => new ValueTask<int>(t.Result * 2), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, ValueTask>, Task<TIn>) with no title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (_, _) => new ValueTask(), Task.FromResult(5))
            .When("noop", _ => Task.CompletedTask)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, Task>, Task<TIn>) with no title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (_, _) => Task.CompletedTask, Task.FromResult(5))
            .When("noop", _ => Task.CompletedTask)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }
    
    [Scenario("Given(ctx, string, Func<TIn, TOut>, TIn) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_Transform_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, x => x * 2, 5)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }
    
    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, TOut>, ValueTask<TIn>) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformTaskValueTask_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (_, _) => new ValueTask<int>(5), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();   
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>, CancellationToken, TOut>, Task<TIn>) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformTaskAsync_NoSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (_, _) => Task.FromResult(5), Task.FromResult(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 5)
            .AssertPassed();   
    }

    [Scenario("Given(ctx, string, Func<ValueTask<TIn>, TOut>, ValueTask<TIn>) with no title executes and returns Func result")]
    [Fact]
    public async Task Given_NoTitle_TransformValueTaskToNon_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, t => t.Result * 2, new ValueTask<int>(5))
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", x => x == 10)
            .AssertPassed();
    }
}