namespace TinyBDD;

/// <summary>
/// Minimal, framework-agnostic base class for TinyBDD tests that wires the
/// ambient <see cref="ScenarioContext"/> and offers convenience <c>Given</c> helpers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TestBase"/> centralizes TinyBDD test wiring for any unit-test framework
/// (xUnit, NUnit, MSTest). It expects a derived type (or framework-specific base)
/// to set <see cref="Ambient.Current"/> to a live <see cref="ScenarioContext"/> before
/// a test runs, and to call <see cref="CleanUp"/> afterward (typically in teardown).
/// </para>
/// <para>
/// All <c>Given</c> overloads delegate to <see cref="Flow"/> which in turn forwards to
/// <see cref="Bdd"/> using the current ambient context. If the ambient context is not set,
/// an <see cref="InvalidOperationException"/> is thrown when invoking any step.
/// </para>
/// <para>
/// The base also exposes <see cref="From()"/> to start chains against an explicit
/// <see cref="ScenarioContext"/>—useful when composing helper methods that construct
/// sub-chains without relying on global ambient state.
/// </para>
/// </remarks>
/// <example>
/// <para>xUnit (call <see cref="CleanUp"/> in teardown):</para>
/// <code>
/// using TinyBDD;
/// using TinyBDD.Xunit;
/// using Xunit;
///
/// public sealed class MyXunitBase : TestBase, IAsyncLifetime
/// {
///     protected override IBddReporter Reporter { get; } = new XunitReporter();
///
///     public Task InitializeAsync()
///     {
///         Ambient.Current.Value = Bdd.CreateContext(this, featureName: "Sample Feature");
///         return Task.CompletedTask;
///     }
///
///     public Task DisposeAsync()
///     {
///         CleanUp(); // writes Gherkin summary, clears Ambient.Current
///         return Task.CompletedTask;
///     }
/// }
///
/// public sealed class SampleTests : MyXunitBase
/// {
///     [Fact]
///     public async Task Demo()
///     {
///         await Given("a number", () => 2)
///             .When("double it", x => x * 2)
///             .Then("equals 4", v => v == 4);
///
///         Scenario.AssertPassed();
///     }
/// }
/// </code>
/// <para>MSTest (use [TestInitialize]/[TestCleanup]):</para>
/// <code>
/// using Microsoft.VisualStudio.TestTools.UnitTesting;
///
/// [TestClass]
/// public sealed class MyMsTestBase : TestBase
/// {
///     protected override IBddReporter Reporter { get; } = new MsTestReporter(TestContext!);
///     public TestContext? TestContext { get; set; }
///
///     [TestInitialize]
///     public void SetUp()
///         => Ambient.Current.Value = Bdd.CreateContext(this, featureName: "Feature");
///
///     [TestCleanup]
///     public void TearDown() => CleanUp();
/// }
/// </code>
/// <para>NUnit (use [SetUp]/[TearDown]):</para>
/// <code>
/// using NUnit.Framework;
///
/// public sealed class MyNUnitBase : TestBase
/// {
///     protected override IBddReporter Reporter { get; } = new NUnitReporter();
///
///     [SetUp]
///     public void SetUp()
///         => Ambient.Current.Value = Bdd.CreateContext(this, featureName: "Feature");
///
///     [TearDown]
///     public void TearDown() => CleanUp();
/// }
/// </code>
/// </example>
/// <seealso cref="Ambient"/>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="Flow"/>
/// <seealso cref="Bdd"/>
/// <seealso cref="GherkinFormatter"/>
public abstract class TestBase
{
    /// <summary>
    /// The current <see cref="ScenarioContext"/> for the running test, sourced from
    /// <see cref="Ambient.Current"/>. This is non-null during a properly initialized test.
    /// </summary>
    protected ScenarioContext Scenario => Ambient.Current.Value!;

    /// <summary>
    /// The reporter used to emit scenario output (e.g., Gherkin-style summaries) at cleanup.
    /// Framework-specific base classes should provide an appropriate implementation.
    /// </summary>
    protected abstract IBddReporter Reporter { get; }

    /// <summary>
    /// Writes a concise Gherkin-style run summary (via <see cref="GherkinFormatter"/>) if a
    /// <see cref="ScenarioContext"/> is present, then clears <see cref="Ambient.Current"/>.
    /// </summary>
    /// <remarks>
    /// Call this from your framework teardown hook (e.g., xUnit <c>DisposeAsync</c>,
    /// MSTest <c>[TestCleanup]</c>, or NUnit <c>[TearDown]</c>). It is safe to call even
    /// if no scenario was started (no-op aside from clearing ambient state).
    /// </remarks>
    protected void CleanUp()
    {
        var ctx = Ambient.Current.Value;
        if (ctx is not null)
            GherkinFormatter.Write(ctx, Reporter);

        Ambient.Current.Value = null;
    }

    /// <summary>
    /// Starts a <c>Given</c> step with an explicit title and synchronous setup.
    /// </summary>
    /// <typeparam name="T">Type produced by the setup function.</typeparam>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory for the initial value.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> for chaining <c>When</c>/<c>Then</c>.</returns>
    protected ScenarioChain<T> Given<T>(string title, Func<T> setup) =>
        Flow.Given(title, setup);

    /// <summary>
    /// Starts a <c>Given</c> step with an explicit title and <see cref="ValueTask{TResult}"/> setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(string title, Func<ValueTask<T>> setup) =>
        Flow.Given(title, setup);

    /// <summary>
    /// Starts a token-aware <c>Given</c> step with an explicit title and <see cref="ValueTask{TResult}"/> setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(string title, Func<CancellationToken, ValueTask<T>> setup) =>
        Flow.Given(title, setup);

    /// <summary>
    /// Starts a <c>Given</c> step with an explicit title and asynchronous setup.
    /// </summary>
    /// <remarks>Use this overload when the initial value requires async I/O.</remarks>
    protected ScenarioChain<T> Given<T>(string title, Func<Task<T>> setup) =>
        Flow.Given(title, setup);

    /// <summary>
    /// Starts a token-aware <c>Given</c> step with an explicit title and asynchronous setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup) =>
        Flow.Given(title, setup);

    /// <summary>
    /// Starts a <c>Given</c> step with a default title and synchronous setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(Func<T> setup) =>
        Flow.Given(setup);

    /// <summary>
    /// Starts a <c>Given</c> step with a default title and <see cref="ValueTask{TResult}"/> setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(Func<ValueTask<T>> setup) =>
        Flow.Given(setup);

    /// <summary>
    /// Starts a <c>Given</c> step with a default title and asynchronous setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(Func<Task<T>> setup) =>
        Flow.Given(setup);

    /// <summary>
    /// Starts a token-aware <c>Given</c> step with a default title and asynchronous setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup) =>
        Flow.Given(setup);

    /// <summary>
    /// Starts a token-aware <c>Given</c> step with a default title and <see cref="ValueTask{TResult}"/> setup.
    /// </summary>
    protected ScenarioChain<T> Given<T>(Func<CancellationToken, ValueTask<T>> setup) =>
        Flow.Given(setup);

    /// <summary>
    /// Creates a helper bound to the current ambient <see cref="Scenario"/> for starting chains
    /// without relying on static <see cref="Flow"/> methods in downstream helpers.
    /// </summary>
    /// <returns>A <see cref="FromContext"/> tied to <see cref="Scenario"/>.</returns>
    protected FromContext From() => Flow.From(Scenario);

    /// <summary>
    /// Creates a helper for starting chains from an explicit <see cref="ScenarioContext"/>.
    /// </summary>
    /// <param name="ctx">The scenario context to use for subsequent calls.</param>
    /// <returns>A <see cref="FromContext"/> bound to <paramref name="ctx"/>.</returns>
    protected FromContext From(ScenarioContext ctx) => Flow.From(ctx);

    #region Feature Setup and Teardown

    /// <summary>
    /// Gets or sets the feature state captured after <see cref="ExecuteFeatureSetupAsync"/> completes.
    /// </summary>
    /// <remarks>
    /// This property is populated by <see cref="ExecuteFeatureSetupAsync"/> after running
    /// the feature setup steps configured in <see cref="ConfigureFeatureSetup"/>.
    /// Feature state is shared across all scenarios in the test class.
    /// </remarks>
    protected object? FeatureState { get; set; }

    /// <summary>
    /// Gets a value indicating whether feature setup has been executed.
    /// </summary>
    protected bool FeatureSetupExecuted { get; set; }

    /// <summary>
    /// Override to configure feature-level setup steps that run once before any scenarios.
    /// </summary>
    /// <returns>
    /// A <see cref="ScenarioChain{T}"/> representing the feature setup steps,
    /// or <see langword="null"/> if no feature setup is needed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Feature setup steps are executed once before any test method runs in the test class.
    /// The final value from the chain is stored in <see cref="FeatureState"/> and can be
    /// accessed from all scenarios.
    /// </para>
    /// <para>
    /// This is useful for expensive setup operations that can be shared across multiple scenarios,
    /// such as creating a test database connection, initializing a web server, or loading test data.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override ScenarioChain&lt;object&gt;? ConfigureFeatureSetup()
    /// {
    ///     return Given("test server started", () => new TestServer())
    ///         .And("database seeded", server => { server.SeedData(); return server; });
    /// }
    /// </code>
    /// </example>
    protected virtual ScenarioChain<object>? ConfigureFeatureSetup() => null;

    /// <summary>
    /// Override to configure feature-level teardown steps that run once after all scenarios complete.
    /// </summary>
    /// <returns>
    /// A <see cref="ScenarioChain{T}"/> representing the feature teardown steps,
    /// or <see langword="null"/> if no feature teardown is needed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Feature teardown steps are executed once after all test methods in the test class complete.
    /// Use this to clean up expensive resources initialized in <see cref="ConfigureFeatureSetup"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override ScenarioChain&lt;object&gt;? ConfigureFeatureTeardown()
    /// {
    ///     if (FeatureState is TestServer server)
    ///     {
    ///         return Given("cleanup server", () => server)
    ///             .And("dispose", s => { s.Dispose(); return s; });
    ///     }
    ///     return null;
    /// }
    /// </code>
    /// </example>
    protected virtual ScenarioChain<object>? ConfigureFeatureTeardown() => null;

    /// <summary>
    /// Executes the feature setup steps configured in <see cref="ConfigureFeatureSetup"/>.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this method from your test framework's one-time initialization hook
    /// (e.g., xUnit <c>IAsyncLifetime.InitializeAsync</c> with static state,
    /// MSTest <c>[ClassInitialize]</c>, or NUnit <c>[OneTimeSetUp]</c>).
    /// </para>
    /// <para>
    /// The final value from the feature setup chain is captured in <see cref="FeatureState"/>.
    /// </para>
    /// </remarks>
    protected async Task ExecuteFeatureSetupAsync(CancellationToken ct = default)
    {
        var featureSetup = ConfigureFeatureSetup();
        if (featureSetup is null)
        {
            FeatureSetupExecuted = true;
            return;
        }

        object? capturedState = null;
        await featureSetup
            .Then("feature setup complete", state =>
            {
                capturedState = state;
                return true;
            })
            .AssertPassed(ct);

        FeatureState = capturedState;
        FeatureSetupExecuted = true;
    }

    /// <summary>
    /// Executes the feature teardown steps configured in <see cref="ConfigureFeatureTeardown"/>.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this method from your test framework's one-time cleanup hook
    /// (e.g., xUnit <c>IAsyncLifetime.DisposeAsync</c> with static state,
    /// MSTest <c>[ClassCleanup]</c>, or NUnit <c>[OneTimeTearDown]</c>).
    /// </para>
    /// </remarks>
    protected async Task ExecuteFeatureTeardownAsync(CancellationToken ct = default)
    {
        var featureTeardown = ConfigureFeatureTeardown();
        if (featureTeardown is null)
            return;

        try
        {
            await featureTeardown
                .Then("feature teardown complete", _ => true)
                .AssertPassed(ct);
        }
        catch
        {
            // Log but don't throw - teardown failures shouldn't fail the test run
        }
    }

    #endregion

    #region Background Steps

    /// <summary>
    /// Gets or sets the background state captured after <see cref="ExecuteBackgroundAsync"/> completes.
    /// </summary>
    /// <remarks>
    /// This property is populated by <see cref="ExecuteBackgroundAsync"/> after running
    /// the background steps configured in <see cref="ConfigureBackground"/>.
    /// </remarks>
    protected object? BackgroundState { get; private set; }

    /// <summary>
    /// Gets a value indicating whether background steps have been executed.
    /// </summary>
    protected bool BackgroundExecuted { get; private set; }

    /// <summary>
    /// Override to configure background steps that run before each scenario.
    /// </summary>
    /// <returns>
    /// A <see cref="ScenarioChain{T}"/> representing the background steps,
    /// or <see langword="null"/> if no background is needed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Background steps are executed once before each test method by calling
    /// <see cref="ExecuteBackgroundAsync"/>. The final value from the chain
    /// is stored in <see cref="BackgroundState"/> and can be accessed via
    /// <see cref="GivenBackground{T}()"/>.
    /// </para>
    /// <para>
    /// Override this method in your test class to define shared setup steps.
    /// The background chain should not call <c>AssertPassed()</c>; that is
    /// handled automatically by <see cref="ExecuteBackgroundAsync"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override ScenarioChain&lt;object&gt;? ConfigureBackground()
    /// {
    ///     return Given("a database connection", () => new DbConnection())
    ///         .And("test data seeded", conn => { SeedData(conn); return conn; });
    /// }
    /// </code>
    /// </example>
    protected virtual ScenarioChain<object>? ConfigureBackground() => null;

    /// <summary>
    /// Executes the background steps configured in <see cref="ConfigureBackground"/>.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this method from your test framework's initialization hook
    /// (e.g., <c>[TestInitialize]</c>, <c>[SetUp]</c>, or <c>InitializeAsync</c>)
    /// after setting up the ambient context.
    /// </para>
    /// <para>
    /// The final value from the background chain is captured in <see cref="BackgroundState"/>.
    /// </para>
    /// </remarks>
    protected async Task ExecuteBackgroundAsync(CancellationToken ct = default)
    {
        var background = ConfigureBackground();
        if (background is null)
        {
            BackgroundExecuted = true;
            return;
        }

        object? capturedState = null;
        await background
            .Then("background complete", state =>
            {
                capturedState = state;
                return true;
            })
            .AssertPassed(ct);

        BackgroundState = capturedState;
        BackgroundExecuted = true;
    }

    /// <summary>
    /// Starts a Given step that continues from the background state.
    /// </summary>
    /// <typeparam name="T">The expected type of the background state.</typeparam>
    /// <returns>A <see cref="ScenarioChain{T}"/> starting with the background state.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if background steps have not been executed or the state is null.
    /// </exception>
    /// <example>
    /// <code>
    /// [Test]
    /// public async Task TestWithBackground()
    /// {
    ///     await GivenBackground&lt;DbConnection&gt;()
    ///         .When("querying users", conn => conn.Query&lt;User&gt;("SELECT * FROM Users"))
    ///         .Then("users exist", users => users.Any())
    ///         .AssertPassed();
    /// }
    /// </code>
    /// </example>
    protected ScenarioChain<T> GivenBackground<T>() where T : class
    {
        if (!BackgroundExecuted)
            throw new InvalidOperationException(
                "Background steps have not been executed. Call ExecuteBackgroundAsync() first.");

        var state = BackgroundState as T
            ?? throw new InvalidOperationException(
                $"Background state is not of type {typeof(T).Name}. " +
                $"Actual type: {BackgroundState?.GetType().Name ?? "null"}");

        return Given("background", () => state);
    }

    /// <summary>
    /// Starts a Given step that continues from the background state with a custom title.
    /// </summary>
    /// <typeparam name="T">The expected type of the background state.</typeparam>
    /// <param name="title">A custom title for the Given step.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> starting with the background state.</returns>
    protected ScenarioChain<T> GivenBackground<T>(string title) where T : class
    {
        if (!BackgroundExecuted)
            throw new InvalidOperationException(
                "Background steps have not been executed. Call ExecuteBackgroundAsync() first.");

        var state = BackgroundState as T
            ?? throw new InvalidOperationException(
                $"Background state is not of type {typeof(T).Name}. " +
                $"Actual type: {BackgroundState?.GetType().Name ?? "null"}");

        return Given(title, () => state);
    }

    #endregion

    #region Feature State Access

    /// <summary>
    /// Starts a Given step that continues from the feature state.
    /// </summary>
    /// <typeparam name="T">The expected type of the feature state.</typeparam>
    /// <returns>A <see cref="ScenarioChain{T}"/> starting with the feature state.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if feature setup has not been executed or the state is null.
    /// </exception>
    /// <example>
    /// <code>
    /// [Test]
    /// public async Task TestWithFeature()
    /// {
    ///     await GivenFeature&lt;TestServer&gt;()
    ///         .When("sending request", server => server.Get("/api/users"))
    ///         .Then("response is ok", response => response.StatusCode == 200)
    ///         .AssertPassed();
    /// }
    /// </code>
    /// </example>
    protected ScenarioChain<T> GivenFeature<T>() where T : class
    {
        if (!FeatureSetupExecuted)
            throw new InvalidOperationException(
                "Feature setup has not been executed. Call ExecuteFeatureSetupAsync() first.");

        var state = FeatureState as T
            ?? throw new InvalidOperationException(
                $"Feature state is not of type {typeof(T).Name}. " +
                $"Actual type: {FeatureState?.GetType().Name ?? "null"}");

        return Given("feature", () => state);
    }

    /// <summary>
    /// Starts a Given step that continues from the feature state with a custom title.
    /// </summary>
    /// <typeparam name="T">The expected type of the feature state.</typeparam>
    /// <param name="title">A custom title for the Given step.</param>
    /// <returns>A <see cref="ScenarioChain{T}"/> starting with the feature state.</returns>
    protected ScenarioChain<T> GivenFeature<T>(string title) where T : class
    {
        if (!FeatureSetupExecuted)
            throw new InvalidOperationException(
                "Feature setup has not been executed. Call ExecuteFeatureSetupAsync() first.");

        var state = FeatureState as T
            ?? throw new InvalidOperationException(
                $"Feature state is not of type {typeof(T).Name}. " +
                $"Actual type: {FeatureState?.GetType().Name ?? "null"}");

        return Given(title, () => state);
    }

    #endregion
}