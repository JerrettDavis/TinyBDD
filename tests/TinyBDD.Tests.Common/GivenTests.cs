using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("Given", "As a developer, I should be able to use Given with any sort of input")]
public class GivenTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Supports Given(title) with synchronous factory")]
    [Fact]
    public async Task Given_Sync_Func()
    {
        await Flow.Given("wire", () => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();   
    }

    [Scenario("Supports Given(title) with ValueTask factory")]
    [Fact]
    public async Task Given_ValueTask_Func()
    {
        await Flow.Given("wire", () => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();
    }

    [Scenario("Supports Given(title) with ValueTask factory (CancellationToken)")]
    [Fact]
    public async Task Given_ValueTask_Func_CancellationToken()
    {
        await Flow.Given("wire", _ => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();
    }

    [Scenario("Supports Given(title) with Task-based factory")]
    [Fact]
    public async Task Given_Async_Func()
    {
        await Flow.Given("wire", () => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed(); 
    }

    [Scenario("Supports Given(title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task Given_Async_Func_CancellationToken()
    {
        await Flow.Given("wire", _ => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2);

        Scenario.AssertPassed();
    }

    [Scenario("Supports Given(no title) with synchronous factory")]
    [Fact]
    public async Task Given_NoTitle_Sync_Func()
    {
        await Flow.Given(() => 1)
            .When("act", (x, _) => Task.FromResult(x + 1))
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();
    }
    
    [Scenario("Supports Given(no title) with Task-based factory")]
    [Fact]
    public async Task Given_NoTitle_Async_Func()
    {
        await Flow.Given(() => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();
    }
    
    [Scenario("Supports Given(no title) with Task-based factory (CancellationToken)")]
    [Fact]
    public async Task Given_NoTitle_Async_Func_CancellationToken()
    {
        await Flow.Given(() => Task.FromResult(1))
            .When("act", x => x + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();
    }

    [Scenario("Supports Given(no title) with ValueTask factory")]
    [Fact]
    public async Task Given_NoTitle_ValueTask_Func()
    {
        await Flow.Given(() => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed();
    }

    [Scenario("Supports Given(no title) with ValueTask factory (CancellationToken)")]
    [Fact]
    public async Task Given_NoTitle_ValueTask_Func_CancellationToken()
    {
        await Flow.Given(() => new ValueTask<int>(1))
            .When("act", v => v + 1)
            .Then("assert", v => v == 2);
        
        Scenario.AssertPassed(); 
    }
}