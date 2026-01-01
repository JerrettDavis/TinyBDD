namespace PatternKit.Core;

/// <summary>
/// Identifies the high-level BDD phase of a step: <c>Given</c>, <c>When</c>, or <c>Then</c>.
/// </summary>
/// <remarks>
/// These phases follow the classic BDD structure:
/// <list type="bullet">
///   <item><description><see cref="Given"/> - Setup and preconditions (arrange)</description></item>
///   <item><description><see cref="When"/> - Action or behavior under test (act)</description></item>
///   <item><description><see cref="Then"/> - Verification and assertions (assert)</description></item>
/// </list>
/// </remarks>
public enum StepPhase
{
    /// <summary>Setup and preconditions (arrange phase).</summary>
    Given,

    /// <summary>Action or behavior under test (act phase).</summary>
    When,

    /// <summary>Verification and assertions (assert phase).</summary>
    Then
}
