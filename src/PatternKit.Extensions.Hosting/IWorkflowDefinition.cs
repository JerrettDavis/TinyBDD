using PatternKit.Core;

namespace PatternKit.Extensions.Hosting;

/// <summary>
/// Defines a workflow that can be executed by the workflow host.
/// </summary>
/// <remarks>
/// Implement this interface to define reusable workflows that can be
/// registered with the host and executed as background services.
/// </remarks>
public interface IWorkflowDefinition
{
    /// <summary>
    /// Gets the unique name of this workflow.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets an optional description of this workflow.
    /// </summary>
    string? Description => null;

    /// <summary>
    /// Executes the workflow using the provided context.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    ValueTask ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a workflow that produces a result.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IWorkflowDefinition<TResult> : IWorkflowDefinition
{
    /// <summary>
    /// Executes the workflow and returns a result.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    new ValueTask<TResult> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);

    /// <inheritdoc />
    async ValueTask IWorkflowDefinition.ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Defines a recurring workflow that runs on a schedule.
/// </summary>
public interface IRecurringWorkflowDefinition : IWorkflowDefinition
{
    /// <summary>
    /// Gets the interval between executions.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Gets whether to run immediately on startup.
    /// </summary>
    bool RunImmediately => false;
}
