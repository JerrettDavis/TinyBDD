namespace TinyBDD.MSTest.Tests;

[Feature("MSTest adapter")]
[TestClass]
public class MsTestAdapterTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Init()
    {
        // Make MSTest’s context available to the bridge/reporter
        MsTestTraitBridge.TestContext = TestContext;
    }

    [Scenario("Trait bridge no-throw + end-to-end")]
    [TestMethod]
    public async Task TraitBridge_AddTag_NoThrow_And_Scenario_Runs()
    {
        var traits = new MsTestTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);

        // Should log to TestContext and not throw
        ctx.AddTag("mstest-tag-1");
        ctx.AddTag("mstest-tag-2");

        await Bdd.Given(ctx, "wire", () => 2)
            .When("double", (x, _) => Task.FromResult(x * 2))
            .Then("is 4", v =>
            {
                Assert.AreEqual(4, v);
                return Task.CompletedTask;
            });

        Assert.HasCount(3, ctx.Steps);
    }

    [Scenario("Reporter no-throw")]
    [TestMethod]
    public void Reporter_WriteLine_NoThrow()
    {
        var reporter = new MsTestBddReporter();
        reporter.WriteLine("mstest hello"); // should write via TestContext; just ensure no throw
    }
}