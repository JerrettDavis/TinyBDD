using TinyBDD.Extensions.FileBased.Configuration;
using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Tests;

public class FileBasedDslOptionsBuilderTests
{
    private class TestDriver : IApplicationDriver
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void AddFeatureFiles_WithValidPattern_AddsPatternAndSetsGherkinParser()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act
        builder.AddFeatureFiles("**/*.feature")
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.Contains("**/*.feature", options.FilePatterns);
        Assert.IsType<GherkinDslParser>(options.Parser);
    }

    [Fact]
    public void AddFeatureFiles_WithNullPattern_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddFeatureFiles(null!));
    }

    [Fact]
    public void AddFeatureFiles_WithEmptyPattern_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddFeatureFiles(""));
    }

    [Fact]
    public void AddYamlFiles_WithValidPattern_AddsPatternAndSetsYamlParser()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act
        builder.AddYamlFiles("**/*.yml")
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.Contains("**/*.yml", options.FilePatterns);
        Assert.IsType<YamlDslParser>(options.Parser);
    }

    [Fact]
    public void AddYamlFiles_WithNullPattern_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddYamlFiles(null!));
    }

    [Fact]
    public void AddYamlFiles_WithEmptyPattern_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddYamlFiles(""));
    }

    [Fact]
    public void WithBaseDirectory_WithValidPath_SetsBaseDirectory()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();
        var basePath = "/test/path";

        // Act
        builder.AddFeatureFiles("*.feature")
               .WithBaseDirectory(basePath)
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.Equal(basePath, options.BaseDirectory);
    }

    [Fact]
    public void WithBaseDirectory_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithBaseDirectory(null!));
    }

    [Fact]
    public void WithBaseDirectory_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithBaseDirectory(""));
    }

    [Fact]
    public void UseApplicationDriver_SetsDriverType()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act
        builder.AddFeatureFiles("*.feature")
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.Equal(typeof(TestDriver), options.DriverType);
    }

    [Fact]
    public void WithParser_WithValidParser_SetsCustomParser()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();
        var customParser = new YamlDslParser();

        // Act
        builder.AddFeatureFiles("*.feature")
               .WithParser(customParser)
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.Same(customParser, options.Parser);
    }

    [Fact]
    public void WithParser_WithNullParser_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithParser(null!));
    }

    [Fact]
    public void Build_WithoutDriverType_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();
        builder.AddFeatureFiles("*.feature");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Driver type must be specified", exception.Message);
    }

    [Fact]
    public void Build_WithoutFilePatterns_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();
        builder.UseApplicationDriver<TestDriver>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("At least one file pattern must be specified", exception.Message);
    }

    [Fact]
    public void Build_WithMultiplePatterns_IncludesAllPatterns()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act
        builder.AddFeatureFiles("*.feature")
               .AddFeatureFiles("tests/**/*.feature")
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.Equal(2, options.FilePatterns.Count);
        Assert.Contains("*.feature", options.FilePatterns);
        Assert.Contains("tests/**/*.feature", options.FilePatterns);
    }

    [Fact]
    public void Build_AfterAddingYamlThenFeature_UsesGherkinParser()
    {
        // Arrange
        var builder = new FileBasedDslOptionsBuilder();

        // Act - Last parser set wins
        builder.AddYamlFiles("*.yml")
               .AddFeatureFiles("*.feature")
               .UseApplicationDriver<TestDriver>();
        var options = builder.Build();

        // Assert
        Assert.IsType<GherkinDslParser>(options.Parser);
    }

    [Fact]
    public void FluentApi_CanChainAllMethods()
    {
        // Arrange & Act
        var options = new FileBasedDslOptionsBuilder()
            .AddFeatureFiles("*.feature")
            .WithBaseDirectory("/test")
            .UseApplicationDriver<TestDriver>()
            .Build();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(typeof(TestDriver), options.DriverType);
        Assert.Single(options.FilePatterns);
        Assert.Equal("/test", options.BaseDirectory);
    }
}
