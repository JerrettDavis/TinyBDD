using System.Reflection;

namespace TinyBDD;

/// <summary>
/// Provides an ambient (per-async-flow) implementation of <see cref="ITestMethodResolver"/> that
/// stores the current test <see cref="MethodInfo"/> in an <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// TinyBDD needs to know the currently executing test method to read attributes like
/// <see cref="ScenarioAttribute"/> and <see cref="TagAttribute"/>. Test adapters (xUnit, NUnit, MSTest)
/// can set the active method at the start of each test and clear it afterward. This avoids brittle
/// stack walking and works well with parallel execution.
/// </para>
/// <para>
/// Typical usage is:
/// </para>
/// <list type="bullet">
///   <item><description>Register the resolver once via <see cref="Bdd.Register(ITestMethodResolver)"/>.</description></item>
///   <item><description>Set the current method at test start using <see cref="Set(MethodInfo?)"/>.</description></item>
///   <item><description>Clear it at test end by calling <see cref="Set(MethodInfo?)"/> with <see langword="null"/>.</description></item>
/// </list>
/// <para>
/// If no ambient method is set, TinyBDD may fall back to a stack-trace based resolver (if available).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // MSTest example
/// [TestInitialize]
/// public void Init()
/// {
///     Bdd.Register(AmbientTestMethodResolver.Instance);
///     var mi = typeof(MyTests).GetMethod(TestContext.TestName,
///         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
///     AmbientTestMethodResolver.Set(mi);
///     Ambient.Current.Value = Bdd.CreateContext(this, traits: new MsTestTraitBridge(TestContext));
/// }
///
/// [TestCleanup]
/// public void Cleanup()
/// {
///     Ambient.Current.Value = null;
///     AmbientTestMethodResolver.Set(null);
/// }
/// </code>
/// </example>
/// <seealso cref="ITestMethodResolver"/>
/// <seealso cref="Bdd.Register(ITestMethodResolver)"/>
public sealed class AmbientTestMethodResolver : ITestMethodResolver
{
    /// <summary>
    /// Stores the current test <see cref="MethodInfo"/> for the active async context.
    /// </summary>
    private static readonly AsyncLocal<MethodInfo?> Current = new();

    /// <summary>
    /// Gets the singleton instance of <see cref="AmbientTestMethodResolver"/>.
    /// </summary>
    /// <remarks>
    /// Register this instance once (e.g., in a test base class or adapter attribute) with
    /// <see cref="Bdd.Register(ITestMethodResolver)"/>, then call <see cref="Set(MethodInfo?)"/>
    /// at the beginning of each test.
    /// </remarks>
    public static AmbientTestMethodResolver Instance { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AmbientTestMethodResolver"/> class.
    /// </summary>
    /// <remarks>
    /// Constructor is private to enforce the singleton pattern; use <see cref="Instance"/>.
    /// </remarks>
    private AmbientTestMethodResolver()
    {
    }

    /// <summary>
    /// Sets the current test <see cref="MethodInfo"/> for the active async flow.
    /// </summary>
    /// <param name="method">
    /// The <see cref="MethodInfo"/> representing the test method that is about to run,
    /// or <see langword="null"/> to clear the ambient value at the end of the test.
    /// </param>
    /// <remarks>
    /// This method is typically invoked by a framework adapter (e.g., an attribute or base class)
    /// in a per-test setup/teardown hook. Because it uses <see cref="AsyncLocal{T}"/>, it is safe
    /// to use in parallel test execution scenarios.
    /// </remarks>
    public static void Set(MethodInfo? method) => Current.Value = method;

    /// <summary>
    /// Gets the current test <see cref="MethodInfo"/> previously set for the active async flow.
    /// </summary>
    /// <returns>
    /// The current <see cref="MethodInfo"/> if one has been set via <see cref="Set(MethodInfo?)"/>,
    /// otherwise <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// When this returns <see langword="null"/>, TinyBDD may attempt a slower, stack-trace based
    /// resolution if configured to do so.
    /// </remarks>
    public MethodInfo? GetCurrentTestMethod() => Current.Value;
}