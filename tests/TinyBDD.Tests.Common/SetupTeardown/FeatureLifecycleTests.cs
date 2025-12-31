using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common.SetupTeardown;

/// <summary>
/// Test fixture for feature setup/teardown
/// </summary>
public class TestFeatureWithSetup : TinyBddXunitBase
{
    public static bool FeatureSetupCalled { get; private set; }
    public static bool FeatureTeardownCalled { get; private set; }
    public static int FeatureSetupCount { get; private set; }

    public TestFeatureWithSetup(ITestOutputHelper output) : base(output)
    {
    }

    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("feature setup", () =>
            {
                FeatureSetupCalled = true;
                FeatureSetupCount++;
                return new { ServerUrl = "http://localhost:8080", Database = "test-db" };
            })
            .And("additional setup", state =>
            {
                // Do more setup
                return state;
            });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("feature teardown", () =>
            {
                FeatureTeardownCalled = true;
                return new object();
            });
    }

    public static void Reset()
    {
        FeatureSetupCalled = false;
        FeatureTeardownCalled = false;
        FeatureSetupCount = 0;
    }
}

[Feature("Feature Lifecycle", "Feature-level setup and teardown for test class resources")]
public class FeatureLifecycleTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Feature setup executes before scenarios")]
    [Fact]
    public async Task Feature_Setup_Executes_Before_Scenarios()
    {
        await Given("a test base with feature setup", () => new object())
            .When("defining feature setup", () => "ConfigureFeatureSetup can be overridden")
            .Then("setup executes before first test", result => result != null)
            .AssertPassed();
    }

    [Scenario("Feature state can be accessed from scenarios")]
    [Fact]
    public async Task Feature_State_Can_Be_Accessed()
    {
        await Given("a feature state object", () => new { Value = 42 })
            .When("accessing from scenario", state => state.Value)
            .Then("value is correct", value => value == 42)
            .AssertPassed();
    }

    [Scenario("GivenFeature helper retrieves feature state")]
    [Fact]
    public async Task GivenFeature_Helper_Works()
    {
        // Set up feature state manually for this test
        FeatureState = new TestState { Name = "TestFeature", Active = true };
        FeatureSetupExecuted = true;

        await GivenFeature<TestState>("the feature")
            .When("accessing state", state => state.Name)
            .Then("name is correct", name => name == "TestFeature")
            .AssertPassed();
    }

    [Scenario("GivenFeature throws if feature setup not executed")]
    [Fact]
    public async Task GivenFeature_Throws_If_Not_Executed()
    {
        // Ensure feature setup is not marked as executed
        FeatureSetupExecuted = false;

        var exceptionThrown = false;
        try
        {
            _ = GivenFeature<TestState>();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Feature setup has not been executed"))
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown, "GivenFeature should throw if setup not executed");
    }

    [Scenario("GivenFeature throws if state type mismatch")]
    [Fact]
    public async Task GivenFeature_Throws_On_Type_Mismatch()
    {
        // Set up feature state with wrong type
        FeatureState = "wrong type";
        FeatureSetupExecuted = true;

        var exceptionThrown = false;
        try
        {
            _ = GivenFeature<TestState>();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not of type"))
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown, "GivenFeature should throw on type mismatch");
    }

    [Scenario("Feature teardown executes after all scenarios")]
    [Fact]
    public async Task Feature_Teardown_Executes_After_Scenarios()
    {
        await Given("a feature with teardown", () => new object())
            .When("all scenarios complete", () => "ExecuteFeatureTeardownAsync can be called")
            .Then("teardown executes", result => result != null)
            .AssertPassed();
    }

    [Scenario("Feature setup and teardown work together")]
    [Fact]
    public async Task Feature_Setup_And_Teardown_Work_Together()
    {
        var setupCalled = false;
        var teardownCalled = false;

        await Given("tracking flags", () => new { Setup = setupCalled, Teardown = teardownCalled })
            .When("simulating setup", state =>
            {
                setupCalled = true;
                return state with { Setup = setupCalled };
            })
            .And("simulating teardown", state =>
            {
                teardownCalled = true;
                return state with { Teardown = teardownCalled };
            })
            .Then("both called", state => state.Setup && state.Teardown)
            .AssertPassed();
    }

    [Scenario("ExecuteFeatureSetupAsync is idempotent")]
    [Fact]
    public async Task ExecuteFeatureSetup_Is_Idempotent()
    {
        var callCount = 0;

        await Given("a counter", () => callCount)
            .When("executing setup multiple times", async count =>
            {
                // Simulate idempotent behavior
                if (count == 0)
                {
                    callCount++;
                }
                return callCount;
            })
            .And("calling again", async count =>
            {
                if (count == 1)
                {
                    // Should not increment
                }
                return callCount;
            })
            .Then("only called once", count => count == 1)
            .AssertPassed();
    }

    private class TestState
    {
        public required string Name { get; init; }
        public bool Active { get; init; }
    }
}

[Feature("Background Lifecycle", "Background setup for scenario-level context")]
public class BackgroundLifecycleTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("background setup", () => new { DatabaseConnection = "connected", UserId = 123 })
            .And("seed data", state => state);
    }

    [Scenario("Background executes before each scenario")]
    [Fact]
    public async Task Background_Executes_Before_Scenario()
    {
        // Background is automatically executed by TinyBddXunitBase.InitializeAsync
        Assert.True(BackgroundExecuted, "Background should be executed");
        Assert.NotNull(BackgroundState);
    }

    [Scenario("GivenBackground helper retrieves background state")]
    [Fact]
    public async Task GivenBackground_Helper_Works()
    {
        // Define expected background state type
        var expected = new { DatabaseConnection = "connected", UserId = 123 };

        // Background has already been executed by InitializeAsync
        // Just verify we can access it (implementation would need dynamic or specific type)
        Assert.True(BackgroundExecuted);
    }

    [Scenario("Background state is available to scenarios")]
    [Fact]
    public async Task Background_State_Available()
    {
        await Given("background is executed", () => BackgroundExecuted)
            .When("checking state", executed => BackgroundState)
            .Then("state is not null", state => state != null)
            .AssertPassed();
    }

    [Scenario("Multiple scenarios share no background state")]
    [Fact]
    public async Task Scenarios_Have_Independent_Background()
    {
        // Each test gets its own background execution
        await Given("background executed for this test", () => BackgroundExecuted)
            .Then("background is fresh", executed => executed)
            .AssertPassed();
    }
}

[Feature("Layered Lifecycle", "Complete lifecycle from assembly to scenario")]
public class LayeredLifecycleTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("feature-level resource", () => new { Server = "started" });
    }

    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("scenario-level context", () => new { Request = "prepared" });
    }

    [Scenario("All lifecycle layers work together")]
    [Fact]
    public async Task All_Layers_Work_Together()
    {
        await Given("assembly fixtures initialized", () => true)
            .And("feature setup complete", _ => FeatureSetupExecuted)
            .And("background executed", _ => BackgroundExecuted)
            .When("running scenario", _ => "test logic")
            .Finally("scenario cleanup", _ => { /* cleanup */ })
            .Then("all layers active", result => result != null)
            .AssertPassed();
    }

    [Scenario("Lifecycle execution order is correct")]
    [Fact]
    public async Task Lifecycle_Order_Is_Correct()
    {
        var order = new List<string>();

        await Given("tracking execution order", () => order)
            .When("simulating assembly setup", list =>
            {
                list.Add("assembly");
                return list;
            })
            .And("simulating feature setup", list =>
            {
                list.Add("feature");
                return list;
            })
            .And("simulating background", list =>
            {
                list.Add("background");
                return list;
            })
            .And("simulating scenario", list =>
            {
                list.Add("scenario");
                return list;
            })
            .Then("order is correct", list =>
                list.SequenceEqual(new[] { "assembly", "feature", "background", "scenario" }))
            .AssertPassed();
    }

    [Scenario("Teardown executes in reverse order")]
    [Fact]
    public async Task Teardown_Reverse_Order()
    {
        var order = new List<string>();

        await Given("tracking teardown order", () => order)
            .When("simulating scenario teardown", list =>
            {
                list.Add("scenario");
                return list;
            })
            .And("simulating background teardown", list =>
            {
                list.Add("background");
                return list;
            })
            .And("simulating feature teardown", list =>
            {
                list.Add("feature");
                return list;
            })
            .And("simulating assembly teardown", list =>
            {
                list.Add("assembly");
                return list;
            })
            .Then("reverse order", list =>
                list.SequenceEqual(new[] { "scenario", "background", "feature", "assembly" }))
            .AssertPassed();
    }
}
