using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("When And/But permutations", "Covers chaining And/But after typed When and after Then (typed and untyped)")]
public class WhenAndButPermutationsTests(ITestOutputHelper output) :
    TinyBddXunitBase(output)
{
    // --- And/But after typed When transforms ---

    [Scenario("When(title) -> And(title) transform -> Then typed predicate")]
    [Fact]
    public async Task When_And_Then_Typed()
        => await Given("wire", () => 1)
            .When("+1", x => x + 1)
            .And("+2", x => x + 2)
            .Then("== 4", v => v == 4)
            .AssertPassed();

    [Scenario("When(title) -> But(title) transform -> Then typed predicate")]
    [Fact]
    public async Task When_But_Then_Typed()
        => await Given("wire", () => 1)
            .When("+1", x => x + 1)
            .But("+3", x => x + 3)
            .Then("== 5", v => v == 5)
            .AssertPassed();

    [Scenario("When -> And -> But -> Then (mixed transforms)")]
    [Fact]
    public async Task When_And_But_Then_Typed()
        => await Given("wire", () => 1)
            .When("+1", x => x + 1) // 2
            .And("x2", x => x * 2)  // 4
            .But("-1", x => x - 1)  // 3
            .Then("== 3", v => v == 3)
            .AssertPassed();

    [Scenario("When -> But -> And -> Then (mixed transforms)")]
    [Fact]
    public async Task When_But_And_Then_Typed()
        => await Given("wire", () => 2)
            .When("+2", x => x + 2) // 4
            .But("/2", x => x / 2)  // 2
            .And("+5", x => x + 5)  // 7
            .Then(">= 7", v => v >= 7)
            .AssertPassed();

    [Scenario("When(title) -> And(title) async(token) transform -> Then typed predicate")]
    [Fact]
    public async Task When_And_AsyncToken_Then_Typed()
        => await Given("wire", () => 1)
            .When("+1", (x, _) => Task.FromResult(x + 1)) // 2
            .And("x3", (x, _) => Task.FromResult(x * 3))  // 6
            .Then("== 6", v => v == 6)
            .AssertPassed();

    [Scenario("When(title) -> But(title) async(token) transform -> Then typed predicate")]
    [Fact]
    public async Task When_But_AsyncToken_Then_Typed()
        => await Given("wire", () => 3)
            .When("+1", (x, _) => Task.FromResult(x + 1)) // 4
            .But("-2", (x, _) => Task.FromResult(x - 2))  // 2
            .Then("== 2", v => v == 2)
            .AssertPassed();

    // --- And/But after Then (typed) assertions ---

    [Scenario("When -> Then typed -> And -> But (predicate assertions)")]
    [Fact]
    public async Task ThenTyped_And_But()
        => await Given("wire", () => 5)
            .When("identity", x => x)
            .Then("> 0", v => v > 0)
            .And("<= 10", v => v <= 10)
            .But("!= 6", v => v != 6)
            .And(">= 5", v => v >= 5)
            .AssertPassed();

    // --- And/But after Then (untyped) assertions ---

    [Scenario("When(untyped) -> Then -> And -> But (assertion chain)")]
    [Fact]
    public async Task WhenUntyped_Then_And_But()
        => await Given("wire", () => 1)
            .When("side-effect", (_, _) => Task.CompletedTask)
            .Then("no-op assert", _ => Task.CompletedTask)
            .And("still ok", _ => Task.CompletedTask)
            .But("also ok", _ => Task.CompletedTask)
            .AssertPassed();

    [Scenario("When(no title) -> And(no title) transform -> Then typed predicate")]
    [Fact]
    public async Task When_DefaultTitle_And_Then_Typed()
        => await Given("wire", () => 1)
            .When(x => x + 1) // 2
            .And(x => x + 3)  // 5
            .Then(v => v == 5)
            .AssertPassed();

    [Scenario("When(no title) -> But(no title) transform -> Then typed predicate")]
    [Fact]
    public async Task When_DefaultTitle_But_Then_Typed()
        => await Given("wire", () => 2)
            .When(x => x * 2) // 4
            .But(x => x - 1)  // 3
            .Then(v => v == 3)
            .AssertPassed();
}