using System.Text.Json;

namespace TinyBDD.Extensions.Reporting;

/// <summary>
/// Extension methods for adding JSON reporting to TinyBDD scenarios.
/// </summary>
public static class TinyBddReportingExtensions
{
    /// <summary>
    /// Adds JSON reporting to the scenario pipeline.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <param name="outputPath">Path where the JSON report will be written.</param>
    /// <param name="jsonOptions">Optional JSON serialization options.</param>
    /// <returns>The builder for fluent chaining.</returns>
    /// <remarks>
    /// The JSON report is written after each scenario completes, capturing feature, scenario,
    /// step metadata, timings, and results. The report is suitable for CI artifacts and
    /// trend analysis.
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = TinyBdd.Configure(builder => builder
    ///     .AddJsonReport("artifacts/tinybdd.json"));
    ///
    /// var ctx = Bdd.CreateContext(this, options: options);
    /// await Bdd.Given(ctx, "start", () => 1)
    ///     .When("add", x => x + 1)
    ///     .Then("is 2", x => x == 2);
    /// </code>
    /// </example>
    public static TinyBddOptionsBuilder AddJsonReport(
        this TinyBddOptionsBuilder builder,
        string outputPath,
        JsonSerializerOptions? jsonOptions = null)
    {
        var observer = new JsonReportObserver(outputPath, jsonOptions);
        builder.AddObserver((IScenarioObserver)observer);
        builder.AddObserver((IStepObserver)observer);
        return builder;
    }
}
