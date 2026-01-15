using TinyBDD.Extensions.FileBased.Configuration;

namespace TinyBDD.Extensions.FileBased.Tests;

public class FileDiscoveryTests : IDisposable
{
    private readonly string _testDirectory;

    public FileDiscoveryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TinyBDD_FileDiscovery_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void DiscoverFiles_WithSinglePattern_ReturnsMatchingFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "test1.feature");
        var file2 = Path.Combine(_testDirectory, "test2.feature");
        File.WriteAllText(file1, "Feature: Test1");
        File.WriteAllText(file2, "Feature: Test2");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "*.feature" }, _testDirectory);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(file1, result);
        Assert.Contains(file2, result);
    }

    [Fact]
    public void DiscoverFiles_WithMultiplePatterns_ReturnsCombinedMatches()
    {
        // Arrange
        var featureFile = Path.Combine(_testDirectory, "test.feature");
        var yamlFile = Path.Combine(_testDirectory, "test.yml");
        File.WriteAllText(featureFile, "Feature: Test");
        File.WriteAllText(yamlFile, "feature: Test");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "*.feature", "*.yml" }, _testDirectory);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(featureFile, result);
        Assert.Contains(yamlFile, result);
    }

    [Fact]
    public void DiscoverFiles_WithWildcardInSubdirectory_ReturnsMatchingFiles()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "features");
        Directory.CreateDirectory(subDir);
        var file = Path.Combine(subDir, "test.feature");
        File.WriteAllText(file, "Feature: Test");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "**/*.feature" }, _testDirectory);

        // Assert
        Assert.Single(result);
        Assert.Contains(file, result);
    }

    [Fact]
    public void DiscoverFiles_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var txtFile = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(txtFile, "Some text");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "*.feature" }, _testDirectory);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_WithEmptyPatternList_ReturnsEmptyList()
    {
        // Arrange
        var file = Path.Combine(_testDirectory, "test.feature");
        File.WriteAllText(file, "Feature: Test");

        // Act
        var result = FileDiscovery.DiscoverFiles(Array.Empty<string>(), _testDirectory);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_WithNonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "*.feature" }, nonExistentDir);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_WithNestedDirectories_FindsFilesRecursively()
    {
        // Arrange
        var level1 = Path.Combine(_testDirectory, "level1");
        var level2 = Path.Combine(level1, "level2");
        Directory.CreateDirectory(level2);
        
        var file1 = Path.Combine(_testDirectory, "root.feature");
        var file2 = Path.Combine(level1, "level1.feature");
        var file3 = Path.Combine(level2, "level2.feature");
        
        File.WriteAllText(file1, "Feature: Root");
        File.WriteAllText(file2, "Feature: Level1");
        File.WriteAllText(file3, "Feature: Level2");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "**/*.feature" }, _testDirectory);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(file1, result);
        Assert.Contains(file2, result);
        Assert.Contains(file3, result);
    }

    [Fact]
    public void DiscoverFiles_WithSpecificSubdirectoryPattern_ReturnsOnlyMatchingFiles()
    {
        // Arrange
        var featuresDir = Path.Combine(_testDirectory, "features");
        var testsDir = Path.Combine(_testDirectory, "tests");
        Directory.CreateDirectory(featuresDir);
        Directory.CreateDirectory(testsDir);
        
        var featureFile = Path.Combine(featuresDir, "test.feature");
        var testFile = Path.Combine(testsDir, "test.feature");
        
        File.WriteAllText(featureFile, "Feature: Test");
        File.WriteAllText(testFile, "Feature: Test");

        // Act
        var result = FileDiscovery.DiscoverFiles(new[] { "features/*.feature" }, _testDirectory);

        // Assert
        Assert.Single(result);
        Assert.Contains(featureFile, result);
        Assert.DoesNotContain(testFile, result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }
}
