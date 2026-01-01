using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PatternKit.Core;

namespace PatternKit.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering PatternKit services with <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PatternKit core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPatternKit(
        this IServiceCollection services,
        Action<PatternKitOptions>? configure = null)
    {
        var options = new PatternKitOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IStepHandlerFactory, ServiceProviderStepHandlerFactory>();
        services.TryAddSingleton<IWorkflowContextFactory, DefaultWorkflowContextFactory>();

        if (options.UseScopedContexts)
        {
            services.TryAddScoped(sp =>
            {
                var factory = sp.GetRequiredService<IWorkflowContextFactory>();
                return factory.Create("ScopedWorkflow");
            });
        }
        else
        {
            services.TryAddTransient(sp =>
            {
                var factory = sp.GetRequiredService<IWorkflowContextFactory>();
                return factory.Create("TransientWorkflow");
            });
        }

        return services;
    }

    /// <summary>
    /// Registers a step handler with the service collection.
    /// </summary>
    /// <typeparam name="TRequest">The step request type.</typeparam>
    /// <typeparam name="TResponse">The step response type.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Default is <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStepHandler<TRequest, TResponse, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TRequest : IStep<TResponse>
        where THandler : class, IStepHandler<TRequest, TResponse>
    {
        services.Add(new ServiceDescriptor(
            typeof(IStepHandler<TRequest, TResponse>),
            typeof(THandler),
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers a step handler instance with the service collection.
    /// </summary>
    /// <typeparam name="TRequest">The step request type.</typeparam>
    /// <typeparam name="TResponse">The step response type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The handler instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStepHandler<TRequest, TResponse>(
        this IServiceCollection services,
        IStepHandler<TRequest, TResponse> handler)
        where TRequest : IStep<TResponse>
    {
        services.AddSingleton(handler);
        return services;
    }

    /// <summary>
    /// Registers a step handler using a factory function.
    /// </summary>
    /// <typeparam name="TRequest">The step request type.</typeparam>
    /// <typeparam name="TResponse">The step response type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStepHandler<TRequest, TResponse>(
        this IServiceCollection services,
        Func<IServiceProvider, IStepHandler<TRequest, TResponse>> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TRequest : IStep<TResponse>
    {
        services.Add(new ServiceDescriptor(
            typeof(IStepHandler<TRequest, TResponse>),
            factory,
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers a behavior with the service collection.
    /// </summary>
    /// <typeparam name="T">The value type the behavior operates on.</typeparam>
    /// <typeparam name="TBehavior">The behavior implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Default is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBehavior<T, TBehavior>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TBehavior : class, IBehavior<T>
    {
        services.Add(new ServiceDescriptor(
            typeof(IBehavior<T>),
            typeof(TBehavior),
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers a behavior instance with the service collection.
    /// </summary>
    /// <typeparam name="T">The value type the behavior operates on.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="behavior">The behavior instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBehavior<T>(
        this IServiceCollection services,
        IBehavior<T> behavior)
    {
        services.AddSingleton(behavior);
        return services;
    }

    /// <summary>
    /// Registers the timing behavior for the specified type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTimingBehavior<T>(this IServiceCollection services)
    {
        services.AddSingleton<IBehavior<T>>(TimingBehavior<T>.Instance);
        return services;
    }

    /// <summary>
    /// Registers a retry behavior for the specified type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    /// <param name="baseDelay">Base delay between retries.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRetryBehavior<T>(
        this IServiceCollection services,
        int maxRetries = 3,
        TimeSpan? baseDelay = null)
    {
        services.AddSingleton<IBehavior<T>>(new RetryBehavior<T>(maxRetries, baseDelay));
        return services;
    }

    /// <summary>
    /// Registers a circuit breaker behavior for the specified type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="failureThreshold">Failures before opening circuit.</param>
    /// <param name="openDuration">How long circuit stays open.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCircuitBreakerBehavior<T>(
        this IServiceCollection services,
        int failureThreshold = 5,
        TimeSpan? openDuration = null)
    {
        services.AddSingleton<IBehavior<T>>(new CircuitBreakerBehavior<T>(failureThreshold, openDuration));
        return services;
    }
}
