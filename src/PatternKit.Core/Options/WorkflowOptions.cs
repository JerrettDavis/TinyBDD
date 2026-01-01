namespace PatternKit.Core;

/// <summary>
/// Configuration options that control workflow execution behavior.
/// </summary>
/// <remarks>
/// Use these options to customize how steps are executed:
/// <list type="bullet">
///   <item><description><see cref="ContinueOnError"/> - Continue executing subsequent steps after a failure</description></item>
///   <item><description><see cref="HaltOnFailedAssertion"/> - Stop immediately on assertion failures</description></item>
///   <item><description><see cref="StepTimeout"/> - Maximum time allowed per step</description></item>
///   <item><description><see cref="MarkRemainingAsSkippedOnFailure"/> - Mark pending steps as skipped after failure</description></item>
/// </list>
/// </remarks>
public sealed record WorkflowOptions
{
    /// <summary>
    /// Gets the default workflow options.
    /// </summary>
    public static WorkflowOptions Default { get; } = new();

    /// <summary>
    /// If <see langword="true"/>, continue executing steps after a non-assertion failure.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool ContinueOnError { get; init; }

    /// <summary>
    /// If <see langword="true"/>, immediately halt execution when an assertion fails.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool HaltOnFailedAssertion { get; init; } = true;

    /// <summary>
    /// Optional per-step timeout. If a step exceeds this duration, it will be canceled.
    /// Default is <see langword="null"/> (no timeout).
    /// </summary>
    public TimeSpan? StepTimeout { get; init; }

    /// <summary>
    /// If <see langword="true"/>, mark any remaining steps as skipped when execution stops due to failure.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool MarkRemainingAsSkippedOnFailure { get; init; }
}
