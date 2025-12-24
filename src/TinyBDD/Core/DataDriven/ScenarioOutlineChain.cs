namespace TinyBDD;

/// <summary>
/// Represents a Gherkin-style scenario outline that can be executed with multiple example rows.
/// </summary>
/// <typeparam name="TExample">The type of example data.</typeparam>
/// <remarks>
/// <para>
/// A scenario outline is a template for scenarios that allows you to run the same steps
/// with different data values. Each step receives both the current chain value and the
/// example data, enabling data-driven testing with a fluent API.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await Bdd.ScenarioOutline&lt;(int a, int b, int expected)&gt;(ctx, "Addition")
///     .Given("first number", ex => ex.a)
///     .And("second number", (_, ex) => ex.b)
///     .When("added", (a, b) => a + b)
///     .Then("result matches expected", (sum, ex) => sum == ex.expected)
///     .Examples(
///         (a: 1, b: 2, expected: 3),
///         (a: 5, b: 5, expected: 10))
///     .AssertAllPassedAsync();
/// </code>
/// </example>
public sealed class ScenarioOutlineBuilder<TExample>
{
    private readonly ScenarioContext _baseContext;
    private readonly string _title;
    private readonly List<Func<TExample, ScenarioContext, ThenChain<object>>> _scenarioBuilders = new();

    internal ScenarioOutlineBuilder(ScenarioContext baseContext, string title)
    {
        _baseContext = baseContext;
        _title = title;
    }

    /// <summary>
    /// Starts the scenario outline with a Given step that uses example data.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup.</typeparam>
    /// <param name="title">The step title.</param>
    /// <param name="setup">A function that receives the example and returns the initial value.</param>
    /// <returns>A chain builder for continuing the scenario outline.</returns>
    public ScenarioOutlineChain<TExample, T> Given<T>(string title, Func<TExample, T> setup)
    {
        return new ScenarioOutlineChain<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => Bdd.Given(ctx, title, () => setup(ex)));
    }

    /// <summary>
    /// Starts the scenario outline with a Given step that uses example data asynchronously.
    /// </summary>
    /// <typeparam name="T">The type produced by the setup.</typeparam>
    /// <param name="title">The step title.</param>
    /// <param name="setup">An async function that receives the example and returns the initial value.</param>
    /// <returns>A chain builder for continuing the scenario outline.</returns>
    public ScenarioOutlineChain<TExample, T> Given<T>(string title, Func<TExample, Task<T>> setup)
    {
        return new ScenarioOutlineChain<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => Bdd.Given(ctx, title, () => setup(ex)));
    }
}

/// <summary>
/// Represents a scenario outline chain with a current value type.
/// </summary>
/// <typeparam name="TExample">The type of example data.</typeparam>
/// <typeparam name="T">The current value type in the chain.</typeparam>
public sealed class ScenarioOutlineChain<TExample, T>
{
    private readonly ScenarioContext _baseContext;
    private readonly string _title;
    private readonly Func<TExample, ScenarioContext, ScenarioChain<T>> _chainBuilder;

    internal ScenarioOutlineChain(
        ScenarioContext baseContext,
        string title,
        Func<TExample, ScenarioContext, ScenarioChain<T>> chainBuilder)
    {
        _baseContext = baseContext;
        _title = title;
        _chainBuilder = chainBuilder;
    }

    /// <summary>
    /// Adds a When step that transforms the value using example data.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="title">The step title.</param>
    /// <param name="transform">A function that receives the current value and example data.</param>
    /// <returns>A new chain with the transformed type.</returns>
    public ScenarioOutlineChain<TExample, TOut> When<TOut>(string title, Func<T, TExample, TOut> transform)
    {
        return new ScenarioOutlineChain<TExample, TOut>(
            _baseContext,
            _title,
            (ex, ctx) => _chainBuilder(ex, ctx).When(title, v => transform(v, ex)));
    }

    /// <summary>
    /// Adds a When step that transforms the value without using example data.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="title">The step title.</param>
    /// <param name="transform">A function that receives the current value.</param>
    /// <returns>A new chain with the transformed type.</returns>
    public ScenarioOutlineChain<TExample, TOut> When<TOut>(string title, Func<T, TOut> transform)
    {
        return new ScenarioOutlineChain<TExample, TOut>(
            _baseContext,
            _title,
            (ex, ctx) => _chainBuilder(ex, ctx).When(title, transform));
    }

    /// <summary>
    /// Adds an And step that transforms the value using example data.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="title">The step title.</param>
    /// <param name="transform">A function that receives the current value and example data.</param>
    /// <returns>A new chain with the transformed type.</returns>
    public ScenarioOutlineChain<TExample, TOut> And<TOut>(string title, Func<T, TExample, TOut> transform)
    {
        return new ScenarioOutlineChain<TExample, TOut>(
            _baseContext,
            _title,
            (ex, ctx) => _chainBuilder(ex, ctx).And(title, v => transform(v, ex)));
    }

    /// <summary>
    /// Adds an And step that transforms the value without using example data.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="title">The step title.</param>
    /// <param name="transform">A function that receives the current value.</param>
    /// <returns>A new chain with the transformed type.</returns>
    public ScenarioOutlineChain<TExample, TOut> And<TOut>(string title, Func<T, TOut> transform)
    {
        return new ScenarioOutlineChain<TExample, TOut>(
            _baseContext,
            _title,
            (ex, ctx) => _chainBuilder(ex, ctx).And(title, transform));
    }

    /// <summary>
    /// Adds a Then assertion that uses example data.
    /// </summary>
    /// <param name="title">The step title.</param>
    /// <param name="predicate">A predicate that receives the current value and example data.</param>
    /// <returns>A terminal chain that can be executed with examples.</returns>
    public ScenarioOutlineTerminal<TExample, T> Then(string title, Func<T, TExample, bool> predicate)
    {
        return new ScenarioOutlineTerminal<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => _chainBuilder(ex, ctx).Then(title, v => predicate(v, ex)));
    }

    /// <summary>
    /// Adds a Then assertion without using example data.
    /// </summary>
    /// <param name="title">The step title.</param>
    /// <param name="predicate">A predicate that receives the current value.</param>
    /// <returns>A terminal chain that can be executed with examples.</returns>
    public ScenarioOutlineTerminal<TExample, T> Then(string title, Func<T, bool> predicate)
    {
        return new ScenarioOutlineTerminal<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => _chainBuilder(ex, ctx).Then(title, predicate));
    }
}

/// <summary>
/// Represents a terminal scenario outline that can be executed with examples.
/// </summary>
/// <typeparam name="TExample">The type of example data.</typeparam>
/// <typeparam name="T">The value type in the chain.</typeparam>
public sealed class ScenarioOutlineTerminal<TExample, T>
{
    private readonly ScenarioContext _baseContext;
    private readonly string _title;
    private readonly Func<TExample, ScenarioContext, ThenChain<T>> _chainBuilder;
    private List<ExampleRow<TExample>>? _examples;

    internal ScenarioOutlineTerminal(
        ScenarioContext baseContext,
        string title,
        Func<TExample, ScenarioContext, ThenChain<T>> chainBuilder)
    {
        _baseContext = baseContext;
        _title = title;
        _chainBuilder = chainBuilder;
    }

    /// <summary>
    /// Adds an And assertion that uses example data.
    /// </summary>
    /// <param name="title">The step title.</param>
    /// <param name="predicate">A predicate that receives the current value and example data.</param>
    /// <returns>This terminal chain for further chaining.</returns>
    public ScenarioOutlineTerminal<TExample, T> And(string title, Func<T, TExample, bool> predicate)
    {
        var previousBuilder = _chainBuilder;
        return new ScenarioOutlineTerminal<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => previousBuilder(ex, ctx).And(title, v => predicate(v, ex)));
    }

    /// <summary>
    /// Adds an And assertion without using example data.
    /// </summary>
    /// <param name="title">The step title.</param>
    /// <param name="predicate">A predicate that receives the current value.</param>
    /// <returns>This terminal chain for further chaining.</returns>
    public ScenarioOutlineTerminal<TExample, T> And(string title, Func<T, bool> predicate)
    {
        var previousBuilder = _chainBuilder;
        return new ScenarioOutlineTerminal<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => previousBuilder(ex, ctx).And(title, predicate));
    }

    /// <summary>
    /// Adds a But assertion that uses example data.
    /// </summary>
    /// <param name="title">The step title.</param>
    /// <param name="predicate">A predicate that receives the current value and example data.</param>
    /// <returns>This terminal chain for further chaining.</returns>
    public ScenarioOutlineTerminal<TExample, T> But(string title, Func<T, TExample, bool> predicate)
    {
        var previousBuilder = _chainBuilder;
        return new ScenarioOutlineTerminal<TExample, T>(
            _baseContext,
            _title,
            (ex, ctx) => previousBuilder(ex, ctx).But(title, v => predicate(v, ex)));
    }

    /// <summary>
    /// Specifies the example data rows to execute the scenario with.
    /// </summary>
    /// <param name="examples">The example data values.</param>
    /// <returns>This terminal chain for execution.</returns>
    public ScenarioOutlineTerminal<TExample, T> Examples(params TExample[] examples)
    {
        _examples = examples.Select((e, i) => new ExampleRow<TExample>(i, e)).ToList();
        return this;
    }

    /// <summary>
    /// Executes the scenario outline for all examples and returns the results.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>The aggregated results from all example executions.</returns>
    public async Task<ExamplesResult> RunAsync(CancellationToken ct = default)
    {
        if (_examples is null || _examples.Count == 0)
            throw new InvalidOperationException("No examples provided. Call Examples() before RunAsync().");

        var results = new List<ExampleResult>();

        foreach (var row in _examples)
        {
            var rowContext = Bdd.ReconfigureContext(_baseContext, proto =>
            {
                proto.ScenarioName = row.Label ?? $"{_title} (Example {row.Index + 1})";
            });

            try
            {
                var chain = _chainBuilder(row.Data, rowContext);
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
    /// Executes the scenario outline for all examples and asserts all passed.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <exception cref="AggregateException">Thrown when one or more examples failed.</exception>
    public async Task AssertAllPassedAsync(CancellationToken ct = default)
    {
        var result = await RunAsync(ct);
        result.AssertAllPassed();
    }
}
