# Extensibility & Advanced Usage

This section dives below the surface: customizing execution, reporting, options, and integrating TinyBDD into enterprise test platforms.

## Scenario Options
Configure when creating a context:
```csharp
var ctx = Bdd.CreateContext(this, options: new ScenarioOptions {
    ContinueOnError = true,
    HaltOnFailedAssertion = false,
    MarkRemainingAsSkippedOnFailure = true,
    StepTimeout = TimeSpan.FromSeconds(5)
});
```
| Option | Effect | Typical Use |
|--------|--------|-------------|
| ContinueOnError | Non‑assert exceptions record failure but execution proceeds | Collect multiple failures in a single diagnostic run |
| HaltOnFailedAssertion | Re‑throw assertion (TinyBddAssertionException) immediately | Fast‑fail style CI where first failure is enough |
| MarkRemainingAsSkippedOnFailure | Tag remaining queued steps as skipped | Preserve intent visibility without executing unsafe operations |
| StepTimeout | Cancel any single step exceeding duration | Guard external I/O or long integration steps |

## Pipeline Hooks
`Pipeline.BeforeStep` / `AfterStep` allow lightweight instrumentation (timing, logging, tracing). Adapters internally set loggers; you can customize by forking an adapter or exposing a factory.
```csharp
// Pseudo: wrapping context creation to attach hooks
var ctx = Bdd.CreateContext(this);
var pipe = TinyReflection.GetPipeline(ctx); // hypothetical helper you add
pipe.BeforeStep = (c, meta) => _logger.LogDebug($"BEGIN {meta.Kind} {meta.Title}");
pipe.AfterStep  = (c, result) => _logger.LogInformation($"{result.Kind} {result.Title} [{(result.Error is null ? "OK" : "FAIL")}] {result.Elapsed.TotalMilliseconds:n0} ms");
```
Hooks run on the executing test thread—avoid heavy blocking operations.

## Custom Reporters
Implement `IBddReporter` and feed it to `GherkinFormatter.Write(ctx, reporter)` after execution.
```csharp
public sealed class JsonBddReporter : IBddReporter
{
    private readonly List<string> _lines = new();
    public void WriteLine(string message) => _lines.Add(message);
    public override string ToString() => JsonSerializer.Serialize(_lines);
}

var reporter = new JsonBddReporter();
GherkinFormatter.Write(ctx, reporter);
File.WriteAllText("scenario.json", reporter.ToString());
```
Format is entirely up to you (structured JSON, markdown, HTML, etc.).

## Integrating With CI
- Emit Gherkin output to standard test logs (adapters do this automatically).
- Convert reporter output to build annotations (GitHub Actions `::notice`, Azure DevOps logging commands) for fast triage.
- Persist `ScenarioContext.IO` serialized artifacts for post‑failure analysis.

## Custom Fluent Assertion Extensions
Extend `FluentAssertion<T>` with domain‑specific checks while preserving deferred semantics:
```csharp
public static class DomainAssertionExtensions
{
    public static FluentAssertion<Order> ToHaveLineCount(this FluentAssertion<Order> a, int expected)
        => a.ToSatisfy(o => o.Lines.Count == expected, $"have {expected} line(s)");
}
```
Usage:
```csharp
await Expect.For(order).ToHaveLineCount(2).ToSatisfy(o => o.Total > 0, "have positive total");
```

## Composing Multi‑Subject Assertions
Sometimes you need to compare two evolving states. Capture both in a tuple or small record earlier, then assert on the composite.
```csharp
await Given(() => (Original: GetUser(), Updated: UpdateUser()))
    .Then("email unchanged", t => t.Original.Email == t.Updated.Email)
    .And("version increments", t => t.Updated.Version == t.Original.Version + 1);
```

## Cancellation Strategy
Every step shape has a CancellationToken variant. Inject tokens to wire scenarios into larger harness time budgets or global test cancellations.
```csharp
await Given(ct => SeedAsync(ct)) // or Given("seed", (ct) => ...)
    .When("call api", (s, ct) => CallApiAsync(s, ct))
    .Then("status 200", r => r.Status == 200);
```
If a token cancels mid‑step, the pipeline records the step (Error = OperationCanceledException) then rethrows (unless you handle externally).

## Timeboxed Steps
`ScenarioOptions.StepTimeout` wraps each step in a linked CTS. Your delegate should cooperate with cancellation; otherwise timeout occurs only at `await` boundaries that observe the token.

## Partial Context Reconfiguration
Clone a context with tweaks without losing accumulated tags:
```csharp
var ctx2 = Bdd.ReconfigureContext(ctx, proto => {
    proto.ScenarioName = proto.ScenarioName + " (retry)";
});
```
Use to model variant scenarios sharing feature metadata.

## Working With Multiple Pipelines
You can run several scenarios in parallel using separate contexts. Because ambient isolation uses AsyncLocal, avoid setting `Ambient.Current` across threads you don't control; prefer explicit contexts for heavy parallel harnesses.

## Mixing Unit & BDD Layers
For deep algorithmic units write classic unit tests. At the feature boundary, aggregate them through TinyBDD scenarios focused on **what** not **how**. This separation keeps BDD specs lean and stable through internal refactors.

## Enterprise Hardening Checklist
| Concern | Recommendation |
|---------|---------------|
| Flaky external dependencies | Introduce test doubles; use StepTimeout for guard rails |
| Large data setup | Encapsulate builders; keep Given cheap (< 10ms ideal) |
| Slow integration layers | Mark long‑running scenarios with `[Tag("slow")]` for selective execution |
| Observability | Add hooks or custom reporter emitting JSON; archive artifacts |
| Security sensitive data | Avoid dumping secrets into IO / reporter output |
| Maintainability | Enforce naming conventions in PR review (domain language) |

## Roadmap Ideas (Illustrative)
- Async assertion library adapters
- Built‑in JSON snapshot diffing helper
- Rich HTML reporter (dark/light theme)
- Visual timeline (Gantt) from step timings

If you build one of these, contribute back—TinyBDD intends to stay small, but high‑leverage, opt‑in modules are welcome.

Return to: [Introduction](index.md)

