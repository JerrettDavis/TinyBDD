using Microsoft.Extensions.Options;

namespace TinyBDD.Extensions.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="IScenarioContextFactory"/> that creates
/// scenario contexts using DI-configured options.
/// </summary>
internal sealed class ScenarioContextFactory : IScenarioContextFactory
{
    private readonly TinyBddOptions _options;
    private readonly ITraitBridge _traitBridge;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioContextFactory"/> class.
    /// </summary>
    /// <param name="options">The TinyBDD options from the DI container.</param>
    /// <param name="traitBridge">Optional trait bridge for test framework integration.</param>
    public ScenarioContextFactory(
        IOptions<TinyBddOptions> options,
        ITraitBridge? traitBridge = null)
    {
        _options = options.Value;
        _traitBridge = traitBridge ?? new NullTraitBridge();
    }

    /// <inheritdoc />
    public ScenarioContext Create(
        string featureName,
        string scenarioName,
        string? featureDescription = null)
    {
        return new ScenarioContext(
            featureName,
            featureDescription,
            scenarioName,
            _traitBridge,
            _options.DefaultScenarioOptions with { });
    }

    /// <inheritdoc />
    public ScenarioContext CreateFromAttributes(
        object featureSource,
        string? scenarioName = null)
    {
        return Bdd.CreateContext(
            featureSource,
            scenarioName,
            _traitBridge,
            _options.DefaultScenarioOptions with { });
    }
}
