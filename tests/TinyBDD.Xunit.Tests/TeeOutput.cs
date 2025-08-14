using System.Text;
using Xunit.Abstractions;

namespace TinyBDD.Xunit.Tests;

public sealed class TeeOutput : ITestOutputHelper
{
    private readonly ITestOutputHelper _real;
    private readonly StringBuilder _sb = new();

    public TeeOutput(ITestOutputHelper real) => _real = real;

    public void WriteLine(string message)
    {
        _sb.AppendLine(message);
        _real.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        var s = string.Format(format, args);
        _sb.AppendLine(s);
        _real.WriteLine(s);
    }

    public override string ToString() => _sb.ToString();
}