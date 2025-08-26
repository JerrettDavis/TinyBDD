namespace TinyBDD;

/// <summary>
/// Extends the ScenarioContext class to provide functionality for writing
/// scenarios in Gherkin format, commonly used in BDD (Behavior-Driven Development)
/// tools like Cucumber. This method enables easy integration with custom reporters by formatting and outputting scenario details.
/// </summary>
public static class ScenarioContextGherkinExtensions
{
    /// <summary>
    /// Writes the formatted Gherkin representation of a scenario to an output reporter.
    /// This method delegates to GherkinFormatter.Write to format and output details about the feature,
    /// optional description, scenario, steps, elapsed time, status, and any errors encountered during step execution.
    /// </summary>
    /// <param name="ctx">The current ScenarioContext instance containing all information about the feature and scenario.</param>
    /// <param name="reporter">An IBddReporter that provides a method to output formatted lines of text.</param>
    public static void WriteGherkinTo(
        this ScenarioContext ctx,
        IBddReporter reporter)
        => GherkinFormatter.Write(ctx, reporter);
}