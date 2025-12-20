using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

[Feature("Finally", "As a developer, I should be able to register cleanup handlers that execute after all steps complete")]
public class FinallyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Finally executes after all steps complete")]
    [Fact]
    public async Task Finally_Executes_After_All_Steps()
    {
        var cleanupCalled = false;
        var disposable = new TestDisposable(() => cleanupCalled = true);

        await Given("a disposable", () => disposable)
            .Finally("cleanup", d => d.Dispose())
            .When("use it", d => d.DoWork())
            .Then("work succeeded", result => result == 42)
            .AssertPassed();

        Assert.True(cleanupCalled, "Finally handler should have been called");
    }

    [Scenario("Finally captures state at the point where it was registered")]
    [Fact]
    public async Task Finally_Captures_State_At_Registration()
    {
        var capturedValue = 0;

        await Given("start with 5", () => 5)
            .Finally("capture given", x => capturedValue = x)
            .When("multiply by 2", x => x * 2)
            .Then("result is 10", x => x == 10)
            .AssertPassed();

        Assert.Equal(5, capturedValue);
    }

    [Scenario("Multiple Finally handlers execute in registration order")]
    [Fact]
    public async Task Multiple_Finally_Handlers_Execute_In_Order()
    {
        var log = new List<string>();

        await Given("start", () => "value")
            .Finally("first", _ => log.Add("first"))
            .When("transform", s => s.ToUpper())
            .Finally("second", _ => log.Add("second"))
            .Then("verify", s => s == "VALUE")
            .Finally("third", _ => log.Add("third"))
            .AssertPassed();

        Assert.Equal(new[] { "first", "second", "third" }, log);
    }

    [Scenario("Finally executes even when a step throws")]
    [Fact]
    public async Task Finally_Executes_On_Exception()
    {
        var cleanupCalled = false;

        try
        {
            await Given("start", () => 5)
                .Finally("cleanup", _ => cleanupCalled = true)
                .When("throw", (Action<int>)(_ => throw new InvalidOperationException("boom")))
                .Then("never reached", x => x == 5)
                .AssertPassed();
        }
        catch (BddStepException)
        {
            // Expected
        }

        Assert.True(cleanupCalled, "Finally should execute even when step throws");
    }

    [Scenario("Finally in ThenChain executes correctly")]
    [Fact]
    public async Task Finally_In_ThenChain_Executes()
    {
        var cleanupCalled = false;

        await Given("start", () => 10)
            .When("double", x => x * 2)
            .Then("is 20", x => x == 20)
            .Finally("cleanup", _ => cleanupCalled = true)
            .And("is positive", x => x > 0)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with async handler executes correctly")]
    [Fact]
    public async Task Finally_Async_Handler_Executes()
    {
        bool cleanupCalled;
        var cleanupValue = 0;

        await Given("start", () => Task.FromResult(5))
            .Finally("async cleanup", x => cleanupValue = x)
            .When("add 1", x => Task.FromResult(x + 1))
            .Then("is 6", x => x == 6)
            .AssertPassed();

        cleanupCalled = cleanupValue == 5;
        Assert.True(cleanupCalled);
    }

    [Scenario("Finally can handle different types in chain")]
    [Fact]
    public async Task Finally_Handles_Type_Changes()
    {
        var stringValue = "";
        var intValue = 0;

        await Given("start with string", () => "hello")
            .Finally("capture string", s => stringValue = s)
            .When("get length", s => s.Length)
            .Finally("capture int", i => Task.Run(() => intValue = i))
            .Then("length is 5", len => len == 5)
            .AssertPassed();

        Assert.Equal("hello", stringValue);
        Assert.Equal(5, intValue);
    }

    [Scenario("Finally without title uses default")]
    [Fact]
    public async Task Finally_Without_Title()
    {
        var cleanupCalled = false;

        await Given("start", () => 1)
            .Finally(_ => cleanupCalled = true)
            .When("add", x => x + 1)
            .Then("is 2", x => x == 2)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with CancellationToken receives token")]
    [Fact]
    public async Task Finally_With_CancellationToken()
    {
        CancellationToken? receivedToken = null;
        await Given("start", () => 5)
            .Finally("capture token", (_, ct) =>
            {
                receivedToken = ct;
                return ValueTask.CompletedTask;
            })
            .When("add", x => x + 1)
            .Then("is 6", x => x == 6)
            .AssertPassed();

        Assert.NotNull(receivedToken);
    }

    [Scenario("Finally handlers execute even with ContinueOnError=true")]
    [Fact]
    public async Task Finally_Executes_With_ContinueOnError()
    {
        var cleanupCalled = false;
        
        var ctx = Bdd.CreateContext(this, options: new ScenarioOptions { ContinueOnError = true });

        await Bdd.Given(ctx, "start", () => 5)
            .Finally("cleanup", _ => cleanupCalled = true)
            .When("throw", (Action<int>)(_ => throw new InvalidOperationException("boom")))
            .Then("continue", x => x == 5);

        Assert.True(cleanupCalled);
    }

    [Scenario("Disposable scenario with Finally cleanup")]
    [Fact]
    public async Task Disposable_Scenario_With_Finally()
    {
        var disposed = false;
        var resource = new TestDisposable(() => disposed = true);

        await Given("a resource", () => resource)
            .Finally("dispose resource", r => r.Dispose())
            .When("use resource", r => r.DoWork())
            .Then("work completed", result => result == 42)
            .And("not disposed yet during test", _ => !disposed)
            .AssertPassed();

        Assert.True(disposed, "Resource should be disposed after all steps");
    }

    [Scenario("User's original example: Disposable with Finally executes in correct order")]
    [Fact]
    public async Task Original_Example_Disposable_Finally()
    {
        var disposed = false;
        var workResult = 0;

        await Given("a disposable", () => new TestDisposable(() => disposed = true))
            .Finally("dispose", d => d.Dispose())
            .When("do work and return int", d => d.DoWork())
            .Then("result > 1", i =>
            {
                workResult = i;
                return i > 1;
            })
            .AssertPassed();

        Assert.True(workResult == 42, "Work should have been performed");
        Assert.True(disposed, "Disposable should be disposed after all steps including Then");
    }

    private class TestDisposable(Action onDispose) : IDisposable
    {
        private bool _disposed;

        public int DoWork()
        {
            return _disposed ? throw new ObjectDisposedException(nameof(TestDisposable)) : 42;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            onDispose();
        }
    }
}

