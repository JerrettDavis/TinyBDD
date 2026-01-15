using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Tests;

public class GherkinDslParserTests
{
    [Fact]
    public async Task ParseAsync_ValidFeatureFile_ReturnsFeatureDefinition()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var featurePath = Path.Combine(Directory.GetCurrentDirectory(), "Features", "Calculator.feature");

        // Act
        var feature = await parser.ParseAsync(featurePath);

        // Assert
        Assert.NotNull(feature);
        Assert.Equal("Calculator Operations", feature.Name);
        Assert.Equal(2, feature.Scenarios.Count);
    }

    [Fact]
    public async Task ParseAsync_FirstScenario_HasCorrectSteps()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var featurePath = Path.Combine(Directory.GetCurrentDirectory(), "Features", "Calculator.feature");

        // Act
        var feature = await parser.ParseAsync(featurePath);
        var scenario = feature.Scenarios[0];

        // Assert
        Assert.Equal("Add two numbers", scenario.Name);
        Assert.Contains("calculator", scenario.Tags);
        Assert.Contains("smoke", scenario.Tags);
        Assert.Equal(3, scenario.Steps.Count);
        
        Assert.Equal("Given", scenario.Steps[0].Keyword);
        Assert.Equal("a calculator", scenario.Steps[0].Text);
        
        Assert.Equal("When", scenario.Steps[1].Keyword);
        Assert.Equal("I add 5 and 3", scenario.Steps[1].Text);
        
        Assert.Equal("Then", scenario.Steps[2].Keyword);
        Assert.Equal("the result should be 8", scenario.Steps[2].Text);
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutline_ExpandsToMultipleScenarios()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var featurePath = Path.Combine(Directory.GetCurrentDirectory(), "Features", "ScenarioOutline.feature");

        // Act
        var feature = await parser.ParseAsync(featurePath);

        // Assert
        Assert.NotNull(feature);
        Assert.Equal("Scenario Outline Examples", feature.Name);
        // Should create 3 scenarios (one per example row)
        Assert.Equal(3, feature.Scenarios.Count);
        
        // Verify first expanded scenario
        var scenario1 = feature.Scenarios[0];
        Assert.StartsWith("Multiply two numbers (Example:", scenario1.Name);
        Assert.Contains("outline", scenario1.Tags);
        Assert.Contains("smoke", scenario1.Tags);
        Assert.Equal(3, scenario1.Steps.Count);
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutline_ExpandsStepsWithExampleValues()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var featurePath = Path.Combine(Directory.GetCurrentDirectory(), "Features", "ScenarioOutline.feature");

        // Act
        var feature = await parser.ParseAsync(featurePath);

        // Assert - Check first example (2 * 3 = 6)
        var scenario1 = feature.Scenarios[0];
        Assert.Equal("When", scenario1.Steps[1].Keyword);
        Assert.Equal("I multiply 2 and 3", scenario1.Steps[1].Text);
        Assert.Equal("Then", scenario1.Steps[2].Keyword);
        Assert.Equal("the result should be 6", scenario1.Steps[2].Text);
        
        // Assert - Check second example (4 * 5 = 20)
        var scenario2 = feature.Scenarios[1];
        Assert.Equal("I multiply 4 and 5", scenario2.Steps[1].Text);
        Assert.Equal("the result should be 20", scenario2.Steps[2].Text);
        
        // Assert - Check third example (0 * 9 = 0)
        var scenario3 = feature.Scenarios[2];
        Assert.Equal("I multiply 0 and 9", scenario3.Steps[1].Text);
        Assert.Equal("the result should be 0", scenario3.Steps[2].Text);
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutline_PreservesParametersInSteps()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var featurePath = Path.Combine(Directory.GetCurrentDirectory(), "Features", "ScenarioOutline.feature");

        // Act
        var feature = await parser.ParseAsync(featurePath);

        // Assert - Verify parameters are extracted
        var scenario1 = feature.Scenarios[0];
        Assert.True(scenario1.Steps[1].Parameters.ContainsKey("a"));
        Assert.Equal("2", scenario1.Steps[1].Parameters["a"]);
        Assert.True(scenario1.Steps[1].Parameters.ContainsKey("b"));
        Assert.Equal("3", scenario1.Steps[1].Parameters["b"]);
        Assert.True(scenario1.Steps[2].Parameters.ContainsKey("expected"));
        Assert.Equal("6", scenario1.Steps[2].Parameters["expected"]);
    }

    [Fact]
    public async Task ParseAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var featurePath = "NonExistent.feature";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await parser.ParseAsync(featurePath));
    }
}
