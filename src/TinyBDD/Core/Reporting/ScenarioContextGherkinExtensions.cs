namespace TinyBDD;

/// <summary>
/// Extension methods for writing a <see cref="ScenarioContext"/> in Gherkin format via an <see cref="IBddReporter"/>.
/// </summary>
/// <remarks>
/// These helpers delegate to <see cref="GherkinFormatter"/> to format the context into human-friendly text.
/// Use them after a scenario has executed to emit a compact report.
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "start", () => 2)
///          .When("double", x => x * 2)
///          .Then(">= 4", v => v >= 4);
/// ctx.WriteGherkinTo(new StringBddReporter());
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="GherkinFormatter"/>
/// <seealso cref="IBddReporter"/>
public static class ScenarioContextGherkinExtensions
{
    /// <summary>
    /// Writes the formatted Gherkin representation of a scenario to an output reporter.
    /// This method delegates to <see cref="GherkinFormatter.Write(ScenarioContext, IBddReporter)"/> to format and output details about the feature,
    /// optional description, scenario, steps, elapsed time, status, and any errors encountered during step execution.
    /// </summary>
    /// <param name="ctx">The current <see cref="ScenarioContext"/> containing feature and scenario information.</param>
    /// <param name="reporter">An <see cref="IBddReporter"/> that receives formatted lines of text.</param>
    public static void WriteGherkinTo(
        this ScenarioContext ctx,
        IBddReporter reporter)
        => GherkinFormatter.Write(ctx, reporter);
}