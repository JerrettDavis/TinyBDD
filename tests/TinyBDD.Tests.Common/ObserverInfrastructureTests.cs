namespace TinyBDD.Tests.Common;

public class ObserverInfrastructureTests
{
    [Feature("Extensibility")]
    private sealed class Host { }

    [Scenario("Scenario observer receives lifecycle events")]
    [Fact]
    public async Task ScenarioObserver_ReceivesLifecycleEvents()
    {
        var observer = new TestScenarioObserver();
        var options = Bdd.Configure(b => b.AddObserver(observer));

        var ctx = Bdd.CreateContext(new Host(), options: options);
        await Bdd.Given(ctx, "start", () => 1)
            .When("add", x => x + 1)
            .Then("is 2", x => x == 2);

        Assert.Equal(1, observer.StartCount);
        Assert.Equal(1, observer.FinishCount);
        Assert.NotNull(observer.LastContext);
        Assert.Equal("Extensibility", observer.LastContext.FeatureName);
    }

    [Scenario("Step observer receives lifecycle events")]
    [Fact]
    public async Task StepObserver_ReceivesLifecycleEvents()
    {
        var observer = new TestStepObserver();
        var options = Bdd.Configure(b => b.AddObserver(observer));

        var ctx = Bdd.CreateContext(new Host(), options: options);
        await Bdd.Given(ctx, "start", () => 1)
            .When("add", x => x + 1)
            .Then("is 2", x => x == 2);

        Assert.Equal(3, observer.StartCount);
        Assert.Equal(3, observer.FinishCount);
        
        // Verify step kinds
        Assert.Collection(observer.StepsStarted,
            s => Assert.Equal("Given", s.Kind),
            s => Assert.Equal("When", s.Kind),
            s => Assert.Equal("Then", s.Kind));
    }

    [Scenario("Multiple observers are invoked in order")]
    [Fact]
    public async Task MultipleObservers_InvokedInOrder()
    {
        var observer1 = new TestScenarioObserver();
        var observer2 = new TestScenarioObserver();
        
        var options = Bdd.Configure(b => b
            .AddObserver(observer1)
            .AddObserver(observer2));

        var ctx = Bdd.CreateContext(new Host(), options: options);
        await Bdd.Given(ctx, "start", () => 1)
            .Then("is 1", x => x == 1);

        Assert.Equal(1, observer1.StartCount);
        Assert.Equal(1, observer1.FinishCount);
        Assert.Equal(1, observer2.StartCount);
        Assert.Equal(1, observer2.FinishCount);
    }

    [Scenario("Observer exceptions don't mask test failures")]
    [Fact]
    public async Task ObserverException_DoesNotMaskTestFailure()
    {
        var observer = new FaultyObserver();
        var options = Bdd.Configure(b => b.AddObserver(observer));

        var ctx = Bdd.CreateContext(new Host(), options: options);
        
        // Should complete despite observer throwing
        await Bdd.Given(ctx, "start", () => 1)
            .Then("is 1", x => x == 1);

        Assert.Single(ctx.Steps.Where(s => s.Kind == "Given"));
    }

    [Scenario("Step observer captures StepIO")]
    [Fact]
    public async Task StepObserver_CapturesStepIO()
    {
        var observer = new TestStepObserver();
        var options = Bdd.Configure(b => b.AddObserver(observer));

        var ctx = Bdd.CreateContext(new Host(), options: options);
        await Bdd.Given(ctx, "start", () => 5)
            .When("double", x => x * 2)
            .Then("is 10", x => x == 10);

        // Verify IO was captured
        Assert.NotEmpty(observer.StepsFinished);
        var whenStep = observer.StepsFinished.FirstOrDefault(s => s.io.Kind == "When");
        Assert.NotNull(whenStep);
        Assert.Equal(5, whenStep.io.Input);
        Assert.Equal(10, whenStep.io.Output);
    }

    // Test helper classes

    private class TestScenarioObserver : IScenarioObserver
    {
        public int StartCount { get; private set; }
        public int FinishCount { get; private set; }
        public ScenarioContext? LastContext { get; private set; }

        public ValueTask OnScenarioStarting(ScenarioContext context)
        {
            StartCount++;
            LastContext = context;
            return default;
        }

        public ValueTask OnScenarioFinished(ScenarioContext context)
        {
            FinishCount++;
            LastContext = context;
            return default;
        }
    }

    private class TestStepObserver : IStepObserver
    {
        public int StartCount { get; private set; }
        public int FinishCount { get; private set; }
        public List<StepInfo> StepsStarted { get; } = new();
        public List<(StepInfo step, StepResult result, StepIO io)> StepsFinished { get; } = new();

        public ValueTask OnStepStarting(ScenarioContext context, StepInfo step)
        {
            StartCount++;
            StepsStarted.Add(step);
            return default;
        }

        public ValueTask OnStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io)
        {
            FinishCount++;
            StepsFinished.Add((step, result, io));
            return default;
        }
    }

    private class FaultyObserver : IScenarioObserver
    {
        public ValueTask OnScenarioStarting(ScenarioContext context)
        {
            throw new InvalidOperationException("Observer failed");
        }

        public ValueTask OnScenarioFinished(ScenarioContext context)
        {
            throw new InvalidOperationException("Observer failed");
        }
    }
}
