namespace TinyBDD.Extensions.FileBased.Tests;

public class FailingCalculatorTests : FileBasedTestBase<CalculatorDriver>
{
    [Fact]
    public async Task ExecuteFailingCalculatorScenarios_ShouldThrowException()
    {
        await Assert.ThrowsAsync<BddStepException>(async () =>
        {
            await ExecuteScenariosAsync(options =>
            {
                options.AddYamlFiles("TestScenarios/FailingCalculator.yml")
                       .WithBaseDirectory(Directory.GetCurrentDirectory());
            });
        });
    }
}
