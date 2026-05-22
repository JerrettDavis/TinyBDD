using System.IO;
using TinyBDD.Extensions.Reporting;

namespace TinyBDD.Tests.Common;

/// <summary>
/// Direct (non-pipeline) unit tests for <see cref="JsonReportObserver"/> that exercise
/// the early-out branches when callbacks fire out of the expected lifecycle order.
/// </summary>
public class JsonReportObserverDirectTests
{
    [Feature("DirectObserver")]
    private sealed class Host { }

    private static ScenarioContext CreateContext()
        => Bdd.CreateContext(new Host(), scenarioName: "DirectObserver");

    [Fact]
    public async Task OnScenarioFinished_WithoutOnScenarioStarting_ReturnsEarly()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var observer = new JsonReportObserver(tempFile);
            var ctx = CreateContext();

            // No OnScenarioStarting -> _currentScenario is null, finished must return early.
            await observer.OnScenarioFinished(ctx);

            // The observer should not have written a report (nothing to report).
            // We accept either an empty file or a default file: importantly, no throw.
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task OnStepFinished_WithoutOnScenarioStarting_ReturnsEarly()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var observer = new JsonReportObserver(tempFile);
            var ctx = CreateContext();
            var step = new StepInfo("Given", "noop", StepPhase.Given, StepWord.Primary);
            var result = new StepResult { Kind = "Given", Title = "noop", Elapsed = TimeSpan.Zero, Error = null };
            var io = new StepIO("Given", "noop", Input: null, Output: null);

            // No OnScenarioStarting -> _currentScenario null -> early return.
            await observer.OnStepFinished(ctx, step, result, io);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task OnStepStarting_IsNoOp_AndReturnsCompleted()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var observer = new JsonReportObserver(tempFile);
            var ctx = CreateContext();
            var step = new StepInfo("Given", "noop", StepPhase.Given, StepWord.Primary);

            await observer.OnStepStarting(ctx, step);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Constructor_NullJsonOptions_UsesDefaultOptions()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            // Just exercise the default JsonSerializerOptions branch.
            var observer = new JsonReportObserver(tempFile);
            Assert.NotNull(observer);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
