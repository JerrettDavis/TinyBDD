using TinyBDD.Extensions.FileBased.Core;

namespace TinyBDD.Extensions.FileBased.Tests;

public class DriverMethodAttributeTests
{
    [Fact]
    public void Constructor_WithValidPattern_StoresPattern()
    {
        // Arrange
        var pattern = "I perform {action}";

        // Act
        var attribute = new DriverMethodAttribute(pattern);

        // Assert
        Assert.Equal(pattern, attribute.StepPattern);
    }

    [Fact]
    public void Constructor_WithNullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        string? pattern = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DriverMethodAttribute(pattern!));
        Assert.Equal("stepPattern", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyPattern_StoresEmptyString()
    {
        // Arrange
        var pattern = string.Empty;

        // Act
        var attribute = new DriverMethodAttribute(pattern);

        // Assert
        Assert.Equal(string.Empty, attribute.StepPattern);
    }

    [Fact]
    public void Constructor_WithComplexPattern_StoresPattern()
    {
        // Arrange
        var pattern = "I add {a} and {b} with operator {op}";

        // Act
        var attribute = new DriverMethodAttribute(pattern);

        // Assert
        Assert.Equal(pattern, attribute.StepPattern);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_StoresPattern()
    {
        // Arrange
        var pattern = "I test with special chars: +*[]()";

        // Act
        var attribute = new DriverMethodAttribute(pattern);

        // Assert
        Assert.Equal(pattern, attribute.StepPattern);
    }

    [Fact]
    public void Attribute_CanBeAppliedToMethod()
    {
        // Arrange & Act
        var method = typeof(TestDriver).GetMethod(nameof(TestDriver.TestMethod));
        var attributes = method?.GetCustomAttributes(typeof(DriverMethodAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.NotNull(attributes);
        Assert.Single(attributes);
        var attr = attributes[0] as DriverMethodAttribute;
        Assert.NotNull(attr);
        Assert.Equal("test pattern", attr.StepPattern);
    }

    [Fact]
    public void Attribute_AllowsMultiple()
    {
        // Arrange & Act
        var method = typeof(TestDriver).GetMethod(nameof(TestDriver.MultiplePatternMethod));
        var attributes = method?.GetCustomAttributes(typeof(DriverMethodAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.NotNull(attributes);
        Assert.Equal(2, attributes.Length);
    }

    private class TestDriver
    {
        [DriverMethod("test pattern")]
        public void TestMethod() { }

        [DriverMethod("pattern 1")]
        [DriverMethod("pattern 2")]
        public void MultiplePatternMethod() { }
    }
}
