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
