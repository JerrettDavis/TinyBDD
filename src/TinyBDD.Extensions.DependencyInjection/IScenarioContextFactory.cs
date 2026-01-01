namespace TinyBDD.Extensions.DependencyInjection;

/// <summary>
/// Factory for creating <see cref="ScenarioContext"/> instances with dependency injection support.
/// </summary>
/// <remarks>
/// This factory integrates TinyBDD's scenario context creation with the DI container,
/// allowing scenarios to be configured with container-provided services and options.
/// </remarks>
public interface IScenarioContextFactory
{
    /// <summary>
    /// Creates a new <see cref="ScenarioContext"/> with the specified feature and scenario names.
    /// </summary>
    /// <param name="featureName">The name of the feature being tested.</param>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="featureDescription">Optional description for the feature.</param>
    /// <returns>A new <see cref="ScenarioContext"/> configured with DI-provided options.</returns>
    ScenarioContext Create(
        string featureName,
        string scenarioName,
        string? featureDescription = null);

    /// <summary>
    /// Creates a new <see cref="ScenarioContext"/> from an object's type attributes.
    /// </summary>
    /// <param name="featureSource">An object whose type is inspected for <see cref="FeatureAttribute"/> and <see cref="TagAttribute"/>.</param>
    /// <param name="scenarioName">Optional scenario name override.</param>
    /// <returns>A new <see cref="ScenarioContext"/> populated from attributes and DI options.</returns>
    ScenarioContext CreateFromAttributes(
        object featureSource,
        string? scenarioName = null);
}
