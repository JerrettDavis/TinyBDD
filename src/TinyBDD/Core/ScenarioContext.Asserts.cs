namespace TinyBDD;

/// <summary>
/// Assertion helpers for verifying the outcome of a recorded scenario.
/// </summary>
public static class ScenarioContextAsserts
{
    /// <summary>
    /// Ensures the scenario has no failed steps. Throws <see cref="InvalidOperationException"/>
    /// if any recorded <see cref="StepResult.Error"/> is non-null.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when at least one step failed.</exception>
    public static void AssertPassed(this ScenarioContext ctx)
    {
        foreach (var s in ctx.Steps)
            if (s.Error is not null)
                throw new InvalidOperationException(
                    $"Step failed: {s.Kind} {s.Title}: {s.Error.GetType().Name}: {s.Error.Message}");
    }

    /// <summary>
    /// Ensures the scenario has at least one failed step. Throws <see cref="InvalidOperationException"/>
    /// when all steps passed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no failed steps are present.</exception>
    public static void AssertFailed(this ScenarioContext ctx)
    {
        foreach (var s in ctx.Steps)
            if (s.Error is not null)
                return;
        throw new InvalidOperationException("Scenario had no failed steps.");
    }
}