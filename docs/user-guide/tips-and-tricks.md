# Tips & Tricks

A grab‑bag of pragmatic techniques to keep scenarios fast, readable, and low‑ceremony.

## 1. Prefer Delegates / Function Pointers Over Inline Lambdas (Where Hot)
For extremely hot paths (thousands of scenarios), the JIT can better optimize static delegates or function pointers.
```csharp
static int Inc(int x) => x + 1;
await Given(() => 1)
    .When("+1", Inc) // delegate reuse
    .Then("> 0", v => v > 0);
```
In most test suites the difference is negligible—use only if profiling justifies it.

## 2. Title Economy
Keep step titles **business readable**; avoid leaking implementation detail unless it clarifies intent.
Bad: `When invoke Handler.Process(OrderDto{...})`
Better: `When submitting a valid order`

## 3. Reuse Seed Builders
Extract object graph creation into factories:
```csharp
static Order NewOrder(params string[] skus) => new(skus.Select(s => new LineItem(s)).ToList());
await Given("new order", () => NewOrder("A123")) ...
```

## 4. Fast Data Variations
Leverage parameterized tests (xUnit `[Theory]`, NUnit `[TestCase]`) wrapping TinyBDD chains for data sets.
```csharp
[Theory]
[InlineData(0)]
[InlineData(5)]
public async Task Adds(int start) =>
    await Given(() => start)
        .When("+1", x => x + 1)
        .Then("> start", v => v > start)
        .AssertPassed();
```

## 5. Combine Predicates with Fluent Expectations
Use simple predicates for binary checks, fluent expectations for multi‑facet diagnostics.
```csharp
.Then("status ok", r => r.Status == 200)
.And("payload ok", r => Expect.For(r.Body).ToNotBeNull().ToSatisfy(b => b.Length > 0, "have content"))
```

## 6. Snapshot / Golden File Verification
If you want to snapshot outputs, capture `CurrentItem` after the chain and compare serialized form (serialize with stable ordering). Keep snapshots versioned.

## 7. Parallelization Caution
Ambient context uses AsyncLocal—safe for parallel test runners. Avoid mutating shared static state in step delegates.

## 8. Timeouts Strategically
Use `ScenarioOptions.StepTimeout` for untrusted integrations; keep local pure logic un‑timed to avoid noise.
```csharp
var ctx = Bdd.CreateContext(this, options: new ScenarioOptions { StepTimeout = TimeSpan.FromSeconds(2) });
```

## 9. Partial Scenario Reconfiguration
Duplicate setups with slight meta differences via `Bdd.ReconfigureContext`.
```csharp
var fastCtx = Bdd.ReconfigureContext(ctx, proto => proto.ScenarioName = proto.ScenarioName + " (fast) ");
```

## 10. Rich Failure Context
Attach hints (`With`) sparingly to point to actionable remediation steps (config file, external dependency name, etc.). Avoid restating the assertion itself.

## 11. Logging During Execution
Use `Pipeline.BeforeStep` / `AfterStep` hooks (via adapter that exposes pipeline) to stream to logs. Or inspect `ScenarioContext.IO` after run.

## 12. Keep Scenarios Deterministic
Eliminate non‑determinism: random seeds, time providers, network calls replaced with fakes. Deterministic tests reduce flaky noise and improve perceived reliability.

Proceed to: [Extensibility & Advanced](advanced-usage.md)

