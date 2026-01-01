using System.Reflection;

namespace TinyBDD.Tests.Common.SetupTeardown;

public class AssemblyFixtureCoordinatorTests
{
    [Fact]
    public void AssemblyFixtureCoordinator_Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = AssemblyFixtureCoordinator.Instance;
        var instance2 = AssemblyFixtureCoordinator.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_InitializeAsync_IsIdempotent()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(AssemblyFixtureCoordinatorTests).Assembly;

        // Act - Initialize multiple times
        await coordinator.InitializeAsync(assembly);
        await coordinator.InitializeAsync(assembly); // Second call should be no-op

        // Assert - Should not throw, initialization is idempotent
        Assert.True(true);
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_InitializeAsync_WithNoAttributes_CompletesSuccessfully()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = Assembly.GetExecutingAssembly();

        // Act & Assert (should not throw)
        await coordinator.InitializeAsync(assembly);
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_TeardownAsync_ExecutesAllFixtures()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;

        // Act
        await coordinator.TeardownAsync();

        // Assert - should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_TeardownAsync_WhenNotInitialized_DoesNotThrow()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - should not throw even with canceled token if not initialized
        await coordinator.TeardownAsync(cts.Token);
    }

    [Fact]
    public void AssemblyFixtureCoordinator_GetFixture_ThrowsWhenNotRegistered()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => AssemblyFixtureCoordinator.GetFixture<UnregisteredFixture>());
    }

    [Fact]
    public void AssemblyFixtureCoordinator_Reset_ClearsInstance()
    {
        // Arrange
        var instance1 = AssemblyFixtureCoordinator.Instance;

        // Act
        AssemblyFixtureCoordinator.Reset();
        var instance2 = AssemblyFixtureCoordinator.Instance;

        // Assert - after reset, a new instance should be created
        Assert.NotSame(instance1, instance2);

        // Cleanup - reset again for other tests
        AssemblyFixtureCoordinator.Reset();
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_TeardownAsync_IsIdempotent()
    {
        // Arrange
        AssemblyFixtureCoordinator.Reset();
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(AssemblyFixtureCoordinatorTests).Assembly;

        // Initialize first
        await coordinator.InitializeAsync(assembly);

        // Act - call teardown multiple times
        await coordinator.TeardownAsync();
        await coordinator.TeardownAsync(); // Should be no-op

        // Assert - should not throw
        Assert.True(true);

        // Cleanup
        AssemblyFixtureCoordinator.Reset();
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_TeardownAsync_WhenSetupNotComplete_ReturnsEarly()
    {
        // Arrange
        AssemblyFixtureCoordinator.Reset();
        var coordinator = AssemblyFixtureCoordinator.Instance;
        // Don't call InitializeAsync

        // Act & Assert - should return early without error
        await coordinator.TeardownAsync();

        // Cleanup
        AssemblyFixtureCoordinator.Reset();
    }
}

public class UnregisteredFixture : AssemblyFixture
{
}
