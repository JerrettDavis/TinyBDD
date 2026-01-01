using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PatternKit.Extensions.DependencyInjection;

namespace PatternKit.Extensions.Hosting;

/// <summary>
/// Extension methods for registering PatternKit with <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds PatternKit workflow hosting to the host builder.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configurePatternKit">Optional PatternKit configuration.</param>
    /// <param name="configureHosting">Optional hosting configuration.</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UsePatternKit(
        this IHostBuilder hostBuilder,
        Action<PatternKitOptions>? configurePatternKit = null,
        Action<WorkflowHostingOptions>? configureHosting = null)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddPatternKitHosting(configurePatternKit, configureHosting);
        });
    }

    /// <summary>
    /// Adds PatternKit workflow hosting to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurePatternKit">Optional PatternKit configuration.</param>
    /// <param name="configureHosting">Optional hosting configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPatternKitHosting(
        this IServiceCollection services,
        Action<PatternKitOptions>? configurePatternKit = null,
        Action<WorkflowHostingOptions>? configureHosting = null)
    {
        services.AddPatternKit(configurePatternKit);

        var hostingOptions = new WorkflowHostingOptions();
        configureHosting?.Invoke(hostingOptions);
        services.AddSingleton(hostingOptions);

        services.AddHostedService<WorkflowHostedService>();
        services.AddSingleton<IWorkflowRunner, WorkflowRunner>();

        return services;
    }

    /// <summary>
    /// Registers a workflow definition with the host.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow definition type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflow<TWorkflow>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TWorkflow : class, IWorkflowDefinition
    {
        services.Add(new ServiceDescriptor(
            typeof(IWorkflowDefinition),
            typeof(TWorkflow),
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers a workflow definition instance with the host.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="workflow">The workflow instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflow(
        this IServiceCollection services,
        IWorkflowDefinition workflow)
    {
        services.AddSingleton(workflow);
        return services;
    }

    /// <summary>
    /// Registers a workflow definition using a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflow(
        this IServiceCollection services,
        Func<IServiceProvider, IWorkflowDefinition> factory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.Add(new ServiceDescriptor(
            typeof(IWorkflowDefinition),
            factory,
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers a recurring workflow definition with the host.
    /// </summary>
    /// <typeparam name="TWorkflow">The recurring workflow definition type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRecurringWorkflow<TWorkflow>(
        this IServiceCollection services)
        where TWorkflow : class, IRecurringWorkflowDefinition
    {
        services.AddSingleton<IWorkflowDefinition, TWorkflow>();
        return services;
    }
}
