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
/// <para>Example: writing a simple scenario</para>
/// <code>
/// // Arrange
/// var ctx = new ScenarioContext(
///     featureName: "Login",
///     featureDescription: "Users can sign in with valid credentials.",
///     scenarioName: "Successful sign-in",
///     steps: new[]
///     {
///         new StepContext(StepKind.Given, "a registered user exists", TimeSpan.FromMilliseconds(12)),
///         new StepContext(StepKind.When,  "they sign in with correct credentials", TimeSpan.FromMilliseconds(34)),
///         new StepContext(StepKind.Then,  "they are redirected to the dashboard", TimeSpan.FromMilliseconds(9))
///     });
///
/// IBddReporter reporter = new ConsoleBddReporter(); // your implementation
///
/// // Act
/// GherkinFormatter.Write(ctx, reporter);
///
/// // Possible output:
/// // Feature: Login
/// //   Users can sign in with valid credentials.
/// // Scenario: Successful sign-in
/// //   Given a registered user exists [OK] 12 ms
/// //   When they sign in with correct credentials [OK] 34 ms
/// //   Then they are redirected to the dashboard [OK] 9 ms
/// </code>
/// </example>
/// <example>
/// <para>Example: error reporting</para>
/// <code>
/// var failing = new ScenarioContext(
///     featureName: "Profile",
///     featureDescription: "",
///     scenarioName: "Show profile picture",
///     steps: new[]
///     {
///         new StepContext(StepKind.Given, "a user with a profile", TimeSpan.FromMilliseconds(5)),
///         new StepContext(
///             StepKind.When,
///             "they open their profile page",
///             TimeSpan.FromMilliseconds(27),
///             error: new InvalidOperationException("Profile service unavailable"))
///     });
///
/// GherkinFormatter.Write(failing, reporter);
///
/// // Output:
/// // Feature: Profile
/// // Scenario: Show profile picture
/// //   Given a user with a profile [OK] 5 ms
/// //   When they open their profile page [FAIL] 27 ms
/// //     Error: InvalidOperationException: Profile service unavailable
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="IBddReporter"/>
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