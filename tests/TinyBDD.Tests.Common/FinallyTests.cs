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

    [Scenario("Finally with Task handler (with title) in ScenarioChain")]
    [Fact]
    public async Task Finally_Task_Handler_With_Title_ScenarioChain()
    {
        var cleanupCalled = false;

        Func<int, Task> taskHandler = async x =>
        {
            await Task.Delay(1);
            cleanupCalled = x == 5;
        };

        await Given("start", () => 5)
            .Finally("async cleanup task", taskHandler)
            .When("add 1", x => x + 1)
            .Then("is 6", x => x == 6)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task+CT handler (with title) in ScenarioChain")]
    [Fact]
    public async Task Finally_Task_CT_Handler_With_Title_ScenarioChain()
    {
        var cleanupCalled = false;
        CancellationToken? receivedToken = null;

        Func<int, CancellationToken, Task> taskCtHandler = async (x, ct) =>
        {
            receivedToken = ct;
            await Task.Delay(1, ct);
            cleanupCalled = x == 10;
        };

        await Given("start", () => 10)
            .Finally("async cleanup task+ct", taskCtHandler)
            .When("double", x => x * 2)
            .Then("is 20", x => x == 20)
            .AssertPassed();

        Assert.True(cleanupCalled);
        Assert.NotNull(receivedToken);
    }

    [Scenario("Finally with ValueTask handler (with title) in ScenarioChain")]
    [Fact]
    public async Task Finally_ValueTask_Handler_With_Title_ScenarioChain()
    {
        var cleanupCalled = false;

        await Given("start", () => "test")
            .Finally("valuetask cleanup", (string s) =>
            {
                cleanupCalled = s == "test";
                return ValueTask.CompletedTask;
            })
            .When("upper", s => s.ToUpper())
            .Then("is TEST", s => s == "TEST")
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task handler (no title) in ScenarioChain")]
    [Fact]
    public async Task Finally_Task_Handler_NoTitle_ScenarioChain()
    {
        var cleanupCalled = false;

        Func<int, Task> taskHandler = async x =>
        {
            await Task.Delay(1);
            cleanupCalled = x == 7;
        };

        await Given("start", () => 7)
            .Finally(taskHandler)
            .When("add 3", x => x + 3)
            .Then("is 10", x => x == 10)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task+CT handler (no title) in ScenarioChain")]
    [Fact]
    public async Task Finally_Task_CT_Handler_NoTitle_ScenarioChain()
    {
        var cleanupCalled = false;

        Func<int, CancellationToken, Task> taskCtHandler = async (x, ct) =>
        {
            await Task.Delay(1, ct);
            cleanupCalled = x == 3;
        };

        await Given("start", () => 3)
            .Finally(taskCtHandler)
            .When("multiply 4", x => x * 4)
            .Then("is 12", x => x == 12)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with ValueTask handler (no title) in ScenarioChain")]
    [Fact]
    public async Task Finally_ValueTask_Handler_NoTitle_ScenarioChain()
    {
        var cleanupCalled = false;

        await Given("start", () => 100)
            .Finally((int x) =>
            {
                cleanupCalled = x == 100;
                return ValueTask.CompletedTask;
            })
            .When("divide 2", x => x / 2)
            .Then("is 50", x => x == 50)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task handler (with title) in ThenChain")]
    [Fact]
    public async Task Finally_Task_Handler_With_Title_ThenChain()
    {
        var cleanupCalled = false;

        Func<int, Task> taskHandler = async x =>
        {
            await Task.Delay(1);
            cleanupCalled = x == 5;
        };

        await Given("start", () => 8)
            .When("subtract 3", x => x - 3)
            .Then("is 5", x => x == 5)
            .Finally("async cleanup task", taskHandler)
            .And("is positive", x => x > 0)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task+CT handler (with title) in ThenChain")]
    [Fact]
    public async Task Finally_Task_CT_Handler_With_Title_ThenChain()
    {
        var cleanupCalled = false;

        Func<int, CancellationToken, Task> taskCtHandler = async (x, ct) =>
        {
            await Task.Delay(1, ct);
            cleanupCalled = x == 20;
        };

        await Given("start", () => 15)
            .When("add 5", x => x + 5)
            .Then("is 20", x => x == 20)
            .Finally("async cleanup task+ct", taskCtHandler)
            .And("is even", x => x % 2 == 0)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with ValueTask handler (with title) in ThenChain")]
    [Fact]
    public async Task Finally_ValueTask_Handler_With_Title_ThenChain()
    {
        var cleanupCalled = false;

        await Given("start", () => "hello")
            .When("concat world", s => s + " world")
            .Then("is hello world", s => s == "hello world")
            .Finally("valuetask cleanup", (string s) =>
            {
                cleanupCalled = s == "hello world";
                return ValueTask.CompletedTask;
            })
            .And("length is 11", s => s.Length == 11)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with ValueTask+CT handler (with title) in ThenChain")]
    [Fact]
    public async Task Finally_ValueTask_CT_Handler_With_Title_ThenChain()
    {
        var cleanupCalled = false;
        CancellationToken? receivedToken = null;

        await Given("start", () => 25)
            .When("multiply 2", x => x * 2)
            .Then("is 50", x => x == 50)
            .Finally("valuetask+ct cleanup", (int x, CancellationToken ct) =>
            {
                receivedToken = ct;
                cleanupCalled = x == 50;
                return ValueTask.CompletedTask;
            })
            .And("greater than 40", x => x > 40)
            .AssertPassed();

        Assert.True(cleanupCalled);
        Assert.NotNull(receivedToken);
    }

    [Scenario("Finally with Action handler (no title) in ThenChain")]
    [Fact]
    public async Task Finally_Action_Handler_NoTitle_ThenChain()
    {
        var cleanupCalled = false;

        await Given("start", () => 6)
            .When("double", x => x * 2)
            .Then("is 12", x => x == 12)
            .Finally((int x) => cleanupCalled = x == 12)
            .And("is even", x => x % 2 == 0)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task handler (no title) in ThenChain")]
    [Fact]
    public async Task Finally_Task_Handler_NoTitle_ThenChain()
    {
        var cleanupCalled = false;

        Func<int, Task> taskHandler = async x =>
        {
            await Task.Delay(1);
            cleanupCalled = x == 10;
        };

        await Given("start", () => 9)
            .When("add 1", x => x + 1)
            .Then("is 10", x => x == 10)
            .Finally(taskHandler)
            .And("is divisible by 5", x => x % 5 == 0)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with Task+CT handler (no title) in ThenChain")]
    [Fact]
    public async Task Finally_Task_CT_Handler_NoTitle_ThenChain()
    {
        var cleanupCalled = false;

        Func<int, CancellationToken, Task> taskCtHandler = async (x, ct) =>
        {
            await Task.Delay(1, ct);
            cleanupCalled = x == 12;
        };

        await Given("start", () => 4)
            .When("multiply 3", x => x * 3)
            .Then("is 12", x => x == 12)
            .Finally(taskCtHandler)
            .And("greater than 10", x => x > 10)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with ValueTask handler (no title) in ThenChain")]
    [Fact]
    public async Task Finally_ValueTask_Handler_NoTitle_ThenChain()
    {
        var cleanupCalled = false;

        await Given("start", () => "test")
            .When("upper", s => s.ToUpper())
            .Then("is TEST", s => s == "TEST")
            .Finally((string s) =>
            {
                cleanupCalled = s == "TEST";
                return ValueTask.CompletedTask;
            })
            .And("length is 4", s => s.Length == 4)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    [Scenario("Finally with ValueTask+CT handler (no title) in ThenChain")]
    [Fact]
    public async Task Finally_ValueTask_CT_Handler_NoTitle_ThenChain()
    {
        var cleanupCalled = false;

        await Given("start", () => 11)
            .When("add 4", x => x + 4)
            .Then("is 15", x => x == 15)
            .Finally((int x, CancellationToken ct) =>
            {
                cleanupCalled = x == 15;
                return ValueTask.CompletedTask;
            })
            .And("is odd", x => x % 2 == 1)
            .AssertPassed();

        Assert.True(cleanupCalled);
    }

    #region Finally Exception Suppression

    [Scenario("Finally handler exception is suppressed")]
    [Fact]
    public async Task Finally_Handler_SuppressesExceptions()
    {
        var handlerExecuted = false;
        Action<string> throwingAction = _ =>
        {
            handlerExecuted = true;
            throw new InvalidOperationException("Cleanup failed!");
        };
        await Given("resource", () => "test")
            .Finally("throwing cleanup", throwingAction)
            .Then("pass", _ => true)
            .AssertPassed();
        Assert.True(handlerExecuted);
    }

    [Scenario("Multiple finally handlers all execute even when one fails")]
    [Fact]
    public async Task Finally_MultipleFinallyHandlers_AllExecuteEvenWhenOneFails()
    {
        var executed = new List<int>();
        Action<string> firstAction = _ =>
        {
            executed.Add(1);
            throw new InvalidOperationException("First cleanup failed!");
        };
        Action<string> secondAction = _ => executed.Add(2);
        await Given("resource", () => "test")
            .Finally("first cleanup", firstAction)
            .Finally("second cleanup", secondAction)
            .Then("pass", _ => true)
            .AssertPassed();
        Assert.Contains(1, executed);
        Assert.Contains(2, executed);
    }

    #endregion

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

