namespace PatternKit.Core;

/// <summary>
/// State of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>Normal operation - requests are allowed.</summary>
    Closed,

    /// <summary>Failure threshold exceeded - requests are blocked.</summary>
    Open,

    /// <summary>Testing if service has recovered - limited requests allowed.</summary>
    HalfOpen
}

/// <summary>
/// A behavior that implements the circuit breaker pattern.
/// </summary>
/// <typeparam name="T">The type flowing through the workflow.</typeparam>
public sealed class CircuitBreakerBehavior<T> : IBehavior<T>
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly object _lock = new();

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTime _openedAt;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt > _openDuration)
                {
                    _state = CircuitState.HalfOpen;
                }
                return _state;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CircuitBreakerBehavior{T}"/>.
    /// </summary>
    /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
    /// <param name="openDuration">Duration the circuit stays open before transitioning to half-open.</param>
    public CircuitBreakerBehavior(int failureThreshold = 5, TimeSpan? openDuration = null)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Creates a circuit breaker with the specified configuration.
    /// </summary>
    public static CircuitBreakerBehavior<T> Create(int failureThreshold = 5, TimeSpan? openDuration = null)
        => new(failureThreshold, openDuration);

    /// <inheritdoc />
    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct)
    {
        var currentState = State;

        if (currentState == CircuitState.Open)
        {
            context.SetMetadata($"circuit:{step.Title}:state", "open");
            throw new CircuitBreakerOpenException(
                $"Circuit breaker is open for step: {step.Title}");
        }

        try
        {
            var result = await next(ct).ConfigureAwait(false);

            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }

            context.SetMetadata($"circuit:{step.Title}:state", "closed");
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            lock (_lock)
            {
                _failureCount++;
                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _openedAt = DateTime.UtcNow;
                    context.SetMetadata($"circuit:{step.Title}:state", "open");
                    context.SetMetadata($"circuit:{step.Title}:failures", _failureCount);
                }
            }

            throw;
        }
    }
}

/// <summary>
/// Exception thrown when a circuit breaker is open and blocking requests.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="CircuitBreakerOpenException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}
