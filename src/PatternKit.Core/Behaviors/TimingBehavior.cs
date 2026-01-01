using System.Diagnostics;

namespace PatternKit.Core;

/// <summary>
/// A behavior that records step execution timing in the workflow context metadata.
/// </summary>
/// <remarks>
/// Timing is stored in the context metadata with key format: <c>timing:{step.Title}</c>.
/// </remarks>
/// <typeparam name="T">The type flowing through the workflow.</typeparam>
public sealed class TimingBehavior<T> : IBehavior<T>
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static TimingBehavior<T> Instance { get; } = new();

    private TimingBehavior() { }

    /// <inheritdoc />
    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(ct).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            context.SetMetadata($"timing:{step.Title}", sw.Elapsed);
        }
    }
}
