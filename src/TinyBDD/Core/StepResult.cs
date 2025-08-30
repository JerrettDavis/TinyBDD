namespace TinyBDD;

/// <summary>
/// Represents a single executed step in a scenario (Given/When/Then/And/But),
/// including title, elapsed time, and any error captured.
/// </summary>
public sealed class StepResult
{
    /// <summary>Step kind keyword as rendered in reports (e.g., <c>Given</c>, <c>When</c>, <c>Then</c>, <c>And</c>, <c>But</c>).</summary>
    public required string Kind { get; init; }     // Given, When, Then, And, But

    /// <summary>Human-readable step title.</summary>
    public required string Title { get; init; }

    /// <summary>Elapsed time for executing the step.</summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Exception captured during step execution, if any. 
    /// </summary>
    public Exception? Error { get; init; }
}
