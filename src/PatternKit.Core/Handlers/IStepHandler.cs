namespace PatternKit.Core;

/// <summary>
/// Handles a specific step type in a workflow.
/// </summary>
/// <remarks>
/// <para>
/// Step handlers implement the mediator pattern, allowing decoupling of step
/// definitions from their implementations. This enables:
/// </para>
/// <list type="bullet">
///   <item><description>Dependency injection of step implementations</description></item>
///   <item><description>Easier testing via mock handlers</description></item>
///   <item><description>Separation of workflow orchestration from business logic</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TRequest">The step request type.</typeparam>
/// <typeparam name="TResponse">The step response type.</typeparam>
/// <example>
/// <code>
/// public record AddNumbers(int A, int B) : IStep&lt;int&gt;;
///
/// public class AddNumbersHandler : IStepHandler&lt;AddNumbers, int&gt;
/// {
///     public ValueTask&lt;int&gt; HandleAsync(AddNumbers request, CancellationToken ct)
///         => new(request.A + request.B);
/// }
/// </code>
/// </example>
public interface IStepHandler<in TRequest, TResponse>
    where TRequest : IStep<TResponse>
{
    /// <summary>
    /// Handles the step request and returns the response.
    /// </summary>
    /// <param name="request">The step request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The step response.</returns>
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

/// <summary>
/// Marker interface for step requests.
/// </summary>
/// <typeparam name="TResponse">The expected response type.</typeparam>
public interface IStep<out TResponse>
{
}

/// <summary>
/// Marker interface for step requests that don't return a value.
/// </summary>
public interface IStep : IStep<Unit>
{
}

/// <summary>
/// Represents a void/unit return type for steps that don't produce a value.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// Gets the singleton Unit value.
    /// </summary>
    public static Unit Value { get; } = default;
}
