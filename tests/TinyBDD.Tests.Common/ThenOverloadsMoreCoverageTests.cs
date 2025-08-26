using System.Diagnostics.CodeAnalysis;

namespace TinyBDD.Tests.Common;

[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
public class ThenOverloadsMoreCoverageTests
{
    [Feature("ThenOverloads")] private sealed class Host {}

    [Scenario("Then directly after Given with token-aware assertion")]
    [Fact]
    public async Task Given_Then_Assertion_With_Token()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 42)
            .Then("equals 42 (token)", (v, ct) =>
            {
                Assert.Equal(42, v);
                Assert.Equal(CancellationToken.None, ct);
                return Task.CompletedTask;
            })
            .And("still 42", _ => Task.CompletedTask);

        ctx.AssertPassed();
    }

    [Scenario("Typed When->Then with token-aware assertion")]
    [Fact]
    public async Task Typed_When_Then_With_Token()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "start", () => 5)
            .When("double", (x, _) => Task.FromResult(x * 2))
            .Then("is 10 (token)", (v, ct) =>
            {
                Assert.Equal(10, v);
                Assert.Equal(CancellationToken.None, ct);
                return Task.CompletedTask;
            });

        ctx.AssertPassed();
    }
}

