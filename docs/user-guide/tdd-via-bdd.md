# BDD + TDD Workflow

> How to apply Test Driven Development practices through Behavior Driven scenarios using TinyBDD.

Classic TDD: Red → Green → Refactor for a *unit*. BDD layers a conversation and narrative on top. You still iterate quickly—but the outer loop is a behavior slice.

## Dual Loops (Inner vs Outer)
```
Outer (Behavior) Loop: Discuss → Capture Scenario → Drive to Pass → Review Example
Inner (Code) Loop:    Write Failing Step Impl → Make It Pass → Refactor
```
A scenario often encapsulates several inner TDD cycles while you acquire just enough domain logic to satisfy the example.

## Practical Flow
1. Agree on a single scenario (business + dev):
   - Name it using domain language.
   - Identify Givens (state), When (action), Thens (observables).
2. Write the scenario skeleton in code FIRST:
```csharp
await Given("empty cart", () => new Cart())
    .When("add apple", c => { c.Add("apple"); return c; })
    .Then("total items == 1", c => c.Count == 1)
    .AssertPassed(); // Fails initially
```
3. Run: it fails (no implementation / failing predicate).
4. Implement the minimum to pass.
5. Refactor: simplify internals—scenario stays green.
6. Add a contrasting scenario (different branch / edge case).
7. Repeat until feature intent feels *exhaustively explained by examples*.

## When to Split Scenarios
A single scenario should read like a concise example, not an integration script. Split when:
- More than ~5 logical steps.
- Multiple unrelated business rules being asserted.
- Conditional branches appear mid‑flow ("If user is admin ...").

## Outside‑In with TinyBDD
You can drive implementation from the top (scenario) downward:
1. Write scenario calling *hypothetical* domain APIs.
2. Let the compiler guide stubbing them.
3. Use unit tests underneath if a domain component becomes algorithmically complex.
4. Keep scenario language stable—rename only with domain alignment changes.

## Reducing Feedback Time
- Prefer `ValueTask` and synchronous overloads when possible to avoid unnecessary Task allocation.
- Use in‑memory fakes instead of external resources until later hardening passes.
- Keep Given light: heavy setup belongs in builders or test data factories.

## Evolving Assertions
Start with quick predicates, then upgrade to richer assertions where clarity adds value:
```csharp
.Then("has single line item", cart => cart.Items.Single().Sku == "apple")
```
Later you may switch to deferred fluent style:
```csharp
.Then("apple line item", cart => Expect.For(cart.Items.Single(), "item").ToSatisfy(i => i.Sku == "apple"))
```
Because `Expect.For(...)` returns an awaitable assertion (ValueTask convertible) TinyBDD treats it like any other step delegate.

## Handling Refactors Safely
Refactor underneath passing scenarios. If refactor changes *observable behavior wording* update titles to remain truthful. Preserve domain vocabulary consistency—rename both code and scenario titles together.

## Triangulation Strategy
Add scenarios that:
- Cover happy path
- Cover boundary (zero / max / empty)
- Cover error / invalid input surface
- Cover concurrency or race aspects (optionally using parallel Given seeds)

## Anti‑Patterns
| Smell | Why it's harmful | Fix |
| ----- | ---------------- | --- |
| Giant scenario (10+ steps) | Hard to parse intent | Split into focused examples |
| Multiple assertions in one Then with no clear theme | Obscures failure meaning | Use distinct Then or And steps |
| Hidden side effects inside assertions | Blurs boundaries | Keep side effects in When steps |
| Rebuilding large object graphs manually | Noise, brittle | Use factories/builders in Given |

## Deciding What NOT to Specify
Avoid encoding every micro rule: if a detail is sufficiently covered by lower level unit tests and its failure wouldn't mislead a reader at the behavior level, keep it out of the scenario.

## Lifecycle Summary
1. Capture one scenario.
2. Drive it red → green using minimal domain code.
3. Refactor internals.
4. Add contrast scenario(s).
5. Repeat until behavioral surface is confident and expressive.

Proceed to: [Expectations & Assertions](assertions-and-expectations.md)

