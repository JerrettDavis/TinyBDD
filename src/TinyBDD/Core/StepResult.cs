namespace TinyBDD;

/// <summary>
/// Represents a single executed step in a scenario (Given/When/Then/And/But),
/// including title, elapsed time, and any error captured.
/// </summary>
/// <remarks>
/// <para>
/// Instances of this type are created by TinyBDD when executing steps and are exposed via
/// <see cref="ScenarioContext.Steps"/> for reporting and diagnostics. The properties are immutable
/// and reflect the outcome of a single step execution.
/// </para>
/// <para>
/// The <see cref="Elapsed"/> value captures the duration of the user-provided delegate for the step
/// as measured by TinyBDD. The <see cref="Error"/> is populated when a step throws; frameworks may
/// display this information alongside the step in test output.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "seed", () => 1)
///          .When("+1", x => x + 1)
///          .Then("== 2", v => v == 2);
///
/// // Inspect executed steps for custom reporting
/// foreach (var step in ctx.Steps)
/// {
///     Console.WriteLine($"{step.Kind} {step.Title} [{step.Elapsed.TotalMilliseconds} ms] " +
///                       (step.Error is null ? "OK" : $"FAILED: {step.Error.Message}"));
/// }
/// </code>
/// </example>
/// <seealso href="xref:TinyBDD.ScenarioContext"/>
/// <seealso href="xref:TinyBDD.GherkinFormatter"/>
public sealed class StepResult
{
    /// <summary>Step kind keyword as rendered in reports (e.g., <c>Given</c>, <c>When</c>, <c>Then</c>, <c>And</c>, <c>But</c>).</summary>
    public required string Kind { get; init; }     // Given, When, Then, And, But

    /// <summary>Human-readable step title.</summary>
    public required string Title { get; init; }

    /// <summary>Elapsed time for executing the step.</summary>
    public TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Exception captured during step execution, if any. 
    /// </summary>
    public Exception? Error { get; init; }
}
