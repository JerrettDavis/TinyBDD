namespace TinyBDD.MSTest;

/// <summary>
/// Writes TinyBDD report lines to MSTest's <see cref="TestContext"/> output.
/// </summary>
public sealed class MsTestBddReporter : IBddReporter
{
    /// <summary>Writes a line to the MSTest test output window.</summary>
    public void WriteLine(string message) => MsTestTraitBridge.TestContext?.WriteLine(message);
}