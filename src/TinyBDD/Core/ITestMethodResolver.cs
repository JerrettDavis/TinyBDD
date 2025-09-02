using System.Reflection;

namespace TinyBDD;

/// <summary>
/// Provides an abstraction for resolving the currently executing test method.
/// </summary>
/// <remarks>
/// <para>
/// TinyBDD uses an implementation of this interface to obtain the <see cref="MethodInfo"/> of
/// the currently running test method, which is then used to read metadata such as
/// <see cref="ScenarioAttribute"/> or <see cref="TagAttribute"/>.
/// </para>
/// <para>
/// Test framework adapters (xUnit, NUnit, MSTest, etc.) implement this interface to supply
/// a reliable way of identifying the active test without relying solely on stack walking,
/// which can be brittle or slow.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A minimal implementation using an AsyncLocal
/// public sealed class AmbientTestMethodResolver : ITestMethodResolver
/// {
///     private static readonly AsyncLocal&gt;MethodInfo?&lt; Current = new();
///
///     public static void Set(MethodInfo? method) => Current.Value = method;
///
///     public MethodInfo? GetCurrentTestMethod() => Current.Value;
/// }
///
/// // Registration in test setup
/// Bdd.Register(AmbientTestMethodResolver.Instance);
/// AmbientTestMethodResolver.Set(currentMethodInfo);
/// </code>
/// </example>
/// <seealso cref="AmbientTestMethodResolver"/>
/// <seealso cref="Bdd.Register(ITestMethodResolver)"/>
public interface ITestMethodResolver
{
    /// <summary>
    /// Returns the <see cref="MethodInfo"/> for the currently executing test method, or
    /// <see langword="null"/> if no method could be resolved.
    /// </summary>
    /// <returns>
    /// The <see cref="MethodInfo"/> of the active test method, or <see langword="null"/> if
    /// no method is known for the current execution context.
    /// </returns>
    /// <remarks>
    /// Implementations should ensure this method is safe to call concurrently from multiple
    /// tests, e.g., by using <see cref="AsyncLocal{T}"/> or other thread/async-safe mechanisms.
    /// </remarks>
    MethodInfo? GetCurrentTestMethod();
}