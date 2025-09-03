namespace TinyBDD.Tests.Common;

public class ThenOverloadsTests
{
    [Scenario("Predicate passes")]
    [Fact]
    public async Task Then_Predicate_Passes()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd.Given(ctx, "wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("is two", v => Task.FromResult(v == 2))
            .AssertPassed();
    }

    [Scenario("Predicate fails")]
    [Fact]
    public async Task Then_Predicate_Fails()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd.Given(ctx, "wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("is three", v => Task.FromResult(v == 3))
            .AssertFailed();
    }
}