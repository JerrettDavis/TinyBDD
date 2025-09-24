# Introduction

TinyBDD is a tiny, fluent BDD helper for .NET tests. It gives you a clean Given/When/Then style without tying you to a
specific test framework.

<img src="images/tinyBDD.png" alt="TinyBDD" width="110" align="right" />

Why TinyBDD

- Tiny: a small core you can read in minutes
- Fluent: expressive chains for Given, When, Then, And, But
- Pragmatic: first-class adapters for xUnit, NUnit, and MSTest, but works fine without them
- Deferred fluent expectations: `Expect.For/That` let you compose `.Because()` / `.With()` / checks in any order and only throw when awaited
- Built-in step lineage: every step’s input/output captured in `ScenarioContext.IO` plus `CurrentItem` pointer for post-mortem analysis

Core ideas

- Two entry points
    - Explicit: create a ScenarioContext with Bdd.CreateContext and pass it around
    - Ambient: set Ambient.Current (or inherit a TinyBDD base class) and use Flow
- Cross-framework: adapters only handle output and tag bridging; assertions and flow stay the same
- Minimal assertions: bring your favorite assertion library; TinyBDD only needs a bool predicate or action per step

What a scenario looks like

- Explicit (no base class required):

```csharp
var ctx = Bdd.CreateContext(this); // reads [Feature]/[Scenario] and method/test attributes
await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
         .When("sum", (arr, _) => Task.FromResult(arr.Sum()))
         .Then("> 0", sum => sum > 0)
         .AssertPassed();
```

- Ambient (inherit a base class or set Ambient.Current):

```csharp
await Given(() => 1)
      .When("double", x => x * 2)
      .Then("== 2", v => v == 2)
      .AssertPassed();
```

Attributes and tags

- [Feature] on a class gives a friendly feature name (and optional description)
- [Scenario] on a test method provides a friendly scenario name and optional tags
- [Tag] on classes or methods adds tags; adapters forward tags to your test framework’s trait/category output

Assertions made simple

- Action-based: Then("works", () => Task.CompletedTask)
- Predicate-based: Then("x == 2", v => v == 2) — throws BddAssertException on false
- Fluent deferred: await Expect.For(value, "subject").Because("why").With("hint").ToBe(expected);
- With or without CancellationToken, and available for And/But too

Reporting

- Base classes emit Gherkin-style output automatically to the framework’s test log
- Manual reporting: GherkinFormatter.Write(ctx, reporter) with an IBddReporter

Design notes

- Steps are recorded in ScenarioContext.Steps as they execute
- Per-step Input/Output captured (ScenarioContext.IO) and latest CurrentItem maintained
- Failures are captured and rethrown as BddStepException with the original exception as InnerException
- Async-friendly by default; chains are Task-based and CancellationToken-friendly where it helps

Where next

- Fundamentals: bdd-fundamentals.md
- BDD + TDD Workflow: tdd-via-bdd.md
- Expectations & Assertions: assertions-and-expectations.md
- Step IO & State Tracking: step-io-and-state.md
- Tips & Tricks: tips-and-tricks.md
- Extensibility & Advanced: advanced-usage.md
- Getting Started: getting-started.md
- API Reference: api/
