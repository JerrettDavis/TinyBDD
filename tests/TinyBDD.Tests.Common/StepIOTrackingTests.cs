namespace TinyBDD.Tests.Common;

public class StepIOTrackingTests
{
    [Feature("Step IO Tracking")] private sealed class Host {}

    [Scenario("Tracks IO and CurrentItem across Given/When/Then (pass)")]
    [Fact]
    public async Task Tracks_IO_On_Pass()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "seed", () => 1)
            .When("+1", x => x + 1)
            .Then("== 2", v => v == 2)
            .AssertPassed();

        Assert.Equal(3, ctx.IO.Count);

        var io0 = ctx.IO[0];
        Assert.Equal("Given", io0.Kind);
        Assert.Equal("seed", io0.Title);
        Assert.Null(io0.Input);
        Assert.Equal(1, io0.Output);

        var io1 = ctx.IO[1];
        Assert.Equal("When", io1.Kind);
        Assert.Equal("+1", io1.Title);
        Assert.Equal(1, io1.Input);
        Assert.Equal(2, io1.Output);

        var io2 = ctx.IO[2];
        Assert.Equal("Then", io2.Kind);
        Assert.Equal("== 2", io2.Title);
        Assert.Equal(2, io2.Input);
        Assert.Equal(2, io2.Output);

        Assert.Equal(2, ctx.CurrentItem);
    }

    [Scenario("Tracks IO and CurrentItem when Then fails")]
    [Fact]
    public async Task Tracks_IO_On_Fail()
    {
        var ctx = Bdd.CreateContext(new Host());

        await Bdd.Given(ctx, "seed", () => 1)
            .When("+1", x => x + 1)
            .Then("== 3", v => v == 3)
            .AssertFailed();

        Assert.Equal(3, ctx.IO.Count);

        var io2 = ctx.IO[2];
        Assert.Equal("Then", io2.Kind);
        Assert.Equal("== 3", io2.Title);
        Assert.Equal(2, io2.Input);
        Assert.Equal(2, io2.Output);

        // CurrentItem should still reflect the latest state
        Assert.Equal(2, ctx.CurrentItem);

        // Verify a failure was recorded on the Then step
        Assert.NotNull(ctx.Steps[2].Error);
    }
}

