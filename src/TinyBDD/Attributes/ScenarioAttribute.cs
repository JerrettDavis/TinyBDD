namespace TinyBDD;

/// <summary>
/// Marks a test method as a BDD <c>Scenario</c>, optionally providing a friendly name and tags.
/// </summary>
/// <remarks>
/// The attribute is read by <see cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/> to resolve
/// the scenario name and to collect tags alongside any <see cref="TagAttribute"/>s.
/// </remarks>
/// <example>
/// <code>
/// [Feature("Checkout")]
/// public class CheckoutTests
/// {
///     [Scenario("Successful purchase", "happy-path", "checkout")]
///     public async Task Purchase_Succeeds() { /* ... */ }
/// }
/// </code>
/// </example>
/// <seealso cref="FeatureAttribute"/>
/// <seealso cref="TagAttribute"/>
/// <seealso cref="ScenarioContext"/>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ScenarioAttribute(string? name = null, params string[]? tags) : Attribute
{
    /// <summary>Optional scenario name shown in reports; falls back to the method name when omitted.</summary>
    public string? Name { get; } = name;

    /// <summary>Optional tags associated with the scenario.</summary>
    public string[] Tags { get; } = tags ?? [];
}
