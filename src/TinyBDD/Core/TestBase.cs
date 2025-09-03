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
}