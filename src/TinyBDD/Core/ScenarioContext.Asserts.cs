namespace TinyBDD;

public static class ScenarioContextAsserts
{
    public static void AssertPassed(this ScenarioContext ctx)
    {
        foreach (var s in ctx.Steps)
            if (s.Error is not null)
                throw new InvalidOperationException(
                    $"Step failed: {s.Kind} {s.Title}: {s.Error.GetType().Name}: {s.Error.Message}");
    }

    public static void AssertFailed(this ScenarioContext ctx)
    {
        foreach (var s in ctx.Steps)
            if (s.Error is not null)
                return;
        throw new InvalidOperationException("Scenario had no failed steps.");
    }
}