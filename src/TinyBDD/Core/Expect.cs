namespace TinyBDD;

public static class Expect
{
    public static bool True(bool condition) => condition;
    public static bool Equal<T>(T actual, T expected) => EqualityComparer<T>.Default.Equals(actual, expected);
    public static bool NotNull(object? o) => o is not null;
}