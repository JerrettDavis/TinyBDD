namespace TinyBDD;

/// <summary>
/// Declares a tag on a feature class or scenario method. Tags are recorded in
/// <see cref="ScenarioContext.Tags"/> and forwarded to the active <see cref="ITraitBridge"/>
/// so test frameworks can expose them as categories/traits.
/// </summary>
/// <example>
/// <code>
/// [Feature("Orders")]
/// [Tag("integration"), Tag("slow")]
/// public class OrderTests { /* ... */ }
/// 
/// public class ShippingTests
/// {
///     [Scenario("Ships overnight"), Tag("shipping"), Tag("fast")]
///     public async Task ShipsOvernight() { /* ... */ }
/// }
/// </code>
/// </example>
/// <seealso cref="ScenarioAttribute"/>
/// <seealso cref="FeatureAttribute"/>
/// <seealso cref="ITraitBridge"/>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class TagAttribute(string name) : Attribute
{
    /// <summary>The tag value.</summary>
    public string Name { get; } = name;
}