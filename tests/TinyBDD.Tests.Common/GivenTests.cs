using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("Given", "As a developer, I should be able to use Given with any sort of input")]
public class GivenTests(ITestOutputHelper output) :
    TinyBddXunitBase(output)
{
    [Scenario("Supports Given(title) with synchronous factory")]
    [Fact]
    public async Task Given_Sync_Func()
        => await Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(title) with ValueTask factory")]
    [Fact]
    public async Task Given_ValueTask_Func()
        => await Given("wire", () => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(title) with ValueTask factory (with cancellation)")]
    [Fact]
    public async Task Given_ValueTask_Func_With_Cancellation()
        => await Given("wire", _ => new ValueTask<int>(1))
            .When("act", (_, _) => new ValueTask<int>(1))
            .Then("assert", v => v == 1)
            .AssertPassed();

    [Scenario("Supports Given(no title) with ValueTask factory (CancellationToken)")]
    [Fact]
    public async Task Given_ValueTask_Func_CancellationToken()
        => await Given(_ => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(title) with Task-based factory")]
    [Fact]
    public async Task Given_Async_Func()
        => await Given("wire", () => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(no title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task Given_Async_Func_With_Cancellation()
        => await Given(_ => Task.FromResult(1))
            .When("act", (_, _) => Task.FromResult(1))
            .Then("assert", v => v == 1)
            .AssertPassed();

    [Scenario("Supports Given(title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task Given_Async_Func_CancellationToken()
        => await Given("wire", _ => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(no title) with synchronous factory")]
    [Fact]
    public async Task Given_NoTitle_Sync_Func()
        => await Given(() => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(no title) with Task-based factory")]
    [Fact]
    public async Task Given_NoTitle_Async_Func()
        => await Given(() => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(no title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task Given_NoTitle_Async_Func_CancellationToken()
        => await Given(() => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(no title) with ValueTask factory")]
    [Fact]
    public async Task Given_NoTitle_ValueTask_Func()
        => await Given(() => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();

    [Scenario("Supports Given(no title) with ValueTask factory (CancellationToken)")]
    [Fact]
    public async Task Given_NoTitle_ValueTask_Func_CancellationToken()
        => await Given(() => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2)
            .AssertPassed();
}