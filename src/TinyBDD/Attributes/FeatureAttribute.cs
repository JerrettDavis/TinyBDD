namespace TinyBDD;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FeatureAttribute : Attribute
{
    public string Name { get; }
    public string? Description { get; }
    public FeatureAttribute(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}
