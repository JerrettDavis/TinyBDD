namespace PatternKit.Core;

/// <summary>
/// Captures the outcome of executing a single step in a workflow.
/// </summary>
/// <remarks>
/// Each step execution produces a result containing:
/// <list type="bullet">
///   <item><description>The display keyword (Given/When/Then/And/But)</description></item>
///   <item><description>The step title</description></item>
///   <item><description>Execution duration</description></item>
///   <item><description>Any error that occurred</description></item>
/// </list>
/// </remarks>
public readonly record struct StepResult
{
    /// <summary>
    /// The display keyword for the step (e.g., <c>Given</c>, <c>When</c>, <c>Then</c>, <c>And</c>, <c>But</c>).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// The human-readable title describing this step's purpose.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The time taken to execute this step.
    /// </summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// The exception thrown during step execution, or <see langword="null"/> if the step succeeded.
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether this step completed successfully.
    /// </summary>
    public bool Passed => Error is null;
}
