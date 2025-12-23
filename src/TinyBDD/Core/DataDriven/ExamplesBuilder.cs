namespace TinyBDD;

/// <summary>
/// Fluent builder for data-driven scenarios that executes a scenario template
/// for each example row.
/// </summary>
/// <typeparam name="TExample">The type of example data.</typeparam>
public sealed class ExamplesBuilder<TExample>
{
    private readonly ScenarioContext _baseContext;
    private readonly string _scenarioTitle;
    private readonly IReadOnlyList<ExampleRow<TExample>> _examples;

    internal ExamplesBuilder(
        ScenarioContext baseContext,
        string scenarioTitle,
        IReadOnlyList<ExampleRow<TExample>> examples)
    {
        _baseContext = baseContext;
        _scenarioTitle = scenarioTitle;
        _examples = examples;
    }

    /// <summary>
    /// Executes the scenario for each example row using a builder function
    /// that creates a <see cref="ScenarioChain{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type carried through the scenario chain.</typeparam>
    /// <param name="scenarioBuilder">
    /// A function that receives the example row and returns a scenario chain.
    /// The chain should end with a Then step but not call AssertPassed.
    /// </param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>An <see cref="ExamplesResult"/> containing results for all examples.</returns>
    public async Task<ExamplesResult> ForEachAsync<T>(
        Func<ExampleRow<TExample>, ScenarioChain<T>> scenarioBuilder,
        CancellationToken ct = default)
    {
        var results = new List<ExampleResult>();

        foreach (var row in _examples)
        {
            var rowContext = Bdd.ReconfigureContext(_baseContext, proto =>
            {
                proto.ScenarioName = row.Label ?? $"{_scenarioTitle} (Example {row.Index + 1})";
            });

            try
            {
                var chain = scenarioBuilder(row);
                await chain.Then(_ => true).AssertPassed(ct);
                results.Add(new ExampleResult(row.Index, row.Data, rowContext, true));
            }
            catch (Exception ex)
            {
                results.Add(new ExampleResult(row.Index, row.Data, rowContext, false, ex));
            }
        }

        return new ExamplesResult(results);
    }

    /// <summary>
    /// Executes the scenario for each example row using a builder function
    /// that creates a <see cref="ThenChain{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type carried through the scenario chain.</typeparam>
    /// <param name="scenarioBuilder">
    /// A function that receives the example row and returns a terminal then chain.
    /// </param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>An <see cref="ExamplesResult"/> containing results for all examples.</returns>
    public async Task<ExamplesResult> ForEachAsync<T>(
        Func<ExampleRow<TExample>, ThenChain<T>> scenarioBuilder,
        CancellationToken ct = default)
    {
        var results = new List<ExampleResult>();

        foreach (var row in _examples)
        {
            var rowContext = Bdd.ReconfigureContext(_baseContext, proto =>
            {
                proto.ScenarioName = row.Label ?? $"{_scenarioTitle} (Example {row.Index + 1})";
            });

            try
            {
                var chain = scenarioBuilder(row);
                await chain.AssertPassed(ct);
                results.Add(new ExampleResult(row.Index, row.Data, rowContext, true));
            }
            catch (Exception ex)
            {
                results.Add(new ExampleResult(row.Index, row.Data, rowContext, false, ex));
            }
        }

        return new ExamplesResult(results);
    }

    /// <summary>
    /// Executes the scenario for each example row and asserts all passed.
    /// </summary>
    /// <typeparam name="T">The type carried through the scenario chain.</typeparam>
    /// <param name="scenarioBuilder">
    /// A function that receives the example row and returns a terminal then chain.
    /// </param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <exception cref="AggregateException">Thrown when one or more examples failed.</exception>
    public async Task AssertAllPassedAsync<T>(
        Func<ExampleRow<TExample>, ThenChain<T>> scenarioBuilder,
        CancellationToken ct = default)
    {
        var result = await ForEachAsync(scenarioBuilder, ct);
        result.AssertAllPassed();
    }
}
