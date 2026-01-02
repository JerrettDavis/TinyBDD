namespace TinyBDD;

/// <summary>
/// Observes step-level lifecycle events during scenario execution.
/// </summary>
/// <remarks>
/// Implementations can use this interface to add cross-cutting concerns like structured logging,
/// distributed tracing, or diagnostics at the individual step level. Observers are registered
/// via <see cref="TinyBddOptionsBuilder.AddObserver(IStepObserver)"/>.
/// </remarks>
/// <example>
/// <code>
/// public class StepTimingObserver : IStepObserver
/// {
///     public ValueTask OnStepStarting(ScenarioContext context, StepInfo step)
///     {
///         Console.WriteLine($"Starting: {step.Kind} {step.Title}");
///         return default;
///     }
///
///     public ValueTask OnStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io)
///     {
///         Console.WriteLine($"Finished: {step.Kind} {step.Title} in {result.Elapsed.TotalMilliseconds}ms");
///         return default;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IScenarioObserver"/>
/// <seealso cref="StepInfo"/>
/// <seealso cref="TinyBddOptionsBuilder"/>
public interface IStepObserver
{
    /// <summary>
    /// Called immediately before a step begins executing.
    /// </summary>
    /// <param name="context">The scenario context containing feature, scenario, and tag metadata.</param>
    /// <param name="step">Metadata about the step that is about to execute.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    ValueTask OnStepStarting(ScenarioContext context, StepInfo step);

    /// <summary>
    /// Called after a step completes execution, regardless of success or failure.
    /// </summary>
    /// <param name="context">The scenario context with all executed steps.</param>
    /// <param name="step">Metadata about the step that executed.</param>
    /// <param name="result">The execution result including timing and error information.</param>
    /// <param name="io">The input/output values captured during step execution.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    ValueTask OnStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io);
}
