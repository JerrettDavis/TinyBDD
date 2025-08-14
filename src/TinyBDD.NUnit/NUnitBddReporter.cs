using NUnit.Framework;

namespace TinyBDD.NUnit;

public sealed class NUnitBddReporter : IBddReporter
{
    public void WriteLine(string message) => TestContext.Out.WriteLine(message);
}