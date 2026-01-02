# Expectations & Assertions

TinyBDD keeps assertion mechanics deliberately small so you can mix and match styles without lock‑in. This guide covers:
- Predicate / action based steps
- Deferred fluent expectations (Expect.For / Expect.That)
- Integrating external assertion libraries
- Writing custom reusable predicates
- Failure diagnostics & exception types

## 1. Predicate vs Action Steps
Every `Then` (and `And` / `But` under a Then) ultimately produces a boolean result or throws.

| Shape | Example | Behavior |
|-------|---------|----------|
| Predicate returning bool | `Then("== 2", v => v == 2)` | If false → framework throws TinyBddAssertionException (wrapped) |
| Predicate returning Task<bool>/ValueTask<bool> | `Then("> 0 async", async v => await IsPositive(v))` | Awaited; false triggers failure |
| Action returning void/Task/ValueTask | `Then("no error", () => DoCheck())` | Exceptions propagate as failures |
| Awaitable assertion (ValueTask) | `Then("deferred", v => Expect.For(v).ToBe(5))` | Pipeline awaits; failures produced after evaluation |

## 2. Deferred Fluent Expectations
The `Expect` API provides a small, English‑like DSL whose checks **do not throw until awaited**. This lets you compose message decorations (`Because`, `With`) in any order before evaluation.

```csharp
await Expect.For(user.Name, "user name")
    .Because("we require a default")
    .With("seed data mismatch")
    .ToBe("Alice");
```
Or embedded in a `Then` predicate:
```csharp
await Given(() => new User { Name = "Bob" })
    .Then("name is Alice (demonstrates failure message)", u =>
        Expect.For(u.Name, "user name")
              .Because("system should auto‑assign")
              .With("check provisioning job")
              .ToBe("Alice"))
    .AssertFailed();
```

### Chaining Order Flexibility
These all produce the same behavior:
```csharp
Expect.For(v).Because("why").With("hint").ToBe(5);
Expect.For(v).ToBe(5).Because("why").With("hint"); // still fine (message metadata stored then applied when awaited)
```

### Collection Assertion Examples
```csharp
// Exact count
await Expect.That(items, "cart items").ToHaveCount(3);

// Empty check
await Expect.That(emptyList).ToBeEmpty();

// Range checks
await Expect.That(results).ToHaveAtLeast(1);
await Expect.That(results).ToHaveNoMoreThan(100);

// Contains checks
await Expect.That(names).ToContain("Alice");
await Expect.That(orders).ToContainMatch<Order>(o => o.Total > 100, "expensive orders");

// Predicate matching with counts
await Expect.That(users).ToHaveCountMatching<User>(3, u => u.IsActive, "active users");
await Expect.That(items).ToHaveMoreThanCountMatching<Item>(5, i => i.InStock);
```

### Instance State Examples
```csharp
// Type checks
await Expect.That(result).ToBeOfType<CustomerDto>();
await Expect.That(handler).ToBeAssignableTo<IRequestHandler>();
```

### Exception Assertion Examples
```csharp
// Any exception
await Expect.That<object>(null!).ToThrow(() => service.Execute());

// Specific exception type
await Expect.That<object>(null!).ToThrowExactly<ArgumentException>(
    () => validator.Validate(null));

// Exception with message
await Expect.That<object>(null!).ToThrowWithMessage(
    () => parser.Parse("invalid"), 
    "Invalid format");

// Specific type with message
await Expect.That<object>(null!).ToThrowExactlyWithMessage<FormatException>(
    () => parser.Parse("bad"), 
    "Invalid format");

// No exception expected
await Expect.That<object>(null!).ToNotThrow(() => service.SafeOperation());
```

### Supported Fluent Methods

#### Basic Value Assertions
| Method | Purpose |
|--------|---------|
| `ToBe(expected)` | Equality via `EqualityComparer<T>.Default` |
| `ToEqual(expected)` | Equality (object semantics) useful for differing generic types |
| `ToBeTrue()` / `ToBeFalse()` | Strong boolean check (type + value) |
| `ToBeNull()` / `ToNotBeNull()` | Nullability checks |
| `ToSatisfy(predicate, description?)` | Arbitrary predicate with optional human description |

#### Collection Assertions
| Method | Purpose |
|--------|---------|
| `ToHaveCount(expectedCount)` | Verify exact collection count |
| `ToBeEmpty()` | Verify collection has no items |
| `ToHaveAtLeast(minCount)` | Verify collection has at least N items |
| `ToHaveNoMoreThan(maxCount)` | Verify collection has no more than N items |
| `ToContain<TItem>(item)` | Verify collection contains specific item |
| `ToContainMatch<TItem>(predicate, description?)` | Verify collection contains item matching predicate |
| `ToHaveCountMatching<TItem>(expectedCount, predicate, description?)` | Verify N items match predicate |
| `ToHaveFewerThanCountMatching<TItem>(maxCount, predicate, description?)` | Verify fewer than N items match predicate |
| `ToHaveMoreThanCountMatching<TItem>(minCount, predicate, description?)` | Verify more than N items match predicate |

#### Instance State Assertions
| Method | Purpose |
|--------|---------|
| `ToBeOfType<TExpected>()` | Verify exact type match |
| `ToBeAssignableTo<TExpected>()` | Verify type compatibility / assignability |

#### Exception Assertions
| Method | Purpose |
|--------|---------|
| `ToThrow(action)` | Verify action throws any exception |
| `ToThrowExactly<TException>(action)` | Verify action throws specific exception type |
| `ToThrowWithMessage(action, expectedMessage)` | Verify action throws with specific message |
| `ToThrowExactlyWithMessage<TException>(action, expectedMessage)` | Verify action throws specific type with specific message |
| `ToNotThrow(action)` | Verify action completes without throwing |

#### Message Decorators
| Method | Purpose |
|--------|---------|
| `Because(reason)` | Adds a `because {reason}` suffix segment |
| `With(hint)` | Adds a trailing parenthetical hint `(hint)` |
| `As(subject)` | Override / set subject label used in messages |

### Failure Message Anatomy
```
expected user name to be "Alice", but was "Bob" because system should auto‑assign (check provisioning job)
```
Segments appear only if populated (subject, because, hint).

### Exceptions
- `TinyBddAssertionException` is thrown for fluent expectations.
  - Enriched with `Expected`, `Actual`, `Subject`, `Because`, `WithHint` properties to assist reporters.
- During pipeline execution TinyBDD may wrap assertion failures inside `BddStepException` while recording the original.

## 3. External Assertion Libraries
Use any library inside an action assertion variant:
```csharp
.Then("fluent assertions", item =>
{
    item.Value.Should().Be(42);
})
```
If an assertion library throws its own exception, TinyBDD records it; messages appear under that step.

## 4. Composable Predicates
You can centralize domain predicates to keep scenarios narrative‑focused:
```csharp
static bool HasSingleItem(Cart c) => c.Items.Count == 1;

await Given(() => new Cart())
    .When("add apple", c => { c.Add("apple"); return c; })
    .Then("one item", HasSingleItem)
    .AssertPassed();
```

## 5. ValueTask Integration
All fluent expectations implicitly convert to `ValueTask`, so any chain expecting a `Func<T, ValueTask>` acceptance form works without ceremony.

## 6. Choosing Between Styles
| Need | Recommendation |
|------|----------------|
| Quick boolean check | Simple predicate form |
| Rich, human message with reason/hint | Fluent `Expect.For/That` |
| Collection validation (count, contains, etc.) | Fluent collection assertions (`ToHaveCount`, `ToContain`, etc.) |
| Type checking | Instance state assertions (`ToBeOfType`, `ToBeAssignableTo`) |
| Exception validation | Exception assertions (`ToThrow`, `ToThrowExactly`, etc.) |
| 3rd party library integration | Action variant (`Then("desc", v => lib.Assertion(v))`) |
| Many related checks on one subject | Chain multiple fluent methods before awaiting |

## 7. ElementAtOrDefault Utility
The `ShouldExtensions.ElementAtOrDefault` avoids LINQ throwing patterns and returns default on null / out of range. Handy for log or queue peeks without guarding.
```csharp
var firstLine = log.ElementAtOrDefault(0);
await Expect.For(firstLine, "first log line").ToNotBeNull();
```

## 8. Diagnostics Tips
- Use `.As("subject alias")` early when the natural variable name is cryptic.
- Favor one expectation per business rule to isolate failures.
- Attach a `Because` only when it clarifies *intent*, not obvious truths.

## 9. Extending the Fluent API
You can wrap custom extension methods around `FluentAssertion<T>`:
```csharp
public static class FluentAssertionExtensions
{
    public static FluentAssertion<string> ToBeTitleCase(this FluentAssertion<string> a)
        => a.ToSatisfy(s => s == TitleCase(s), "be title case");
}
```
(Implementation snippet intentionally brief—focus on pattern.)

Proceed to: [Step IO & State Tracking](step-io-and-state.md)

