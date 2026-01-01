namespace TinyBDD.Tests.Common.SetupTeardown;

public class AssemblySetupAttributeTests
{
    [Fact]
    public void AssemblySetupAttribute_Constructor_SetsFixtureType()
    {
        // Arrange & Act
        var attribute = new AssemblySetupAttribute(typeof(TestAssemblyFixture));

        // Assert
        Assert.Equal(typeof(TestAssemblyFixture), attribute.FixtureType);
    }

    [Fact]
    public void AssemblySetupAttribute_WithNonFixtureType_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => new AssemblySetupAttribute(typeof(string)));

        Assert.Contains("must derive from AssemblyFixture", ex.Message);
    }

    [Fact]
    public void AssemblySetupAttribute_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new AssemblySetupAttribute(null!));
    }

    [Fact]
    public void AssemblySetupAttribute_WithAbstractFixture_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => new AssemblySetupAttribute(typeof(AbstractTestFixture)));

        Assert.Contains("cannot be abstract", ex.Message);
    }

    [Fact]
    public void AssemblySetupAttribute_WithValidFixture_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        var attribute = new AssemblySetupAttribute(typeof(TestAssemblyFixture));
        Assert.NotNull(attribute);
    }
}

public abstract class AbstractTestFixture : AssemblyFixture
{
}
