namespace TinyBDD;

/// <summary>
/// Defines a BDD (Behavior-Driven Development) test reporter that reports feature
/// execution in a Given/When/Then style.  
/// Implementations of this interface can integrate with different testing frameworks
/// such as NUnit, xUnit, or MSTest, providing consistent reporting across tools.
/// </summary>
public interface IBddReporter
{
    /// <summary>
    /// Writes a line of output to the BDD reporter.  
    /// This can be used to log execution steps, outcomes, or contextual messages
    /// during the lifecycle of a feature, scenario, or step.
    /// </summary>
    /// <param name="message">
    /// The text to be written to the report output.  
    /// Typically represents a BDD step description (e.g., "Given the user is logged in").
    /// </param>
    void WriteLine(string message);
}
