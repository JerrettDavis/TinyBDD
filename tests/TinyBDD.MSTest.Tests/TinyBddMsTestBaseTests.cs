namespace TinyBDD.MSTest.Tests;

[Feature("MSTest base coverage")]
[TestClass]
public class TinyBddMsTestBaseTests
{
    private static FakeTestContext Ctx(string fqcn, string name) => new(fqcn, name);

    [Scenario("ResolveCurrentMethod picks exact method by name")]
    [TestMethod]
    public void Init_Resolves_Exact_Method()
    {
        var type = typeof(MsProbeTypes.Exact);
        var ctx = Ctx(Fqcn.Of(type), nameof(MsProbeTypes.Exact.TestExact));
        var drv = new MsBaseDriver();

        drv.SetContext(ctx);
        drv.CallInit();

        var mi = AmbientTestMethodResolver.Instance.GetCurrentTestMethod();
        Assert.IsNotNull(mi);
        Assert.AreEqual(type, mi.DeclaringType);
        Assert.AreEqual(nameof(MsProbeTypes.Exact.TestExact), mi.Name);
        Assert.IsNotNull(drv.Current);

        drv.CallCleanup();
        Assert.IsNull(Ambient.Current.Value);
    }

    [Scenario("ResolveCurrentMethod picks method by prefix for data-driven names")]
    [TestMethod]
    public void Init_Resolves_Prefix_Name()
    {
        var type = typeof(MsProbeTypes.Prefix);
        // Simulate MSTest display like: "TestPrefix (1, 2)"
        var ctx = Ctx(Fqcn.Of(type), nameof(MsProbeTypes.Prefix.TestPrefix) + " (1, 2)");
        var drv = new MsBaseDriver();

        drv.SetContext(ctx);
        drv.CallInit();

        var mi = AmbientTestMethodResolver.Instance.GetCurrentTestMethod();
        Assert.IsNotNull(mi);
        Assert.AreEqual(type, mi.DeclaringType);
        Assert.AreEqual(nameof(MsProbeTypes.Prefix.TestPrefix), mi.Name);

        drv.CallCleanup();
        Assert.IsNull(Ambient.Current.Value);
    }

    [Scenario("ResolveCurrentMethod falls back to [Scenario]-decorated method")]
    [TestMethod]
    public void Init_Resolves_Scenario_Fallback()
    {
        var type = typeof(MsProbeTypes.ScenarioFallback);
        var ctx = Ctx(Fqcn.Of(type), "DoesNotExist");
        var drv = new MsBaseDriver();

        drv.SetContext(ctx);
        drv.CallInit();

        var mi = AmbientTestMethodResolver.Instance.GetCurrentTestMethod();
        Assert.IsNotNull(mi);
        Assert.AreEqual(nameof(MsProbeTypes.ScenarioFallback.UniqueScenarioMethod), mi.Name);

        drv.CallCleanup();
        Assert.IsNull(Ambient.Current.Value);
    }

    [Scenario("ResolveCurrentMethod returns null when type cannot be loaded")]
    [TestMethod]
    public void Init_When_Type_Not_Found_Sets_Null_Method()
    {
        var ctx = Ctx("Bogus.Namespace.Type, Bogus.Assembly", "Whatever");
        var drv = new MsBaseDriver();

        drv.SetContext(ctx);
        drv.CallInit();

        var mi = AmbientTestMethodResolver.Instance.GetCurrentTestMethod();
        Assert.IsNull(mi);
        Assert.IsNotNull(drv.Current);

        drv.CallCleanup();
        Assert.IsNull(Ambient.Current.Value);
    }

    [Scenario("Cleanup writes report only when a context exists and is a no-op otherwise")]
    [TestMethod]
    public void Cleanup_With_And_Without_Context()
    {
        var type = typeof(MsProbeTypes.Exact);
        var drv = new MsBaseDriver();

        // init → cleanup path (report written, ambient cleared)
        drv.SetContext(Ctx(Fqcn.Of(type), nameof(MsProbeTypes.Exact.TestExact)));
        drv.CallInit();
        drv.CallCleanup();
        Assert.IsNull(Ambient.Current.Value);

        // cleanup without init → no throw
        drv.CallCleanup();
    }
}
