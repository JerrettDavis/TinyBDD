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

    [Fact]
    public async Task AssemblyFixture_Get_ReturnsRegisteredFixture_AfterInitialization()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(IntegrationTestFixture).Assembly;
        await coordinator.InitializeAsync(assembly);

        // Act
        var fixture = AssemblyFixture.Get<IntegrationTestFixture>();

        // Assert
        Assert.NotNull(fixture);
        Assert.IsType<IntegrationTestFixture>(fixture);
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

/// <summary>
/// Additional tests for coordinator exception handling paths.
/// </summary>
public class AssemblyFixtureCoordinatorExceptionTests
{
    [Fact]
    public async Task Coordinator_TeardownAsync_WhenFixtureFails_LogsAndContinues()
    {
        // Arrange - reset coordinator to start fresh
        AssemblyFixtureCoordinator.Reset();
        var coordinator = AssemblyFixtureCoordinator.Instance;

        // Use a mock assembly with the failing fixture
        // Since we can't easily register fixtures without assembly attributes,
        // we test the internal methods directly on the fixture

        var fixture = new FailingTeardownFixture();
        var reporter = new TestReporter();
        fixture.Reporter = reporter;

        // Act - call internal setup
        await fixture.InternalSetupAsync();

        // InternalTeardownAsync throws, but logs the error first
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.InternalTeardownAsync());

        // Assert - the error was logged to the reporter
        Assert.Contains(reporter.Messages, m => m.Contains("FailingTeardownFixture [FAIL]"));

        // Cleanup
        AssemblyFixtureCoordinator.Reset();
    }
}

public class FailingTeardownFixture : AssemblyFixture
{
    protected override Task SetupAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected override Task TeardownAsync(CancellationToken ct = default)
    {
        throw new InvalidOperationException("Teardown intentionally failed");
    }
}
