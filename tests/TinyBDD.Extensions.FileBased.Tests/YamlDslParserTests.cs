using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Tests;

public class YamlDslParserTests
{
    [Fact]
    public async Task ParseAsync_ValidYaml_ReturnsFeatureDefinition()
    {
        // Arrange
        var parser = new YamlDslParser();
        var yamlPath = Path.Combine(Directory.GetCurrentDirectory(), "TestScenarios", "Calculator.yml");

        // Act
        var feature = await parser.ParseAsync(yamlPath);

        // Assert
        Assert.NotNull(feature);
        Assert.Equal("Calculator Operations", feature.Name);
        Assert.Equal("Basic arithmetic operations", feature.Description);
        Assert.Contains("calculator", feature.Tags);
        Assert.Contains("smoke", feature.Tags);
        Assert.Equal(2, feature.Scenarios.Count);
    }

    [Fact]
    public async Task ParseAsync_FirstScenario_HasCorrectSteps()
    {
        // Arrange
        var parser = new YamlDslParser();
        var yamlPath = Path.Combine(Directory.GetCurrentDirectory(), "TestScenarios", "Calculator.yml");

        // Act
        var feature = await parser.ParseAsync(yamlPath);
        var scenario = feature.Scenarios[0];

        // Assert
        Assert.Equal("Add two numbers", scenario.Name);
        Assert.Equal("Verify that two numbers can be added", scenario.Description);
        Assert.Contains("addition", scenario.Tags);
        Assert.Equal(3, scenario.Steps.Count);
        
        Assert.Equal("Given", scenario.Steps[0].Keyword);
        Assert.Equal("a calculator", scenario.Steps[0].Text);
        
        Assert.Equal("When", scenario.Steps[1].Keyword);
        Assert.Equal("I add 5 and 3", scenario.Steps[1].Text);
        // YAML deserializes numbers as strings, int, long, etc. depending on size
        Assert.True(scenario.Steps[1].Parameters.ContainsKey("a"));
        Assert.True(scenario.Steps[1].Parameters.ContainsKey("b"));
        
        Assert.Equal("Then", scenario.Steps[2].Keyword);
        Assert.Equal("the result should be 8", scenario.Steps[2].Text);
        Assert.True(scenario.Steps[2].Parameters.ContainsKey("expected"));
    }

    [Fact]
    public async Task ParseAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var parser = new YamlDslParser();
        var yamlPath = "NonExistent.yml";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await parser.ParseAsync(yamlPath));
    }
}
