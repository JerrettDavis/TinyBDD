namespace TinyBDD;

/// <summary>
/// Holds feature, scenario and step information during a single BDD run.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ScenarioContext"/> is created via <see cref="Bdd.CreateContext(object, string?, ITraitBridge?)"/>
/// or set as ambient using <see cref="Ambient.Current"/> when using the <see cref="Flow"/> API.
/// </para>
/// <para>
/// Test adapters use <see cref="TraitBridge"/> to register tags/categories with the underlying
/// framework while steps are recorded in <see cref="Steps"/> as the scenario executes.
/// </para>
/// </remarks>
public sealed class ScenarioContext
{
    /// <summary>The logical feature under test, e.g. a capability in your system.</summary>
    public string FeatureName { get; }

    /// <summary>Optional human-readable feature description.</summary>
    public string? FeatureDescription { get; }

    /// <summary>The specific scenario name, typically the test method name or <see cref="ScenarioAttribute.Name"/>.</summary>
    public string ScenarioName { get; }

    /// <summary>All tags attached via <see cref="TagAttribute"/> on class/method or <see cref="ScenarioAttribute.Tags"/>.</summary>
    public IReadOnlyList<string> Tags => _tags;

    /// <summary>All recorded steps in execution order.</summary>
    public IReadOnlyList<StepResult> Steps => _steps;

    private readonly List<StepResult> _steps = new();
    private readonly List<string> _tags = new();

    /// <summary>
    /// Bridge for integrating tags/categories with a host test framework.
    /// </summary>
    public ITraitBridge TraitBridge { get; }

    /// <summary>
    /// Creates a new scenario context.
    /// </summary>
    /// <param name="featureName">Feature name.</param>
    /// <param name="featureDescription">Optional feature description.</param>
    /// <param name="scenarioName">Scenario name.</param>
    /// <param name="traitBridge">Bridge for traits/categories.</param>
    public ScenarioContext(
        string featureName,
        string? featureDescription,
        string scenarioName,
        ITraitBridge traitBridge)
    {
        FeatureName = featureName;
        FeatureDescription = featureDescription;
        ScenarioName = scenarioName;
        TraitBridge = traitBridge;
    }

    /// <summary>
    /// Adds a tag to the scenario and forwards it to <see cref="TraitBridge"/>.
    /// </summary>
    public void AddTag(string tag)
    {
        _tags.Add(tag);
        TraitBridge.AddTag(tag);
    }

    /// <summary>
    /// Adds a recorded step to <see cref="Steps"/>. Intended for internal use by the framework.
    /// </summary>
    internal void AddStep(StepResult s) => _steps.Add(s);
}