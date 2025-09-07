using System.Text;
using Xunit.Abstractions;

namespace TinyBDD.Xunit.Tests;

public sealed class TeeOutput(ITestOutputHelper real) : ITestOutputHelper
{
    private readonly StringBuilder _sb = new();

    public void WriteLine(string message) =>
        WriteLine(message, []);

    public void WriteLine(string format, params object[] args)
    {
        var s = string.Format(format, args);
        _sb.AppendLine(s);
        real.WriteLine(s);
    }

    public override string ToString() => _sb.ToString();
}