namespace PatternKit.Core;

/// <summary>
/// Cross-cutting behavior that wraps step execution.
/// </summary>
/// <remarks>
/// <para>
/// Behaviors implement the Chain of Responsibility pattern, allowing you to add
/// cross-cutting concerns like logging, timing, retries, or circuit breakers
/// without modifying the core step logic.
/// </para>
/// <para>
/// Multiple behaviors can be composed together, each wrapping the next in the chain.
/// </para>
/// </remarks>
/// <typeparam name="T">The type flowing through the workflow at this point.</typeparam>
/// <example>
/// <code>
/// public class TimingBehavior&lt;T&gt; : IBehavior&lt;T&gt;
/// {
///     public async ValueTask&lt;T&gt; WrapAsync(
///         Func&lt;CancellationToken, ValueTask&lt;T&gt;&gt; next,
///         WorkflowContext context,
///         StepMetadata step,
///         CancellationToken ct)
///     {
///         var sw = Stopwatch.StartNew();
///         try { return await next(ct); }
///         finally { context.SetMetadata($"timing:{step.Title}", sw.Elapsed); }
///     }
/// }
/// </code>
/// </example>
public interface IBehavior<T>
{
    /// <summary>
    /// Wraps the execution of a step, optionally adding before/after logic.
    /// </summary>
    /// <param name="next">The next handler in the chain (either another behavior or the actual step).</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="step">Metadata about the step being executed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the step execution.</returns>
    ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct);
}

/// <summary>
/// Non-generic behavior interface for behaviors that don't need to interact with the value.
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Wraps the execution of a step.
    /// </summary>
    /// <param name="next">The next handler in the chain.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="step">Metadata about the step being executed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the step execution as object.</returns>
    ValueTask<object?> WrapAsync(
        Func<CancellationToken, ValueTask<object?>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct);
}
