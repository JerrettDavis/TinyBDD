namespace TinyBDD.MSTest.Tests;

public static class MsProbeTypes
{
    [Feature("Probe")]
    public sealed class Exact
    {
        [Scenario("exact")]
        public void TestExact()
        {
        }
    }

    [Feature("Probe")]
    public sealed class Prefix
    {
        [Scenario("prefix")]
        public void TestPrefix()
        {
        }
    }

    [Feature("Probe")]
    public sealed class ScenarioFallback
    {
        [Scenario("fallback")]
        public void UniqueScenarioMethod()
        {
        }

        public void Helper()
        {
        }
    }
}