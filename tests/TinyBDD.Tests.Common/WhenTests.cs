using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("When", "As a developer, I should be able to use When with any sort of input")]
public class WhenTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Supports When(title) with synchronous factory")]
    [Fact]
    public async Task When_Sync_Func()
        => await Given("wire", () => 1)
            .When("act", x => x + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(title) with ValueTask factory")]
    [Fact]
    public async Task When_ValueTask_Func()
        => await Given("wire", () => 1)
            .When("act", x => new ValueTask<int>(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(title) with ValueTask factory (CancellationToken)")]
    [Fact]
    public async Task When_ValueTask_Func_CancellationToken()
        => await Given("wire", () => 1)
            .When("act", (x, _) => new ValueTask<int>(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();


    [Scenario("Supports When(title) with ValueTask(Task) factory (CancellationToken)")]
    [Fact]
    public async Task When_NoReturnValueTask_Func_CancellationToken()
        => await Given("wire", () => 1)
            .When("act", (x, _) => new ValueTask(Task.FromResult(x + 1))) // We aren't incrementing the value reference
            .Then("assert", v => v == 1)
            .AssertPassed();

    [Scenario("Supports When(title) with Task-based factory")]
    [Fact]
    public async Task When_Async_Func()
        => await Given("wire", () => 1)
            .When("act", x => Task.FromResult(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task When_Async_Func_CancellationToken()
        => await Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(no title) with synchronous factory")]
    [Fact]
    public async Task When_NoTitle_Sync_Func()
        => await Given("wire", () => 1)
            .When(x => x + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(no title) with Task-based factory")]
    [Fact]
    public async Task When_NoTitle_Async_Func()
        => await Given("wire", () => 1)
            .When(x => Task.FromResult(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(no title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task When_NoTitle_Async_Func_CancellationToken()
        => await Given("wire", () => 1)
            .When((x, _) => Task.FromResult(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(no title) with ValueTask factory")]
    [Fact]
    public async Task When_NoTitle_ValueTask_Func()
        => await Given("wire", () => 1)
            .When(x => new ValueTask<int>(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(no title) with ValueTask factory (CancellationToken)")]
    [Fact]
    public async Task When_NoTitle_ValueTask_Func_CancellationToken()
        => await Given("wire", () => 1)
            .When((x, _) => new ValueTask<int>(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports When(title) side-effect ValueTask (keeps T)")]
    [Fact]
    public async Task When_Title_SideEffect_ValueTask()
        => await Given("wire", () => 1)
            .When("side-effect vt", _ => new ValueTask())
            .Then("assert", v => v == 1)
            .AssertPassed();

    [Scenario("Supports When(title) side-effect ValueTask with CancellationToken (keeps T)")]
    [Fact]
    public async Task When_Title_SideEffect_ValueTask_CancellationToken()
        => await Given("wire", () => 1)
            .When("side-effect vt token", (_, _) => new ValueTask())
            .Then("assert", v => v == 1)
            .AssertPassed();

    [Scenario("Supports When(no title) side-effect ValueTask (keeps T)")]
    [Fact]
    public async Task When_NoTitle_SideEffect_ValueTask()
        => await Given("wire", () => 1)
            .When(_ => new ValueTask())
            .Then("assert", v => v == 1)
            .AssertPassed();

    [Scenario("Supports When(no title) side-effect ValueTask with CancellationToken (keeps T)")]
    [Fact]
    public async Task When_NoTitle_SideEffect_ValueTask_CancellationToken()
        => await Given("wire", () => 1)
            .When((_, _) => new ValueTask())
            .Then("assert", v => v == 1)
            .AssertPassed();
}