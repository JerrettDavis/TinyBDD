namespace TinyBDD;

/// <summary>
/// Thrown by TinyBDD when an assertion predicate in a <c>Then</c>/<c>And</c>/<c>But</c> step evaluates to false.
/// </summary>
/// <remarks>
/// TinyBDD converts failed boolean predicates (returning false) into this exception type. The
/// exception message typically contains the step title for easier diagnostics in reports.
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "value", () => 1)
///          .Then("== 2", v => v == 2); // will throw BddAssertException when awaited
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="StepResult"/>
public sealed class BddAssertException(string message) : Exception(message);