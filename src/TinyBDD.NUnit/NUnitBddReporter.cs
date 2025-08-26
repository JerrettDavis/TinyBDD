using NUnit.Framework;

namespace TinyBDD.NUnit;

/// <summary>
/// Writes TinyBDD report lines to NUnit's <see cref="TestContext"/> output stream.
/// </summary>
public sealed class NUnitBddReporter : IBddReporter
{
    /// <summary>Writes a line to NUnit's test output.</summary>
    public void WriteLine(string message) => TestContext.Out.WriteLine(message);
}