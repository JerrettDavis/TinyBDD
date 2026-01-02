namespace TinyBDD;

/// <summary>
/// Lightweight service registry for extensions and observers.
/// </summary>
/// <remarks>
/// This provides a minimal DI container for standalone scenarios without requiring
/// Microsoft.Extensions.DependencyInjection. Services are singleton-scoped.
/// </remarks>
public interface ITinyBddServiceRegistry
{
    /// <summary>
    /// Registers a service instance.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    void AddSingleton<TService>(TService instance) where TService : notnull;

    /// <summary>
    /// Registers a service with a factory function.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">Factory function to create the service instance.</param>
    void AddSingleton<TService>(Func<TService> factory) where TService : notnull;
}

/// <summary>
/// Service registry for reading registered services.
/// </summary>
internal interface IServiceRegistry
{
    /// <summary>
    /// Gets a registered service.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>The service instance, or null if not registered.</returns>
    TService? GetService<TService>() where TService : class;
}

/// <summary>
/// Implementation of the service registry for building.
/// </summary>
internal sealed class TinyBddServiceRegistry : ITinyBddServiceRegistry
{
    private readonly Dictionary<Type, object> _services;

    public TinyBddServiceRegistry(Dictionary<Type, object> services)
    {
        _services = services;
    }

    public void AddSingleton<TService>(TService instance) where TService : notnull
    {
        _services[typeof(TService)] = instance;
    }

    public void AddSingleton<TService>(Func<TService> factory) where TService : notnull
    {
        _services[typeof(TService)] = new Lazy<TService>(factory);
    }
}

/// <summary>
/// Read-only view of the service registry.
/// </summary>
internal sealed class ReadOnlyServiceRegistry : IServiceRegistry
{
    private readonly IReadOnlyDictionary<Type, object> _services;

    public ReadOnlyServiceRegistry(Dictionary<Type, object> services)
    {
        _services = services;
    }

    public TService? GetService<TService>() where TService : class
    {
        if (!_services.TryGetValue(typeof(TService), out var service))
            return null;

        return service switch
        {
            Lazy<TService> lazy => lazy.Value,
            TService instance => instance,
            _ => null
        };
    }
}
