using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TinyBDD.Extensions.DependencyInjection;

namespace TinyBDD.Extensions.Hosting;

/// <summary>
/// Extension methods for adding TinyBDD hosting services to an <see cref="IHostBuilder"/>
/// or <see cref="IServiceCollection"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds TinyBDD hosting services to the host builder.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same host builder for chaining.</returns>
    public static IHostBuilder UseTinyBdd(this IHostBuilder builder)
        => builder.UseTinyBdd(_ => { });

    /// <summary>
    /// Adds TinyBDD hosting services to the host builder with configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">An action to configure <see cref="TinyBddHostingOptions"/>.</param>
    /// <returns>The same host builder for chaining.</returns>
    public static IHostBuilder UseTinyBdd(
        this IHostBuilder builder,
        Action<TinyBddHostingOptions> configure)
    {
        return builder.ConfigureServices((_, services) =>
        {
            services.AddTinyBddHosting(configure);
        });
    }

    /// <summary>
    /// Adds TinyBDD hosting services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTinyBddHosting(this IServiceCollection services)
        => services.AddTinyBddHosting(_ => { });

    /// <summary>
    /// Adds TinyBDD hosting services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure <see cref="TinyBddHostingOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTinyBddHosting(
        this IServiceCollection services,
        Action<TinyBddHostingOptions> configure)
    {
        // Add core TinyBDD DI services
        services.AddTinyBdd();

        // Configure hosting options
        services.Configure(configure);

        // Register the workflow runner
        services.TryAddScoped<IWorkflowRunner, WorkflowRunner>();

        return services;
    }

    /// <summary>
    /// Adds a workflow as a hosted service that executes on startup.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow definition type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowHostedService<TWorkflow>(this IServiceCollection services)
        where TWorkflow : class, IWorkflowDefinition
    {
        services.AddSingleton<TWorkflow>();
        services.AddHostedService<WorkflowHostedService<TWorkflow>>();
        return services;
    }

    /// <summary>
    /// Adds a workflow instance as a hosted service that executes on startup.
    /// </summary>
    /// <typeparam name="TWorkflow">The workflow definition type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="workflow">The workflow instance.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowHostedService<TWorkflow>(
        this IServiceCollection services,
        TWorkflow workflow)
        where TWorkflow : class, IWorkflowDefinition
    {
        services.AddSingleton(workflow);
        services.AddHostedService<WorkflowHostedService<TWorkflow>>();
        return services;
    }
}
