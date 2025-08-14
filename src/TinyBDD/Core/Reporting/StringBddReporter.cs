using System.Text;

namespace TinyBDD;

public sealed class StringBddReporter : IBddReporter
{
    private readonly StringBuilder _sb = new();
    public void WriteLine(string message) => _sb.AppendLine(message);
    public override string ToString() => _sb.ToString();
}
