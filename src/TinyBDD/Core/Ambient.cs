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
/// </remarks>
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
    /// </remarks>
    public static readonly AsyncLocal<ScenarioContext?> Current = new();
}