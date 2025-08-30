using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("Then overloads", "Covers Then assertions and predicate overloads plus And/But on ThenChain")] 
public class ThenTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // --- Then with title: Task assertion (no T)
    [Scenario("Then(title) Task assertion after When side-effect")] 
    [Fact]
    public async Task Then_Title_Task_NoValue()
    {
        await Flow.Given("seed", () => 1)
            .When("noop", _ => Task.CompletedTask)
            .Then("assert", () => Task.CompletedTask);

        Scenario.AssertPassed();
    }

    // --- Then with title: Task assertion with value
    [Scenario("Then(title) Task assertion with value")] 
    [Fact]
    public async Task Then_Title_Task_WithValue()
    {
        await Flow.Given("seed", () => 2)
            .When("+3", x => x + 3)
            .Then("== 5 (async)", v => Task.CompletedTask)
            .And("== 5 (sync)", v => v == 5);

        Scenario.AssertPassed();
    }

    // --- Then with title: token-aware Task assertion
    [Scenario("Then(title) token-aware Task assertion")] 
    [Fact]
    public async Task Then_Title_Task_Token()
    {
        await Flow.Given("seed", () => 3)
            .When("x2", x => x * 2)
            .Then("check token", (v, _) => Task.CompletedTask)
            .And("> 0", v => v > 0);

        Scenario.AssertPassed();
    }

    // --- Then with title: predicate Task<bool>
    [Scenario("Then(title) async predicate Task<bool>")] 
    [Fact]
    public async Task Then_Title_AsyncPredicate()
    {
        await Flow.Given("seed", () => 4)
            .When("+6", x => x + 6)
            .Then("== 10?", v => Task.FromResult(v == 10));

        Scenario.AssertPassed();
    }

    // --- Then with title: predicate ValueTask<bool> (token)
    [Scenario("Then(title) ValueTask<bool> with token")] 
    [Fact]
    public async Task Then_Title_ValueTaskBool_Token()
    {
        await Flow.Given("seed", () => 5)
            .When("+5", x => x + 5)
            .Then("== 10?", (v, _) => new ValueTask<bool>(v == 10));

        Scenario.AssertPassed();
    }

    // --- Then default title: Task assertion with value
    [Scenario("Then(default title) Task assertion with value")] 
    [Fact]
    public async Task Then_DefaultTitle_Task_WithValue()
    {
        await Flow.Given("seed", () => 6)
            .When("+4", x => x + 4)
            .Then(v => Task.CompletedTask)
            .And(v => v == 10);

        Scenario.AssertPassed();
    }

    // --- Then default title: ValueTask assertion (no token)
    [Scenario("Then(default title) ValueTask assertion with value")]
    [Fact]
    public async Task Then_DefaultTitle_ValueTask_WithValue()
    {
        await Flow.Given("seed", () => 11)
            .When("identity", x => x)
            .Then(_ => new ValueTask());

        Scenario.AssertPassed();
    }

    // --- Then default title: ValueTask assertion with CancellationToken
    [Scenario("Then(default title) ValueTask assertion with CancellationToken")]
    [Fact]
    public async Task Then_DefaultTitle_ValueTask_Token_WithValue()
    {
        await Flow.Given("seed", () => 12)
            .When("identity", x => x)
            .Then((_, _) => new ValueTask());

        Scenario.AssertPassed();
    }

    // --- Then default title: ValueTask<bool> predicate
    [Scenario("Then(default title) ValueTask<bool> predicate")]
    [Fact]
    public async Task Then_DefaultTitle_ValueTaskBool_Predicate()
    {
        await Flow.Given("seed", () => 13)
            .When("+7", x => x + 7)
            .Then(v => new ValueTask<bool>(v == 20));

        Scenario.AssertPassed();
    }

    // --- Then default title: ValueTask<bool> predicate with CancellationToken
    [Scenario("Then(default title) ValueTask<bool> predicate (CancellationToken)")]
    [Fact]
    public async Task Then_DefaultTitle_ValueTaskBool_Token_Predicate()
    {
        await Flow.Given("seed", () => 14)
            .When("+6", x => x + 6)
            .Then((v, _) => new ValueTask<bool>(v == 20));

        Scenario.AssertPassed();
    }

    // --- And/But after Then (typed) with various assertion forms

    [Scenario("Then -> And(title) token-aware Task assertion")] 
    [Fact]
    public async Task Then_And_Title_Task_Token()
    {
        await Flow.Given("seed", () => 7)
            .When("+3", x => x + 3)
            .Then(">= 10?", v => v >= 10)
            .And("token ok", (v, _) => Task.CompletedTask);

        Scenario.AssertPassed();
    }

    [Scenario("Then -> But(title) async predicate Task<bool>")] 
    [Fact]
    public async Task Then_But_Title_AsyncPredicate()
    {
        await Flow.Given("seed", () => 8)
            .When("+2", x => x + 2)
            .Then("== 10?", v => v == 10)
            .But("<= 10 (async)", v => Task.FromResult(v <= 10));

        Scenario.AssertPassed();
    }

    [Scenario("Then -> And(default) ValueTask<bool> predicate")] 
    [Fact]
    public async Task Then_And_Default_ValueTaskBool()
    {
        await Flow.Given("seed", () => 9)
            .When("+1", x => x + 1)
            .Then(v => v == 10)
            .And(v => new ValueTask<bool>(v == 10));

        Scenario.AssertPassed();
    }

    [Scenario("Then -> But(default) sync predicate and action")] 
    [Fact]
    public async Task Then_But_Default_SyncPredicate_Action()
    {
        await Flow.Given("seed", () => 10)
            .When("+0", x => x)
            .Then(v => v == 10)
            .But(v => v <= 10)
            .And("noop", _ => { });

        Scenario.AssertPassed();
    }

    // --- And(title) ValueTask assertion ---
    [Scenario("Then -> And(title) ValueTask assertion")] 
    [Fact]
    public async Task Then_And_Title_ValueTask_Assertion()
    {
        await Flow.Given("seed", () => 21)
            .When("identity", x => x)
            .Then("> 0", v => v > 0)
            .And("vt ok", _ => new ValueTask());

        Scenario.AssertPassed();
    }

    // --- And(title) ValueTask assertion with CancellationToken ---
    [Scenario("Then -> And(title) ValueTask assertion (CancellationToken)")] 
    [Fact]
    public async Task Then_And_Title_ValueTask_Token_Assertion()
    {
        await Flow.Given("seed", () => 22)
            .When("identity", x => x)
            .Then("> 0", v => v > 0)
            .And("vt token ok", (_, _) => new ValueTask());

        Scenario.AssertPassed();
    }

    // --- And(title) ValueTask<bool> predicate ---
    [Scenario("Then -> And(title) ValueTask<bool> predicate")] 
    [Fact]
    public async Task Then_And_Title_ValueTaskBool_Predicate()
    {
        await Flow.Given("seed", () => 15)
            .When("+5", x => x + 5)
            .Then(">= 20", v => v >= 20)
            .And("== 20 vt", v => new ValueTask<bool>(v == 20));

        Scenario.AssertPassed();
    }

    // --- And(title) ValueTask<bool> predicate with CancellationToken ---
    [Scenario("Then -> And(title) ValueTask<bool> predicate (CancellationToken)")] 
    [Fact]
    public async Task Then_And_Title_ValueTaskBool_Token_Predicate()
    {
        await Flow.Given("seed", () => 30)
            .When("/3", x => x / 3)
            .Then("> 0", v => v > 0)
            .And("== 10 vt token", (v, _) => new ValueTask<bool>(v == 10));

        Scenario.AssertPassed();
    }

    // --- And(default) Action assertion ---
    [Scenario("Then -> And(default) Action assertion")] 
    [Fact]
    public async Task Then_And_Default_Action_Assertion()
    {
        await Flow.Given("seed", () => 5)
            .When("identity", x => x)
            .Then(v => v == 5)
            .And(_ => { /* no-op */ });

        Scenario.AssertPassed();
    }

    // --- And(default) Task assertion with CancellationToken ---
    [Scenario("Then -> And(default) Task assertion (CancellationToken)")] 
    [Fact]
    public async Task Then_And_Default_Task_Token_Assertion()
    {
        await Flow.Given("seed", () => 7)
            .When("identity", x => x)
            .Then(v => v == 7)
            .And((_, _) => Task.CompletedTask);

        Scenario.AssertPassed();
    }

    // --- And(default) ValueTask assertion ---
    [Scenario("Then -> And(default) ValueTask assertion")] 
    [Fact]
    public async Task Then_And_Default_ValueTask_Assertion()
    {
        await Flow.Given("seed", () => 8)
            .When("identity", x => x)
            .Then(v => v == 8)
            .And(_ => new ValueTask());

        Scenario.AssertPassed();
    }

    // --- And(default) ValueTask assertion with CancellationToken ---
    [Scenario("Then -> And(default) ValueTask assertion (CancellationToken)")] 
    [Fact]
    public async Task Then_And_Default_ValueTask_Token_Assertion()
    {
        await Flow.Given("seed", () => 9)
            .When("identity", x => x)
            .Then(v => v == 9)
            .And((_, _) => new ValueTask());

        Scenario.AssertPassed();
    }

    // --- And(default) Task<bool> predicate ---
    [Scenario("Then -> And(default) Task<bool> predicate")] 
    [Fact]
    public async Task Then_And_Default_TaskBool_Predicate()
    {
        await Flow.Given("seed", () => 3)
            .When("+4", x => x + 4)
            .Then(v => v == 7)
            .And(v => Task.FromResult(v == 7));

        Scenario.AssertPassed();
    }

    // --- And(default) Task<bool> predicate with CancellationToken ---
    [Scenario("Then -> And(default) Task<bool> predicate (CancellationToken)")] 
    [Fact]
    public async Task Then_And_Default_TaskBool_Token_Predicate()
    {
        await Flow.Given("seed", () => 12)
            .When("+3", x => x + 3)
            .Then(v => v == 15)
            .And((v, _) => Task.FromResult(v == 15));

        Scenario.AssertPassed();
    }

    // --- And(default) ValueTask<bool> predicate with CancellationToken ---
    [Scenario("Then -> And(default) ValueTask<bool> predicate (CancellationToken)")] 
    [Fact]
    public async Task Then_And_Default_ValueTaskBool_Token_Predicate()
    {
        await Flow.Given("seed", () => 16)
            .When("+4", x => x + 4)
            .Then(v => v == 20)
            .And((v, _) => new ValueTask<bool>(v == 20));

        Scenario.AssertPassed();
    }

    // --- But(default) Task (no value) ---
    [Scenario("Then -> But(default) Task assertion (no value)")] 
    [Fact]
    public async Task Then_But_Default_Task_NoValue()
    {
        await Flow.Given("seed", () => 2)
            .When("identity", x => x)
            .Then(v => v == 2)
            .But(() => Task.CompletedTask);

        Scenario.AssertPassed();
    }

    // --- But(default) Task with value ---
    [Scenario("Then -> But(default) Task assertion with value")] 
    [Fact]
    public async Task Then_But_Default_Task_WithValue()
    {
        await Flow.Given("seed", () => 4)
            .When("identity", x => x)
            .Then(v => v == 4)
            .But(_ => Task.CompletedTask);

        Scenario.AssertPassed();
    }

    // --- But(default) Task with CancellationToken ---
    [Scenario("Then -> But(default) Task assertion with CancellationToken")] 
    [Fact]
    public async Task Then_But_Default_Task_Token_WithValue()
    {
        await Flow.Given("seed", () => 6)
            .When("identity", x => x)
            .Then(v => v == 6)
            .But((_, _) => Task.CompletedTask);

        Scenario.AssertPassed();
    }
}