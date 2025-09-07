namespace TinyBDD.Tests.Common;

public class ScenarioOptionsTests
{
    [Feature("Options")]
    private sealed class Host { }

    [Scenario("HaltOnFailedAssertion = true causes assertion to throw immediately")]
    [Fact]
    public async Task HaltOnFailedAssertion_True_ThrowsAndRecordsStep()
    {
        var ctx = Bdd.CreateContext(new Host(), options: new ScenarioOptions
        {
            HaltOnFailedAssertion = true
        });

        await Assert.ThrowsAsync<BddAssertException>(async () =>
            await Bdd.Given(ctx, "start", () => 1)
                .Then("fail", v => v == 2)
                .And("after", _ => Task.CompletedTask));

        // failed assertion should be recorded
        var failed = ctx.Steps.Last(x => x.Error is not null);
        Assert.Equal("Then", failed.Kind);
        Assert.Equal("fail", failed.Title);
    }

    [Scenario("HaltOnFailedAssertion = false records failure but continues")]
    [Fact]
    public async Task HaltOnFailedAssertion_False_RecordsFailureAndContinues()
    {
        var ctx = Bdd.CreateContext(new Host(), options: new ScenarioOptions
        {
            HaltOnFailedAssertion = false
        });

        await Bdd.Given(ctx, "start", () => 1)
            .Then("fail", v => v == 2)
            .And("after", _ => Task.CompletedTask)
            // We expect the And step to be recorded as failed
            .AssertFailed();

        var last = ctx.Steps[^1];
        Assert.Equal("And", last.Kind);
        Assert.Equal("after", last.Title);
        Assert.Null(last.Error);
    }

    [Scenario("StepTimeout causes BddStepException and marks remaining steps as skipped when configured")]
    [Fact]
    public async Task StepTimeout_ThrowsAndSkipsRemaining_WhenConfigured()
    {
        var ctx = Bdd.CreateContext(new Host(), options: new ScenarioOptions
        {
            StepTimeout = TimeSpan.FromMilliseconds(50),
            MarkRemainingAsSkippedOnFailure = true
        });

        await Assert.ThrowsAsync<BddStepException>(async () =>
            await Bdd.Given(ctx, "start", () => 1)
                .When("long", Long)
                .Then("reached", _ => Task.CompletedTask));
        
        // the errored step should be the Then since they get marked with an InvalidOperationException
        var failed = ctx.Steps.Last(x => x.Error is not null);
        Assert.Equal("Then", failed.Kind);
        Assert.Equal("reached", failed.Title);

        // the remaining Then should have been marked skipped
        var last = ctx.Steps.Last();
        Assert.Equal("Then", last.Kind);
        Assert.Equal("reached", last.Title);
        Assert.IsType<InvalidOperationException>(last.Error);
        Assert.Equal("Skipped due to previous failure.", last.Error.Message);
    }

    [Scenario("ContinueOnError allows execution to continue after step timeout")]
    [Fact]
    public async Task StepTimeout_WithContinueOnError_DoesNotThrow_Continues()
    {
        var ctx = Bdd.CreateContext(new Host(), options: new ScenarioOptions
        {
            StepTimeout = TimeSpan.FromMilliseconds(50),
            ContinueOnError = true
        });

        await Bdd.Given(ctx, "start", () => 1)
            .When("long", Long)
            .Then("reached", _ => Task.CompletedTask);

        // no exception and Then executed
        var last = ctx.Steps.Last();
        Assert.Equal("Then", last.Kind);
        Assert.Equal("reached", last.Title);
        Assert.Null(last.Error);

        // earlier failed step is recorded
        var failed = ctx.Steps.Last(x => x.Error is not null);
        Assert.Equal("When", failed.Kind);
        Assert.Equal("long", failed.Title);
    }

    [Scenario("When a failure occurs and MarkRemainingAsSkippedOnFailure = false, remaining steps are not recorded (throw path)")]
    [Fact]
    public async Task Failure_NoMarkRemaining_DoesNotAddSkippedSteps()
    {
        var ctx = Bdd.CreateContext(new Host(), options: new ScenarioOptions
        {
            StepTimeout = TimeSpan.FromMilliseconds(50),
            MarkRemainingAsSkippedOnFailure = false
        });

        await Assert.ThrowsAsync<BddStepException>(async () =>
            await Bdd.Given(ctx, "start", () => 1)
                .When("long", Long)
                .Then("reached", _ => Task.CompletedTask));

        // only the failing step should be present (no skipped Then)
        Assert.Contains(ctx.Steps, s => s.Kind == "When" && s.Title == "long" && s.Error is not null);
        // Assert that there are no "Then" steps with error
        Assert.DoesNotContain(ctx.Steps, s => s.Kind == "Then" && s.Title == "reached" && s.Error is not null);
        // If any "Then" step with error exists, ensure its error message is not "Skipped due to previous failure."
        foreach (var step in ctx.Steps.Where(s => s.Kind == "Then" && s.Title == "reached" && s.Error is not null))
        {
            Assert.NotEqual("Skipped due to previous failure.", step.Error!.Message);
        }
    }


    private static async Task<int> Long(int v, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        return v;
    }
}