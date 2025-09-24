---
_layout: landing
---

# TinyBDD — Fluent BDD for .NET, zero ceremony

Write expressive Given/When/Then tests that feel great and run anywhere (xUnit, NUnit, MSTest) without framework lock-in.

## Why TinyBDD

<img src="images/tinyBDD.png" alt="TinyBDD" width="110" align="right" />

- Tiny core you can read in minutes
- Fluent, async-first chains (Given/When/Then/And/But)
- Deferred fluent expectations (`Expect.For/That`) — compose reasons/hints, throw only when awaited
- Automatic Step IO lineage (inputs/outputs + current item tracked)
- Works with any assertion library
- Adapters for xUnit, NUnit, MSTest (optional)
- Gherkin-style reporting to your test output

## Try it in 30 seconds

Ambient (with adapter base class or Ambient.Current set):
```csharp
await Given(() => 1)
     .When("double", x => x * 2)
     .Then("== 2", v => v == 2)
     .AssertPassed();
```

Explicit (no base class required):
```csharp
var ctx = Bdd.CreateContext(this);
await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
         .When("sum", (arr, _) => Task.FromResult(arr.Sum()))
         .Then("> 0", sum => sum > 0)
         .AssertPassed();
```

## Documentation Map
- Introduction: [introduction.md](/user-guide/index.md)
- Getting Started: [getting-started.md](/user-guide/getting-started.md)
- Fundamentals (BDD & Gherkin): [bdd-fundamentals.md](/user-guide/bdd-fundamentals.md)
- BDD + TDD Workflow: [tdd-via-bdd.md](/user-guide/tdd-via-bdd.md)
- Expectations & Assertions: [assertions-and-expectations.md](/user-guide/assertions-and-expectations.md)
- Step IO & State Tracking: [step-io-and-state.md](/user-guide/step-io-and-state.md)
- Tips & Tricks: [tips-and-tricks.md](/user-guide/tips-and-tricks.md)
- Extensibility & Advanced: [advanced-usage.md](/user-guide/advanced-usage.md)

Tip: add [Feature], [Scenario], and [Tag] to make reports shine. Base classes emit Gherkin output automatically.
