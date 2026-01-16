namespace TinyBDD.Extensions.FileBased.Models;

/// <summary>
/// Represents a feature containing one or more scenarios.
/// </summary>
public sealed class FeatureDefinition
{
    /// <summary>
    /// Feature name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional feature description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Scenarios in this feature.
    /// </summary>
    public List<ScenarioDefinition> Scenarios { get; set; } = new();

    /// <summary>
    /// Tags applied to the feature.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
