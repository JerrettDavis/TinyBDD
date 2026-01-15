using TinyBDD.Xunit;

namespace TinyBDD.Extensions.FileBased.Tests;

[Feature("File-Based DSL - Gherkin")]
public class FeatureFileTests : FileBasedTestBase<CalculatorDriver>
{
    [Scenario("Execute calculator scenarios from .feature file")]
    [Fact]
    public async Task ExecuteCalculatorFeatureFile()
    {
        await ExecuteScenariosAsync(options =>
        {
            options.AddFeatureFiles("Features/Calculator.feature")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }
}
