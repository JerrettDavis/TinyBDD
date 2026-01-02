using Microsoft.Extensions.DependencyInjection;

namespace TinyBDD;

/// <summary>
/// Fluent builder for configuring TinyBDD with observers, output options, and extensibility features.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides an EF Core-style fluent API for adding cross-cutting functionality
/// to TinyBDD scenarios via observers and service registration. Extensions like structured logging,
/// OpenTelemetry, and JSON reporting are added through methods on this builder.
/// </para>
/// <para>
/// The builder is typically used via <see cref="TinyBdd.Configure(System.Action{TinyBddOptionsBuilder})"/>
/// for standalone scenarios, or injected into test adapter base classes for framework integration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = TinyBdd.Configure(builder => builder
///     .AddObserver(new LoggingObserver())
///     .AddObserver(new TelemetryObserver()));
///
/// await TinyBddScenario.With(options)
///     .Given("start", () => 1)
///     .When("add", x => x + 1)
///     .Then("is 2", x => x == 2)
///     .AssertPassed();
/// </code>
/// </example>
/// <seealso cref="TinyBddExtensibilityOptions"/>
/// <seealso cref="IScenarioObserver"/>
/// <seealso cref="IStepObserver"/>
public sealed class TinyBddOptionsBuilder
{
    private readonly List<IScenarioObserver> _scenarioObservers = new();
    private readonly List<IStepObserver> _stepObservers = new();
    private readonly ServiceCollection _services = new();

    /// <summary>
    /// Adds a scenario-level observer to the pipeline.
    /// </summary>
    /// <param name="observer">The observer to add.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <remarks>
    /// Observers are invoked in the order they are added. Multiple observers of the same type can be registered.
    /// </remarks>
    public TinyBddOptionsBuilder AddObserver(IScenarioObserver observer)
    {
        _scenarioObservers.Add(observer);
        return this;
    }

    /// <summary>
    /// Adds a step-level observer to the pipeline.
    /// </summary>
    /// <param name="observer">The observer to add.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <remarks>
    /// Observers are invoked in the order they are added. Multiple observers of the same type can be registered.
    /// </remarks>
    public TinyBddOptionsBuilder AddObserver(IStepObserver observer)
    {
        _stepObservers.Add(observer);
        return this;
    }

    /// <summary>
    /// Configures services available to extensions and observers using Microsoft's IServiceCollection.
    /// </summary>
    /// <param name="configure">An action that configures the service collection.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <remarks>
    /// This uses Microsoft.Extensions.DependencyInjection for service registration,
    /// providing first-class DI features while maintaining flexibility for standalone scenarios.
    /// </remarks>
    public TinyBddOptionsBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Builds the configured <see cref="TinyBddExtensibilityOptions"/> instance.
    /// </summary>
    /// <returns>A configured options instance ready for use.</returns>
    internal TinyBddExtensibilityOptions Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        
        return new TinyBddExtensibilityOptions
        {
            ScenarioObservers = _scenarioObservers.ToArray(),
            StepObservers = _stepObservers.ToArray(),
            ServiceProvider = serviceProvider
        };
    }
}
