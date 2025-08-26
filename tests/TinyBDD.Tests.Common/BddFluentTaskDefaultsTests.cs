namespace TinyBDD.Tests.Common;

public class BddFluentTaskDefaultsTests
{
    [Feature("Defaults")]
    private sealed class Host { }

    [Scenario("When async action without title (untyped When)")]
    [Fact]
    public async Task When_AsyncAction_DefaultTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 1)
            .When(_ => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("When typed transform with token, default title")]
    [Fact]
    public async Task When_Typed_WithToken_DefaultTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 1)
            .When<int, int>((v, ct) => Task.FromResult(v + 1))
            .Then("== 2", v => v == 2);

        ctx.AssertPassed();
    }

    [Scenario("When typed transform without token, default title")]
    [Fact]
    public async Task When_Typed_NoToken_DefaultTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 2)
            .When<int, int>(v => Task.FromResult(v * 2))
            .Then("== 4", v => v == 4);

        ctx.AssertPassed();
    }

    [Scenario("When side-effect action with token, default title")]
    [Fact]
    public async Task When_SideEffect_Token_DefaultTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "list", () => new List<int>())
            .When((List<int> l, CancellationToken ct) => l.Add(3))
            .Then("has item", l => l.Contains(3));

        ctx.AssertPassed();
    }

    [Scenario("Then after Given: explicit title no-token, default-title token, and default-title no-token")]
    [Fact]
    public async Task Then_After_Given_Variants()
    {
        var ctx = Bdd.CreateContext(new Host());

        // explicit title, no token
        await Bdd.Given(ctx, "start1", () => 10)
            .Then("assert", v => Task.CompletedTask);

        // default title, token-aware
        await Bdd.Given(ctx, "start2", () => 11)
            .Then((int v, CancellationToken ct) => Task.CompletedTask);

        // default title, no token
        await Bdd.Given(ctx, "start3", () => 12)
            .Then(v => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("Then after untyped When: default-title (token + no-token)")]
    [Fact]
    public async Task Then_After_Untyped_When_DefaultTitles()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "prep1", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then(ct => Task.CompletedTask);

        await Bdd.Given(ctx, "prep2", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then(() => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("Then after typed When: default-title (token + no-token)")]
    [Fact]
    public async Task Then_After_Typed_When_DefaultTitles()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 3)
            .When("double", (x, _) => Task.FromResult(x * 2))
            .Then((v, ct) => Task.CompletedTask)
            .And(v => Task.CompletedTask) // also covers typed And default-title no-token
            .But("token assert", (v, ct) => Task.CompletedTask); // covers token assertion on But

        // also cover default-title typed Then no-token
        await Bdd.Given(ctx, "start2", () => 5)
            .When("noop", (x, _) => Task.FromResult(x))
            .Then(v => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("And/But default-title on untyped ThenBuilder")]
    [Fact]
    public async Task And_But_DefaultTitle_On_Untyped_Then()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "prep", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .And(ct => Task.CompletedTask)
            .And(() => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("Predicate defaults: untyped Then (async + sync) with default titles")]
    [Fact]
    public async Task Predicate_Defaults_Untyped_Then()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "prep1", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then(() => Task.FromResult(true));

        await Bdd.Given(ctx, "prep2", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then(() => true);

        ctx.AssertPassed();
    }

    [Scenario("Predicate defaults: typed Then (token-async, async, sync) default titles")]
    [Fact]
    public async Task Predicate_Defaults_Typed_Then()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start1", () => 4)
            .When("id", (x, _) => Task.FromResult(x))
            .Then((v, ct) => Task.FromResult(v == 4));

        await Bdd.Given(ctx, "start2", () => 6)
            .When("id", (x, _) => Task.FromResult(x))
            .Then(v => Task.FromResult(v == 6));

        await Bdd.Given(ctx, "start3", () => 7)
            .When("id", (x, _) => Task.FromResult(x))
            .Then(v => v == 7);

        ctx.AssertPassed();
    }

    [Scenario("Predicate defaults on typed ThenBuilder: And/But default titles (sync/async)")]
    [Fact]
    public async Task Predicate_Defaults_On_Typed_ThenBuilder()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 9)
            .When("id", (x, _) => Task.FromResult(x))
            .Then(">= 9", v => v >= 9)
            .And(v => true) // default title sync predicate
            .But(v => true) // default title sync predicate for But
            .And("async ok", v => Task.FromResult(true))
            .But("async ok", v => Task.FromResult(true));

        ctx.AssertPassed();
    }
}

