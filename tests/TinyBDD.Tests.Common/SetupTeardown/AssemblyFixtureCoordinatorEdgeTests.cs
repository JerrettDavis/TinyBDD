using System.Reflection;

namespace TinyBDD.Tests.Common.SetupTeardown;

/// <summary>
/// Additional coordinator tests that exercise edge cases not covered by the
/// shared singleton-based tests, including the teardown exception path and
/// the inside-the-lock idempotency early-returns.
/// </summary>
[Collection(AssemblyFixtureCoordinatorCollection.Name)]
public class AssemblyFixtureCoordinatorEdgeTests
{
    private sealed class CoordinatorFailingTeardownFixture : AssemblyFixture
    {
        public bool TeardownAttempted { get; private set; }

        protected override Task SetupAsync(CancellationToken ct = default)
            => Task.CompletedTask;

        protected override Task TeardownAsync(CancellationToken ct = default)
        {
            TeardownAttempted = true;
            throw new InvalidOperationException("Teardown intentionally failed in coordinator");
        }
    }

    private sealed class CoordinatorSucceedingFixture : AssemblyFixture
    {
        public bool SetupExecuted { get; private set; }
        public bool TeardownExecuted { get; private set; }

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

    private static AssemblyFixtureCoordinator GetFreshCoordinator()
    {
        AssemblyFixtureCoordinator.Reset();
        return AssemblyFixtureCoordinator.Instance;
    }

    private static void RegisterFixture(AssemblyFixtureCoordinator coordinator, AssemblyFixture fixture)
    {
        var fixturesField = typeof(AssemblyFixtureCoordinator)
            .GetField("_fixtures", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var orderField = typeof(AssemblyFixtureCoordinator)
            .GetField("_fixtureOrder", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var setupCompleteField = typeof(AssemblyFixtureCoordinator)
            .GetField("_setupComplete", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var isInitField = typeof(AssemblyFixtureCoordinator)
            .GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Static)!;

        var fixtures = (System.Collections.Concurrent.ConcurrentDictionary<Type, AssemblyFixture>)fixturesField.GetValue(coordinator)!;
        var order = (List<AssemblyFixture>)orderField.GetValue(coordinator)!;

        fixtures[fixture.GetType()] = fixture;
        order.Add(fixture);

        setupCompleteField.SetValue(coordinator, true);
        isInitField.SetValue(null, true);
    }

    [Fact]
    public async Task TeardownAsync_WhenFixtureThrows_ContinuesAndLogsToReporter()
    {
        var coordinator = GetFreshCoordinator();
        var reporter = new TestReporter();

        var failing = new CoordinatorFailingTeardownFixture { Reporter = reporter };
        var succeeding = new CoordinatorSucceedingFixture { Reporter = reporter };

        try
        {
            RegisterFixture(coordinator, succeeding);
            RegisterFixture(coordinator, failing);

            await coordinator.TeardownAsync();

            // Both teardowns attempted, in reverse order:
            // failing (last registered) attempts first, throws, swallowed; logged.
            Assert.True(failing.TeardownAttempted);
            Assert.True(succeeding.TeardownExecuted);
            Assert.Contains(reporter.Messages, m => m.Contains("Assembly teardown failed"));
            Assert.Contains(reporter.Messages, m => m.Contains(nameof(CoordinatorFailingTeardownFixture)));
        }
        finally
        {
            AssemblyFixtureCoordinator.Reset();
        }
    }

    [Fact]
    public async Task TeardownAsync_WhenFixtureThrows_WithoutReporter_DoesNotThrow()
    {
        var coordinator = GetFreshCoordinator();
        var failing = new CoordinatorFailingTeardownFixture(); // no reporter

        try
        {
            RegisterFixture(coordinator, failing);
            await coordinator.TeardownAsync();
            Assert.True(failing.TeardownAttempted);
        }
        finally
        {
            AssemblyFixtureCoordinator.Reset();
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyInitialized_ReturnsEarly()
    {
        var coordinator = GetFreshCoordinator();
        var assembly = typeof(AssemblyFixtureCoordinatorEdgeTests).Assembly;

        try
        {
            await coordinator.InitializeAsync(assembly);
            // Subsequent calls must short-circuit on the outer "_isInitialized" guard.
            await coordinator.InitializeAsync(assembly);
            await coordinator.InitializeAsync(assembly);
        }
        finally
        {
            AssemblyFixtureCoordinator.Reset();
        }
    }

    [Fact]
    public async Task TeardownAsync_WhenAlreadyTornDown_ReturnsEarly()
    {
        var coordinator = GetFreshCoordinator();
        var fixture = new CoordinatorSucceedingFixture();

        try
        {
            RegisterFixture(coordinator, fixture);
            await coordinator.TeardownAsync();

            // Subsequent calls must short-circuit on the outer "_teardownComplete" guard.
            await coordinator.TeardownAsync();
            await coordinator.TeardownAsync();
        }
        finally
        {
            AssemblyFixtureCoordinator.Reset();
        }
    }
}
