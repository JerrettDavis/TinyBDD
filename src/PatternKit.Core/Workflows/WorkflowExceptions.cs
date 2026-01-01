namespace PatternKit.Core;

/// <summary>
/// Exception thrown when a workflow step fails.
/// </summary>
/// <remarks>
/// This exception wraps the original exception and provides access to the workflow context
/// for debugging and diagnostics.
/// </remarks>
public class WorkflowStepException : Exception
{
    /// <summary>
    /// Gets the workflow context at the time of failure.
    /// </summary>
    public WorkflowContext Context { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowStepException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="inner">The original exception.</param>
    public WorkflowStepException(string message, WorkflowContext context, Exception inner)
        : base(message, inner)
    {
        Context = context;
    }
}

/// <summary>
/// Exception thrown when an assertion fails within a workflow step.
/// </summary>
/// <remarks>
/// This is the base exception type for assertion failures. Test frameworks
/// can derive from this or catch it to identify failed assertions.
/// </remarks>
public class WorkflowAssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowAssertionException"/>.
    /// </summary>
    /// <param name="message">The assertion failure message.</param>
    public WorkflowAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowAssertionException"/>.
    /// </summary>
    /// <param name="message">The assertion failure message.</param>
    /// <param name="inner">The inner exception.</param>
    public WorkflowAssertionException(string message, Exception inner) : base(message, inner)
    {
    }
}
