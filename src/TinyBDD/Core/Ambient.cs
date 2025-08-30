namespace TinyBDD;

/// <summary>
/// Provides access to the ambient <see cref="ScenarioContext"/> used by the fluent
/// <see cref="Flow"/> API.
/// </summary>
/// <remarks>
/// <para>
/// TinyBDD supports two entry points: the explicit <see cref="Bdd"/> API where a
/// <see cref="ScenarioContext"/> is passed around, and the ambient <see cref="Flow"/> API where
/// a context is obtained from this static holder. Test frameworks can set
/// <see cref="Current"/> at the beginning of a test to avoid plumbing the context through
/// method parameters.
/// </para>
/// <para>
/// If you use <see cref="Flow"/> methods without assigning a value to <see cref="Current"/>,
/// TinyBDD will throw an <see cref="InvalidOperationException"/> to signal that no scenario
/// is active. You can set it explicitly or inherit from one of the TinyBDD.* base classes
/// that manage it for you.
/// </para>
/// <para>
/// The value is stored in an <see cref="AsyncLocal{T}"/> so each asynchronous execution flow
/// (for example, within <see cref="Task"/> continuations) observes its own value. This makes it
/// safe to use in parallel tests where multiple scenarios run concurrently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a context and make it ambient for Flow.* calls
/// var previous = Ambient.Current.Value;
/// Ambient.Current.Value = Bdd.CreateContext(this);
/// try
/// {
///     await Flow.Given(() => 1)
///               .When("double", x => x * 2)
///               .Then("> 0", v => v > 0);
/// }
/// finally
/// {
///     // Always restore to avoid leaking context across tests
///     Ambient.Current.Value = previous;
/// }
/// </code>
/// </example>
/// <seealso href="xref:TinyBDD.Flow"/>
/// <seealso href="xref:TinyBDD.ScenarioContext"/>
/// <seealso href="xref:TinyBDD.Xunit.TinyBddXunitBase"/>
/// <seealso href="xref:TinyBDD.NUnit.TinyBddNUnitBase"/>
/// <seealso href="xref:TinyBDD.MSTest.TinyBddMsTestBase"/>
public static class Ambient
{
    /// <summary>
    /// The ambient scenario context for the current async flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field uses <see cref="AsyncLocal{T}"/> so that each asynchronous execution flow
    /// observes its own value. It is safe to use in parallel tests.
    /// </para>
    /// <para>
    /// Framework adapters like TinyBDD.Xunit/NUnit/MSTest set this automatically for the
    /// duration of a test. If you set it manually, prefer the save/restore pattern shown in
    /// the example to avoid leaking state between tests.
    /// </para>
    /// </remarks>
    public static readonly AsyncLocal<ScenarioContext?> Current = new();
}