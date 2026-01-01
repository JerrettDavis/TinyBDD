namespace PatternKit.Core;

/// <summary>
/// Extension methods for working with workflows.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Executes a step handler within a workflow chain.
    /// </summary>
    /// <typeparam name="T">The current chain value type.</typeparam>
    /// <typeparam name="TRequest">The step request type.</typeparam>
    /// <typeparam name="TResponse">The step response type.</typeparam>
    /// <param name="chain">The workflow chain.</param>
    /// <param name="title">The step title.</param>
    /// <param name="factory">The handler factory.</param>
    /// <param name="createRequest">Function to create the request from the current value.</param>
    /// <returns>A new chain carrying the response.</returns>
    public static WorkflowChain<TResponse> Handle<T, TRequest, TResponse>(
        this WorkflowChain<T> chain,
        string title,
        IStepHandlerFactory factory,
        Func<T, TRequest> createRequest)
        where TRequest : IStep<TResponse>
    {
        return chain.When(title, async (value, ct) =>
        {
            var handler = factory.Create<TRequest, TResponse>()
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

            var request = createRequest(value);
            return await handler.HandleAsync(request, ct).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Adds all steps from one workflow context to another.
    /// </summary>
    /// <param name="target">The target context to add steps to.</param>
    /// <param name="source">The source context to copy steps from.</param>
    public static void MergeSteps(this WorkflowContext target, WorkflowContext source)
    {
        foreach (var step in source.Steps)
        {
            target.AddStep(step);
        }

        foreach (var io in source.IO)
        {
            target.AddIO(io);
        }
    }

    /// <summary>
    /// Gets the total elapsed time for all steps.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <returns>The sum of all step durations.</returns>
    public static TimeSpan TotalElapsed(this WorkflowContext context)
        => TimeSpan.FromTicks(context.Steps.Sum(s => s.Elapsed.Ticks));

    /// <summary>
    /// Gets all failed steps.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <returns>Enumerable of failed step results.</returns>
    public static IEnumerable<StepResult> GetFailedSteps(this WorkflowContext context)
        => context.Steps.Where(s => !s.Passed);

    /// <summary>
    /// Asserts that all steps in the context passed.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <exception cref="WorkflowAssertionException">Thrown if any step failed.</exception>
    public static void AssertPassed(this WorkflowContext context)
    {
        if (!context.AllPassed)
        {
            var first = context.FirstFailure;
            throw new WorkflowAssertionException(
                $"Workflow failed at step: {first?.Kind} {first?.Title}");
        }
    }
}
