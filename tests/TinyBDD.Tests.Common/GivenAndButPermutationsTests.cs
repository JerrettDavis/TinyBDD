using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("Given And/But permutations", "Covers chaining And/But around Given and When, then asserting with Then")]
public class GivenAndButPermutationsTests(
    ITestOutputHelper output
) :
    TinyBddXunitBase(output)
{
    [Scenario("Given -> And -> When -> Then (sync transforms)")]
    [Fact]
    public async Task Given_And_When_Then_Sync()
        => await Given("seed 1", () => 1) // 1
            .And("+2", x => x + 2)        // 3
            .When("x3", x => x * 3)       // 9
            .Then("== 9", v => v == 9)
            .AssertPassed();

    [Scenario("Given -> But -> When -> Then (sync transforms)")]
    [Fact]
    public async Task Given_But_When_Then_Sync()
        => await Given("seed 2", () => 2) // 2
            .But("+5", x => x + 5)        // 7
            .When("-1", x => x - 1)       // 6
            .Then("== 6", v => v == 6)
            .AssertPassed();

    [Scenario("Given -> And -> But -> When -> Then (mixed transforms)")]
    [Fact]
    public async Task Given_And_But_When_Then_Mixed()
        => await Given("seed 3", () => 3) // 3
            .And("x2", x => x * 2)        // 6
            .But("-1", x => x - 1)        // 5
            .When("+10", x => x + 10)     // 15
            .Then(">= 15", v => v >= 15)
            .AssertPassed();

    [Scenario("Given -> But -> And -> When -> Then (mixed transforms)")]
    [Fact]
    public async Task Given_But_And_When_Then_Mixed()
        => await Given("seed 4", () => 4) // 4
            .But("/2", x => x / 2)        // 2
            .And("+8", x => x + 8)        // 10
            .When("-3", x => x - 3)       // 7
            .Then("== 7", v => v == 7)
            .AssertPassed();

    [Scenario("Given -> And(async token) -> When -> Then")]
    [Fact]
    public async Task Given_AndAsyncToken_When_Then()
        => await Given("seed 5", () => 5)                 // 5
            .And("+5", (x, _) => Task.FromResult(x + 5))  // 10
            .When("/2", (x, _) => Task.FromResult(x / 2)) // 5
            .Then("== 5", v => v == 5)
            .AssertPassed();

    [Scenario("Given -> But(async token) -> When -> Then")]
    [Fact]
    public async Task Given_ButAsyncToken_When_Then()
        => await Given("seed 10", () => 10)               // 10
            .But("-4", (x, _) => Task.FromResult(x - 4))  // 6
            .When("x2", (x, _) => Task.FromResult(x * 2)) // 12
            .Then("== 12", v => v == 12)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) -> When(no title) -> Then(no title)")]
    [Fact]
    public async Task GivenNoTitle_And_WhenNoTitle_ThenNoTitle()
        => await Given(() => 7)                  // 7
            .And(x => new ValueTask<int>(x + 3)) // 10
            .When(x => x - 4)                    // 6
            .Then(v => v == 6)
            .AssertPassed();

    [Scenario("Given -> When -> And -> Then (And inherits When phase)")]
    [Fact]
    public async Task Given_When_And_Then()
        => await Given("seed 2", () => 2) // 2
            .When("+3", x => x + 3)       // 5
            .And("x2", x => x * 2)        // 10
            .Then("== 10", v => v == 10)
            .AssertPassed();

    [Scenario("Given -> When -> But -> Then (But inherits When phase)")]
    [Fact]
    public async Task Given_When_But_Then()
        => await Given("seed 9", () => 9) // 9
            .When("-4", x => x - 4)       // 5
            .But("+11", x => x + 11)      // 16
            .Then("> 15", v => v > 15)
            .AssertPassed();

    // --- Side-effect And/But after Given (keep T) ---

    [Scenario("Given -> And(title) side-effect Action -> When -> Then")]
    [Fact]
    public async Task Given_And_SideEffect_Action_When_Then()
        => await Given("seed 1", () => 1)
            .And("noop", _ =>
            {
                /* no-op */
            })
            .When("+1", x => x + 1)
            .Then("== 2", v => v == 2)
            .AssertPassed();

    [Scenario("Given -> But(title) side-effect Task -> When -> Then")]
    [Fact]
    public async Task Given_But_SideEffect_Task_When_Then()
        => await Given("seed 1", () => 1)
            .But("noop", _ => Task.CompletedTask)
            .When("+1", x => x + 1)
            .Then("== 2", v => v == 2)
            .AssertPassed();

    [Scenario("Given -> And(title) side-effect ValueTask(token) -> When -> Then")]
    [Fact]
    public async Task Given_And_SideEffect_ValueTask_Token_When_Then()
        => await Given("seed 1", () => 1)
            .And("noop", (_, _) => new ValueTask())
            .When("+1", x => x + 1)
            .Then("== 2", v => v == 2)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) side-effect Task(token) -> When(no title) -> Then(no title)")]
    [Fact]
    public async Task GivenNoTitle_And_SideEffect_TaskToken_WhenNoTitle_ThenNoTitle()
        => await Given(() => 2)
            .And((_, _) => Task.CompletedTask)
            .When(x => x + 3)
            .Then(v => v == 5)
            .AssertPassed();

    // --- Transform And/But after Given (ValueTask + token variants) ---

    [Scenario("Given -> And(title) transform ValueTask(token) -> When -> Then")]
    [Fact]
    public async Task Given_And_Transform_ValueTask_Token_When_Then()
        => await Given("seed 2", () => 2)                   // 2
            .And("+8", (x, _) => new ValueTask<int>(x + 8)) // 10
            .When("-3", x => x - 3)                         // 7
            .Then("== 7", v => v == 7)
            .AssertPassed();

    [Scenario("Given(no title) -> But(no title) transform Task(token) -> When(no title) -> Then(no title)")]
    [Fact]
    public async Task GivenNoTitle_But_Transform_TaskToken_WhenNoTitle_ThenNoTitle()
        => await Given(() => 3)
            .But((x, _) => Task.FromResult(x * 3)) // 9
            .When(x => x - 4)                      // 5
            .Then(v => v == 5)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) side-effect Action -> When(no title) -> Then(no title)")]
    [Fact]
    public async Task GivenNoTitle_And_SideEffect_Action_WhenNoTitle_ThenNoTitle()
        => await Given(() => 1)
            .And(_ => { })
            .When(x => x + 1)
            .Then(v => v == 2)
            .AssertPassed();

    [Scenario("Given(no title) -> But(no title) side-effect Task -> When(no title) -> Then(no title)")]
    [Fact]
    public async Task GivenNoTitle_But_SideEffect_Task_WhenNoTitle_ThenNoTitle_TitleRequired()
        => await Given(() => 1)
            .But("noop", _ => Task.CompletedTask)
            .When(x => x + 1)
            .Then(v => v == 2)
            .AssertPassed();

    // -------- And transform (title) missing variants --------

    [Scenario("Given -> And(title) transform Task -> Then")]
    [Fact]
    public async Task Given_And_Title_Transform_Task_Then()
        => await Given("seed", () => 1)
            .And("+9", x => Task.FromResult(x + 9))
            .Then("== 10", v => v == 10)
            .AssertPassed();

    [Scenario("Given -> And(title) transform ValueTask -> Then")]
    [Fact]
    public async Task Given_And_Title_Transform_ValueTask_Then()
        => await Given("seed", () => 1)
            .And("+9", x => new ValueTask<int>(x + 9))
            .Then("== 10", v => v == 10)
            .AssertPassed();

    // -------- And transform (no title) missing variants --------

    [Scenario("Given(no title) -> And(no title) transform sync -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_Transform_Sync_Then()
        => await Given(() => 1)
            .And(x => x + 9)
            .Then(v => v == 10)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) transform Task -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_Transform_Task_Then()
        => await Given(() => 1)
            .And(x => Task.FromResult(x + 9))
            .Then(v => v == 10)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) transform Task(token) -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_Transform_TaskToken_Then()
        => await Given(() => 1)
            .And((x, _) => Task.FromResult(x + 9))
            .Then(v => v == 10)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) transform ValueTask(token) -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_Transform_ValueTaskToken_Then()
        => await Given(() => 1)
            .And((x, _) => new ValueTask<int>(x + 9))
            .Then(v => v == 10)
            .AssertPassed();

    // -------- But transform (title) missing variants --------

    [Scenario("Given -> But(title) transform Task -> Then")]
    [Fact]
    public async Task Given_But_Title_Transform_Task_Then()
        => await Given("seed", () => 1)
            .But("+9", x => Task.FromResult(x + 9))
            .Then("== 10", v => v == 10)
            .AssertPassed();

    [Scenario("Given -> But(title) transform ValueTask -> Then")]
    [Fact]
    public async Task Given_But_Title_Transform_ValueTask_Then()
        => await Given("seed", () => 1)
            .But("+9", x => new ValueTask<int>(x + 9))
            .Then("== 10", v => v == 10)
            .AssertPassed();

    [Scenario("Given -> But(title) transform ValueTask(token) -> Then")]
    [Fact]
    public async Task Given_But_Title_Transform_ValueTaskToken_Then()
        => await Given("seed", () => 1)
            .But("+9", (x, _) => new ValueTask<int>(x + 9))
            .Then("== 10", v => v == 10)
            .AssertPassed();

    // -------- But transform (no title) missing variants --------

    [Scenario("Given(no title) -> But(no title) transform sync -> Then(no title)")]
    [Fact]
    public async Task Given_But_Default_Transform_Sync_Then()
        => await Given(() => 1)
            .But(x => x + 9)
            .Then(v => v == 10)
            .AssertPassed();

    [Scenario("Given(no title) -> But(no title) transform Task -> Then(no title)")]
    [Fact]
    public async Task Given_But_Default_Transform_Task_Then()
        => await Given(() => 1)
            .But(x => Task.FromResult(x + 9))
            .Then(v => v == 10)
            .AssertPassed();

    [Scenario("Given(no title) -> But(no title) transform ValueTask -> Then(no title)")]
    [Fact]
    public async Task Given_But_Default_Transform_ValueTask_Then()
        => await Given(() => 1)
            .But(x => new ValueTask<int>(x + 9))
            .Then(v => v == 10)
            .AssertPassed();

    [Scenario("Given(no title) -> But(no title) transform ValueTask(token) -> Then(no title)")]
    [Fact]
    public async Task Given_But_Default_Transform_ValueTaskToken_Then()
        => await Given(() => 1)
            .But((x, _) => new ValueTask<int>(x + 9))
            .Then(v => v == 10)
            .AssertPassed();

    // -------- And side-effect (title) missing variants --------

    [Scenario("Given -> And(title) side-effect Task -> Then")]
    [Fact]
    public async Task Given_And_Title_SideEffect_Task_Then()
        => await Given("seed", () => 1)
            .And("noop", _ => Task.CompletedTask)
            .Then("== 1", v => v == 1)
            .AssertPassed();

    [Scenario("Given -> And(title) side-effect ValueTask -> Then")]
    [Fact]
    public async Task Given_And_Title_SideEffect_ValueTask_Then()
        => await Given("seed", () => 1)
            .And("noop", _ => new ValueTask())
            .Then("== 1", v => v == 1)
            .AssertPassed();

    [Scenario("Given -> And(title) side-effect Task(token) -> Then")]
    [Fact]
    public async Task Given_And_Title_SideEffect_TaskToken_Then()
        => await Given("seed", () => 1)
            .And("noop", (_, _) => Task.CompletedTask)
            .Then("== 1", v => v == 1)
            .AssertPassed();

    // -------- And side-effect (no title) missing variants --------

    [Scenario("Given(no title) -> And(no title) side-effect Task -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_SideEffect_Task_Then()
        => await Given(() => 1)
            .And(_ => Task.CompletedTask)
            .Then(v => v == 1)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) side-effect ValueTask -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_SideEffect_ValueTask_Then()
        => await Given(() => 1)
            .And(_ => new ValueTask())
            .Then(v => v == 1)
            .AssertPassed();

    [Scenario("Given(no title) -> And(no title) side-effect ValueTask(token) -> Then(no title)")]
    [Fact]
    public async Task Given_And_Default_SideEffect_ValueTaskToken_Then()
        => await Given(() => 1)
            .And((_, _) => new ValueTask())
            .Then(v => v == 1)
            .AssertPassed();

    // -------- But side-effect (title) missing variants --------

    [Scenario("Given -> But(title) side-effect Action -> Then")]
    [Fact]
    public async Task Given_But_Title_SideEffect_Action_Then()
        => await Given("seed", () => 1)
            .But("noop", _ => { })
            .Then("== 1", v => v == 1)
            .AssertPassed();

    [Scenario("Given -> But(title) side-effect ValueTask -> Then")]
    [Fact]
    public async Task Given_But_Title_SideEffect_ValueTask_Then()
        => await Given("seed", () => 1)
            .But("noop", _ => new ValueTask())
            .Then("== 1", v => v == 1)
            .AssertPassed();

    [Scenario("Given -> But(title) side-effect Task(token) -> Then")]
    [Fact]
    public async Task Given_But_Title_SideEffect_TaskToken_Then()
        => await Given("seed", () => 1)
            .But("noop", (_, _) => Task.CompletedTask)
            .Then("== 1", v => v == 1)
            .AssertPassed();

    [Scenario("Given -> But(title) side-effect ValueTask(token) -> Then")]
    [Fact]
    public async Task Given_But_Title_SideEffect_ValueTaskToken_Then()
        => await Given("seed", () => 1)
            .But("noop", (_, _) => new ValueTask())
            .Then("== 1", v => v == 1)
            .AssertPassed();
}