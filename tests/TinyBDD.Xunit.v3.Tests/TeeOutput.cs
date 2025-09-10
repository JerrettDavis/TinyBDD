using System.Text;

namespace TinyBDD.Xunit.v3.Tests;

public sealed class TeeOutput(ITestOutputHelper real) : ITestOutputHelper
{
    private readonly StringBuilder _sb = new();

    public void Write(string message)
    {
        _sb.Append(message);
        real.Write(message);
    }

    public void Write(string format, params object[] args)
    {
        var s = string.Format(format, args);
        _sb.Append(s);
        real.Write(s);
    }

    public void WriteLine(string message) =>
        WriteLine(message, []);

    public void WriteLine(string format, params object[] args)
    {
        var s = string.Format(format, args);
        _sb.AppendLine(s);
        real.WriteLine(s);
    }

    public string Output => _sb.ToString();

    public override string ToString() => _sb.ToString();
}