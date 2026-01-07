namespace TinyBDD;

/// <summary>
/// Marks a test method for compile-time optimization via Roslyn source generation.
/// </summary>
/// <remarks>
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
/// **When to use:**
/// - ✅ Performance-critical test suites with many BDD scenarios
/// - ✅ CI/CD pipelines where faster tests mean faster feedback
/// - ✅ Benchmarking scenarios that need accurate measurements
/// - ⚠️ Skip if using IStepObserver or IScenarioObserver (not yet supported)
/// </para>
/// <para>
/// Example:
/// <code>
/// [GenerateOptimized]
/// public async Task MyScenario()
/// {
///     await Given("user ID", () => 123)
///          .When("fetch user", id => GetUser(id))
///          .Then("has email", user => user.Email != null);
/// }
/// </code>
/// </para>
/// <para>
/// The generator creates a `{MethodName}_Optimized` method that the original calls.
/// Generated code is placed in `obj/.../generated/` for inspection during development.
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
