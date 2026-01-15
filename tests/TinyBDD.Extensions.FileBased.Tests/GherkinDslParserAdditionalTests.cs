using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Tests;

/// <summary>
/// Additional tests for GherkinDslParser to cover missing branches including file validation and scenario outline expansion.
/// </summary>
public class GherkinDslParserAdditionalTests
{
    [Fact]
    public async Task ParseAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.feature");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await parser.ParseAsync(nonExistentFile));
        
        Assert.Contains("Feature file not found", exception.Message);
        Assert.Contains(nonExistentFile, exception.Message);
    }

    [Fact]
    public async Task ParseAsync_WhenFileContentIsValid_ParsesSuccessfully()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var tempFile = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid()}.feature");
        
        try
        {
            // Create a minimal valid feature file
            await File.WriteAllTextAsync(tempFile, "Feature: Test Feature");

            // Act
            var feature = await parser.ParseAsync(tempFile);
            
            // Assert
            Assert.NotNull(feature);
            Assert.Equal("Test Feature", feature.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutlineWithoutExamples_DoesNotExpand()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var tempFile = Path.Combine(Path.GetTempPath(), $"outline-no-examples-{Guid.NewGuid()}.feature");
        
        try
        {
            var content = @"Feature: Test Feature

Scenario Outline: Test Outline
  Given a value of <input>
  When I process it
  Then the result should be <output>
";
            await File.WriteAllTextAsync(tempFile, content);

            // Act
            var feature = await parser.ParseAsync(tempFile);

            // Assert - Scenario Outline without Examples remains as-is (not expanded)
            Assert.Single(feature.Scenarios);
            Assert.Equal("Test Outline", feature.Scenarios[0].Name);
            Assert.Equal(3, feature.Scenarios[0].Steps.Count);
            // Steps should still contain <placeholders> since no expansion occurred
            Assert.Contains("<input>", feature.Scenarios[0].Steps[0].Text);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutlineWithEmptyExamples_DoesNotExpand()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var tempFile = Path.Combine(Path.GetTempPath(), $"outline-empty-examples-{Guid.NewGuid()}.feature");
        
        try
        {
            var content = @"Feature: Test Feature

Scenario Outline: Test Outline
  Given a value of <input>
  When I process it
  Then the result should be <output>

Examples:
  | input | output |
";
            await File.WriteAllTextAsync(tempFile, content);

            // Act
            var feature = await parser.ParseAsync(tempFile);

            // Assert - Empty examples (header only) should not create expanded scenarios
            Assert.Single(feature.Scenarios);
            Assert.Equal("Test Outline", feature.Scenarios[0].Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseAsync_MultipleScenarioOutlines_ExpandsEachSeparately()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var tempFile = Path.Combine(Path.GetTempPath(), $"multiple-outlines-{Guid.NewGuid()}.feature");
        
        try
        {
            var content = @"Feature: Test Feature

Scenario Outline: First Outline
  Given a value of <x>

Examples:
  | x |
  | 1 |

Scenario Outline: Second Outline
  Given a value of <y>

Examples:
  | y |
  | 2 |
  | 3 |
";
            await File.WriteAllTextAsync(tempFile, content);

            // Act
            var feature = await parser.ParseAsync(tempFile);

            // Assert - Should have 3 scenarios total (1 from first outline, 2 from second)
            Assert.Equal(3, feature.Scenarios.Count);
            Assert.Contains("First Outline", feature.Scenarios[0].Name);
            Assert.Contains("Second Outline", feature.Scenarios[1].Name);
            Assert.Contains("Second Outline", feature.Scenarios[2].Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutlineAtEndOfFile_ExpandsProperly()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var tempFile = Path.Combine(Path.GetTempPath(), $"outline-at-end-{Guid.NewGuid()}.feature");
        
        try
        {
            var content = @"Feature: Test Feature

Scenario Outline: Final Outline
  Given a value of <val>

Examples:
  | val |
  | 10  |
  | 20  |";
            await File.WriteAllTextAsync(tempFile, content);

            // Act
            var feature = await parser.ParseAsync(tempFile);

            // Assert - Scenario at end of file should still expand
            Assert.Equal(2, feature.Scenarios.Count);
            Assert.Contains("10", feature.Scenarios[0].Name);
            Assert.Contains("20", feature.Scenarios[1].Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseAsync_ScenarioOutlineStepWithoutPlaceholder_StaysUnchanged()
    {
        // Arrange
        var parser = new GherkinDslParser();
        var tempFile = Path.Combine(Path.GetTempPath(), $"outline-no-placeholder-{Guid.NewGuid()}.feature");
        
        try
        {
            var content = @"Feature: Test Feature

Scenario Outline: Test
  Given a constant step
  When I use <value>

Examples:
  | value |
  | 5     |
";
            await File.WriteAllTextAsync(tempFile, content);

            // Act
            var feature = await parser.ParseAsync(tempFile);

            // Assert - Step without placeholder stays the same
            Assert.Single(feature.Scenarios);
            Assert.Equal("a constant step", feature.Scenarios[0].Steps[0].Text);
            Assert.Equal("I use 5", feature.Scenarios[0].Steps[1].Text);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
