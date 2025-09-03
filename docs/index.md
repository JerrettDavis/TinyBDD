---
_layout: landing
---

# TinyBDD — Fluent BDD for .NET, zero ceremony


Write expressive Given/When/Then tests that feel great and run anywhere (xUnit, NUnit, MSTest) without framework lock-in.

## Why TinyBDD

<img src="images/tinyBDD.png" alt="TinyBDD" width="110" align="right" />

- Tiny core you can read in minutes
- Fluent, async-first chains (Given/When/Then/And/But)
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

## Get started
- Introduction: [introduction.md](introduction.md)
- Getting Started: [getting-started.md](getting-started.md)

Tip: add [Feature], [Scenario], and [Tag] to make reports shine. Base classes emit Gherkin output automatically.
