namespace TinyBDD;

/// <summary>
/// Defines a BDD (Behavior-Driven Development) test reporter that reports feature
/// execution in a Given/When/Then style.  
/// Implementations of this interface can integrate with different testing frameworks
/// such as NUnit, xUnit, or MSTest, providing consistent reporting across tools.
/// </summary>
/// <remarks>
/// Built-in implementations include <see cref="StringBddReporter"/>,
/// <see href="xref:TinyBDD.NUnit.NUnitBddReporter"/>,
/// <see href="xref:TinyBDD.Xunit.XunitBddReporter"/>, and
/// <see href="xref:TinyBDD.MSTest.MsTestBddReporter"/>. Use
/// <see cref="GherkinFormatter.Write(ScenarioContext, IBddReporter)"/> to render a scenario to a reporter.
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "number", () => 1)
///          .When("+1", v => v + 1)
///          .Then("== 2", v => v == 2);
/// var reporter = new StringBddReporter();
/// GherkinFormatter.Write(ctx, reporter);
/// Console.WriteLine(reporter.ToString());
/// </code>
/// </example>
/// <seealso cref="GherkinFormatter"/>
/// <seealso cref="ScenarioContext"/>
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
