namespace PatternKit.Core;

/// <summary>
/// Records the input and output values for a single step execution.
/// </summary>
/// <remarks>
/// This allows post-execution analysis of the data flow through a workflow.
/// </remarks>
/// <param name="Kind">The display keyword for the step.</param>
/// <param name="Title">The step title.</param>
/// <param name="Input">The value received as input to this step.</param>
/// <param name="Output">The value produced by this step.</param>
public readonly record struct StepIO(
    string Kind,
    string Title,
    object? Input,
    object? Output);
