namespace TinyBDD.Tests.Common;

public class PipelineHookTests
{
    [Feature("Pipeline")]
    private sealed class Host { }

    private static ScenarioContext NewContext() =>
        Bdd.CreateContext(new Host(), options: new ScenarioOptions());

    private static Func<object?, CancellationToken, ValueTask<object?>> Const(object? value) =>
        (_, _) => new ValueTask<object?>(value);

    private static Func<object?, CancellationToken, ValueTask<object?>> Noop() =>
        (_, _) => new ValueTask<object?>(new {});

    [Scenario("BeforeStep supplies correct metadata (Kind, Title, Phase, Word) and honors inherited phase")]
    [Fact]
    public async Task BeforeStep_Metadata_Are_Correct()
    {
        var ctx = NewContext();

        var seen = new List<(string Kind, string Title, StepPhase Phase, StepWord Word)>();

        var pipe = new Pipeline(ctx)
        {
            BeforeStep = (_, m) => seen.Add((m.Kind, m.Title, m.Phase, m.Word))
        };

        pipe.Enqueue(StepPhase.Given, StepWord.Primary, "start", Const(1));
        pipe.Enqueue(StepPhase.When, StepWord.Primary, "act", Const(2));
        pipe.EnqueueInherit("and more", Const(3), StepWord.And);
        pipe.EnqueueInherit("but alternative", Const(4), StepWord.But);

        await pipe.RunAsync(CancellationToken.None);

        Assert.Collection(seen,
            m =>
            {
                var (kind, title, phase, word) = m;
                Assert.Equal("Given", kind);
                Assert.Equal("start", title);
                Assert.Equal(StepPhase.Given, phase);
                Assert.Equal(StepWord.Primary, word);
            },
            m =>
            {
                var (kind, title, phase, word) = m;
                Assert.Equal("When", kind);
                Assert.Equal("act", title);
                Assert.Equal(StepPhase.When, phase);
                Assert.Equal(StepWord.Primary, word);
            },
            m =>
            {
                var (kind, title, phase, word) = m;
                Assert.Equal("And", kind);
                Assert.Equal("and more", title);
                Assert.Equal(StepPhase.When, phase);   // inherited
                Assert.Equal(StepWord.And, word);
            },
            m =>
            {
                var (kind, title, phase, word) = m;
                Assert.Equal("But", kind);
                Assert.Equal("but alternative", title);
                Assert.Equal(StepPhase.When, phase);   // inherited
                Assert.Equal(StepWord.But, word);
            });
    }

    [Scenario("AfterStep is invoked with results matching those recorded in ScenarioContext")]
    [Fact]
    public async Task AfterStep_Results_Match_Context_Steps()
    {
        var ctx = NewContext();

        var after = new List<StepResult>();

        var pipe = new Pipeline(ctx)
        {
            AfterStep = (_, r) => after.Add(r)
        };

        pipe.Enqueue(StepPhase.Given, StepWord.Primary, "seed", Const(1));
        pipe.Enqueue(StepPhase.When, StepWord.Primary, "compute", Const(2));
        pipe.Enqueue(StepPhase.Then, StepWord.Primary, "verify", Noop());

        await pipe.RunAsync(CancellationToken.None);

        // AfterStep fires once per executed step
        Assert.Equal(3, after.Count);

        // Compare AfterStep results with the context steps (order & fields)
        Assert.Equal(ctx.Steps.Count, after.Count);
        foreach (var (ctxStep, hookStep) in ctx.Steps.Zip(after))
        {
            Assert.Equal(ctxStep.Kind, hookStep.Kind);
            Assert.Equal(ctxStep.Title, hookStep.Title);
            Assert.Equal(ctxStep.Error?.GetType(), hookStep.Error?.GetType());
        }
    }

    [Scenario("Hooks fire on failure; ContinueOnError=true records failure and continues")]
    [Fact]
    public async Task Hooks_On_Failure_With_ContinueOnError()
    {
        var ctx = Bdd.CreateContext(new Host(), options: new ScenarioOptions
        {
            ContinueOnError = true
        });

        var befores = new List<string>();
        var afters = new List<(string Kind, string Title, Exception? Error)>();

        var pipe = new Pipeline(ctx)
        {
            BeforeStep = (_, m) => befores.Add($"{m.Kind} {m.Title}"),
            AfterStep = (_, r) => afters.Add((r.Kind, r.Title, r.Error))
        };

        pipe.Enqueue(StepPhase.Given, StepWord.Primary, "start", Const(1));
        pipe.Enqueue(StepPhase.When, StepWord.Primary, "boom", (_, _) => throw new InvalidOperationException("kaboom"));
        pipe.Enqueue(StepPhase.Then, StepWord.Primary, "after", Noop());

        await pipe.RunAsync(CancellationToken.None);

        // Hooks called for each step
        Assert.Equal(3, befores.Count);
        Assert.Equal(3, afters.Count);

        // Middle step failed and was recorded as such
        var failed = afters[1];
        Assert.Equal("When", failed.Kind);
        Assert.Equal("boom", failed.Title);
        Assert.NotNull(failed.Error);
        Assert.IsType<InvalidOperationException>(failed.Error);

        // Final step executed and succeeded
        var last = afters[^1];
        Assert.Equal("Then", last.Kind);
        Assert.Equal("after", last.Title);
        Assert.Null(last.Error);
    }

    [Scenario("Title falls back to phase name when empty for both metadata and result")]
    [Fact]
    public async Task Empty_Title_Falls_Back_To_Phase()
    {
        var ctx = NewContext();

        Pipeline.StepMetadata? firstMeta = null;
        StepResult? firstResult = null;

        var pipe = new Pipeline(ctx)
        {
            BeforeStep = (_, m) => firstMeta ??= m,
            AfterStep  = (_, r) => firstResult ??= r
        };

        // Intentionally empty title
        pipe.Enqueue(StepPhase.Given, StepWord.Primary, "", Const(42));

        await pipe.RunAsync(CancellationToken.None);

        Assert.NotNull(firstMeta);
        Assert.Equal("Given", firstMeta!.Value.Kind);
        Assert.Equal("Given", firstMeta.Value.Title);

        Assert.NotNull(firstResult);
        Assert.Equal("Given", firstResult!.Kind);
        Assert.Equal("Given", firstResult.Title);
        Assert.Null(firstResult.Error);
    }
    
    [Scenario("StepMetadata init accessors are exercised via with-expression")]
    [Fact]
    public async Task StepMetadata_With_Expression_Sets_Init_Props()
    {
        var ctx = NewContext();

        Pipeline.StepMetadata? captured = null;

        var pipe = new Pipeline(ctx)
        {
            BeforeStep = (_, m) => captured ??= m
        };

        pipe.Enqueue(StepPhase.Given, StepWord.Primary, "start", Const(1));
        await pipe.RunAsync(CancellationToken.None);

        // we have an original metadata instance
        var m = captured!.Value;

        // exercise init-only setters via with-expression (hits init for ALL properties)
        // ReSharper disable once WithExpressionModifiesAllMembers
        var m2 = m with
        {
            Kind  = "CustomKind",
            Title = "CustomTitle",
            Phase = StepPhase.When,
            Word  = StepWord.But
        };

        // original unchanged
        Assert.Equal("Given", m.Kind);
        Assert.Equal("start", m.Title);
        Assert.Equal(StepPhase.Given, m.Phase);
        Assert.Equal(StepWord.Primary, m.Word);

        // cloned values reflect init changes
        Assert.Equal("CustomKind", m2.Kind);
        Assert.Equal("CustomTitle", m2.Title);
        Assert.Equal(StepPhase.When, m2.Phase);
        Assert.Equal(StepWord.But, m2.Word);
    }
    
    
    [Scenario("StepResult should be populated with appropriate property when the operation is cancelled")]
    [Fact]
    public async Task StepResult_Populated_With_Cancelled_On_Operation_Cancelled()
    {
        var ctx = NewContext();
        var pipe = new Pipeline(ctx);
        var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        pipe.Enqueue(StepPhase.Given, StepWord.Primary, "start", Const(1));
        pipe.Enqueue(StepPhase.When, StepWord.Primary, "act", Const(2));
        pipe.Enqueue(StepPhase.When, StepWord.Primary, "wait", async (v,ct) =>
        {
             await Task.Delay(1000, ct);
             return v;
        });
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pipe.RunAsync(tokenSource.Token));
        var result = ctx.Steps.Last();
        Assert.NotNull(result.Error);
        Assert.IsType<OperationCanceledException>(result.Error);
    }
}
