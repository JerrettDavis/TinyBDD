namespace TinyBDD;

/// <summary>
/// Observes scenario-level lifecycle events during execution.
/// </summary>
/// <remarks>
/// Implementations can use this interface to add cross-cutting concerns like logging,
/// telemetry, reporting, or persistence without modifying core scenario execution logic.
/// Observers are registered via <see cref="TinyBddOptionsBuilder.AddObserver(IScenarioObserver)"/>.
/// </remarks>
/// <example>
/// <code>
/// public class LoggingObserver : IScenarioObserver
/// {
///     public ValueTask OnScenarioStarting(ScenarioContext context)
///     {
///         Console.WriteLine($"Starting: {context.ScenarioName}");
///         return default;
///     }
///
///     public ValueTask OnScenarioFinished(ScenarioContext context)
///     {
///         Console.WriteLine($"Finished: {context.ScenarioName}");
///         return default;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IStepObserver"/>
/// <seealso cref="TinyBddOptionsBuilder"/>
public interface IScenarioObserver
{
    /// <summary>
    /// Called immediately before a scenario begins executing its steps.
    /// </summary>
    /// <param name="context">The scenario context containing feature, scenario, and tag metadata.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    ValueTask OnScenarioStarting(ScenarioContext context);

    /// <summary>
    /// Called after all scenario steps have completed, regardless of success or failure.
    /// </summary>
    /// <param name="context">The scenario context with all executed steps and their results.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    ValueTask OnScenarioFinished(ScenarioContext context);
}
