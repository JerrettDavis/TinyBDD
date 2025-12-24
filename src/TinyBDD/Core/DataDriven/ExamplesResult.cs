namespace TinyBDD;

/// <summary>
/// Represents the result of a single example execution in a data-driven scenario.
/// </summary>
public sealed class ExampleResult
{
    /// <summary>
    /// Gets the zero-based index of this example in the examples collection.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the example data that was used for this execution.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Gets the scenario context for this example's execution.
    /// </summary>
    public ScenarioContext Context { get; }

    /// <summary>
    /// Gets a value indicating whether this example passed all assertions.
    /// </summary>
    public bool Passed { get; }

    /// <summary>
    /// Gets the exception that caused this example to fail, if any.
    /// </summary>
    public Exception? Exception { get; }

    internal ExampleResult(int index, object? data, ScenarioContext context, bool passed, Exception? exception = null)
    {
        Index = index;
        Data = data;
        Context = context;
        Passed = passed;
        Exception = exception;
    }
}

/// <summary>
/// Aggregated result from data-driven scenario execution.
/// </summary>
public sealed class ExamplesResult
{
    /// <summary>
    /// Gets the list of individual example results.
    /// </summary>
    public IReadOnlyList<ExampleResult> Results { get; }

    /// <summary>
    /// Gets the number of examples that passed.
    /// </summary>
    public int PassedCount => Results.Count(r => r.Passed);

    /// <summary>
    /// Gets the number of examples that failed.
    /// </summary>
    public int FailedCount => Results.Count(r => !r.Passed);

    /// <summary>
    /// Gets a value indicating whether all examples passed.
    /// </summary>
    public bool AllPassed => FailedCount == 0;

    /// <summary>
    /// Gets the total number of examples executed.
    /// </summary>
    public int TotalCount => Results.Count;

    internal ExamplesResult(IEnumerable<ExampleResult> results)
    {
        Results = results.ToList();
    }

    /// <summary>
    /// Throws an exception if any example failed.
    /// </summary>
    /// <exception cref="AggregateException">Thrown when one or more examples failed.</exception>
    public void AssertAllPassed()
    {
        if (AllPassed) return;

        var failures = Results
            .Where(r => !r.Passed)
            .Select(r => new InvalidOperationException(
                $"Example {r.Index + 1} failed: {r.Data}",
                r.Exception))
            .ToList();

        throw new AggregateException(
            $"{FailedCount} of {TotalCount} examples failed.",
            failures);
    }
}
