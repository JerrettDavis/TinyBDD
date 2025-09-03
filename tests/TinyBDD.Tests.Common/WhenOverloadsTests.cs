namespace TinyBDD.Tests.Common;

public class WhenOverloadsTests
{
    [Scenario("When directly after Given with And-chaining for more Whens")]
    [Fact]
    public async Task Given_When_And_Then()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd.Given(ctx, "wire", () => 1)
            .When("act 1", (x, _) => Task.FromResult(x + 1))
            .And("act 2", (x, _) => Task.FromResult(x + 2))
            .And("act 3", (x, _) => Task.FromResult(x + 3))
            .Then("assert", (x, _) => Task.FromResult(x == 7))
            .AssertPassed();
    }

    [Scenario("When directly after Given with And-chaining, and bad assumptions")]
    [Fact]
    public async Task Given_When_And_Then_BadAssumptions()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd
            .Given(ctx, "wire", () => 1)
            .When("act 1", (x, _) => Task.FromResult(x + 1))
            .And("act 2", x => x + 2)
            .And("act 3", (x, _) => Task.FromResult(x + 3))
            .Then("assert", (x, _) => Task.FromResult(x == 8))
            .AssertFailed();
    }

    [Scenario("When directly after Given with And-but-chaining.")]
    [Fact]
    public async Task Given_When_And_But_Then()
    {
        var ctx = Bdd.CreateContext(this);

        await Bdd
            .Given(ctx, "wire", () => 1)
            .When("act 1", (x, _) => Task.FromResult(x + 1))
            .And("act 2", (x, _) => Task.FromResult(x + 2))
            .But("act 3", (x, _) => Task.FromResult(x + 3))
            .But("act 4", (x, _) => Task.FromResult(x + 4))
            .Then("assert", (x, _) => Task.FromResult(x == 11))
            .AssertPassed();
    }
}