namespace PatternKit.Core;

/// <summary>
/// Utility for computing the human-readable keyword displayed for a step line.
/// </summary>
/// <remarks>
/// Returns <c>And</c> or <c>But</c> for connective steps; otherwise the phase name
/// (<c>Given</c>, <c>When</c>, <c>Then</c>).
/// </remarks>
internal static class KindStrings
{
    /// <summary>
    /// Gets the display keyword for a step based on its phase and connective.
    /// </summary>
    /// <param name="phase">The BDD phase.</param>
    /// <param name="word">The connective for the step.</param>
    /// <returns>
    /// <c>And</c> or <c>But</c> for connective steps; otherwise the phase name.
    /// </returns>
    public static string For(StepPhase phase, StepWord word)
        => word switch { StepWord.And => "And", StepWord.But => "But", _ => phase.ToString() };
}
