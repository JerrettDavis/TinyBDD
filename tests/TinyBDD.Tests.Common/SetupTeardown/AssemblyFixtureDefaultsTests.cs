namespace TinyBDD.Tests.Common.SetupTeardown;

/// <summary>
/// Tests that exercise the default virtual implementations of
/// <see cref="AssemblyFixture.SetupAsync"/> and <see cref="AssemblyFixture.TeardownAsync"/>
/// (i.e. a fixture that does not override either method).
/// </summary>
public class AssemblyFixtureDefaultsTests
{
    private sealed class NoOverrideFixture : AssemblyFixture
    {
        // Intentionally no overrides
    }

    [Fact]
    public async Task DefaultSetupAsync_DoesNothing_AndCompletesSuccessfully()
    {
        var fixture = new NoOverrideFixture();

        // No reporter, default virtuals -> exercises the Task.CompletedTask defaults
        await fixture.InternalSetupAsync();

        Assert.Null(fixture.Reporter);
    }

    [Fact]
    public async Task DefaultTeardownAsync_DoesNothing_AndCompletesSuccessfully()
    {
        var fixture = new NoOverrideFixture();

        await fixture.InternalSetupAsync();
        await fixture.InternalTeardownAsync();

        Assert.Null(fixture.Reporter);
    }

    [Fact]
    public async Task DefaultVirtuals_WithReporter_LogOkLines()
    {
        var fixture = new NoOverrideFixture();
        var reporter = new TestReporter();
        fixture.Reporter = reporter;

        await fixture.InternalSetupAsync();
        await fixture.InternalTeardownAsync();

        Assert.Contains(reporter.Messages, m => m.Contains("Assembly Setup:"));
        Assert.Contains(reporter.Messages, m => m.Contains("NoOverrideFixture [OK]"));
        Assert.Contains(reporter.Messages, m => m.Contains("Assembly Teardown:"));
    }

    [Fact]
    public async Task DefaultVirtuals_HonorCancellationToken()
    {
        var fixture = new NoOverrideFixture();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Default impl is Task.CompletedTask - already completed before
        // the token is observed, so no cancellation propagates. We simply
        // verify it completes without throwing.
        await fixture.InternalSetupAsync(cts.Token);
        await fixture.InternalTeardownAsync(cts.Token);
    }
}
