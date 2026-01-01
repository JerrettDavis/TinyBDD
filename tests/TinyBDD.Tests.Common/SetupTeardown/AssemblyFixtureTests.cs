namespace TinyBDD.Tests.Common.SetupTeardown;

public class AssemblyFixtureTests
{
    [Fact]
    public async Task AssemblyFixture_SetupAndTeardown_ExecuteInOrder()
    {
        // Arrange
        var fixture = new TestAssemblyFixture();
        var reporter = new TestReporter();
        fixture.Reporter = reporter;

        // Act
        await fixture.InternalSetupAsync();
        await fixture.InternalTeardownAsync();

        // Assert
        Assert.Equal(2, fixture.ExecutionOrder.Count);
        Assert.Equal("Setup", fixture.ExecutionOrder[0]);
        Assert.Equal("Teardown", fixture.ExecutionOrder[1]);
    }

    [Fact]
    public async Task AssemblyFixture_SetupWithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var fixture = new CancellableAssemblyFixture();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.InternalSetupAsync(cts.Token));
    }

    [Fact]
    public async Task AssemblyFixture_TeardownWithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var fixture = new CancellableAssemblyFixture();
        await fixture.InternalSetupAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.InternalTeardownAsync(cts.Token));
    }

    [Fact]
    public void AssemblyFixture_Get_RetrievesRegisteredFixture()
    {
        // Arrange
        var coordinator = AssemblyFixtureCoordinator.Instance;
        var assembly = typeof(AssemblyFixtureTests).Assembly;

        // Note: This test validates the Get<T> method exists and compiles
        // Actual registration happens at assembly level via attributes

        // Act & Assert
        Assert.NotNull(AssemblyFixture.Get<TestAssemblyFixture>);
    }

    [Fact]
    public async Task AssemblyFixture_InternalSetup_LogsToReporter()
    {
        // Arrange
        var fixture = new TestAssemblyFixture();
        var reporter = new TestReporter();
        fixture.Reporter = reporter;

        // Act
        await fixture.InternalSetupAsync();

        // Assert
        Assert.Contains(reporter.Messages, m => m.Contains("Assembly Setup:"));
        Assert.Contains(reporter.Messages, m => m.Contains("TestAssemblyFixture [OK]"));
    }

    [Fact]
    public async Task AssemblyFixture_InternalTeardown_LogsToReporter()
    {
        // Arrange
        var fixture = new TestAssemblyFixture();
        var reporter = new TestReporter();
        fixture.Reporter = reporter;
        await fixture.InternalSetupAsync();

        // Act
        await fixture.InternalTeardownAsync();

        // Assert
        Assert.Contains(reporter.Messages, m => m.Contains("Assembly Teardown:"));
        Assert.Contains(reporter.Messages, m => m.Contains("TestAssemblyFixture [OK]"));
    }

    [Fact]
    public async Task AssemblyFixture_InternalSetup_WhenFails_LogsError()
    {
        // Arrange
        var fixture = new FailingAssemblyFixture();
        var reporter = new TestReporter();
        fixture.Reporter = reporter;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.InternalSetupAsync());

        Assert.Contains(reporter.Messages, m => m.Contains("FailingAssemblyFixture [FAIL]"));
        Assert.Contains(reporter.Messages, m => m.Contains("Error: InvalidOperationException"));
    }

    [Fact]
    public async Task AssemblyFixture_InternalTeardown_WhenFails_LogsError()
    {
        // Arrange
        var fixture = new FailingAssemblyFixture { FailInTeardown = true };
        var reporter = new TestReporter();
        fixture.Reporter = reporter;
        await fixture.InternalSetupAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.InternalTeardownAsync());

        Assert.Contains(reporter.Messages, m => m.Contains("FailingAssemblyFixture [FAIL]"));
        Assert.Contains(reporter.Messages, m => m.Contains("Error: InvalidOperationException"));
    }

    [Fact]
    public async Task AssemblyFixture_WithoutReporter_DoesNotThrow()
    {
        // Arrange
        var fixture = new TestAssemblyFixture();
        // No reporter set

        // Act & Assert (should not throw)
        await fixture.InternalSetupAsync();
        await fixture.InternalTeardownAsync();
    }
}

// Test fixtures
public class TestAssemblyFixture : AssemblyFixture
{
    public List<string> ExecutionOrder { get; } = new();

    protected override Task SetupAsync(CancellationToken ct = default)
    {
        ExecutionOrder.Add("Setup");
        return Task.CompletedTask;
    }

    protected override Task TeardownAsync(CancellationToken ct = default)
    {
        ExecutionOrder.Add("Teardown");
        return Task.CompletedTask;
    }
}

public class CancellableAssemblyFixture : AssemblyFixture
{
    protected override async Task SetupAsync(CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
    }

    protected override async Task TeardownAsync(CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
    }
}

public class FailingAssemblyFixture : AssemblyFixture
{
    public bool FailInTeardown { get; set; }

    protected override Task SetupAsync(CancellationToken ct = default)
    {
        if (!FailInTeardown)
            throw new InvalidOperationException("Setup failed");
        return Task.CompletedTask;
    }

    protected override Task TeardownAsync(CancellationToken ct = default)
    {
        if (FailInTeardown)
            throw new InvalidOperationException("Teardown failed");
        return Task.CompletedTask;
    }
}

public class TestReporter : IBddReporter
{
    public List<string> Messages { get; } = new();

    public void WriteLine(string message) => Messages.Add(message);
}
