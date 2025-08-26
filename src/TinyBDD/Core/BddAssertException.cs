namespace TinyBDD;

/// <summary>
/// Thrown by TinyBDD when an assertion predicate in a <c>Then</c>/<c>And</c>/<c>But</c> step evaluates to false.
/// </summary>
public sealed class BddAssertException(string message) : Exception(message);