namespace TinyBDD;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ScenarioAttribute(string? name = null, params string[]? tags) : Attribute
{
    public string? Name { get; } = name;
    public string[] Tags { get; } = tags ?? [];
}
