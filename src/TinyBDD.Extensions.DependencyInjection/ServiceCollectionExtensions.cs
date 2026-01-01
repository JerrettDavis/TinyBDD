using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TinyBDD.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding TinyBDD services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TinyBDD services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddTinyBdd();
    /// </code>
    /// </example>
    public static IServiceCollection AddTinyBdd(this IServiceCollection services)
        => services.AddTinyBdd(_ => { });

    /// <summary>
    /// Adds TinyBDD services to the specified <see cref="IServiceCollection"/> with configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An action to configure <see cref="TinyBddOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddTinyBdd(options =>
    /// {
    ///     options.DefaultScenarioOptions = new ScenarioOptions
    ///     {
    ///         ContinueOnError = true
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTinyBdd(
        this IServiceCollection services,
        Action<TinyBddOptions> configure)
    {
        services.Configure(configure);

        // Register the context factory as scoped so each request/scope gets its own contexts
        services.TryAddScoped<IScenarioContextFactory, ScenarioContextFactory>();

        // Register a default null trait bridge if none is provided
        services.TryAddSingleton<ITraitBridge, NullTraitBridge>();

        return services;
    }

    /// <summary>
    /// Adds a custom <see cref="ITraitBridge"/> implementation to the service collection.
    /// </summary>
    /// <typeparam name="TBridge">The type of trait bridge to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTinyBddTraitBridge<TBridge>(this IServiceCollection services)
        where TBridge : class, ITraitBridge
    {
        services.AddSingleton<ITraitBridge, TBridge>();
        return services;
    }

    /// <summary>
    /// Adds a custom <see cref="ITraitBridge"/> instance to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="traitBridge">The trait bridge instance to use.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTinyBddTraitBridge(
        this IServiceCollection services,
        ITraitBridge traitBridge)
    {
        services.AddSingleton(traitBridge);
        return services;
    }
}
