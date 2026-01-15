using TinyBDD.Xunit;

namespace TinyBDD.Extensions.FileBased.Tests;

[Feature("File-Based DSL")]
public class CalculatorTests : FileBasedTestBase<CalculatorDriver>
{
    [Scenario("Execute calculator scenarios")]
    [Fact]
    public async Task ExecuteCalculatorScenarios()
    {
        await ExecuteScenariosAsync(options =>
        {
            options.AddYamlFiles("TestScenarios/Calculator.yml")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }
}
