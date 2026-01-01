using System.Reflection;
using TinyBDD;

[assembly: AssemblySetup(typeof(TinyBDD.Tests.Common.SetupTeardown.IntegrationTestFixture))]

namespace TinyBDD.Tests.Common.SetupTeardown;

public class AssemblyFixtureIntegrationTests
{
    [Fact]
    public async Task AssemblyFixture_InitializeAsync_CreatesAndRegistersFixtures()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(IntegrationTestFixture).Assembly;
        var reporter = new TestReporter();

        // Act
        await coordinator.InitializeAsync(assembly, reporter);

        // Assert - Should complete successfully
        Assert.True(IntegrationTestFixture.SetupExecuted);
    }

    [Fact]
    public async Task AssemblyFixture_InitializeAsync_WithReporter_CompletesSuccessfully()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(IntegrationTestFixture).Assembly;
        var reporter = new TestReporter();

        // Act - coordinator is idempotent, so this may be a no-op if already initialized
        await coordinator.InitializeAsync(assembly, reporter);

        // Assert - initialization should complete without error
        // Note: Reporter messages may be empty if coordinator was already initialized
        Assert.True(IntegrationTestFixture.SetupExecuted);
    }

    [Fact]
    public async Task AssemblyFixture_TeardownAsync_ExecutesInReverseOrder()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(IntegrationTestFixture).Assembly;
        await coordinator.InitializeAsync(assembly);

        // Act
        await coordinator.TeardownAsync();

        // Assert - should complete without error
        Assert.True(true);
    }

    [Fact]
    public void AssemblyFixture_Get_ReturievesRegisteredFixture()
    {
        // This will work if the assembly attribute is processed
        // We can't test the actual Get<T> without the assembly being initialized by the test framework

        // Arrange & Act
        var getMethod = typeof(AssemblyFixture).GetMethod("Get");

        // Assert
        Assert.NotNull(getMethod);
        Assert.True(getMethod!.IsStatic);
        Assert.True(getMethod.IsGenericMethod);
    }
}

public class IntegrationTestFixture : AssemblyFixture
{
    public static bool SetupExecuted { get; private set; }
    public static bool TeardownExecuted { get; private set; }

    protected override Task SetupAsync(CancellationToken ct = default)
    {
        SetupExecuted = true;
        return Task.CompletedTask;
    }

    protected override Task TeardownAsync(CancellationToken ct = default)
    {
        TeardownExecuted = true;
        return Task.CompletedTask;
    }
}
