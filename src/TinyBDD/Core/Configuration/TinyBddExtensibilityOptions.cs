namespace TinyBDD;

/// <summary>
/// Configuration options for TinyBDD scenarios with extensibility support.
/// </summary>
/// <remarks>
/// This class is built via <see cref="TinyBddOptionsBuilder"/> and contains registered
/// observers and services. It is typically constructed through <see cref="TinyBdd.Configure(System.Action{TinyBddOptionsBuilder})"/>.
/// </remarks>
/// <seealso cref="TinyBddOptionsBuilder"/>
/// <seealso cref="IScenarioObserver"/>
/// <seealso cref="IStepObserver"/>
public sealed class TinyBddExtensibilityOptions
{
    /// <summary>
    /// Gets the registered scenario-level observers.
    /// </summary>
    internal IReadOnlyList<IScenarioObserver> ScenarioObservers { get; init; } = Array.Empty<IScenarioObserver>();

    /// <summary>
    /// Gets the registered step-level observers.
    /// </summary>
    internal IReadOnlyList<IStepObserver> StepObservers { get; init; } = Array.Empty<IStepObserver>();

    /// <summary>
    /// Gets the service registry for extensions.
    /// </summary>
    internal IServiceRegistry Services { get; init; } = new ReadOnlyServiceRegistry(new Dictionary<Type, object>());
}
