namespace TinyBDD;

public static class BddFluentTaskExtensions
{
    /// <summary>
    /// Adds a <c>When</c> step to the current <c>Given</c> chain using a synchronous transformation
    /// that receives the given value and a cancellation token.
    /// </summary>
    /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
    /// <typeparam name="TOut">The type produced by this <c>When</c> step.</typeparam>
    /// <param name="given">The <see cref="GivenBuilder{TGiven}"/> to extend.</param>
    /// <param name="title">The display title for this <c>When</c> step in the scenario output.</param>
    /// <param name="transform">
    /// A synchronous transformation that receives the <typeparamref name="TGiven"/> value
    /// and a <see cref="CancellationToken"/>, and returns a <typeparamref name="TOut"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that resolves to a <see cref="WhenBuilder{TGiven, TOut}"/>
    /// allowing further <c>Then</c> chaining.
    /// </returns>
    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, CancellationToken, TOut> transform)
        => given.When<TGiven, TOut>(title, (g, ct) => Task.FromResult(transform(g, ct)));

    // Given -> When<TOut>, sync no token
    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, TOut> transform)
        => given.When<TGiven, TOut>(title, (g, _) => Task.FromResult(transform(g)));

    // Title-less conveniences
    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        Func<TGiven, CancellationToken, TOut> transform)
        => given.When("When action", transform);

    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        Func<TGiven, TOut> transform)
        => given.When("When action", transform);

    // ---------------------------
    // When(...) after Given(...)
    // ---------------------------

    public static async Task<WhenBuilder<TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, CancellationToken, Task> actionAsync)
    {
        // Execute Given now (still lazy until outer await reaches here)
        var value = await Bdd.RunStepAsync(given._ctx, "Given", given._title,
            () => given._fn(CancellationToken.None)).ConfigureAwait(false);
        return new WhenBuilder<TGiven>(given._ctx, value, title, actionAsync);
    }

    public static Task<WhenBuilder<TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, Task> actionAsync)
        => given.When(title, (g, _) => actionAsync(g));

    public static Task<WhenBuilder<TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        Func<TGiven, CancellationToken, Task> actionAsync)
        => given.When("When action", actionAsync);

    public static Task<WhenBuilder<TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        Func<TGiven, Task> actionAsync)
        => given.When("When action", actionAsync);

    // TOut
    public static async Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, CancellationToken, Task<TOut>> actionAsync)
    {
        var value = await Bdd.RunStepAsync(given._ctx, "Given", given._title,
            () => given._fn(CancellationToken.None)).ConfigureAwait(false);
        return new WhenBuilder<TGiven, TOut>(given._ctx, value, title, actionAsync);
    }

    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, Task<TOut>> actionAsync)
        => given.When<TGiven, TOut>(title, (g, _) => actionAsync(g));

    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        Func<TGiven, CancellationToken, Task<TOut>> actionAsync)
        => given.When("When action", actionAsync);

    public static Task<WhenBuilder<TGiven, TOut>> When<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        Func<TGiven, Task<TOut>> actionAsync)
        => given.When("When action", actionAsync);

    // Given -> When (side-effect only), sync with token
    public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        string title,
        Action<TGiven, CancellationToken> action)
        => given.When(title, (g, ct) =>
        {
            action(g, ct);
            return Task.FromResult(g);
        });

    // Given -> When (side-effect only), sync no token
    public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        string title,
        Action<TGiven> action)
        => given.When(title, (g, _) =>
        {
            action(g);
            return Task.FromResult(g);
        });

    // Title-less conveniences
    public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        Action<TGiven, CancellationToken> action)
        => given.When("When action", action);

    public static Task<WhenBuilder<TGiven, TGiven>> When<TGiven>(
        this GivenBuilder<TGiven> given,
        Action<TGiven> action)
        => given.When("When action", action);

    // ---------------------------
    // Then(...) after When(...) (untyped)
    // ---------------------------

    public static async Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        string title,
        Func<CancellationToken, Task> assertion)
    {
        var when = await whenTask.ConfigureAwait(false);

        // Execute When
        await Bdd.RunStepAsync(when._ctx, "When", when._title,
            () => when._fn(when._given, CancellationToken.None)).ConfigureAwait(false);

        // Execute Then
        await Bdd.RunStepAsync(when._ctx, "Then", title,
            () => assertion(CancellationToken.None)).ConfigureAwait(false);

        return new ThenBuilder(when._ctx);
    }

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        string title,
        Func<Task> assertion)
        => whenTask.Then(title, _ => assertion());

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        Func<CancellationToken, Task> assertion)
        => whenTask.Then("Then assertion", assertion);

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        Func<Task> assertion)
        => whenTask.Then("Then assertion", assertion);

    // ---------------------------
    // Then(...) after When(..., TOut) (typed)
    // ---------------------------

    public static async Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        string title,
        Func<TOut, CancellationToken, Task> assertion)
    {
        var when = await whenTask.ConfigureAwait(false);

        var result = await Bdd.RunStepAsync(when._ctx, "When", when._title,
            () => when._fn(when._given, CancellationToken.None)).ConfigureAwait(false);

        await Bdd.RunStepAsync(when._ctx, "Then", title,
            () => assertion(result, CancellationToken.None)).ConfigureAwait(false);

        return new ThenBuilder<TOut>(when._ctx, result);
    }

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        string title,
        Func<TOut, Task> assertion)
        => whenTask.Then(title, (v, _) => assertion(v));

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        Func<TOut, CancellationToken, Task> assertion)
        => whenTask.Then("Then assertion", assertion);

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        Func<TOut, Task> assertion)
        => whenTask.Then("Then assertion", assertion);

    // ---------------------------
    // And / But after Then (untyped)
    // ---------------------------

    public static async Task<ThenBuilder> And(
        this Task<ThenBuilder> thenTask,
        string title,
        Func<CancellationToken, Task> assertion,
        string stepName = nameof(And))
    {
        var then = await thenTask.ConfigureAwait(false);
        await Bdd.RunStepAsync(then.Ctx, stepName, title,
            () => assertion(CancellationToken.None)).ConfigureAwait(false);
        return then;
    }

    public static Task<ThenBuilder> And(
        this Task<ThenBuilder> thenTask,
        string title,
        Func<Task> assertion,
        string stepName = nameof(And))
        => thenTask.And(title, _ => assertion(), stepName);

    public static Task<ThenBuilder> And(
        this Task<ThenBuilder> thenTask,
        Func<CancellationToken, Task> assertion)
        => thenTask.And("And assertion", assertion);

    public static Task<ThenBuilder> And(
        this Task<ThenBuilder> thenTask,
        Func<Task> assertion)
        => thenTask.And("And assertion", assertion);

    public static Task<ThenBuilder> But(
        this Task<ThenBuilder> thenTask,
        string title,
        Func<CancellationToken, Task> assertion)
        => thenTask.And(title, assertion, nameof(But));

    public static Task<ThenBuilder> But(
        this Task<ThenBuilder> thenTask,
        string title,
        Func<Task> assertion)
        => thenTask.And(title, assertion);

    // ---------------------------
    // And / But after Then<T> (typed)
    // ---------------------------

    public static async Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, CancellationToken, Task> assertion,
        string stepName = nameof(And))
    {
        var then = await thenTask.ConfigureAwait(false);
        await Bdd.RunStepAsync(then.Ctx, stepName, title,
            () => assertion(then.Value, CancellationToken.None)).ConfigureAwait(false);
        return then;
    }

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, Task> assertion)
        => thenTask.And(title, (v, _) => assertion(v));

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        Func<T, CancellationToken, Task> assertion)
        => thenTask.And("And assertion", assertion);

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        Func<T, Task> assertion)
        => thenTask.And("And assertion", assertion);

    public static Task<ThenBuilder<T>> But<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, CancellationToken, Task> assertion)
        => thenTask.And(title, assertion);

    public static Task<ThenBuilder<T>> But<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, Task> assertion)
        => thenTask.And(title, assertion);

    // -------------------------------------------------------
    // THEN (untyped When)  — predicate overloads
    // -------------------------------------------------------

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        string title,
        Func<CancellationToken, Task<bool>> predicate)
        => whenTask.Then(title, async ct =>
        {
            if (!await predicate(ct).ConfigureAwait(false))
                throw new BddAssertException($"Assertion failed: {title}");
        });

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        string title,
        Func<Task<bool>> predicate)
        => whenTask.Then(title, _ => predicate());

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        Func<CancellationToken, Task<bool>> predicate)
        => whenTask.Then("Then assertion", predicate);

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        Func<Task<bool>> predicate)
        => whenTask.Then("Then assertion", predicate);

    // -------------------------------------------------------
    // THEN (typed When<TOut>) — predicate overloads
    // -------------------------------------------------------

    /// <summary>
    /// Adds a <c>Then</c> step after a typed <c>When</c> step, using a synchronous boolean predicate.
    /// </summary>
    /// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
    /// <typeparam name="TOut">The type produced by the preceding <c>When</c> step.</typeparam>
    /// <param name="whenTask">The <see cref="Task"/> that resolves to a <see cref="WhenBuilder{TGiven, TOut}"/>.</param>
    /// <param name="title">The display title for this <c>Then</c> step.</param>
    /// <param name="predicate">
    /// A synchronous predicate evaluated against the <typeparamref name="TOut"/> value.
    /// If it returns <see langword="false"/>, a <see cref="BddAssertException"/> is thrown.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> resolving to a <see cref="ThenBuilder{TOut}"/> for further chaining.
    /// </returns>
    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        string title,
        Func<TOut, CancellationToken, Task<bool>> predicate)
        => whenTask.Then(title, async (value, ct) =>
        {
            if (!await predicate(value, ct).ConfigureAwait(false))
                throw new BddAssertException($"Assertion failed: {title}");
        });

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        string title,
        Func<TOut, Task<bool>> predicate)
        => whenTask.Then(title, (v, _) => predicate(v));

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        Func<TOut, CancellationToken, Task<bool>> predicate)
        => whenTask.Then("Then assertion", predicate);

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        Func<TOut, Task<bool>> predicate)
        => whenTask.Then("Then assertion", predicate);

    // ---------------------------
    // Then(...) after When(..., TOut) (typed) — SYNC BOOL
    // ---------------------------

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        string title,
        Func<TOut, bool> predicate)
        => whenTask.Then(title, (v, _) =>
        {
            if (!predicate(v)) throw new BddAssertException(title);
            return Task.CompletedTask;
        });

    public static Task<ThenBuilder<TOut>> Then<TGiven, TOut>(
        this Task<WhenBuilder<TGiven, TOut>> whenTask,
        Func<TOut, bool> predicate)
        => whenTask.Then("Then assertion", predicate);


    // ---------------------------
    // Then(...) after When(... TGiven) (untyped) — SYNC BOOL
    // ---------------------------

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        string title,
        Func<bool> predicate)
        => whenTask.Then(title, _ =>
        {
            if (!predicate()) throw new BddAssertException(title);
            return Task.CompletedTask;
        });

    public static Task<ThenBuilder> Then<TGiven>(
        this Task<WhenBuilder<TGiven>> whenTask,
        Func<bool> predicate)
        => whenTask.Then("Then assertion", predicate);

    // -------------------------------------------------------
    // AND / BUT on Then<T> — predicate overloads
    // -------------------------------------------------------

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, CancellationToken, Task<bool>> predicate)
        => thenTask.And(title, async (v, ct) =>
        {
            if (!await predicate(v, ct).ConfigureAwait(false))
                throw new BddAssertException($"Assertion failed: {title}");
        });

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, Task<bool>> predicate)
        => thenTask.And(title, (v, _) => predicate(v));

    public static Task<ThenBuilder<T>> But<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, CancellationToken, Task<bool>> predicate)
        => thenTask.And(title, predicate);

    public static Task<ThenBuilder<T>> But<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, Task<bool>> predicate)
        => thenTask.And(title, (v, _) => predicate(v));

    // Given -> Then(transform)   (alias to When<TOut>)
    public static Task<WhenBuilder<TGiven, TOut>> Then<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, CancellationToken, Task<TOut>> transform)
        => given.When(title, transform);

    public static Task<WhenBuilder<TGiven, TOut>> Then<TGiven, TOut>(
        this GivenBuilder<TGiven> given,
        string title,
        Func<TGiven, Task<TOut>> transform)
        => given.When<TGiven, TOut>(title, (g, _) => transform(g));


    // ---------------------------
    // And / But after Then<T> — SYNC BOOL
    // ---------------------------

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, bool> predicate,
        string stepName = nameof(And))
        => thenTask.And(title, (v, _) =>
        {
            if (!predicate(v)) throw new BddAssertException(title);
            return Task.CompletedTask;
        }, stepName);

    public static Task<ThenBuilder<T>> And<T>(
        this Task<ThenBuilder<T>> thenTask,
        Func<T, bool> predicate)
        => thenTask.And("And assertion", predicate);

    public static Task<ThenBuilder<T>> But<T>(
        this Task<ThenBuilder<T>> thenTask,
        string title,
        Func<T, bool> predicate)
        => thenTask.And(title, predicate, nameof(But));

    public static Task<ThenBuilder<T>> But<T>(
        this Task<ThenBuilder<T>> thenTask,
        Func<T, bool> predicate)
        => thenTask.And("But assertion", predicate, nameof(But));
}