namespace TinyBDD.Tests.Common;

/// <summary>
/// Tests for state-passing overloads that avoid closure allocations.
/// </summary>
public class StatePassingOverloadTests
{
    [Fact]
    public async Task Given_WithState_Sync_PassesStateToFactory()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var multiplier = 10;

        // Act & Assert
        await Bdd.Given(ctx, "state value", multiplier, static state => state * 2)
            .Then("equals 20", v => v == 20)
            .AssertPassed();
    }

    [Fact]
    public async Task Given_WithState_AsyncTask_PassesStateToFactory()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var baseValue = 5;

        // Use explicit delegate to avoid ambiguity
        static Task<int> Setup(int state) => Task.FromResult(state + 3);

        // Act & Assert
        await Bdd.Given(ctx, "async state value", baseValue, Setup)
            .Then("equals 8", v => v == 8)
            .AssertPassed();
    }

    [Fact]
    public async Task Given_WithState_AsyncValueTask_PassesStateToFactory()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var factor = 3;

        // Act & Assert
        await Bdd.Given(ctx, "valuetask state value", factor, static state => new ValueTask<int>(state * 3))
            .Then("equals 9", v => v == 9)
            .AssertPassed();
    }

    [Fact]
    public async Task Given_WithState_AsyncTaskWithToken_PassesStateAndToken()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        using var cts = new CancellationTokenSource();
        var value = 42;

        // Use explicit delegate to avoid ambiguity
        static Task<int> Setup(int state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(state);
        }

        // Act & Assert
        await Bdd.Given(ctx, "token-aware state value", value, Setup)
            .Then("equals 42", v => v == 42)
            .AssertPassed(cts.Token);
    }

    [Fact]
    public async Task When_WithState_Sync_TransformsWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var multiplier = 5;

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => 4)
            .When("multiply by state", multiplier, static (v, state) => v * state)
            .Then("equals 20", v => v == 20)
            .AssertPassed();
    }

    [Fact]
    public async Task When_WithState_AsyncTask_TransformsWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var addition = 100;

        // Use explicit delegate to avoid ambiguity
        static Task<int> Transform(int v, int state) => Task.FromResult(v + state);

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => 10)
            .When("add state async", addition, Transform)
            .Then("equals 110", v => v == 110)
            .AssertPassed();
    }

    [Fact]
    public async Task When_WithState_AsyncValueTask_TransformsWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var divisor = 2;

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => 50)
            .When("divide by state", divisor, static (v, state) => new ValueTask<int>(v / state))
            .Then("equals 25", v => v == 25)
            .AssertPassed();
    }

    [Fact]
    public async Task When_WithState_SideEffect_ExecutesWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var list = new List<int>();
        var valueToAdd = 99;

        // Act & Assert
        await Bdd.Given(ctx, "list", () => list)
            .When("add value from state", valueToAdd, static (lst, val) => lst.Add(val))
            .Then("contains value", lst => lst.Contains(99))
            .AssertPassed();
    }

    [Fact]
    public async Task And_WithState_Sync_TransformsWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var offset = 7;

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => 3)
            .When("double", v => v * 2)
            .And("add offset", offset, static (v, state) => v + state)
            .Then("equals 13", v => v == 13)
            .AssertPassed();
    }

    [Fact]
    public async Task And_WithState_AsyncTask_TransformsWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var factor = 10;

        // Use explicit delegate to avoid ambiguity
        static Task<int> Multiply(int v, int state) => Task.FromResult(v * state);

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => 5)
            .When("increment", v => v + 1)
            .And("multiply async", factor, Multiply)
            .Then("equals 60", v => v == 60)
            .AssertPassed();
    }

    [Fact]
    public async Task And_WithState_SideEffect_ExecutesWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var captured = new List<string>();
        var message = "captured!";

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => "hello")
            .When("uppercase", s => s.ToUpper())
            .And("capture message", message, (_, msg) => captured.Add(msg))
            .Then("message captured", _ => captured.Contains("captured!"))
            .AssertPassed();
    }

    [Fact]
    public async Task But_WithState_Sync_TransformsWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var suffix = "_done";

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => "test")
            .When("process", s => s.ToUpper())
            .But("append suffix", suffix, static (s, state) => s + state)
            .Then("equals TEST_done", s => s == "TEST_done")
            .AssertPassed();
    }

    [Fact]
    public async Task But_WithState_SideEffect_ExecutesWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var log = new List<int>();
        var logValue = 42;

        // Act & Assert
        await Bdd.Given(ctx, "number", () => 100)
            .When("noop", v => v)
            .But("log value", logValue, (_, val) => log.Add(val))
            .Then("logged 42", _ => log.Contains(42))
            .AssertPassed();
    }

    [Fact]
    public async Task Then_WithState_Predicate_ValidatesWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var expectedMin = 10;

        // Act & Assert
        await Bdd.Given(ctx, "value", () => 15)
            .Then("greater than min", expectedMin, static (v, min) => v > min)
            .AssertPassed();
    }

    [Fact]
    public async Task Then_WithState_AsyncPredicate_ValidatesWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var expectedMax = 100;

        // Use explicit delegate to avoid ambiguity
        static Task<bool> Validate(int v, int max) => Task.FromResult(v < max);

        // Act & Assert
        await Bdd.Given(ctx, "value", () => 50)
            .Then("less than max async", expectedMax, Validate)
            .AssertPassed();
    }

    [Fact]
    public async Task Then_WithState_Assertion_ThrowsOnFailure()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var expectedValue = 999;
        var assertionRan = false;

        // Act & Assert
        await Bdd.Given(ctx, "value", () => 999)
            .Then("assert matches", expectedValue, (v, expected) =>
            {
                assertionRan = true;
                if (v != expected)
                    throw new InvalidOperationException("Values don't match");
            })
            .AssertPassed();

        Assert.True(assertionRan);
    }

    [Fact]
    public async Task Finally_WithState_Sync_ExecutesCleanupWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var cleanupLog = new List<string>();
        var cleanupMessage = "cleaned!";

        // Act
        await Bdd.Given(ctx, "resource", () => "resource")
            .Finally("cleanup with message", cleanupMessage, (_, msg) => cleanupLog.Add(msg))
            .Then("pass", _ => true)
            .AssertPassed();

        // Assert
        Assert.Contains("cleaned!", cleanupLog);
    }

    [Fact]
    public async Task Finally_WithState_AsyncTask_ExecutesCleanupWithState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var cleanupLog = new List<int>();
        var cleanupValue = 123;

        // Use explicit delegate to avoid ambiguity
        static Task Cleanup(object _, int val, List<int> log)
        {
            log.Add(val);
            return Task.CompletedTask;
        }

        // Act
        await Bdd.Given(ctx, "resource", () => new object())
            .Finally("async cleanup", cleanupValue, (obj, val) => Cleanup(obj, val, cleanupLog))
            .Then("pass", _ => true)
            .AssertPassed();

        // Assert
        Assert.Contains(123, cleanupLog);
    }

    [Fact]
    public async Task ComplexChain_WithMultipleStateOverloads_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var initial = 10;
        var multiplier = 3;
        var offset = 5;
        var expected = 35;
        var cleanup = new List<string>();

        // Act & Assert
        await Bdd.Given(ctx, "start with state", initial, static state => state)
            .When("multiply", multiplier, static (v, m) => v * m)
            .And("add offset", offset, static (v, o) => v + o)
            .Finally("record cleanup", cleanup, static (_, log) => log.Add("done"))
            .Then("equals expected", expected, static (v, exp) => v == exp)
            .AssertPassed();

        Assert.Contains("done", cleanup);
    }

    [Fact]
    public async Task Flow_Given_WithState_UsesAmbientContext()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        Ambient.Current.Value = ctx;
        var factor = 7;

        try
        {
            // Act & Assert
            await Flow.Given("state via flow", factor, static state => state * 2)
                .Then("equals 14", v => v == 14)
                .AssertPassed();
        }
        finally
        {
            Ambient.Current.Value = null;
        }
    }

    [Fact]
    public async Task When_WithState_TokenAware_PassesCancellationToken()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        using var cts = new CancellationTokenSource();
        var multiplier = 2;
        var tokenWasPassed = false;

        // Use a local lambda that captures tokenWasPassed
        async Task<int> Transform(int v, int m, CancellationToken ct)
        {
            tokenWasPassed = ct == cts.Token;
            await Task.Yield();
            return v * m;
        }

        // Act & Assert
        await Bdd.Given(ctx, "initial", () => 5)
            .When("token-aware transform", multiplier, Transform)
            .Then("equals 10", v => v == 10)
            .AssertPassed(cts.Token);

        Assert.True(tokenWasPassed);
    }
}
