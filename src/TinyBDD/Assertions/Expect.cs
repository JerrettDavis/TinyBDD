namespace TinyBDD.Assertions;

/// <summary>
/// Minimal helper predicates for quick assertions inside TinyBDD fluent chains.
/// </summary>
/// <remarks>
/// These helpers return boolean values intended to be used with <c>Then</c>/<c>And</c>/<c>But</c> predicate overloads.
/// They are convenience methods only; you can use your preferred assertion library directly.
///
/// New: A fluent, English-readable DSL is available via <see cref="Expect.For{T}(T, string?)"/> and <see cref="Expect.That{T}(T, string?)"/>,
/// which now defer throwing until awaited. This means you can compose assertions in any order and only trigger evaluation
/// by awaiting the returned assertion (or passing it into TinyBDD's <c>Then/And/But</c> overloads that accept <c>Func&lt;T, ValueTask&gt;</c>).
/// </remarks>
public static class Expect
{
    /// <summary>
    /// Creates a fluent assertion builder that defers throwing a TinyBddAssertionException until awaited.
    /// Example: await Expect.For(log.ElementAtOrDefault(0), "first log line").ToEqual("GET /health");
    /// </summary>
    public static FluentAssertion<T> For<T>(T actual, string? subject = null) => new(actual, subject);

    /// <summary>
    /// Creates a fluent throwing assertion builder that defers throwing a TinyBddAssertionException until awaited.
    /// Example: await Expect.That(log.ElementAtOrDefault(0), "first log line").ToEqual("GET /health");
    /// </summary>
    public static FluentAssertion<T> That<T>(T actual, string? subject = null) => new(actual, subject);

    /// <summary>Pass-through for a boolean condition.</summary>
    /// <param name="condition">The boolean expression to evaluate.</param>
    /// <returns><see langword="true"/> when the condition holds; otherwise <see langword="false"/>.</returns>
    public static bool True(bool condition) => condition;

    /// <summary>Compares two values using <see cref="EqualityComparer{T}.Default"/>.</summary>
    /// <typeparam name="T">The value type to compare.</typeparam>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><see langword="true"/> when <paramref name="actual"/> equals <paramref name="expected"/>; otherwise <see langword="false"/>.</returns>
    public static bool Equal<T>(T actual, T expected) => EqualityComparer<T>.Default.Equals(actual, expected);

    /// <summary>Checks that a value is not null.</summary>
    /// <param name="o">The value to test.</param>
    /// <returns><see langword="true"/> when <paramref name="o"/> is not null; otherwise <see langword="false"/>.</returns>
    public static bool NotNull(object? o) => o is not null;
}

/// <summary>Rich assertion failure used to surface English-readable messages to report writers.</summary>
public sealed class TinyBddAssertionException : Exception
{
    public TinyBddAssertionException(string message) : base(message)
    {
    }

    public TinyBddAssertionException(string message, Exception inner) : base(message, inner)
    {
    }

    // Structured fields to help reporters and tests print richer diagnostics
    public object? Expected { get; set; }
    public object? Actual { get; set; }
    public string? Subject { get; set; }
    public string? Because { get; set; }
    public string? WithHint { get; set; }
}

/// <summary>
/// Fluent, predicate-returning expectation builder. Methods return bool for use in Then/And/But predicate overloads.
/// </summary>
public readonly struct FluentPredicate<T>
{
    private readonly T _actual;
    private readonly string? _subject;
    private readonly string? _because;
    private readonly string? _with;

    public FluentPredicate(T actual, string? subject)
    {
        _actual = actual;
        _subject = subject;
        _because = null;
        _with = null;
    }

    private FluentPredicate(T actual, string? subject, string? because, string? with)
    {
        _actual = actual;
        _subject = subject;
        _because = because;
        _with = with;
    }

    /// <summary>Adds a reason that will be included in the composed failure message (when used by wrappers).</summary>
    public FluentPredicate<T> Because(string? because) => new(_actual, _subject, because, _with);

    /// <summary>Adds an extra hint to be appended to messages.</summary>
    public FluentPredicate<T> With(string? hint) => new(_actual, _subject, _because, hint);

    public bool ToBe(T expected)
        => EqualityComparer<T>.Default.Equals(_actual, expected);

    public bool ToEqual<TExpected>(TExpected expected)
        => Equals(_actual, expected);

    public bool ToBeTrue()
        => _actual is true;

    public bool ToBeFalse()
        => _actual is false;

    public bool ToBeNull()
        => _actual is null;

    public bool ToNotBeNull()
        => _actual is not null;

    public bool ToSatisfy(Func<T, bool> predicate)
        => predicate(_actual);

    public override string ToString()
        => _subject ?? "value";
}

/// <summary>
/// Internal mutable state for a deferred assertion, enabling order-independent composition (Because/With before or after checks).
/// </summary>
internal sealed class FluentAssertionState<T>(T actual, string? subject)
{
    public T Actual { get; } = actual;
    public string? Subject { get; set; } = subject;
    public string? Because { get; set; }
    public string? With { get; set; }
    public List<Func<ValueTask>> Checks { get; } = new();
}

/// <summary>
/// Fluent, deferred assertion builder. Methods add checks; failures throw TinyBddAssertionException when awaited.
/// </summary>
public sealed class FluentAssertion<T>(T actual, string? subject)
{
    private readonly FluentAssertionState<T> _state = new(actual, subject);

    /// <summary>Describes the subject under test to improve error readability (e.g., "first log line").</summary>
    public FluentAssertion<T> As(string subject)
    {
        _state.Subject = subject;
        return this;
    }

    /// <summary>Adds a reason to be woven into the assertion message.</summary>
    public FluentAssertion<T> Because(string because)
    {
        _state.Because = because;
        return this;
    }

    /// <summary>Appends an extra hint to the end of the message.</summary>
    public FluentAssertion<T> With(string hint)
    {
        _state.With = hint;
        return this;
    }

    /// <summary>
    /// Adds an equality check to the assertion with deferred throwing.
    /// Uses <see cref="EqualityComparer{T}.Default"/> for comparison.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToBe(T expected)
    {
        _state.Checks.Add(() =>
        {
            if (!EqualityComparer<T>.Default.Equals(_state.Actual, expected))
                Throw($"expected {_state.Subject ?? "value"} to be {Fmt(expected)}, but was {Fmt(_state.Actual)}", expected, _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds an equality check to the assertion with deferred throwing.
    /// Uses <see cref="EqualityComparer{T}.Default"/> for comparison.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <typeparam name="TExpected">The expected value type, which may differ from <typeparamref name="T"/>.</typeparam>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToEqual<TExpected>(TExpected expected)
    {
        _state.Checks.Add(() =>
        {
            var equals = EqualityComparer<object?>.Default.Equals(_state.Actual, expected);
            if (!equals)
                Throw($"expected {_state.Subject ?? "value"} to equal {Fmt(expected)}, but was {Fmt(_state.Actual)}", expected, _state.Actual);
            return default;
        });
        return this;
    }
    
    /// <summary>
    /// Adds a boolean true check to the assertion with deferred throwing.
    /// </summary>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToBeTrue()
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not bool b || !b)
                Throw($"expected {_state.Subject ?? "value"} to be true, but was {Fmt(_state.Actual)}", true, _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a boolean false check to the assertion with deferred throwing.
    /// </summary>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToBeFalse()
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not bool b || b)
                Throw($"expected {_state.Subject ?? "value"} to be false, but was {Fmt(_state.Actual)}", false, _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a null check to the assertion with deferred throwing.
    /// </summary>
    /// <returns>The current assertion instance for further chaining.</returns>   
    public FluentAssertion<T> ToBeNull()
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not null)
                Throw($"expected {_state.Subject ?? "value"} to be null, but was {Fmt(_state.Actual)}", null, _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a non-null check to the assertion with deferred throwing.
    /// </summary>
    /// <returns>The current assertion instance for further chaining.</returns>  
    public FluentAssertion<T> ToNotBeNull()
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is null)
                Throw($"expected {_state.Subject ?? "value"} to be non-null", "non-null", _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a predicate check to the assertion with deferred throwing.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="description">An optional description to include in the failure message.</param>
    /// <returns>The current assertion instance for further chaining.</returns> 
    public FluentAssertion<T> ToSatisfy(Func<T, bool> predicate, string? description = null)
    {
        _state.Checks.Add(() =>
        {
            if (!predicate(_state.Actual))
                Throw($"expected {_state.Subject ?? "value"} {(description is null ? "to satisfy condition" : $"to {description}")}, but it did not", description ?? "satisfy condition", _state.Actual);
            return default;
        });
        return this;
    }

    // ====== Collection Assertions ======

    /// <summary>
    /// Adds a count check for collections with deferred throwing.
    /// </summary>
    /// <param name="expectedCount">The expected number of items in the collection.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToHaveCount(int expectedCount)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not System.Collections.IEnumerable enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable for count check, but was {Fmt(_state.Actual)}", expectedCount, _state.Actual);
                return default;
            }
            
            var actualCount = enumerable.Cast<object?>().Count();
            if (actualCount != expectedCount)
                Throw($"expected {_state.Subject ?? "collection"} to have count {expectedCount}, but was {actualCount}", expectedCount, actualCount);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds an empty check for collections with deferred throwing.
    /// </summary>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToBeEmpty()
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not System.Collections.IEnumerable enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable for empty check, but was {Fmt(_state.Actual)}", "empty enumerable", _state.Actual);
                return default;
            }
            
            var items = enumerable.Cast<object?>().ToList();
            if (items.Count > 0)
                Throw($"expected {_state.Subject ?? "collection"} to be empty, but it had {items.Count} item(s)", 0, items.Count);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a minimum count check for collections with deferred throwing.
    /// </summary>
    /// <param name="minCount">The minimum expected number of items.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToHaveAtLeast(int minCount)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not System.Collections.IEnumerable enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable for count check, but was {Fmt(_state.Actual)}", $"at least {minCount}", _state.Actual);
                return default;
            }
            
            var actualCount = enumerable.Cast<object?>().Count();
            if (actualCount < minCount)
                Throw($"expected {_state.Subject ?? "collection"} to have at least {minCount} item(s), but was {actualCount}", $">= {minCount}", actualCount);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a maximum count check for collections with deferred throwing.
    /// </summary>
    /// <param name="maxCount">The maximum expected number of items.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToHaveNoMoreThan(int maxCount)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not System.Collections.IEnumerable enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable for count check, but was {Fmt(_state.Actual)}", $"no more than {maxCount}", _state.Actual);
                return default;
            }
            
            var actualCount = enumerable.Cast<object?>().Count();
            if (actualCount > maxCount)
                Throw($"expected {_state.Subject ?? "collection"} to have no more than {maxCount} item(s), but was {actualCount}", $"<= {maxCount}", actualCount);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the collection contains a specific item with deferred throwing.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToContain<TItem>(TItem item)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not System.Collections.IEnumerable enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable for contains check, but was {Fmt(_state.Actual)}", $"contains {Fmt(item)}", _state.Actual);
                return default;
            }
            
            var found = enumerable.Cast<object?>().Any(x => EqualityComparer<object?>.Default.Equals(x, item));
            if (!found)
                Throw($"expected {_state.Subject ?? "collection"} to contain {Fmt(item)}, but it did not", item, _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the collection contains an item matching the predicate with deferred throwing.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="description">An optional description of the match criteria.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToContainMatch<TItem>(Func<TItem, bool> predicate, string? description = null)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not IEnumerable<TItem> enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable of {typeof(TItem).Name} for contains match check, but was {Fmt(_state.Actual)}", description ?? "matching item", _state.Actual);
                return default;
            }
            
            if (!enumerable.Any(predicate))
                Throw($"expected {_state.Subject ?? "collection"} to contain an item {(description is null ? "matching predicate" : description)}, but it did not", description ?? "matching item", _state.Actual);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the collection has a specific count of items matching the predicate with deferred throwing.
    /// </summary>
    /// <param name="expectedCount">The expected count of matching items.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="description">An optional description of the match criteria.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToHaveCountMatching<TItem>(int expectedCount, Func<TItem, bool> predicate, string? description = null)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not IEnumerable<TItem> enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable of {typeof(TItem).Name} for count matching check, but was {Fmt(_state.Actual)}", expectedCount, _state.Actual);
                return default;
            }
            
            var actualCount = enumerable.Count(predicate);
            if (actualCount != expectedCount)
                Throw($"expected {_state.Subject ?? "collection"} to have {expectedCount} item(s) {(description is null ? "matching predicate" : description)}, but was {actualCount}", expectedCount, actualCount);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the collection has fewer than a specific count of items matching the predicate with deferred throwing.
    /// </summary>
    /// <param name="maxCount">The maximum expected count of matching items (exclusive).</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="description">An optional description of the match criteria.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToHaveFewerThanCountMatching<TItem>(int maxCount, Func<TItem, bool> predicate, string? description = null)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not IEnumerable<TItem> enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable of {typeof(TItem).Name} for count matching check, but was {Fmt(_state.Actual)}", $"< {maxCount}", _state.Actual);
                return default;
            }
            
            var actualCount = enumerable.Count(predicate);
            if (actualCount >= maxCount)
                Throw($"expected {_state.Subject ?? "collection"} to have fewer than {maxCount} item(s) {(description is null ? "matching predicate" : description)}, but was {actualCount}", $"< {maxCount}", actualCount);
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the collection has more than a specific count of items matching the predicate with deferred throwing.
    /// </summary>
    /// <param name="minCount">The minimum expected count of matching items (exclusive).</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="description">An optional description of the match criteria.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToHaveMoreThanCountMatching<TItem>(int minCount, Func<TItem, bool> predicate, string? description = null)
    {
        _state.Checks.Add(() =>
        {
            if (_state.Actual is not IEnumerable<TItem> enumerable)
            {
                Throw($"expected {_state.Subject ?? "value"} to be enumerable of {typeof(TItem).Name} for count matching check, but was {Fmt(_state.Actual)}", $"> {minCount}", _state.Actual);
                return default;
            }
            
            var actualCount = enumerable.Count(predicate);
            if (actualCount <= minCount)
                Throw($"expected {_state.Subject ?? "collection"} to have more than {minCount} item(s) {(description is null ? "matching predicate" : description)}, but was {actualCount}", $"> {minCount}", actualCount);
            return default;
        });
        return this;
    }

    // ====== Instance State Assertions ======

    /// <summary>
    /// Adds a type check with deferred throwing.
    /// </summary>
    /// <typeparam name="TExpected">The expected type.</typeparam>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToBeOfType<TExpected>()
    {
        _state.Checks.Add(() =>
        {
            var expectedType = typeof(TExpected);
            var actualType = _state.Actual?.GetType();
            if (actualType != expectedType)
                Throw($"expected {_state.Subject ?? "value"} to be of type {expectedType.Name}, but was {actualType?.Name ?? "null"}", expectedType.Name, actualType?.Name ?? "null");
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a type assignability check with deferred throwing.
    /// </summary>
    /// <typeparam name="TExpected">The expected assignable type.</typeparam>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToBeAssignableTo<TExpected>()
    {
        _state.Checks.Add(() =>
        {
            var expectedType = typeof(TExpected);
            var actualType = _state.Actual?.GetType();
            if (actualType is null || !expectedType.IsAssignableFrom(actualType))
                Throw($"expected {_state.Subject ?? "value"} to be assignable to {expectedType.Name}, but was {actualType?.Name ?? "null"}", expectedType.Name, actualType?.Name ?? "null");
            return default;
        });
        return this;
    }

    // ====== Exception Assertions ======

    /// <summary>
    /// Adds a check that the action throws any exception with deferred throwing.
    /// </summary>
    /// <param name="action">The action that is expected to throw.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToThrow(Action action)
    {
        _state.Checks.Add(() =>
        {
            try
            {
                action();
                Throw($"expected {_state.Subject ?? "action"} to throw an exception, but it did not", "exception", "no exception");
            }
            catch (TinyBddAssertionException)
            {
                throw; // Re-throw assertion failures
            }
            catch (Exception)
            {
                // Expected - action threw an exception
            }
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the action throws a specific exception type with deferred throwing.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The action that is expected to throw.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToThrowExactly<TException>(Action action) where TException : Exception
    {
        _state.Checks.Add(() =>
        {
            try
            {
                action();
                Throw($"expected {_state.Subject ?? "action"} to throw {typeof(TException).Name}, but it did not", typeof(TException).Name, "no exception");
            }
            catch (TinyBddAssertionException)
            {
                throw; // Re-throw assertion failures
            }
            catch (TException)
            {
                // Expected exact type
            }
            catch (Exception ex)
            {
                Throw($"expected {_state.Subject ?? "action"} to throw {typeof(TException).Name}, but threw {ex.GetType().Name}", typeof(TException).Name, ex.GetType().Name);
            }
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the action throws an exception with a specific message with deferred throwing.
    /// </summary>
    /// <param name="action">The action that is expected to throw.</param>
    /// <param name="expectedMessage">The expected exception message.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToThrowWithMessage(Action action, string expectedMessage)
    {
        _state.Checks.Add(() =>
        {
            try
            {
                action();
                Throw($"expected {_state.Subject ?? "action"} to throw an exception with message {Fmt(expectedMessage)}, but it did not", expectedMessage, "no exception");
            }
            catch (TinyBddAssertionException)
            {
                throw; // Re-throw assertion failures
            }
            catch (Exception ex)
            {
                if (ex.Message != expectedMessage)
                    Throw($"expected {_state.Subject ?? "action"} to throw exception with message {Fmt(expectedMessage)}, but message was {Fmt(ex.Message)}", expectedMessage, ex.Message);
            }
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the action throws a specific exception type with a specific message with deferred throwing.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The action that is expected to throw.</param>
    /// <param name="expectedMessage">The expected exception message.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToThrowExactlyWithMessage<TException>(Action action, string expectedMessage) where TException : Exception
    {
        _state.Checks.Add(() =>
        {
            try
            {
                action();
                Throw($"expected {_state.Subject ?? "action"} to throw {typeof(TException).Name} with message {Fmt(expectedMessage)}, but it did not", $"{typeof(TException).Name}: {expectedMessage}", "no exception");
            }
            catch (TinyBddAssertionException)
            {
                throw; // Re-throw assertion failures
            }
            catch (TException ex)
            {
                if (ex.Message != expectedMessage)
                    Throw($"expected {_state.Subject ?? "action"} to throw {typeof(TException).Name} with message {Fmt(expectedMessage)}, but message was {Fmt(ex.Message)}", expectedMessage, ex.Message);
            }
            catch (Exception ex)
            {
                Throw($"expected {_state.Subject ?? "action"} to throw {typeof(TException).Name}, but threw {ex.GetType().Name}", typeof(TException).Name, ex.GetType().Name);
            }
            return default;
        });
        return this;
    }

    /// <summary>
    /// Adds a check that the action does not throw any exception with deferred throwing.
    /// </summary>
    /// <param name="action">The action that is expected not to throw.</param>
    /// <returns>The current assertion instance for further chaining.</returns>
    public FluentAssertion<T> ToNotThrow(Action action)
    {
        _state.Checks.Add(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Throw($"expected {_state.Subject ?? "action"} to not throw, but threw {ex.GetType().Name}: {ex.Message}", "no exception", $"{ex.GetType().Name}: {ex.Message}");
            }
            return default;
        });
        return this;
    }

    /// <summary>
    /// Executes all queued checks and throws on first failure. Awaiting an instance triggers this automatically.
    /// </summary>
    public ValueTask EvaluateAsync()
    {
        // All checks are synchronous today, but keep ValueTask for future async checks.
        return AwaitSlow(_state
            .Checks
            .Select(check => check())
            .FirstOrDefault(vt => !vt.IsCompletedSuccessfully));

        static async ValueTask AwaitSlow(ValueTask vt)
            => await vt.ConfigureAwait(false);
    }

    // Make the assertion awaitable directly
    public System.Runtime.CompilerServices.ValueTaskAwaiter GetAwaiter() => EvaluateAsync().GetAwaiter();

    // Allow passing into APIs expecting a ValueTask
    public static implicit operator ValueTask(FluentAssertion<T> assertion) => assertion.EvaluateAsync();

    private void Throw(string core, object? expected = null, object? actual = null)
    {
        var because = string.IsNullOrWhiteSpace(_state.Because) ? null : $" because {_state.Because!.Trim()}";
        var with = string.IsNullOrWhiteSpace(_state.With) ? null : $" ({_state.With!.Trim()})";
        var ex = new TinyBddAssertionException($"{core}{because}{with}")
        {
            Expected = expected,
            Actual = actual,
            Subject = _state.Subject,
            Because = _state.Because,
            WithHint = _state.With
        };
        throw ex;
    }

    private static string Fmt(object? value)
        => value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => value.ToString() ?? string.Empty
        };
}

/// <summary>English-readable predicate helpers on strings and enumerables for use inside Then/And predicate chains.</summary>
public static class ShouldExtensions
{
    /// <summary>Returns true if the string contains the specified substring (ordinal by default).</summary>
    public static bool ShouldContain(this string? actual, string expected, StringComparison comparison = StringComparison.Ordinal)
        => actual?.IndexOf(expected, comparison) >= 0;

    /// <summary>Returns true if the sequence contains an item matching the predicate.</summary>
    public static bool ShouldContain<T>(this IEnumerable<T>? source, Func<T, bool> predicate)
        => source?.Any(predicate) ?? false;

    /// <summary>Returns true if the sequence contains the item using EqualityComparer&lt;T&gt;.Default.</summary>
    public static bool ShouldContain<T>(this IEnumerable<T>? source, T item)
        => source?.Contains(item) ?? false;

    /// <summary>Returns true if the sequence has the expected count.</summary>
    public static bool ShouldHaveCount<T>(this IEnumerable<T>? source, int expectedCount)
        => source is not null && source.Count() == expectedCount;

    /// <summary>Returns the element at the specified index or the default value when the sequence is null or out of range.</summary>
    public static T? ElementAtOrDefault<T>(this IEnumerable<T>? source, int index)
    {
        if (source is null || index < 0) return default;
        // avoid LINQ to prevent throwing on null; iterate manually
        using var e = source.GetEnumerator();
        var i = 0;
        while (e.MoveNext())
        {
            if (i == index) return e.Current;
            i++;
        }
        return default;
    }
}