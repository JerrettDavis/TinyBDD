namespace TinyBDD;

/// <summary>
/// Captures the input and output state for a single BDD step execution.
/// </summary>
/// <param name="Kind">
/// The BDD phase: <c>Given</c>, <c>When</c>, or <c>Then</c>.
/// </param>
/// <param name="Title">The step title.</param>
/// <param name="Input">The input value to the step, if any.</param>
/// <param name="Output"> The output value from the step, if any.</param>
public record StepIO(string Kind, string Title, object? Input, object? Output);

