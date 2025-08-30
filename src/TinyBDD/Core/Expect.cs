namespace TinyBDD;

/// <summary>
/// Minimal helper predicates for quick assertions inside TinyBDD fluent chains.
/// </summary>
/// <remarks>
/// These helpers return boolean values intended to be used with <c>Then</c>/<c>And</c>/<c>But</c> predicate overloads.
/// They are convenience methods only; you can use your preferred assertion library directly.
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "value", () => 42)
///          .Then("> 0", v => Expect.True(v > 0))
///          .And("equals 42", v => Expect.Equal(v, 42))
///          .But("not null", v => Expect.NotNull(v));
/// ctx.AssertPassed();
/// </code>
/// </example>
/// <seealso cref="Bdd"/>
/// <seealso cref="Flow"/>
public static class Expect
{
    /// <summary>Pass-through for a boolean condition.</summary>
    /// <param name="condition">The boolean expression to evaluate.</param>
    /// <returns><see langword="true"/> when the condition holds; otherwise <see langword="false"/>.</returns>
    public static bool True(bool condition) => condition;

    /// <summary>Compares two values using <see cref="EqualityComparer{T}.Default"/>.</summary>
    /// <typeparam name="T">The value type to compare.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><see langword="true"/> when <paramref name="actual"/> equals <paramref name="expected"/>; otherwise <see langword="false"/>.</returns>
    public static bool Equal<T>(T actual, T expected) => EqualityComparer<T>.Default.Equals(actual, expected);

    /// <summary>Checks that a value is not null.</summary>
    /// <param name="o">The value to test.</param>
    /// <returns><see langword="true"/> when <paramref name="o"/> is not null; otherwise <see langword="false"/>.</returns>
    public static bool NotNull(object? o) => o is not null;
}