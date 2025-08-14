namespace TinyBDD.MSTest;

public sealed class MsTestBddReporter : IBddReporter
{
    public void WriteLine(string message) => MsTestTraitBridge.TestContext?.WriteLine(message);
}