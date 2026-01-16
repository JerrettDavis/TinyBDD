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
    
    [Scenario("Execute scenario outline from .feature file")]
    [Fact]
    public async Task ExecuteScenarioOutlineFeatureFile()
    {
        await ExecuteScenariosAsync(options =>
        {
            options.AddFeatureFiles("Features/ScenarioOutline.feature")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }
    
    [Scenario("Mixing AddFeatureFiles and AddYamlFiles uses last parser set")]
    [Fact]
    public async Task MixedParsers_UsesLastParserSet()
    {
        // This test documents the current behavior where only the last parser is used
        // When both AddFeatureFiles and AddYamlFiles are called, only the last one's parser applies
        
        // This will fail because YAML parser will be used for .feature files
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ExecuteScenariosAsync(options =>
            {
                options.AddFeatureFiles("Features/Calculator.feature")
                       .AddYamlFiles("TestScenarios/Calculator.yml")
                       .WithBaseDirectory(Directory.GetCurrentDirectory());
            });
        });
    }
}
