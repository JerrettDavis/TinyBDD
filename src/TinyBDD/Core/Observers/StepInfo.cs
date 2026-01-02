namespace TinyBDD;

/// <summary>
/// Metadata about a step that is about to execute or has executed.
/// </summary>
/// <remarks>
/// This class provides information about a step's phase, connective keyword, and title
/// to observers before and after execution. It complements <see cref="StepResult"/> which
/// captures timing and error information after execution.
/// </remarks>
/// <param name="Kind">The display keyword for the step (e.g., "Given", "When", "Then", "And", "But").</param>
/// <param name="Title">The human-readable step title.</param>
/// <param name="Phase">The BDD phase this step belongs to (Given, When, or Then).</param>
/// <param name="Word">The connective keyword used (Primary, And, or But).</param>
/// <seealso cref="StepResult"/>
/// <seealso cref="IStepObserver"/>
public record StepInfo(
    string Kind,
    string Title,
    StepPhase Phase,
    StepWord Word);
