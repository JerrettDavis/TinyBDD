namespace TinyBDD;

/// <summary>
/// Disables compile-time source generation optimization for a test method.
/// </summary>
/// <remarks>
/// <para>
/// Starting in TinyBDD v1.1, all BDD test methods are automatically optimized via source
/// generation by default. Use this attribute to opt-out and use the standard pipeline instead.
/// </para>
/// <para>
/// **When to opt-out:**
/// - When using IStepObserver or IScenarioObserver (not yet supported in generated code)
/// - When using BeforeStep/AfterStep hooks
/// - When using complex ScenarioOptions features
/// - When debugging and you want to step through the standard pipeline
/// - When you encounter generator limitations
/// </para>
/// <para>
/// Example:
/// <code>
/// [DisableOptimization]  // Uses standard pipeline
/// public async Task ScenarioWithObservers()
/// {
///     // This will use the standard pipeline with full features
///     await Given("start", () => 1)
///          .When("add", x => x + 1)
///          .Then("equals 2", x => x == 2);
/// }
/// </code>
/// </para>
/// <para>
/// **Performance trade-off:** The standard pipeline adds ~500-800ns overhead per step
/// but provides full extensibility. Generated code runs in ~20-50ns per step.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DisableOptimizationAttribute : Attribute
{
}
