using Microsoft.Extensions.Logging;
using TinyBDD.Extensions.DependencyInjection;

namespace TinyBDD.Extensions.Hosting;

/// <summary>
/// Default implementation of <see cref="IWorkflowRunner"/> that executes workflows
/// with logging and context factory support.
/// </summary>
internal sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly IScenarioContextFactory _contextFactory;
    private readonly ILogger<WorkflowRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRunner"/> class.
    /// </summary>
    /// <param name="contextFactory">Factory for creating scenario contexts.</param>
    /// <param name="logger">Logger for workflow execution events.</param>
    public WorkflowRunner(
        IScenarioContextFactory contextFactory,
        ILogger<WorkflowRunner> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScenarioContext> RunAsync(
        IWorkflowDefinition workflow,
        CancellationToken cancellationToken = default)
    {
        var context = _contextFactory.Create(
            workflow.FeatureName,
            workflow.ScenarioName,
            workflow.FeatureDescription);

        _logger.LogInformation(
            "Starting workflow: {Feature} - {Scenario}",
            workflow.FeatureName,
            workflow.ScenarioName);

        try
        {
            await workflow.ExecuteAsync(context, cancellationToken);

            var failed = context.Steps.Any(s => s.Error is not null);
            if (failed)
            {
                _logger.LogWarning(
                    "Workflow completed with failures: {Feature} - {Scenario}, {FailedCount}/{TotalCount} steps failed",
                    workflow.FeatureName,
                    workflow.ScenarioName,
                    context.Steps.Count(s => s.Error is not null),
                    context.Steps.Count);
            }
            else
            {
                _logger.LogInformation(
                    "Workflow completed successfully: {Feature} - {Scenario}, {StepCount} steps",
                    workflow.FeatureName,
                    workflow.ScenarioName,
                    context.Steps.Count);
            }

            return context;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Workflow cancelled: {Feature} - {Scenario}",
                workflow.FeatureName,
                workflow.ScenarioName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Workflow failed with exception: {Feature} - {Scenario}",
                workflow.FeatureName,
                workflow.ScenarioName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<ScenarioContext> RunAsync(
        string featureName,
        string scenarioName,
        Func<ScenarioContext, CancellationToken, ValueTask> workflow,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            new DelegateWorkflowDefinition(featureName, scenarioName, workflow),
            cancellationToken);
    }

    /// <summary>
    /// Internal workflow definition that wraps a delegate.
    /// </summary>
    private sealed class DelegateWorkflowDefinition : IWorkflowDefinition
    {
        private readonly Func<ScenarioContext, CancellationToken, ValueTask> _workflow;

        public DelegateWorkflowDefinition(
            string featureName,
            string scenarioName,
            Func<ScenarioContext, CancellationToken, ValueTask> workflow)
        {
            FeatureName = featureName;
            ScenarioName = scenarioName;
            _workflow = workflow;
        }

        public string FeatureName { get; }
        public string ScenarioName { get; }
        public string? FeatureDescription => null;

        public ValueTask ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
            => _workflow(context, cancellationToken);
    }
}
