using PatternKit.Core;

namespace PatternKit.Extensions.DependencyInjection;

/// <summary>
/// Factory for creating <see cref="WorkflowContext"/> instances.
/// </summary>
/// <remarks>
/// This factory integrates with dependency injection to provide properly configured
/// workflow contexts with access to registered services.
/// </remarks>
public interface IWorkflowContextFactory
{
    /// <summary>
    /// Creates a new workflow context with the specified name.
    /// </summary>
    /// <param name="workflowName">The name identifying the workflow.</param>
    /// <param name="description">Optional description of the workflow.</param>
    /// <returns>A configured <see cref="WorkflowContext"/>.</returns>
    WorkflowContext Create(string workflowName, string? description = null);

    /// <summary>
    /// Creates a new workflow context with custom options.
    /// </summary>
    /// <param name="workflowName">The name identifying the workflow.</param>
    /// <param name="options">Custom workflow options.</param>
    /// <param name="description">Optional description of the workflow.</param>
    /// <returns>A configured <see cref="WorkflowContext"/>.</returns>
    WorkflowContext Create(string workflowName, WorkflowOptions options, string? description = null);
}

/// <summary>
/// Default implementation of <see cref="IWorkflowContextFactory"/> that uses
/// the configured <see cref="PatternKitOptions"/>.
/// </summary>
internal sealed class DefaultWorkflowContextFactory : IWorkflowContextFactory
{
    private readonly PatternKitOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public DefaultWorkflowContextFactory(
        PatternKitOptions options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public WorkflowContext Create(string workflowName, string? description = null)
        => Create(workflowName, _options.DefaultWorkflowOptions, description);

    public WorkflowContext Create(string workflowName, WorkflowOptions options, string? description = null)
    {
        var context = new WorkflowContext
        {
            WorkflowName = workflowName,
            Description = description,
            Options = options
        };

        // Attach the service provider as an extension for handler resolution
        context.SetExtension(new ServiceProviderExtension(_serviceProvider));

        return context;
    }
}

/// <summary>
/// Extension that provides access to the service provider from within workflow execution.
/// </summary>
public sealed class ServiceProviderExtension : IWorkflowExtension
{
    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceProviderExtension"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public ServiceProviderExtension(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
