namespace TinyBDD.Extensions.Hosting;

/// <summary>
/// Service for executing TinyBDD workflows.
/// </summary>
public interface IWorkflowRunner
{
    /// <summary>
    /// Runs the specified workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="cancellationToken">Token to signal cancellation.</param>
    /// <returns>The scenario context after execution, containing step results.</returns>
    Task<ScenarioContext> RunAsync(
        IWorkflowDefinition workflow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a workflow defined by the provided delegate.
    /// </summary>
    /// <param name="featureName">The feature name for the workflow.</param>
    /// <param name="scenarioName">The scenario name for the workflow.</param>
    /// <param name="workflow">The workflow execution delegate.</param>
    /// <param name="cancellationToken">Token to signal cancellation.</param>
    /// <returns>The scenario context after execution.</returns>
    Task<ScenarioContext> RunAsync(
        string featureName,
        string scenarioName,
        Func<ScenarioContext, CancellationToken, ValueTask> workflow,
        CancellationToken cancellationToken = default);
}
