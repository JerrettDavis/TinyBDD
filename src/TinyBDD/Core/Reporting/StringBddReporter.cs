using System.Text;

namespace TinyBDD;

/// <summary>
/// Reports BDD test execution results by appending formatted strings to an internal buffer.
/// This reporter captures feature/step information during scenario execution, providing a compact text representation of the tests performed.
/// </summary>
public sealed class StringBddReporter : IBddReporter
{
    private readonly StringBuilder _sb = new();

    /// <summary>
    /// Appends a formatted string to an internal buffer for BDD test reporting.
    /// This method writes a line of text (with automatic newline appending) to the report content buffer.
    /// </summary>
    /// <param name="message">The message to be written, typically one step description or feature information</param>
    public void WriteLine(string message) => _sb.AppendLine(message);

    /// <summary>
    /// Converts the BDD reporter instance into a compact text representation of the test execution results.
    /// Returns a formatted string containing feature and scenario information, step titles and statuses (OK/FAIL).
    /// </summary>
    /// <returns>A string representing the complete BDD test report with embedded feature names,
    /// scenario descriptions, steps execution status, and any error messages.</returns>
    public override string ToString() => _sb.ToString();
}
