# Fundamentals: BDD, Gherkin & TinyBDD Mapping

> If you're new to Behavior (and Business) Driven Development, this chapter orients you: *why* BDD exists, the core vocabulary (Given / When / Then / And / But), how it relates to classic Test Driven Development (TDD), and how TinyBDD purposefully narrows the surface so you stay productive.

## What is BDD?
Behavior‑Driven Development focuses on expressing **system behavior** in a ubiquitous, business‑flavoured language so that developers, testers, domain experts and stakeholders share a single understanding. It grew out of TDD with an emphasis on *communication* and *examples as specification*.

| Concept | Plain meaning | TinyBDD artifact |
| ------- | ------------- | ---------------- |
| Feature | A capability delivering business value | `[Feature]` attribute or test class name |
| Scenario | A concrete example (executable specification) | `[Scenario]` attribute or test method name |
| Given | Initial context / preconditions | `Given(...)` chain starters |
| When | Action / event under test | `When(...)` transformations or side‑effects |
| Then | Observable outcome / assertion | `Then(...)` predicates / actions |
| And / But | Additional Givens/Whens/Thens | `And(...)` / `But(...)` chain continuations |

## Gherkin Essentials
Gherkin is the lightweight syntax (Feature, Scenario, Given, When, Then, And, But). TinyBDD mirrors the structure **without parsing external .feature files**. Instead, your C# code *is* the specification.

A canonical Gherkin example:
```
Feature: Inventory display
  In order to make purchasing decisions
  As a store manager
  I want to see current item counts

  Scenario: Shows zero when empty
    Given an empty inventory
    When I open the dashboard
    Then it shows 0 items
```
Same scenario in TinyBDD:
```csharp
[Feature("Inventory display", Description = "Shows counts for purchasing decisions")]
public class InventoryTests : TinyBddXunitBase
{
    [Scenario("Shows zero when empty"), Fact]
    public async Task ShowsZero() =>
        await Given("empty inventory", () => new Inventory())
            .When("open dashboard", inv => inv.Render())
            .Then("shows 0", html => html.Contains("0 items"))
            .AssertPassed();
}
```
No external file, no code generation. Your test runner output becomes the living documentation.

## BDD vs TDD (Short Form)
TDD cycle: *Red → Green → Refactor* on tiny units. BDD broadens the frame:
1. Discuss a behavior with the business (capture language).
2. Express it as an executable example (scenario).
3. Drive implementation until the example passes.
4. Refactor while behavior stays green.

You still get the regression safety of tests, plus alignment with the domain vocabulary.

## Why TinyBDD Stays "Tiny"
Large BDD toolchains introduce friction: glue code, attribute collisions, parser quirks, context injection. TinyBDD deliberately:
- Avoids runtime reflection magic where possible.
- Embraces your test framework instead of replacing it.
- Treats steps as **simple delegates** (`Func<T, bool>`, `Func<CancellationToken, ValueTask>`, etc.).
- Doesn't mandate a feature file DSL.

Result: near‑zero ceremony. You can read the entire core source quickly.

## Mapping the Chain Internals
Every chain builds a pipeline of *step descriptors* executed sequentially:
- Each step produces a `StepResult` (title, kind, timing, optional error).
- For each executed step TinyBDD captures a `StepIO` record containing: phase (Given/When/Then), title, input object, output object.
- The latest successful output becomes `ScenarioContext.CurrentItem` so later steps (including late diagnostics) can inspect it.

## Assertions Strategy
TinyBDD's built‑in assertion surface is intentionally minimal: pass a predicate returning `bool` or an action returning `Task/ValueTask`. If you prefer richer libraries (FluentAssertions, Shouldly, etc.), call them inside a `Then` action variant.

New in the evolving API is a **deferred fluent expectation DSL** (`Expect.For` / `Expect.That`) providing English readable failure messages but only throwing once awaited, letting you compose order‑independent chains (see *Expectations & Assertions* page).

## Tags and Reporting
`[Tag("smoke")]` attributes (class or method) feed `ScenarioContext.Tags`. Adapters forward these to framework trait systems where possible. Reporting surfaces them in output or custom reporters you write via `IBddReporter`.

## When To Use BDD (and when not)
Use BDD scenarios when:
- Behavior spans multiple conceptual steps.
- You benefit from narrative clarity / onboarding.
- Examples clarify ambiguous rules.

Prefer direct unit tests when:
- The logic is a single pure function.
- Behavior narrative adds no communicative value.

Mix both: TinyBDD doesn't replace unit tests—it complements them at the behavior layer.

## Mental Model Summary
1. A Scenario is a linear pipeline.
2. Each step may transform or assert state.
3. IO and results are recorded for diagnosis and reporting.
4. Failure stops the pipeline unless `ScenarioOptions.ContinueOnError` is enabled.

Armed with these fundamentals, proceed to: *BDD + TDD Workflow* → refine your day‑to‑day loop.

