namespace PatternKit.Core;

/// <summary>
/// Factory for creating step handler instances.
/// </summary>
/// <remarks>
/// Implementations can use dependency injection containers to resolve handlers.
/// </remarks>
public interface IStepHandlerFactory
{
    /// <summary>
    /// Creates a handler for the specified step type.
    /// </summary>
    /// <typeparam name="TRequest">The step request type.</typeparam>
    /// <typeparam name="TResponse">The step response type.</typeparam>
    /// <returns>The handler instance, or null if no handler is registered.</returns>
    IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
        where TRequest : IStep<TResponse>;
}

/// <summary>
/// Default step handler factory that uses a dictionary of registered handlers.
/// </summary>
public sealed class DefaultStepHandlerFactory : IStepHandlerFactory
{
    private readonly Dictionary<Type, Func<object>> _factories = new();

    /// <summary>
    /// Registers a handler for a step type.
    /// </summary>
    public DefaultStepHandlerFactory Register<TRequest, TResponse>(
        Func<IStepHandler<TRequest, TResponse>> factory)
        where TRequest : IStep<TResponse>
    {
        _factories[typeof(TRequest)] = () => factory();
        return this;
    }

    /// <summary>
    /// Registers a handler instance for a step type.
    /// </summary>
    public DefaultStepHandlerFactory Register<TRequest, TResponse>(
        IStepHandler<TRequest, TResponse> handler)
        where TRequest : IStep<TResponse>
    {
        _factories[typeof(TRequest)] = () => handler;
        return this;
    }

    /// <inheritdoc />
    public IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
        where TRequest : IStep<TResponse>
    {
        return _factories.TryGetValue(typeof(TRequest), out var factory)
            ? factory() as IStepHandler<TRequest, TResponse>
            : null;
    }
}
