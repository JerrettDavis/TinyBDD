namespace TinyBDD.Xunit.Tests;

[Feature("xUnit Feature Lifecycle")]
public class XunitFeatureLifecycleTests : TinyBddXunitBase
{
    private static int _setupCount = 0;
    private static int _teardownCount = 0;

    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("feature setup runs once", () =>
        {
            _setupCount++;
            return new { SetupData = "initialized" };
        });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("feature teardown runs once", () =>
        {
            _teardownCount++;
            return new object();
        });
    }

    [Scenario("First test in feature")]
    [Fact]
    public async Task FirstTest_VerifiesFeatureSetupRan()
    {
        await Given("feature setup has run", () => _setupCount > 0)
            .Then("setup count is positive", setupRan => setupRan)
            .AssertPassed();
    }

    [Scenario("Second test in feature")]
    [Fact]
    public async Task SecondTest_VerifiesFeatureSetupRanOnlyOnce()
    {
        await Given("feature setup count", () => _setupCount)
            .Then("setup ran exactly once", count => count == 1)
            .AssertPassed();
    }

    [Scenario("Feature state is accessible")]
    [Fact]
    public async Task FeatureState_IsAccessible()
    {
        await Given("feature state", () => FeatureState)
            .Then("state is not null", state => state != null)
            .AssertPassed();
    }
}

[Feature("xUnit Feature with State")]
public class XunitFeatureWithStateTests : TinyBddXunitBase
{
    public class TestData
    {
        public string Value { get; set; } = "";
        public int Counter { get; set; }
    }

    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("feature setup with data", () =>
        {
            return new TestData
            {
                Value = "FeatureData",
                Counter = 42
            };
        });
    }

    [Scenario("Can access feature state")]
    [Fact]
    public async Task CanAccessFeatureState()
    {
        await GivenFeature<TestData>("the feature state")
            .Then("has correct value", data => data.Value == "FeatureData")
            .And("has correct counter", data => data.Counter == 42)
            .AssertPassed();
    }

    [Scenario("Can use feature state in test")]
    [Fact]
    public async Task CanUseFeatureStateInTest()
    {
        var data = FeatureState as TestData;

        await Given("feature data", () => data!)
            .When("incrementing counter", d => { d.Counter++; return d; })
            .Then("counter is incremented", d => d.Counter == 43)
            .AssertPassed();
    }
}
