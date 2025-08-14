namespace TinyBDD;

public static class GherkinFormatter
{
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