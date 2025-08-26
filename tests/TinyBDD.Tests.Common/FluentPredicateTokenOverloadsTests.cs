namespace TinyBDD.Tests.Common;

public class FluentPredicateTokenOverloadsTests
{
    [Feature("TokenOverloads")] private sealed class Host {}

    [Scenario("Untyped Then/And/But with CancellationToken actions")]
    [Fact]
    public async Task Untyped_Token_Actions()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "prep", () => 0)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("ok-token", _ => Task.CompletedTask)
            .And("and-token", _ => Task.CompletedTask)
            .But("but-token", _ => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("Untyped Then with CancellationToken boolean predicate (title + default)")]
    [Fact]
    public async Task Untyped_Token_Bool_Predicate_Variants()
    {
        var ctx = Bdd.CreateContext(new Host());

        // Title variant
        await Bdd.Given(ctx, "start", () => 1)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then("pred-token", _ => Task.FromResult(true));

        // Default title variant
        await Bdd.Given(ctx, "start2", () => 1)
            .When("noop", (_, _) => Task.CompletedTask)
            .Then(_ => Task.FromResult(true));

        ctx.AssertPassed();
    }

    [Scenario("Typed ThenBuilder And/But with token-aware boolean predicates")]
    [Fact]
    public async Task Typed_Then_And_But_Token_Bool()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 2)
            .When("double", x => x * 2)
            .Then(">= 4", v => v >= 4)
            .And("<= 4 token", (v, _) => Task.FromResult(v <= 4))
            .But("!= 5 token", (v, _) => Task.FromResult(v != 5));

        ctx.AssertPassed();
    }
}

