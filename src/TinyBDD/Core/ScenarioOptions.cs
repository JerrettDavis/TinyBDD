namespace TinyBDD;

public record ScenarioOptions
{
    /// <summary>
    /// If true, the scenario will continue executing steps even if one or more previous steps have failed.
    /// Defaults to false.
    /// </summary>
    public bool ContinueOnError { get; init; }

    /// <summary>
    /// If true, all remaining steps will be marked as skipped if the scenario fails.
    /// </summary>
    public bool MarkRemainingAsSkippedOnFailure { get; init; }

    /// <summary>
    /// If set, all steps will be timed out after the specified duration.
    /// </summary>
    public TimeSpan? StepTimeout { get; init; }

    /// <summary>
    /// If true, the scenario will halt execution on the first failed assertion.
    /// </summary>
    public bool HaltOnFailedAssertion { get; init; }

    /// <summary>
    /// If true, background steps will be shown in a separate "Background:" section in Gherkin output.
    /// Defaults to false (background steps are shown inline).
    /// </summary>
    public bool ShowBackgroundSection { get; init; }

    /// <summary>
    /// If true, feature setup steps will be shown in the Gherkin output.
    /// Defaults to false (feature setup is typically internal and not shown).
    /// </summary>
    public bool ShowFeatureSetup { get; init; }

    /// <summary>
    /// If true, feature teardown steps will be shown in the Gherkin output.
    /// Defaults to false (feature teardown is typically internal and not shown).
    /// </summary>
    public bool ShowFeatureTeardown { get; init; }
}