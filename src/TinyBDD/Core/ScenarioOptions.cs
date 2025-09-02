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
}