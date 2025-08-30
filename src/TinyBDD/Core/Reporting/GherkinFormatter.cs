namespace TinyBDD;

/// <summary>
/// Formats a <see cref="ScenarioContext"/> into Gherkin-style text and writes it
/// to an <see cref="IBddReporter"/>.
/// </summary>
/// <remarks>
/// The output format is:
/// <code>
/// Feature: &lt;FeatureName&gt;
///   &lt;FeatureDescription (optional)&gt;
/// Scenario: &lt;ScenarioName&gt;
///   &lt;StepKind&gt; &lt;StepTitle&gt; [OK|FAIL] &lt;ElapsedMs&gt; ms
///     Error: &lt;ExceptionType&gt;: &lt;Message&gt;    (only when a step fails)
/// </code>
/// Behavior:
/// <list type="bullet">
/// <item>
/// <description><c>FeatureDescription</c> is written only when non-empty.</description>
/// </item>
/// <item>
/// <description>Each step line includes status (<c>OK</c> or <c>FAIL</c>) and elapsed time in milliseconds (rounded).</description>
/// </item>
/// <item>
/// <description>When a step has an error, an indented <c>Error:</c> line follows with the exception type and message.</description>
/// </item>
/// </list>
/// <para>
/// This type is stateless and thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <para>Example: writing a simple scenario report</para>
/// <code>
/// // Execute a scenario to populate the context
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "start", () => 2)
///          .When("double", x => x * 2)
///          .Then(">= 4", v => v >= 4);
///
/// // Write as Gherkin
/// var reporter = new StringBddReporter();
/// GherkinFormatter.Write(ctx, reporter);
/// Console.WriteLine(reporter.ToString());
///
/// // Possible output:
/// // Feature: &lt;YourTestClassNameOrFeatureAttributeName&gt;
/// // Scenario: &lt;TestMethodNameOrScenarioAttributeName&gt;
/// //   Given start [OK] 1 ms
/// //   When double [OK] 0 ms
/// //   Then >= 4 [OK] 0 ms
/// </code>
/// </example>
/// <example>
/// <para>Example: error reporting</para>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "seed", () => 1)
///          .When("boom", _ => throw new InvalidOperationException("nope"))
///          .Then("unreached", () => Task.CompletedTask);
///
/// var reporter = new StringBddReporter();
/// GherkinFormatter.Write(ctx, reporter);
/// Console.WriteLine(reporter.ToString());
///
/// // Output will include a FAIL line and an Error: detail for the When step.
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="IBddReporter"/>
/// <seealso cref="StringBddReporter"/>
public static class GherkinFormatter
{
    /// <summary>
    /// Writes the provided <see cref="ScenarioContext"/> to the <paramref name="reporter"/>
    /// using a compact Gherkin-style format.
    /// </summary>
    /// <param name="ctx">The scenario context containing feature, scenario, and step data to render.</param>
    /// <param name="reporter">The reporter that receives the formatted lines.</param>
    /// <remarks>
    /// <para>
    /// This method expects non-null arguments. If <paramref name="ctx"/> or <paramref name="reporter"/> is null,
    /// a runtime exception (e.g., <see cref="NullReferenceException"/>) may occur.
    /// </para>
    /// <para>
    /// Elapsed time is rounded to the nearest millisecond and shown as
    /// <c>&lt;number&gt; ms</c>.
    /// </para>
    /// </remarks>
    public static void Write(ScenarioContext ctx, IBddReporter reporter)
    {
        reporter.WriteLine($"Feature: {ctx.FeatureName}");
        if (!string.IsNullOrWhiteSpace(ctx.FeatureDescription))
            reporter.WriteLine($"  {ctx.FeatureDescription}");
        reporter.WriteLine($"Scenario: {ctx.ScenarioName}");

        foreach (var s in ctx.Steps)
        {
            var status = s.Error is null ? "OK" : "FAIL";
            var ms = $"{(long)Math.Round(s.Elapsed.TotalMilliseconds)} ms";
            reporter.WriteLine($"  {s.Kind} {s.Title} [{status}] {ms}");
            if (s.Error is not null)
                reporter.WriteLine($"    Error: {s.Error.GetType().Name}: {s.Error.Message}");
        }
    }
}