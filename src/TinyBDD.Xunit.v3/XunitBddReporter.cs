using Xunit;

namespace TinyBDD.Xunit.v3;

/// <summary>
/// Writes TinyBDD report lines to xUnit's <see cref="ITestOutputHelper"/>.
/// </summary>
public sealed class XunitBddReporter(ITestOutputHelper output) : IBddReporter
{
    /// <summary>Writes a line to xUnit’s test output sink.</summary>
    public void WriteLine(string message) => output.WriteLine(message);
}