namespace TinyBDD.Extensions.FileBased.Models;

/// <summary>
/// Represents a single scenario with steps.
/// </summary>
public sealed class ScenarioDefinition
{
    /// <summary>
    /// Scenario name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional scenario description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Steps in this scenario.
    /// </summary>
    public List<StepDefinition> Steps { get; set; } = new();

    /// <summary>
    /// Tags applied to the scenario.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
