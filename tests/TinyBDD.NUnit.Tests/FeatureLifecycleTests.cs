using NUnit.Framework;

namespace TinyBDD.NUnit.Tests;

[Feature("NUnit Feature Lifecycle")]
[TestFixture]
public class NUnitFeatureLifecycleTests : TinyBddNUnitBase
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
    [Test]
    public async Task FirstTest_VerifiesFeatureSetupRan()
    {
        await Given("feature setup has run", () => _setupCount > 0)
            .Then("setup count is positive", setupRan => setupRan)
            .AssertPassed();
    }

    [Scenario("Second test in feature")]
    [Test]
    public async Task SecondTest_VerifiesFeatureSetupRanOnlyOnce()
    {
        await Given("feature setup count", () => _setupCount)
            .Then("setup ran exactly once", count => count == 1)
            .AssertPassed();
    }

    [Scenario("Feature state is accessible")]
    [Test]
    public async Task FeatureState_IsAccessible()
    {
        await Given("feature state", () => FeatureState)
            .Then("state is not null", state => state != null)
            .AssertPassed();
    }

    [Scenario("Teardown count verified")]
    [Test]
    public async Task TeardownCount_IsZeroDuringTests()
    {
        await Given("teardown count", () => _teardownCount)
            .Then("teardown has not run yet", count => count == 0)
            .AssertPassed();
    }
}

[Feature("NUnit Feature with State")]
[TestFixture]
public class NUnitFeatureWithStateTests : TinyBddNUnitBase
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

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("feature teardown cleans up", () =>
        {
            // Access state in teardown
            var data = FeatureState as TestData;
            Assert.That(data, Is.Not.Null);
            return new object();
        });
    }

    [Scenario("Can access feature state")]
    [Test]
    public async Task CanAccessFeatureState()
    {
        await GivenFeature<TestData>("the feature state")
            .Then("has correct value", data => data.Value == "FeatureData")
            .And("has correct counter", data => data.Counter == 42)
            .AssertPassed();
    }

    [Scenario("Can use feature state in test")]
    [Test]
    public async Task CanUseFeatureStateInTest()
    {
        var data = FeatureState as TestData;

        await Given("feature data", () => data!)
            .When("incrementing counter", d => { d.Counter++; return d; })
            .Then("counter is incremented", d => d.Counter == 43)
            .AssertPassed();
    }
}

[Feature("NUnit Background Integration")]
[TestFixture]
public class NUnitBackgroundIntegrationTests : TinyBddNUnitBase
{
    private int _backgroundCount = 0;

    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("background runs before each test", () =>
        {
            _backgroundCount++;
            return new object();
        });
    }

    [Scenario("Background runs for first test")]
    [Test]
    public async Task BackgroundRuns_FirstTest()
    {
        await Given("background count", () => _backgroundCount)
            .Then("background ran once", count => count == 1)
            .AssertPassed();
    }

    [Scenario("Background runs for second test")]
    [Test]
    public async Task BackgroundRuns_SecondTest()
    {
        await Given("background count", () => _backgroundCount)
            .Then("background ran again", count => count == 2)
            .AssertPassed();
    }
}
