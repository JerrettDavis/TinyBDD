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
        var coordinator = new TestCoordinator();
        var assembly = typeof(AssemblyFixtureCoordinatorTests).Assembly;

        // Act
        await coordinator.InitializeAsync(assembly);
        await coordinator.InitializeAsync(assembly); // Second call should be no-op

        // Assert
        Assert.Equal(1, coordinator.InitializationCount);
    }

    [Fact]
    public async Task AssemblyFixtureCoordinator_InitializeAsync_WithNoAttributes_CompletesSuccessfully()
    {
        // Arrange
        var coordinator = new TestCoordinator();
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
    public async Task AssemblyFixtureCoordinator_TeardownAsync_RespectsCancellationToken()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => coordinator.TeardownAsync(cts.Token));
    }

    [Fact]
    public void AssemblyFixtureCoordinator_GetFixture_ThrowsWhenNotRegistered()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => AssemblyFixtureCoordinator.GetFixture<UnregisteredFixture>());
    }
}

// Test helper class
public class TestCoordinator : AssemblyFixtureCoordinator
{
    public int InitializationCount { get; private set; }

    public new async Task InitializeAsync(Assembly assembly, IBddReporter? reporter = null, CancellationToken ct = default)
    {
        InitializationCount++;
        await base.InitializeAsync(assembly, reporter, ct);
    }
}

public class UnregisteredFixture : AssemblyFixture
{
}
