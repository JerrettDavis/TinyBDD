namespace TinyBDD;

/// <summary>
/// Marks a test class as a BDD <c>Feature</c>, providing a human-friendly name and an optional description.
/// </summary>
/// <remarks>
/// The attribute is read by <see cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/> to populate
/// <see cref="ScenarioContext.FeatureName"/> and <see cref="ScenarioContext.FeatureDescription"/>.
/// </remarks>
/// <example>
/// <code>
/// [Feature("Shopping Cart", "Customers can add and remove items")]
/// public class CartTests : TinyBddXunitBase { /* ... */ }
/// </code>
/// </example>
/// <seealso cref="ScenarioAttribute"/>
/// <seealso cref="TagAttribute"/>
/// <seealso cref="ScenarioContext"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FeatureAttribute : Attribute
{
    /// <summary>The user-facing feature name displayed in reports.</summary>
    public string Name { get; }

    /// <summary>Optional feature description shown beneath the feature name.</summary>
    public string? Description { get; }

    /// <summary>Creates a new <see cref="FeatureAttribute"/>.</summary>
    /// <param name="name">The feature name to display in reports.</param>
    /// <param name="description">Optional human-readable description shown under the feature.</param>
    public FeatureAttribute(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}
