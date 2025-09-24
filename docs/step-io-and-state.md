# Step IO & State Tracking

TinyBDD automatically records a *data lineage* for each scenario so you can inspect what flowed through every step.

## What Gets Tracked
For every executed step a `StepIO` record is appended to `ScenarioContext.IO`:
```csharp
public record StepIO(string Kind, string Title, object? Input, object? Output);
```
| Field | Meaning |
|-------|---------|
| Kind  | Display keyword actually printed (Given / When / Then / And / But) |
| Title | The human step title (or auto title) |
| Input | The incoming state object before this step ran |
| Output| The state object returned by the step delegate |

In addition, `ScenarioContext.CurrentItem` holds the **latest successful output** so later diagnostics (including custom reporters) can see final state even after a failure.

## Example
```csharp
var ctx = Bdd.CreateContext(this);
await Bdd.Given(ctx, "seed", () => 1)
    .When("+1", x => x + 1)
    .Then("== 2", v => v == 2)
    .AssertPassed();

// Inspect lineage
foreach (var io in ctx.IO)
    Console.WriteLine($"{io.Kind} {io.Title}: in={io.Input ?? "<null>"} out={io.Output ?? "<null>"}");
// CurrentItem == 2
```
Sample output:
```
Given seed: in=<null> out=1
When +1: in=1 out=2
Then == 2: in=2 out=2
```

## Failures
When a `Then` predicate fails (assertion false) or a step throws:
- The failing step's IO is still captured (`Output` is the last produced state, commonly identical to `Input` for assertions).
- `CurrentItem` still points to the last *attempted* output (helpful for post‑mortem analysis).
- `ScenarioContext.Steps` contains the timing + exception for each step (see `StepResult`).

If `ScenarioOptions.MarkRemainingAsSkippedOnFailure` is `true`, queued future steps (not yet executed) are added to `Steps` as skipped **but** they do **not** receive IO entries because they never ran.

## Performance Notes
Capturing IO is O(1) per step and minimally allocates (one record + optional string operations for titles). If you need to disable it globally for extreme perf scenarios you can fork the pipeline write sites (`Pipeline.CaptureStepResult`)—the design keeps IO capture localized.

## Debugging Tips
- When an intermittent failure occurs, dump `ScenarioContext.IO` and `ScenarioContext.Steps` together to correlate timing with data flow.
- Attach a custom `AfterStep` hook (see *Extensibility & Advanced*) to stream IO in real‑time to a log sink.

## Comparing Multiple Runs
You can serialize IO to a structured log (JSON) for diffing across runs:
```csharp
var diffModel = ctx.IO.Select(i => new { i.Kind, i.Title, In = i.Input, Out = i.Output });
File.WriteAllText("io.json", JsonSerializer.Serialize(diffModel));
```

## Anti‑Patterns
| Smell | Consequence | Prefer |
|-------|-------------|-------|
| Mutating a complex object in place across multiple steps without returning it | Harder to understand transformations from IO snapshots | Return a new immutable snapshot or clone on mutation heavy domains |
| Hiding significant state in static singletons | IO lineage loses visibility | Pass required state explicitly through the chain |

Proceed to: [Tips & Tricks](tips-and-tricks.md)

