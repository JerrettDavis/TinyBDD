namespace TinyBDD;

public sealed class ScenarioContext
{
    public string FeatureName { get; }
    public string? FeatureDescription { get; }
    public string ScenarioName { get; }
    public IReadOnlyList<string> Tags => _tags;
    public IReadOnlyList<StepResult> Steps => _steps;

    private readonly List<StepResult> _steps = new();
    private readonly List<string> _tags = new();
    public ITraitBridge TraitBridge { get; }

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

    public void AddTag(string tag)
    {
        _tags.Add(tag);
        TraitBridge.AddTag(tag);
    }

    internal void AddStep(StepResult s) => _steps.Add(s);
}