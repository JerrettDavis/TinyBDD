namespace PatternKit.Core;

/// <summary>
/// Metadata describing a step about to be executed.
/// </summary>
/// <remarks>
/// This is passed to hooks like <see cref="ExecutionPipeline.BeforeStep"/>
/// to provide information about the upcoming step execution.
/// </remarks>
/// <param name="Kind">The display keyword for the step (e.g., <c>Given</c>, <c>And</c>).</param>
/// <param name="Title">The step title.</param>
/// <param name="Phase">The BDD phase.</param>
/// <param name="Word">The connective keyword.</param>
public readonly record struct StepMetadata(
    string Kind,
    string Title,
    StepPhase Phase,
    StepWord Word);
