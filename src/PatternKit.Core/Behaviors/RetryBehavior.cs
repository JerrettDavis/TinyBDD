namespace PatternKit.Core;

/// <summary>
/// A behavior that retries failed steps with exponential backoff.
/// </summary>
/// <typeparam name="T">The type flowing through the workflow.</typeparam>
public sealed class RetryBehavior<T> : IBehavior<T>
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly Func<Exception, bool>? _shouldRetry;

    /// <summary>
    /// Initializes a new instance of <see cref="RetryBehavior{T}"/>.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="baseDelay">Base delay between retries (exponentially increased).</param>
    /// <param name="shouldRetry">Optional predicate to determine if an exception should trigger a retry.</param>
    public RetryBehavior(int maxRetries = 3, TimeSpan? baseDelay = null, Func<Exception, bool>? shouldRetry = null)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(100);
        _shouldRetry = shouldRetry;
    }

    /// <summary>
    /// Creates a retry behavior with the specified configuration.
    /// </summary>
    public static RetryBehavior<T> Create(int maxRetries = 3, TimeSpan? baseDelay = null)
        => new(maxRetries, baseDelay);

    /// <inheritdoc />
    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= _maxRetries)
        {
            try
            {
                return await next(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry on cancellation
            }
            catch (Exception ex) when (attempts < _maxRetries && ShouldRetry(ex))
            {
                lastException = ex;
                attempts++;

                var delay = _baseDelay * Math.Pow(2, attempts - 1);
                context.SetMetadata($"retry:{step.Title}:attempt", attempts);
                context.SetMetadata($"retry:{step.Title}:delay", delay);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry logic error");
    }

    private bool ShouldRetry(Exception ex)
        => _shouldRetry?.Invoke(ex) ?? true;
}
