using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatternKit.Extensions.DependencyInjection;

namespace PatternKit.Extensions.Hosting;

/// <summary>
/// Background service that hosts and executes registered workflows.
/// </summary>
public sealed class WorkflowHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowHostedService> _logger;
    private readonly WorkflowHostingOptions _options;
    private readonly IEnumerable<IWorkflowDefinition> _workflows;

    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowHostedService"/>.
    /// </summary>
    public WorkflowHostedService(
        IServiceProvider serviceProvider,
        ILogger<WorkflowHostedService> logger,
        WorkflowHostingOptions options,
        IEnumerable<IWorkflowDefinition> workflows)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _workflows = workflows;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.StartAutomatically)
        {
            _logger.LogInformation("Workflow host configured to not start automatically");
            return;
        }

        var oneTimeWorkflows = _workflows.Where(w => w is not IRecurringWorkflowDefinition).ToList();
        var recurringWorkflows = _workflows.OfType<IRecurringWorkflowDefinition>().ToList();

        // Execute one-time workflows
        if (oneTimeWorkflows.Count > 0)
        {
            _logger.LogInformation("Executing {Count} one-time workflows", oneTimeWorkflows.Count);
            await ExecuteWorkflowsAsync(oneTimeWorkflows, stoppingToken).ConfigureAwait(false);
        }

        // Start recurring workflows
        if (recurringWorkflows.Count > 0)
        {
            _logger.LogInformation("Starting {Count} recurring workflows", recurringWorkflows.Count);
            var tasks = recurringWorkflows.Select(w => RunRecurringAsync(w, stoppingToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private async Task ExecuteWorkflowsAsync(
        IEnumerable<IWorkflowDefinition> workflows,
        CancellationToken cancellationToken)
    {
        if (_options.EnableParallelExecution)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism ?? Environment.ProcessorCount,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(workflows, parallelOptions, async (workflow, ct) =>
            {
                await ExecuteWorkflowAsync(workflow, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        else
        {
            foreach (var workflow in workflows)
            {
                await ExecuteWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteWorkflowAsync(IWorkflowDefinition workflow, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IWorkflowContextFactory>();
        var context = factory.Create(workflow.Name, workflow.Description);

        try
        {
            _logger.LogDebug("Starting workflow: {Name}", workflow.Name);
            await workflow.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            if (context.AllPassed)
            {
                _logger.LogInformation("Workflow completed successfully: {Name}", workflow.Name);
            }
            else
            {
                var failure = context.FirstFailure;
                _logger.LogWarning(
                    "Workflow completed with failures: {Name} - First failure: {Step}",
                    workflow.Name, failure?.Title);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Workflow cancelled: {Name}", workflow.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow failed: {Name}", workflow.Name);

            if (!_options.ContinueOnFailure)
                throw;
        }
        finally
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task RunRecurringAsync(
        IRecurringWorkflowDefinition workflow,
        CancellationToken cancellationToken)
    {
        if (workflow.RunImmediately)
        {
            await ExecuteWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
        }

        using var timer = new PeriodicTimer(workflow.Interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
                await ExecuteWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring workflow failed: {Name}", workflow.Name);

                if (!_options.ContinueOnFailure)
                    throw;
            }
        }
    }
}
