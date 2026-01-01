using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PatternKit.Core;
using PatternKit.Extensions.DependencyInjection;

namespace PatternKit.Extensions.Hosting;

/// <summary>
/// Default implementation of <see cref="IWorkflowRunner"/>.
/// </summary>
public sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowRunner> _logger;
    private readonly IEnumerable<IWorkflowDefinition> _registeredWorkflows;

    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowRunner"/>.
    /// </summary>
    public WorkflowRunner(
        IServiceProvider serviceProvider,
        ILogger<WorkflowRunner> logger,
        IEnumerable<IWorkflowDefinition> registeredWorkflows)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _registeredWorkflows = registeredWorkflows;
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowContext> RunAsync(string workflowName, CancellationToken cancellationToken = default)
    {
        var workflow = _registeredWorkflows.FirstOrDefault(w =>
            string.Equals(w.Name, workflowName, StringComparison.OrdinalIgnoreCase));

        if (workflow is null)
        {
            throw new InvalidOperationException($"Workflow not found: {workflowName}");
        }

        return await RunAsync(workflow, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowContext> RunAsync(IWorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IWorkflowContextFactory>();
        var context = factory.Create(workflow.Name, workflow.Description);

        try
        {
            _logger.LogDebug("Running workflow: {Name}", workflow.Name);
            await workflow.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Workflow completed: {Name}, Passed: {Passed}", workflow.Name, context.AllPassed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow failed: {Name}", workflow.Name);
            throw;
        }

        return context;
    }

    /// <inheritdoc />
    public async ValueTask<TResult> RunAsync<TResult>(
        IWorkflowDefinition<TResult> workflow,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IWorkflowContextFactory>();
        var context = factory.Create(workflow.Name, workflow.Description);

        try
        {
            _logger.LogDebug("Running workflow: {Name}", workflow.Name);
            var result = await workflow.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Workflow completed: {Name}, Passed: {Passed}", workflow.Name, context.AllPassed);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow failed: {Name}", workflow.Name);
            throw;
        }
        finally
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<T> RunAsync<T>(
        string workflowName,
        Func<WorkflowContext, CancellationToken, ValueTask<T>> buildWorkflow,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IWorkflowContextFactory>();
        var context = factory.Create(workflowName);

        try
        {
            _logger.LogDebug("Running ad-hoc workflow: {Name}", workflowName);
            var result = await buildWorkflow(context, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Workflow completed: {Name}, Passed: {Passed}", workflowName, context.AllPassed);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow failed: {Name}", workflowName);
            throw;
        }
        finally
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }
}
