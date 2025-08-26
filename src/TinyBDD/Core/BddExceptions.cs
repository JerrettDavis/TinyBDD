namespace TinyBDD;

/// <summary>
/// Wraps an exception thrown from a step execution (<c>Given</c>/<c>When</c>/<c>Then</c>/<c>And</c>/<c>But</c>)
/// with additional context about the failing step.
/// </summary>
/// <remarks>
/// The original failure is available in <see cref="Exception.InnerException"/>.
/// </remarks>
public sealed class BddStepException(string message, Exception inner) : 
    Exception(message, inner);
