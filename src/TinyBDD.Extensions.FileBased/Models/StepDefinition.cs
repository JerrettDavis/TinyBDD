namespace TinyBDD.Extensions.FileBased.Models;

/// <summary>
/// Represents a single step in a scenario.
/// </summary>
public sealed class StepDefinition
{
    /// <summary>
    /// Step keyword (Given, When, Then, And, But).
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// Step text/description.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional parameters for the step.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();
}
