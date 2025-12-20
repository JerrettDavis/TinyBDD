using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

public class BddGivenOverloadsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Feature("GivenOverloads")]
    private sealed class Host
    {
    }

    // -------- No-seed factories: WITH title --------
    [Scenario("Given(ctx, string, Func<T>) with title executes")]
    [Fact]
    public async Task Given_WithTitle_Sync()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () => 5)
            .When("noop", NoopAsync)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();

        Assert.Equal("start", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, string, Func<Task<T>>) with title executes")]
    [Fact]
    public async Task Given_WithTitle_Task()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () => Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<ValueTask<T>>) with title executes")]
    [Fact]
    public async Task Given_WithTitle_ValueTask()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () => new ValueTask<int>(5))
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<CancellationToken,Task<T>>) with title executes")]
    [Fact]
    public async Task Given_WithTitle_AsyncCT()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", _ => Task.FromResult("abc"))
            .When("noop", NoopAsync)
            .Then("ok", IsAbc)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<CancellationToken,ValueTask<T>>) with title executes")]
    [Fact]
    public async Task Given_WithTitle_ValueTaskCT()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", _ => new ValueTask<string>("abc"))
            .When("noop", NoopAsync)
            .Then("ok", IsAbc)
            .AssertPassed();
    }

    // -------- No-seed factories: NO title (auto) --------
    [Scenario("Given(ctx, Func<T>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_Sync()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, () => 5)
            .When("noop", NoopAsync)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();

        Assert.Equal("Given Int32", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Func<Task<T>>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_Task()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, () => Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
        Assert.Equal("Given Int32", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Func<ValueTask<T>>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_ValueTask()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, () => new ValueTask<string>("abc"))
            .When("noop", NoopAsync)
            .Then("ok", IsAbc)
            .AssertPassed();
        Assert.Equal("Given String", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Func<CancellationToken,Task<T>>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_Async()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, _ => Task.FromResult("abc"))
            .When("noop", NoopAsync)
            .Then("ok", IsAbc)
            .AssertPassed();
        Assert.Equal("Given String", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Func<CancellationToken,ValueTask<T>>) without title uses default and executes")]
    [Fact]
    public async Task Given_NoTitle_ValueTaskCT()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, _ => new ValueTask<string>("abc"))
            .When("noop", NoopAsync)
            .Then("ok", IsAbc)
            .AssertPassed();
        Assert.Equal("Given String", ctx.Steps[0].Title);
    }

    // -------- Seed value (Action + seed) --------
    [Scenario("Given(ctx, string, Action, T) with title executes and returns seed")]
    [Fact]
    public async Task Given_Title_Action_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", () =>
            {
                /* side-effect */
            }, 5)
            .When("noop", NoopAsync)
            .Then("ok", x => x == 5)
            .AssertPassed();
        Assert.Equal("start", ctx.Steps[0].Title);
    }

    [Scenario("Given(ctx, Action, T) without title executes and returns seed")]
    [Fact]
    public async Task Given_Action_NoTitle_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, () => { }, 5)
            .When("noop", NoopAsync)
            .Then("ok", x => x == 5)
            .AssertPassed();
    }

    // -------- Transform with seed (TIn -> TOut) --------
    [Scenario("Given(ctx, string, Func<TIn,TOut>, TIn) with title executes")]
    [Fact]
    public async Task Given_Title_Transform_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", x => x * 2, 5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn,TOut>, TIn) without title executes")]
    [Fact]
    public async Task Given_NoTitle_Transform_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, x => x * 2, 5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    // -------- Transform with seed (TIn,CT -> Task<TOut>) --------
    [Scenario("Given(ctx, string, Func<TIn,CT,Task<TOut>>, TIn) with title executes")]
    [Fact]
    public async Task Given_Title_TransformAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (x, _) => Task.FromResult(x * 2), 5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn,CT,Task<TOut>>, TIn) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (x, _) => Task.FromResult(x * 2), 5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    // -------- Transform with seed (TIn,CT -> ValueTask<TOut>) --------
    [Scenario("Given(ctx, string, Func<TIn,CT,ValueTask<TOut>>, TIn) with title executes")]
    [Fact]
    public async Task Given_Title_TransformValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, "start", (x, _) => new ValueTask<int>(x * 2), 5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn,CT,ValueTask<TOut>>, TIn) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, (x, _) => new ValueTask<int>(x * 2), 5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    // -------- Transform with seed (T,CT -> ValueTask<T>)  **ambiguous Wrap<T> case** --------
    [Scenario("Given(ctx, string, Func<T,CT,ValueTask<T>>, T) with title executes (disambiguates Wrap<T>)")]
    [Fact]
    public async Task Given_Title_TransformKeepsT_ValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, "start", (x, _) => new ValueTask<int>(x + 1), 5)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 6)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<T,CT,ValueTask<T>>, T) without title executes (disambiguates Wrap<T>)")]
    [Fact]
    public async Task Given_NoTitle_TransformKeepsT_ValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, (x, _) => new ValueTask<int>(x + 1), 5)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 6)
            .AssertPassed();
    }

    // -------- Side-effects with seed (CT -> ValueTask / Task) keep T --------
    [Scenario("Given(ctx, string, Func<CT,ValueTask>, T) with title executes")]
    [Fact]
    public async Task Given_Title_ActionValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, "start", _ => new ValueTask(), 5)
            .When("noop", NoopAsync)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<CT,Task>, T) with title executes")]
    [Fact]
    public async Task Given_Title_ActionAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", ct => NoopAsync(0, ct), 10)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<CT,ValueTask>, T) without title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int>(ctx, _ => new ValueTask(), 10)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<CT,Task>, T) without title executes")]
    [Fact]
    public async Task Given_NoTitle_ActionAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, ct => NoopAsync(0, ct), 10)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 10)
            .AssertPassed();
    }

    // -------- Task<TIn> seed group --------
    [Scenario("Given(ctx, string, Func<Task<TIn>,TOut>, Task<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", t => t.Result * 2, Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>,TOut>, Task<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, t => t.Result * 2, Async5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>,CT,TOut>, Task<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformTask_SyncReturn_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (t, _) => t.Result * 2, Async5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>,CT,TOut>, Task<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformTask_SyncReturn_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (t, _) => t.Result * 2, Async5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>,CT,Task<TOut>>, Task<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (t, _) => Task.FromResult(t.Result * 2), Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>,CT,Task<TOut>>, Task<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, DoubleAsync, Async5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<TIn>,CT,ValueTask<TOut>>, Task<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (t, _) => new ValueTask<int>(t.Result * 2), Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<TIn>,CT,ValueTask<TOut>>, Task<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, TransformInputTaskToAValueTask, Async5())
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<T>,CT,ValueTask>, Task<T>) without title executes (side-effect)")]
    [Fact]
    public async Task Given_NoTitle_ActionTaskValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, Noop, Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<Task<T>,CT,Task>, Task<T>) without title executes (side-effect)")]
    [Fact]
    public async Task Given_NoTitle_ActionTaskAsync_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, NoopAsync, Async5())
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
    }

    // -------- ValueTask<TIn> seed group --------
    [Scenario("Given(ctx, string, Func<ValueTask<TIn>,TOut>, ValueTask<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformValueTaskToNon_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", t => t.Result * 2, new ValueTask<int>(5))
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<ValueTask<TIn>,TOut>, ValueTask<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformValueTaskToNon_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, t => Double(Unwrap(t)), ValueTask5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<ValueTask<TIn>,CT,TOut>, ValueTask<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformValueTask_SyncReturn_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (vt, _) => Unwrap(vt) * 2, ValueTask5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<ValueTask<TIn>,CT,TOut>, ValueTask<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformValueTask_SyncReturn_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (vt, _) => Unwrap(vt) * 2, ValueTask5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<ValueTask<TIn>,CT,ValueTask<TOut>>, ValueTask<TIn>) with title executes")]
    [Fact]
    public async Task Given_Title_TransformValueTask_ValueTaskReturn_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start", (vt, _) => new ValueTask<int>(Unwrap(vt) * 2), ValueTask5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<ValueTask<TIn>,CT,ValueTask<TOut>>, ValueTask<TIn>) without title executes")]
    [Fact]
    public async Task Given_NoTitle_TransformValueTask_ValueTaskReturn_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, (vt, _) => new ValueTask<int>(Unwrap(vt) * 2), ValueTask5())
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<TIn,CT,ValueTask<TOut>>, TIn) returns transformed result")]
    [Fact]
    public async Task Given_Title_TInToTOut_ValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int, string>(ctx, "start",
                (x, _) => new ValueTask<string>((x * 2).ToString()),
                5)
            .When("noop", NoopAsync)
            .Then("ok", s => s == "10")
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<T,CT,ValueTask>, T) side-effect keeps seed")]
    [Fact]
    public async Task Given_Title_T_ValueTask_SideEffect_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start",
                (_, _) => new ValueTask(),
                5)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<TIn,CT,ValueTask<TOut>>, TIn) returns transformed result (no title)")]
    [Fact]
    public async Task Given_NoTitle_TInToTOut_ValueTask_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given<int, int>(ctx,
                (x, _) => new ValueTask<int>(x * 2),
                5)
            .When("noop", NoopAsync)
            .Then("ok", Is10)
            .AssertPassed();
    }

    [Scenario("Given(ctx, Func<T,CT,ValueTask>, T) side-effect keeps seed (no title)")]
    [Fact]
    public async Task Given_NoTitle_T_ValueTask_SideEffect_WithSeed()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx,
                (_, _) => new ValueTask(),
                5)
            .When("noop", NoopAsync)
            .Then("ok", v => v == 5)
            .AssertPassed();
    }

    [Scenario("Given(ctx, string, Func<Task<T>,CT,Task>, Task<T>) side-effect keeps seed")]
    [Fact]
    public async Task Given_Title_TaskSeed_SideEffect_Task()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start",
                (_, _) => Task.CompletedTask,
                Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
    }
    
    
    [Scenario("Given(ctx, string, Func<Task<T>,CT,ValueTask>, Task<T>) side-effect keeps seed")]
    [Fact]
    public async Task Given_Title_TaskSeed_SideEffect_ValueTask()
    {
        var ctx = Bdd.CreateContext(new Host());
        await Bdd.Given(ctx, "start",
                (_, _) => new ValueTask(),
                Task.FromResult(5))
            .When("noop", NoopAsync)
            .Then("ok", Is5)
            .AssertPassed();
    }

    // -------- Little helpers --------
    private static T Unwrap<T>(ValueTask<T> v) => v.IsCompletedSuccessfully ? v.Result : throw new Exception();
    private static int Double(int i) => i * 2;
    private static Task<int> DoubleAsync(Task<int> i, CancellationToken ct) => i.ContinueWith(t => t.Result * 2, ct);

    private static ValueTask<T> TransformInputTaskToAValueTask<T>(Task<T> input, CancellationToken ct) => new(input);
    private static Task<int> Async5() => Task.FromResult(5);
    private static ValueTask<int> ValueTask5() => new(5);

    private static ValueTask Noop<T>(T _, CancellationToken __ = default) => new();
    private static Task NoopAsync<T>(T _, CancellationToken __ = default) => Task.CompletedTask;

    private static bool IsAbc(string s) => s == "abc";
    private static bool Is5(int i) => i == 5;
    private static bool Is10(int i) => i == 10;
}