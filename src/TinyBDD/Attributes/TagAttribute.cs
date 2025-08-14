namespace TinyBDD;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class TagAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}