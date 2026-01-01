using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TinyBDD.Extensions.Hosting;

/// <summary>
/// A hosted service that executes a TinyBDD workflow on startup.
/// </summary>
/// <typeparam name="TWorkflow">The workflow definition type to execute.</typeparam>
internal sealed class WorkflowHostedService<TWorkflow> : BackgroundService
    where TWorkflow : class, IWorkflowDefinition
{
    private readonly TWorkflow _workflow;
    private readonly IWorkflowRunner _runner;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly TinyBddHostingOptions _options;
    private readonly ILogger<WorkflowHostedService<TWorkflow>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHostedService{TWorkflow}"/> class.
    /// </summary>
    public WorkflowHostedService(
        TWorkflow workflow,
        IWorkflowRunner runner,
        IHostApplicationLifetime lifetime,
        IOptions<TinyBddHostingOptions> options,
        ILogger<WorkflowHostedService<TWorkflow>> logger)
    {
        _workflow = workflow;
        _runner = runner;
        _lifetime = lifetime;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.StartupDelay > TimeSpan.Zero)
        {
            _logger.LogDebug(
                "Waiting {Delay} before starting workflow: {WorkflowType}",
                _options.StartupDelay,
                typeof(TWorkflow).Name);

            await Task.Delay(_options.StartupDelay, stoppingToken);
        }

        try
        {
            var context = await _runner.RunAsync(_workflow, stoppingToken);

            var hasFailed = context.Steps.Any(s => s.Error is not null);

            if (hasFailed && _options.StopHostOnFailure)
            {
                _logger.LogInformation(
                    "Stopping host due to workflow failure: {WorkflowType}",
                    typeof(TWorkflow).Name);
                _lifetime.StopApplication();
            }
            else if (!hasFailed && _options.StopHostOnCompletion)
            {
                _logger.LogInformation(
                    "Stopping host after workflow completion: {WorkflowType}",
                    typeof(TWorkflow).Name);
                _lifetime.StopApplication();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown, don't log as error
            _logger.LogDebug(
                "Workflow cancelled during shutdown: {WorkflowType}",
                typeof(TWorkflow).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Workflow hosted service failed: {WorkflowType}",
                typeof(TWorkflow).Name);

            if (_options.StopHostOnFailure)
            {
                _lifetime.StopApplication();
            }
        }
    }
}
