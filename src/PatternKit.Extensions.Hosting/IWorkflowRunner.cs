using PatternKit.Core;

namespace PatternKit.Extensions.Hosting;

/// <summary>
/// Service for running workflows on-demand within a hosted application.
/// </summary>
public interface IWorkflowRunner
{
    /// <summary>
    /// Runs a workflow by name.
    /// </summary>
    /// <param name="workflowName">The name of the registered workflow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow context after execution.</returns>
    ValueTask<WorkflowContext> RunAsync(string workflowName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow context after execution.</returns>
    ValueTask<WorkflowContext> RunAsync(IWorkflowDefinition workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a workflow definition that produces a result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="workflow">The workflow definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    ValueTask<TResult> RunAsync<TResult>(IWorkflowDefinition<TResult> workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a workflow defined by a builder function.
    /// </summary>
    /// <typeparam name="T">The result type of the workflow.</typeparam>
    /// <param name="workflowName">Name for the workflow.</param>
    /// <param name="buildWorkflow">Function that builds and executes the workflow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the workflow.</returns>
    ValueTask<T> RunAsync<T>(
        string workflowName,
        Func<WorkflowContext, CancellationToken, ValueTask<T>> buildWorkflow,
        CancellationToken cancellationToken = default);
}
