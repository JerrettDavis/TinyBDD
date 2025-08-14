using Xunit.Abstractions;

namespace TinyBDD.Xunit;

public sealed class XunitBddReporter(ITestOutputHelper output) : IBddReporter
{
    public void WriteLine(string message) => output.WriteLine(message);
}