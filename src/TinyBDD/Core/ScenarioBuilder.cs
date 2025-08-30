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
    public ValueTaskAwaiter GetAwaiter() => _p.RunAsync(default).GetAwaiter();

    /// <summary>Adds an <c>And</c> assertion with a synchronous action and explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Action<T> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, (s, _) =>
        {
            assertion((T)s!);
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion with an asynchronous action and explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, _) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion receiving the carried value, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion receiving the value and a cancellation token, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value and a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/>, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/> and accepting a token, with explicit title.</summary>
    /// <param name="title">Display title for the assertion step.</param>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/> and observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(string title, Func<T, CancellationToken, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a synchronous boolean predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when <paramref name="predicate"/> evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using an asynchronous boolean predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), title);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using an asynchronous boolean predicate with token. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Token-aware asynchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a <see cref="ValueTask{Boolean}"/> predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), title);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a token-aware <see cref="ValueTask{Boolean}"/> predicate. Throws when predicate is false.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Predicate evaluated against the carried value and <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(string title, Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion with a synchronous action and a default title.</summary>
    /// <param name="assertion">Assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Action<T> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", (s, _) =>
        {
            assertion((T)s!);
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion with an asynchronous action and a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, _) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion receiving the carried value, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion receiving a token, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/>, with a default title.</summary>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> assertion returning <see cref="ValueTask"/> and accepting a token, with a default title.</summary>
    /// <param name="assertion">Assertion that returns a <see cref="ValueTask"/> and observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> And(Func<T, CancellationToken, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a synchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), "And");
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using an asynchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "And");
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a token-aware asynchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Asynchronous predicate evaluated against the carried value and <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "And");
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a <see cref="ValueTask{Boolean}"/> predicate with a default title.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, _) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "And");
            return s;
        });
        return this;
    }

    /// <summary>Adds an <c>And</c> step using a token-aware <see cref="ValueTask{Boolean}"/> predicate with a default title.</summary>
    /// <param name="predicate">Predicate evaluated against the carried value and <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> And(Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "And");
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using a synchronous boolean predicate with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> But(string title, Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using a synchronous boolean predicate with a default title.</summary>
    /// <param name="predicate">Synchronous predicate evaluated against the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    /// <exception cref="BddAssertException">Thrown when the predicate evaluates to <see langword="false"/>.</exception>
    public ThenChain<T> But(Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", (s, _) =>
        {
            AssertUtil.Ensure(predicate((T)s!), "But");
            return VT.From(s);
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(string title, Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, async (s, _) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value, with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(string title, Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value and token, with explicit title.</summary>
    /// <param name="title">Display title for the step.</param>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(string title, Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that may throw to indicate failure.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", async (s, _) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that receives the carried value.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", async (s, _) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    /// <summary>Adds a <c>But</c> step using an asynchronous assertion that receives the value and token, with a default title.</summary>
    /// <param name="assertion">Asynchronous assertion that observes a <see cref="CancellationToken"/>.</param>
    /// <returns>The same chain for further composition.</returns>
    public ThenChain<T> But(Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }
}