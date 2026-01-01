using Microsoft.Extensions.DependencyInjection;
using PatternKit.Core;

namespace PatternKit.Extensions.DependencyInjection;

/// <summary>
/// A step handler factory that resolves handlers from the dependency injection container.
/// </summary>
public sealed class ServiceProviderStepHandlerFactory : IStepHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceProviderStepHandlerFactory"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve handlers from.</param>
    public ServiceProviderStepHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IStepHandler<TRequest, TResponse>? Create<TRequest, TResponse>()
        where TRequest : IStep<TResponse>
    {
        return _serviceProvider.GetService<IStepHandler<TRequest, TResponse>>();
    }
}
