namespace TinyBDD;

/// <summary>
/// Minimal helper predicates for quick assertions inside TinyBDD fluent chains.
/// </summary>
public static class Expect
{
    /// <summary>Pass-through for a boolean condition.</summary>
    public static bool True(bool condition) => condition;

    /// <summary>Compares two values using <see cref="EqualityComparer{T}.Default"/>.</summary>
    public static bool Equal<T>(T actual, T expected) => EqualityComparer<T>.Default.Equals(actual, expected);

    /// <summary>Checks that a value is not null.</summary>
    public static bool NotNull(object? o) => o is not null;
}