namespace TinyBDD;

/// <summary>
/// Explicitly marks a test method for compile-time optimization via Roslyn source generation.
/// </summary>
/// <remarks>
/// <para>
/// **NOTE:** Starting in TinyBDD v1.1, source generation optimization is **ENABLED BY DEFAULT**
/// for all BDD test methods. This attribute is **optional** and only needed for:
/// - Explicitly documenting that a method should be optimized
/// - Temporarily disabling optimization via <c>Enabled = false</c>
/// </para>
/// <para>
/// To disable optimization, prefer using <see cref="DisableOptimizationAttribute"/> instead.
/// </para>
/// <para>
/// The source generator transforms fluent BDD chains into direct procedural code,
/// providing dramatic performance improvements:
/// - **16-40x faster execution** (~814ns → ~20-50ns per step)
/// - **9x less memory** (2,568 bytes → ~290 bytes per scenario)
/// - No boxing/unboxing (each step uses strongly-typed variables)
/// - No runtime casts (type transitions handled at compile-time)  
/// - No delegate allocations (lambdas inlined directly)
/// - Optimal JIT inlining (plain method calls)
/// </para>
/// <para>
/// Example (explicit, but not required):
/// <code>
/// [GenerateOptimized]  // Optional - already happens by default
/// public async Task MyScenario()
/// {
///     await Given("user ID", () => 123)
///          .When("fetch user", id => GetUser(id))
///          .Then("has email", user => user.Email != null);
/// }
/// </code>
/// </para>
/// <para>
/// Example (temporarily disable):
/// <code>
/// [GenerateOptimized(Enabled = false)]  // Or use [DisableOptimization]
/// public async Task MyScenario() { ... }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GenerateOptimizedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether optimization is enabled for this method.
    /// Default is true. Set to false to temporarily disable generation
    /// without removing the attribute.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
