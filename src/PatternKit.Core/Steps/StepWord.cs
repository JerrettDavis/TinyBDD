namespace PatternKit.Core;

/// <summary>
/// Identifies the BDD connective used for a step within a phase: primary keyword, <c>And</c>, or <c>But</c>.
/// </summary>
/// <remarks>
/// Connectives allow multiple related steps within the same phase:
/// <list type="bullet">
///   <item><description><see cref="Primary"/> - The first step in a phase using the phase keyword (Given/When/Then)</description></item>
///   <item><description><see cref="And"/> - Continues the previous phase with an additive condition</description></item>
///   <item><description><see cref="But"/> - Continues the previous phase with a contrasting condition</description></item>
/// </list>
/// </remarks>
public enum StepWord
{
    /// <summary>The primary keyword for the phase (e.g., <c>Given</c>, <c>When</c>, <c>Then</c>).</summary>
    Primary,

    /// <summary>Connective that continues the previous phase: <c>And</c>.</summary>
    And,

    /// <summary>Connective that continues the previous phase with contrast: <c>But</c>.</summary>
    But
}
