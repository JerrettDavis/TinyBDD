namespace TinyBDD.Tests.Common;

public class FluentOverloadsCoverageTests
{
    [Feature("Coverage")]
    private sealed class Host { }

    [Scenario("Untyped Then/And/But Func<Task> overloads")]
    [Fact]
    public async Task Untyped_Then_And_But_FuncTask_Overloads()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "prepare", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .And("ok too", () => Task.CompletedTask)
            .But("still ok", () => Task.CompletedTask)
            .AssertPassed();

        Assert.Equal(5, ctx.Steps.Count);
    }

    [Scenario("When sync transform without token (default title)")]
    [Fact]
    public async Task When_Sync_NoToken_Default_Title()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 10)
            .When(x => x - 3)
            .Then("== 7", v => v == 7)
            .AssertPassed();
    }

    [Scenario("When async action with title (no token)")]
    [Fact]
    public async Task When_ActionAsync_Title_NoToken()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 1)
            .When("act async", _ => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();
    }

    [Scenario("When async action with token (default title)")]
    [Fact]
    public async Task When_ActionAsync_WithToken_DefaultTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 1)
            .When((_, _) => Task.CompletedTask)
            .Then("ok", () => Task.CompletedTask)
            .AssertPassed();
    }

    [Scenario("When side-effect Action<T> with title")]
    [Fact]
    public async Task When_SideEffect_Action_NoToken_WithTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "list", () => new List<int>())
            .When("add item", l => l.Add(42))
            .Then("contains 42", l => l.Contains(42))
            .AssertPassed();
    }

    [Scenario("When side-effect Action<T> default title")]
    [Fact]
    public async Task When_SideEffect_Action_NoToken_DefaultTitle()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "list2", () => new List<int>())
            .When(l => l.Add(7))
            .Then("count is 1", l => l.Count == 1)
            .AssertPassed();
    }

    [Scenario("Direct Then transform after Given (alias) with and without token")]
    [Fact]
    public async Task Given_Then_Transform_Alias_Variants()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 3)
            .Then("triple", v => Task.FromResult(v * 3))
            .And("== 9", v => v == 9)
            .AssertPassed();

        await Bdd.Given(ctx, "start2", () => 4)
            .Then("plus 1", (v, _) => Task.FromResult(v + 1))
            .And("== 5", v => v == 5)
            .AssertPassed();
    }

    [Scenario("Untyped Then with synchronous predicate bool")]
    [Fact]
    public async Task Untyped_Then_SyncPredicateBool()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("always true", () => true)
            .AssertPassed();
    }

    [Scenario("Then directly after typed When with async bool predicate")]
    [Fact]
    public async Task And_After_Typed_When_AsyncBool()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 2)
            .When("double", (x, _) => Task.FromResult(x * 2))
            .Then("== 4 (async)", v => Task.FromResult(v == 4))
            .AssertPassed();
    }

    [Scenario("Untyped Then with async bool predicate")]
    [Fact]
    public async Task Untyped_Then_AsyncPredicateBool()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("always true async", () => Task.FromResult(true))
            .AssertPassed();
    }
}
