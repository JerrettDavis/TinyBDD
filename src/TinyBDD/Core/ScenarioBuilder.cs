using System.Runtime.CompilerServices;

namespace TinyBDD;

/// <summary>
/// Represents a chain of <c>Then</c> assertions that can be further extended with
/// <c>And</c>/<c>But</c> and awaited to execute the recorded scenario.
/// </summary>
/// <typeparam name="T">The value type produced by the preceding step and carried through assertions.</typeparam>
/// <remarks>
/// <para>
/// TinyBDD defers execution: steps are queued until you await the chain. Calling any of the
/// <c>And</c>/<c>But</c> methods only records additional steps; the actual execution happens when the chain is awaited.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "start", () => 2)
///           .When("double", x => x * 2)
///           .Then(">= 4", v => v >= 4)  // returns ThenChain&lt;int&gt;
///           .And("!= 5", v => v != 5);  // add another assertion, then await
/// </code>
/// </example>
/// <seealso cref="ScenarioContext"/>
/// <seealso cref="ScenarioChain{T}"/>
public readonly struct ThenChain<T>
{
    private readonly Pipeline _p;
    internal ThenChain(Pipeline p) => _p = p;

    /// <summary>
    /// Gets an awaiter that executes the queued scenario steps when awaited.
    /// </summary>
    /// <returns>The awaiter for the underlying asynchronous execution.</returns>
    public ValueTaskAwaiter GetAwaiter() => _p.RunAsync(CancellationToken.None).GetAwaiter();

    /// <summary>Adds an <c>And</c> assertion with a synchronous action and explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Action<T> assertion) =>
        Add(StepWord.And, title, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion with an asynchronous action and explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<Task> assertion) =>
        Add(StepWord.And, title, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion receiving the carried value, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, Task> assertion) =>
        Add(StepWord.And, title, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion receiving the value and a cancellation token, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value and a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, CancellationToken, Task> assertion) =>
        Add(StepWord.And, title, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/>, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, ValueTask> assertion) =>
        Add(StepWord.And, title, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/> and accepting a token, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/> and observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, CancellationToken, ValueTask> assertion) =>
        Add(StepWord.And, title, Wrap(assertion));

    /// <summary>Adds an <c>And</c> step using a synchronous boolean predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when <paramref name="predicate"/> evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, bool> predicate) =>
        Add(StepWord.And, title, Wrap(title, predicate));

    /// <summary>Adds an <c>And</c> step using an asynchronous boolean predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, Task<bool>> predicate) =>
        Add(StepWord.And, title, Wrap(title, predicate));

    /// <summary>Adds an <c>And</c> step using an asynchronous boolean predicate with token. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Token-aware asynchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, CancellationToken, Task<bool>> predicate) =>
        Add(StepWord.And, title, Wrap(title, predicate));

    /// <summary>Adds an <c>And</c> step using a <see cref="ValueTask{Boolean}"/> predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, ValueTask<bool>> predicate) =>
        Add(StepWord.And, title, Wrap(title, predicate));

    /// <summary>Adds an <c>And</c> step using a token-aware <see cref="ValueTask{Boolean}"/> predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Predicate evaluated against the carried value and <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, CancellationToken, ValueTask<bool>> predicate) =>
        Add(StepWord.And, title, Wrap(title, predicate));

    /// <summary>Adds an <c>And</c> assertion with a synchronous action and a default title.</summary>
    /// <param name="assertion">Assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Action<T> assertion) =>
        Add(StepWord.And, string.Empty, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion with an asynchronous action and a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<Task> assertion) =>
        Add(StepWord.And, string.Empty, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion receiving the carried value, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, Task> assertion) =>
        Add(StepWord.And, string.Empty, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion receiving a token, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, CancellationToken, Task> assertion) =>
        Add(StepWord.And, string.Empty, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/>, with a default title.</summary>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, ValueTask> assertion) =>
        Add(StepWord.And, string.Empty, Wrap(assertion));

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/> and accepting a token, with a default title.</summary>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/> and observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, CancellationToken, ValueTask> assertion) =>
        Add(StepWord.And, string.Empty, Wrap(assertion));

    /// <summary>Adds an <c>And</c> step using a synchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, bool> predicate) =>
        Add(StepWord.And, string.Empty, Wrap(nameof(And), predicate));

    /// <summary>Adds an <c>And</c> step using an asynchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, Task<bool>> predicate) =>
        Add(StepWord.And, string.Empty, Wrap(nameof(And), predicate));

    /// <summary>Adds an <c>And</c> step using a token-aware asynchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value and <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, CancellationToken, Task<bool>> predicate) =>
        Add(StepWord.And, string.Empty, Wrap(nameof(And), predicate));

    /// <summary>Adds an <c>And</c> step using a <see cref="ValueTask{Boolean}"/> predicate with a default title.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, ValueTask<bool>> predicate) =>
        Add(StepWord.And, string.Empty, Wrap(nameof(And), predicate));

    /// <summary>Adds an <c>And</c> step using a token-aware <see cref="ValueTask{Boolean}"/> predicate with a default title.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value and <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, CancellationToken, ValueTask<bool>> predicate) =>
        Add(StepWord.And, string.Empty, Wrap(nameof(And), predicate));

    /// <summary>Adds a <c>But</c> step using a synchronous boolean predicate with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> But(string title, Func<T, bool> predicate) =>
        Add(StepWord.But, title, Wrap(title, predicate));

    /// <summary>Adds a <c>But</c> step using a synchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> But(Func<T, bool> predicate) =>
        Add(StepWord.But, string.Empty, Wrap(nameof(But), predicate));

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(string title, Func<Task> assertion) =>
        Add(StepWord.But, title, Wrap(assertion));

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value, with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(
        string title,
        Func<T, Task> assertion) =>
        Add(StepWord.But, title, Wrap(assertion));

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value and token, with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(
        string title,
        Func<T, CancellationToken, Task> assertion) =>
        Add(StepWord.But, title, Wrap(assertion));

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(Func<Task> assertion) =>
        Add(StepWord.But, string.Empty, Wrap(assertion));

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(Func<T, Task> assertion) =>
        Add(StepWord.But, string.Empty, Wrap(assertion));

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value and token, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(Func<T, CancellationToken, Task> assertion) =>
        Add(StepWord.But, string.Empty, Wrap(assertion));

    /// <summary>Completes the chain and asserts that all previous steps passed.</summary>
    /// <remarks>
    /// This method is functionally equivalent to calling the extension method <see cref="ScenarioContextAsserts.AssertPassed(ScenarioContext)"/>
    /// on the underlying <see cref="ScenarioContext"/>. It is provided for convenience to
    /// allow fluent chaining at the end of a scenario.
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when at least one step failed.</exception>
    public async Task AssertPassed(CancellationToken cancellationToken = default)
    {
        await _p.RunAsync(cancellationToken);
        _p.Context.AssertPassed();
    }

    /// <summary>Completes the chain and asserts that any previous steps failed.</summary>
    /// <remarks>
    /// This method is functionally equivalent to calling the extension method <see cref="ScenarioContextAsserts.AssertFailed(ScenarioContext)"/>
    /// on the underlying <see cref="ScenarioContext"/>. It is provided for convenience to
    /// allow fluent chaining at the end of a scenario.
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when no step failed.</exception>   
    public async Task AssertFailed(CancellationToken cancellationToken = default)
    {
        await _p.RunAsync(cancellationToken);
        _p.Context.AssertFailed();
    }

    #region Core step implementations

    private ThenChain<T> Add(
        StepWord word,
        string? title,
        Func<object?, CancellationToken, ValueTask<object?>> body)
    {
        _p.Enqueue(StepPhase.Then, word, title ?? "", body);
        return this;
    }

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Action<T> assertion)
        => (s, _) =>
        {
            assertion((T)s!);
            return new ValueTask<object?>(s);
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<Task> assertion)
        => async (s, _) =>
        {
            await assertion().ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, Task> assertion)
        => async (s, _) =>
        {
            await assertion((T)s!).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, CancellationToken, Task> assertion)
        => async (s, ct) =>
        {
            await assertion((T)s!, ct).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, ValueTask> assertion)
        => async (s, _) =>
        {
            await assertion((T)s!).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(Func<T, CancellationToken, ValueTask> assertion)
        => async (s, ct) =>
        {
            await assertion((T)s!, ct).ConfigureAwait(false);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, bool> predicate)
        => (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return new ValueTask<object?>(s);
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, Task<bool>> predicate)
        => async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!).ConfigureAwait(false), title);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, CancellationToken, Task<bool>> predicate)
        => async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct).ConfigureAwait(false), title);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, ValueTask<bool>> predicate)
        => async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!).ConfigureAwait(false), title);
            return s;
        };

    private static Func<object?, CancellationToken, ValueTask<object?>> Wrap(string title, Func<T, CancellationToken, ValueTask<bool>> predicate)
        => async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct).ConfigureAwait(false), title);
            return s;
        };

    #endregion
}