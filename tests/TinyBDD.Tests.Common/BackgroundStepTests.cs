namespace TinyBDD.Tests.Common;

/// <summary>
/// Tests for background step infrastructure in TestBase.
/// </summary>
public class BackgroundStepTests
{
    #region Test Fixtures

    /// <summary>
    /// A simple test double for testing background step execution.
    /// </summary>
    private class SimpleBackgroundTestBase : TestBase
    {
        public List<string> ExecutionLog { get; } = new();
        public int BackgroundValue { get; set; } = 42;

        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground()
        {
            return Flow.Given("setup step", () =>
            {
                ExecutionLog.Add("background-given");
                return BackgroundValue;
            })
            .And("additional setup", v =>
            {
                ExecutionLog.Add("background-and");
                return (object)v;
            });
        }

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);

        public object? GetBackgroundState() => BackgroundState;
        public bool GetBackgroundExecuted() => BackgroundExecuted;
    }

    /// <summary>
    /// Test base that has no background configured.
    /// </summary>
    private class NoBackgroundTestBase : TestBase
    {
        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground() => null;

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);

        public bool GetBackgroundExecuted() => BackgroundExecuted;
    }

    /// <summary>
    /// Test base with async background steps.
    /// </summary>
    private class AsyncBackgroundTestBase : TestBase
    {
        public bool AsyncOperationCompleted { get; private set; }

        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground()
        {
            return Flow.Given("async setup", () =>
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(10);
                    AsyncOperationCompleted = true;
                    return (object)"async-result";
                });
            });
        }

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);
    }

    /// <summary>
    /// A null reporter for testing purposes.
    /// </summary>
    private class NullBddReporter : IBddReporter
    {
        public void WriteLine(string message) { }
    }

    #endregion

    #region ConfigureBackground Tests

    [Fact]
    public async Task ConfigureBackground_IsExecutedByExecuteBackgroundAsync()
    {
        // Arrange
        var testBase = new SimpleBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.Contains("background-given", testBase.ExecutionLog);
            Assert.Contains("background-and", testBase.ExecutionLog);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task ExecuteBackgroundAsync_CapturesBackgroundState()
    {
        // Arrange
        var testBase = new SimpleBackgroundTestBase { BackgroundValue = 100 };
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.Equal(100, testBase.GetBackgroundState());
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task ExecuteBackgroundAsync_SetsBackgroundExecutedFlag()
    {
        // Arrange
        var testBase = new SimpleBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.True(testBase.GetBackgroundExecuted());
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task ExecuteBackgroundAsync_WithNoBackground_StillSetsFlag()
    {
        // Arrange
        var testBase = new NoBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.True(testBase.GetBackgroundExecuted());
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task ExecuteBackgroundAsync_WithAsyncSteps_CompletesCorrectly()
    {
        // Arrange
        var testBase = new AsyncBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.True(testBase.AsyncOperationCompleted);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    #endregion

    #region GivenBackground Tests

    /// <summary>
    /// Test fixture that exposes GivenBackground for testing.
    /// </summary>
    private class GivenBackgroundTestBase : TestBase
    {
        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground()
        {
            return Flow.Given("background setup", () => (object)new TestContext { Value = 42 });
        }

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);

        public ScenarioChain<T> GetGivenBackground<T>() where T : class
            => GivenBackground<T>();

        public ScenarioChain<T> GetGivenBackground<T>(string title) where T : class
            => GivenBackground<T>(title);
    }

    private class TestContext
    {
        public int Value { get; set; }
    }

    [Fact]
    public async Task GivenBackground_ReturnsChainWithBackgroundState()
    {
        // Arrange
        var testBase = new GivenBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act & Assert
            await testBase.GetGivenBackground<TestContext>()
                .Then("has correct value", ctx => ctx.Value == 42)
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task GivenBackground_WithCustomTitle_UsesTitle()
    {
        // Arrange
        var testBase = new GivenBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act
            var chain = testBase.GetGivenBackground<TestContext>("custom background title");

            // Assert - Chain works correctly
            await chain
                .Then("has value", ctx => ctx.Value == 42)
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public void GivenBackground_BeforeExecute_ThrowsInvalidOperation()
    {
        // Arrange
        var testBase = new GivenBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act & Assert - Background not executed yet
            var ex = Assert.Throws<InvalidOperationException>(() =>
                testBase.GetGivenBackground<TestContext>());
            Assert.Contains("Background steps have not been executed", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task GivenBackground_WithWrongType_ThrowsInvalidOperation()
    {
        // Arrange
        var testBase = new GivenBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act & Assert - Wrong type requested
            var ex = Assert.Throws<InvalidOperationException>(() =>
                testBase.GetGivenBackground<string>());
            Assert.Contains("Background state is not of type String", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Background_IntegratesWithScenarioFlow()
    {
        // Arrange
        var testBase = new GivenBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act & Assert - Full scenario using background
            await testBase.GetGivenBackground<TestContext>()
                .When("modify value", ctx =>
                {
                    ctx.Value *= 2;
                    return ctx;
                })
                .Then("value doubled", ctx => ctx.Value == 84)
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task Background_MultipleScenarios_ShareSameSetup()
    {
        // This simulates running multiple test methods with the same background

        // Scenario 1
        var testBase1 = new GivenBackgroundTestBase();
        testBase1.Initialize();
        try
        {
            await testBase1.RunBackgroundAsync();
            await testBase1.GetGivenBackground<TestContext>()
                .Then("has value 42", ctx => ctx.Value == 42)
                .AssertPassed();
        }
        finally
        {
            testBase1.Cleanup();
        }

        // Scenario 2 - Fresh instance, same background behavior
        var testBase2 = new GivenBackgroundTestBase();
        testBase2.Initialize();
        try
        {
            await testBase2.RunBackgroundAsync();
            await testBase2.GetGivenBackground<TestContext>()
                .When("triple", ctx => { ctx.Value *= 3; return ctx; })
                .Then("has value 126", ctx => ctx.Value == 126)
                .AssertPassed();
        }
        finally
        {
            testBase2.Cleanup();
        }
    }

    /// <summary>
    /// Test fixture with complex background that sets up multiple resources.
    /// </summary>
    private class ComplexBackgroundTestBase : TestBase
    {
        public List<string> ResourcesCreated { get; } = new();

        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground()
        {
            return Flow.Given("database connection", () =>
                {
                    ResourcesCreated.Add("database");
                    return new { Database = "connected" };
                })
                .And("cache initialized", db =>
                {
                    ResourcesCreated.Add("cache");
                    return (object)new { db.Database, Cache = "ready" };
                });
        }

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);
    }

    [Fact]
    public async Task Background_ComplexSetup_ExecutesAllSteps()
    {
        // Arrange
        var testBase = new ComplexBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.Equal(2, testBase.ResourcesCreated.Count);
            Assert.Contains("database", testBase.ResourcesCreated);
            Assert.Contains("cache", testBase.ResourcesCreated);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    #endregion

    #region TestBase<T> Tests

    /// <summary>
    /// Test context for typed background testing.
    /// </summary>
    private class TypedTestContext
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Test fixture that inherits from TestBase{T}.
    /// </summary>
    private class TypedBackgroundTestBase : TestBase<TypedTestContext>
    {
        public int ConfigureCallCount { get; private set; }

        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<TypedTestContext> ConfigureTypedBackground()
        {
            ConfigureCallCount++;
            return Flow.Given("typed context", () => new TypedTestContext { Id = 42, Name = "Test" });
        }

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);

        public TypedTestContext GetBackground() => Background;
        public ScenarioChain<TypedTestContext> GetGivenBackground() => GivenBackground();
        public ScenarioChain<TypedTestContext> GetGivenBackground(string title) => GivenBackground(title);
    }

    [Fact]
    public async Task TypedBackground_Background_ReturnsTypedState()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act
            var background = testBase.GetBackground();

            // Assert
            Assert.NotNull(background);
            Assert.Equal(42, background.Id);
            Assert.Equal("Test", background.Name);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public void TypedBackground_Background_BeforeExecute_Throws()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => testBase.GetBackground());
            Assert.Contains("Background steps have not been executed", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task TypedBackground_GivenBackground_ReturnsChain()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act & Assert
            await testBase.GetGivenBackground()
                .Then("has id", ctx => ctx.Id == 42)
                .And("has name", ctx => ctx.Name == "Test")
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task TypedBackground_GivenBackground_WithTitle_UsesTitle()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act & Assert
            await testBase.GetGivenBackground("custom title")
                .Then("works", ctx => ctx.Id == 42)
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public void TypedBackground_GivenBackground_BeforeExecute_Throws()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => testBase.GetGivenBackground());
            Assert.Contains("Background steps have not been executed", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public void TypedBackground_GivenBackgroundWithTitle_BeforeExecute_Throws()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => testBase.GetGivenBackground("title"));
            Assert.Contains("Background steps have not been executed", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task TypedBackground_ConfigureTypedBackground_IsCalled()
    {
        // Arrange
        var testBase = new TypedBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act
            await testBase.RunBackgroundAsync();

            // Assert
            Assert.Equal(1, testBase.ConfigureCallCount);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    #endregion

    #region TestBase Helper Method Tests

    /// <summary>
    /// Test fixture that exposes protected members for testing.
    /// </summary>
    private class HelperMethodTestBase : TestBase
    {
        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground() => null;

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public ScenarioContext GetScenario() => Scenario;
        public FromContext GetFrom() => From();
        public FromContext GetFrom(ScenarioContext ctx) => From(ctx);
    }

    [Fact]
    public void TestBase_Scenario_ReturnsAmbientContext()
    {
        // Arrange
        var testBase = new HelperMethodTestBase();
        testBase.Initialize();

        try
        {
            // Act
            var scenario = testBase.GetScenario();

            // Assert
            Assert.NotNull(scenario);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task TestBase_From_ReturnsFromContextHelper()
    {
        // Arrange
        var testBase = new HelperMethodTestBase();
        testBase.Initialize();

        try
        {
            // Act - Use From() to create a chain
            var from = testBase.GetFrom();

            // Assert - FromContext should work
            await from.Given("test value", () => 42)
                .Then("is 42", v => v == 42)
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task TestBase_From_WithExplicitContext_WorksCorrectly()
    {
        // Arrange
        var testBase = new HelperMethodTestBase();
        testBase.Initialize();

        try
        {
            // Create explicit context
            var ctx = Bdd.CreateContext(testBase);

            // Act - Use From(ctx) to create a chain
            var from = testBase.GetFrom(ctx);

            // Assert - FromContext should work
            await from.Given("test value", () => "hello")
                .Then("has content", v => v.Length > 0)
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    #region GivenBackground with Custom Title Coverage

    /// <summary>
    /// Test fixture to cover GivenBackground with custom title error paths.
    /// </summary>
    private class TitledBackgroundTestBase : TestBase
    {
        protected override IBddReporter Reporter => new NullBddReporter();

        protected override ScenarioChain<object>? ConfigureBackground()
        {
            return Flow.Given("setup", () => (object)"test-value");
        }

        public void Initialize()
        {
            var ctx = Bdd.CreateContext(this);
            Ambient.Current.Value = ctx;
        }

        public void Cleanup()
        {
            Ambient.Current.Value = null;
        }

        public Task RunBackgroundAsync(CancellationToken ct = default)
            => ExecuteBackgroundAsync(ct);

        public ScenarioChain<T> GetGivenBackground<T>(string title) where T : class
            => GivenBackground<T>(title);
    }

    [Fact]
    public void GivenBackgroundWithTitle_BeforeExecute_ThrowsInvalidOperation()
    {
        // Arrange
        var testBase = new TitledBackgroundTestBase();
        testBase.Initialize();

        try
        {
            // Act & Assert - Background not executed yet
            var ex = Assert.Throws<InvalidOperationException>(() =>
                testBase.GetGivenBackground<string>("custom title"));
            Assert.Contains("Background steps have not been executed", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task GivenBackgroundWithTitle_WrongType_ThrowsInvalidOperation()
    {
        // Arrange
        var testBase = new TitledBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act & Assert - Wrong type
            var ex = Assert.Throws<InvalidOperationException>(() =>
                testBase.GetGivenBackground<List<int>>("wrong type title"));
            Assert.Contains("Background state is not of type", ex.Message);
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    [Fact]
    public async Task GivenBackgroundWithTitle_Success()
    {
        // Arrange
        var testBase = new TitledBackgroundTestBase();
        testBase.Initialize();

        try
        {
            await testBase.RunBackgroundAsync();

            // Act
            var chain = testBase.GetGivenBackground<string>("custom title");

            // Assert
            await chain.Then("has value", v => v == "test-value")
                .AssertPassed();
        }
        finally
        {
            testBase.Cleanup();
        }
    }

    #endregion

    #endregion
}
