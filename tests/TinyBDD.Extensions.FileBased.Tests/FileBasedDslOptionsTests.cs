using TinyBDD.Extensions.FileBased.Configuration;
using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Tests;

public class FileBasedDslOptionsTests
{
    [Fact]
    public void DefaultConstructor_InitializesFilePatterns()
    {
        // Arrange & Act
        var options = new FileBasedDslOptions();

        // Assert
        Assert.NotNull(options.FilePatterns);
        Assert.Empty(options.FilePatterns);
    }

    [Fact]
    public void DefaultConstructor_InitializesParserToYaml()
    {
        // Arrange & Act
        var options = new FileBasedDslOptions();

        // Assert
        Assert.NotNull(options.Parser);
        Assert.IsType<YamlDslParser>(options.Parser);
    }

    [Fact]
    public void DefaultConstructor_InitializesBaseDirectoryToCurrentDirectory()
    {
        // Arrange & Act
        var options = new FileBasedDslOptions();

        // Assert
        Assert.NotNull(options.BaseDirectory);
        Assert.Equal(Directory.GetCurrentDirectory(), options.BaseDirectory);
    }

    [Fact]
    public void DefaultConstructor_InitializesDriverTypeToNull()
    {
        // Arrange & Act
        var options = new FileBasedDslOptions();

        // Assert
        Assert.Null(options.DriverType);
    }

    [Fact]
    public void FilePatterns_CanAddPatterns()
    {
        // Arrange
        var options = new FileBasedDslOptions();

        // Act
        options.FilePatterns.Add("*.feature");
        options.FilePatterns.Add("*.yml");

        // Assert
        Assert.Equal(2, options.FilePatterns.Count);
        Assert.Contains("*.feature", options.FilePatterns);
        Assert.Contains("*.yml", options.FilePatterns);
    }

    [Fact]
    public void Parser_CanBeSet()
    {
        // Arrange
        var options = new FileBasedDslOptions();
        var newParser = new GherkinDslParser();

        // Act
        options.Parser = newParser;

        // Assert
        Assert.Same(newParser, options.Parser);
    }

    [Fact]
    public void BaseDirectory_CanBeSet()
    {
        // Arrange
        var options = new FileBasedDslOptions();
        var newDirectory = "/custom/path";

        // Act
        options.BaseDirectory = newDirectory;

        // Assert
        Assert.Equal(newDirectory, options.BaseDirectory);
    }

    [Fact]
    public void DriverType_CanBeSet()
    {
        // Arrange
        var options = new FileBasedDslOptions();
        var driverType = typeof(TestDriver);

        // Act
        options.DriverType = driverType;

        // Assert
        Assert.Equal(driverType, options.DriverType);
    }

    private class TestDriver { }
}
