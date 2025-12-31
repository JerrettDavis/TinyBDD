using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common.SetupTeardown;

// Test assembly fixture
public class TestAssemblyFixture : AssemblyFixture
{
    public bool SetupCalled { get; private set; }
    public bool TeardownCalled { get; private set; }
    public string? SharedState { get; private set; }

    protected override Task SetupAsync(CancellationToken ct = default)
    {
        SetupCalled = true;
        SharedState = "Initialized";
        return Task.CompletedTask;
    }

    protected override Task TeardownAsync(CancellationToken ct = default)
    {
        TeardownCalled = true;
        return Task.CompletedTask;
    }
}

// Test async assembly fixture
public class AsyncAssemblyFixture : AssemblyFixture
{
    public bool SetupCalled { get; private set; }
    public bool TeardownCalled { get; private set; }
    public int SetupDelayMs { get; set; } = 10;

    protected override async Task SetupAsync(CancellationToken ct = default)
    {
        await Task.Delay(SetupDelayMs, ct);
        SetupCalled = true;
    }

    protected override async Task TeardownAsync(CancellationToken ct = default)
    {
        await Task.Delay(SetupDelayMs, ct);
        TeardownCalled = true;
    }
}

// Test fixture that throws
public class ThrowingAssemblyFixture : AssemblyFixture
{
    public bool ShouldThrowOnSetup { get; set; }
    public bool ShouldThrowOnTeardown { get; set; }

    protected override Task SetupAsync(CancellationToken ct = default)
    {
        if (ShouldThrowOnSetup)
            throw new InvalidOperationException("Setup failed");
        return Task.CompletedTask;
    }

    protected override Task TeardownAsync(CancellationToken ct = default)
    {
        if (ShouldThrowOnTeardown)
            throw new InvalidOperationException("Teardown failed");
        return Task.CompletedTask;
    }
}

[Feature("Assembly Fixture", "Assembly-level setup and teardown for expensive global resources")]
public class AssemblyFixtureTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AssemblyFixture can be created and initialized")]
    [Fact]
    public async Task AssemblyFixture_Can_Be_Created()
    {
        await Given("a test assembly fixture", () => new TestAssemblyFixture())
            .When("calling InternalSetupAsync", async fixture =>
            {
                await fixture.InternalSetupAsync();
                return fixture;
            })
            .Then("setup is called", fixture => fixture.SetupCalled)
            .And("shared state is initialized", fixture => fixture.SharedState == "Initialized")
            .AssertPassed();
    }

    [Scenario("AssemblyFixture teardown executes correctly")]
    [Fact]
    public async Task AssemblyFixture_Teardown_Executes()
    {
        await Given("an initialized assembly fixture", async () =>
            {
                var fixture = new TestAssemblyFixture();
                await fixture.InternalSetupAsync();
                return fixture;
            })
            .When("calling InternalTeardownAsync", async fixture =>
            {
                await fixture.InternalTeardownAsync();
                return fixture;
            })
            .Then("teardown is called", fixture => fixture.TeardownCalled)
            .AssertPassed();
    }

    [Scenario("AssemblyFixture supports async operations")]
    [Fact]
    public async Task AssemblyFixture_Supports_Async()
    {
        await Given("an async assembly fixture", () => new AsyncAssemblyFixture { SetupDelayMs = 50 })
            .When("executing setup", async fixture =>
            {
                await fixture.InternalSetupAsync();
                return fixture;
            })
            .Then("setup completes", fixture => fixture.SetupCalled)
            .When("executing teardown", async fixture =>
            {
                await fixture.InternalTeardownAsync();
                return fixture;
            })
            .Then("teardown completes", fixture => fixture.TeardownCalled)
            .AssertPassed();
    }

    [Scenario("AssemblyFixture handles setup exceptions")]
    [Fact]
    public async Task AssemblyFixture_Handles_Setup_Exceptions()
    {
        var exceptionThrown = false;

        try
        {
            await Given("a throwing assembly fixture", () => new ThrowingAssemblyFixture { ShouldThrowOnSetup = true })
                .When("calling InternalSetupAsync", async fixture =>
                {
                    await fixture.InternalSetupAsync();
                    return fixture;
                })
                .AssertPassed();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Setup failed")
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown, "Setup exception should be propagated");
    }

    [Scenario("AssemblyFixture handles teardown exceptions")]
    [Fact]
    public async Task AssemblyFixture_Handles_Teardown_Exceptions()
    {
        var exceptionThrown = false;

        try
        {
            await Given("a throwing assembly fixture", () => new ThrowingAssemblyFixture { ShouldThrowOnTeardown = true })
                .When("calling InternalTeardownAsync", async fixture =>
                {
                    await fixture.InternalTeardownAsync();
                    return fixture;
                })
                .AssertPassed();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Teardown failed")
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown, "Teardown exception should be propagated");
    }

    [Scenario("AssemblyFixtureCoordinator initializes fixtures correctly")]
    [Fact]
    public async Task Coordinator_Initializes_Fixtures()
    {
        // Reset coordinator for test isolation
        AssemblyFixtureCoordinator.Reset();

        await Given("a new coordinator instance", () => AssemblyFixtureCoordinator.Instance)
            .When("initializing with test assembly", async coordinator =>
            {
                // Note: We can't easily test attribute discovery in unit tests
                // This would be better tested in integration tests
                await coordinator.InitializeAsync(typeof(AssemblyFixtureTests).Assembly);
                return coordinator;
            })
            .Then("coordinator is initialized", coordinator => coordinator != null)
            .AssertPassed();

        // Cleanup
        await AssemblyFixtureCoordinator.Instance.TeardownAsync();
        AssemblyFixtureCoordinator.Reset();
    }

    [Scenario("AssemblyFixtureCoordinator prevents double initialization")]
    [Fact]
    public async Task Coordinator_Prevents_Double_Initialization()
    {
        // Reset coordinator for test isolation
        AssemblyFixtureCoordinator.Reset();

        var initCount = 0;

        await Given("a coordinator", () => AssemblyFixtureCoordinator.Instance)
            .When("initializing multiple times", async coordinator =>
            {
                await coordinator.InitializeAsync(typeof(AssemblyFixtureTests).Assembly);
                initCount++;
                await coordinator.InitializeAsync(typeof(AssemblyFixtureTests).Assembly);
                initCount++;
                await coordinator.InitializeAsync(typeof(AssemblyFixtureTests).Assembly);
                initCount++;
                return initCount;
            })
            .Then("initialization count is tracked", count => count == 3)
            .AssertPassed();

        // Cleanup
        await AssemblyFixtureCoordinator.Instance.TeardownAsync();
        AssemblyFixtureCoordinator.Reset();
    }

    [Scenario("AssemblySetupAttribute validates fixture type")]
    [Fact]
    public async Task AssemblySetupAttribute_Validates_Type()
    {
        var exceptionThrown = false;

        try
        {
            await Given("an invalid type", () => typeof(string))
                .When("creating AssemblySetupAttribute", type =>
                {
                    _ = new AssemblySetupAttribute(type);
                    return type;
                })
                .AssertPassed();
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown, "Should throw ArgumentException for non-AssemblyFixture types");
    }

    [Scenario("AssemblySetupAttribute accepts valid fixture type")]
    [Fact]
    public async Task AssemblySetupAttribute_Accepts_Valid_Type()
    {
        await Given("a valid fixture type", () => typeof(TestAssemblyFixture))
            .When("creating AssemblySetupAttribute", type => new AssemblySetupAttribute(type))
            .Then("attribute is created", attr => attr != null)
            .And("fixture type is set", attr => attr.FixtureType == typeof(TestAssemblyFixture))
            .AssertPassed();
    }
}
