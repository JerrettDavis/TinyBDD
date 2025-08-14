namespace TinyBDD;

public static class ScenarioContextGherkinExtensions
{
    public static void WriteGherkinTo(this ScenarioContext ctx, IBddReporter reporter)
        => GherkinFormatter.Write(ctx, reporter);
}