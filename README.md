# TinyBDD

[![CI](https://github.com/JerrettDavis/TinyBDD/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/TinyBDD/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/JerrettDavis/TinyBDD/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/TinyBDD)
[![CodeQL](https://github.com/JerrettDavis/TinyBDD/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/JerrettDavis/TinyBDD/security/code-scanning)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
![.NET Versions](https://img.shields.io/badge/.NET%208.0%20%7C%209.0-blue)

**NuGet Packages:**

| Package | Version | Downloads |
|--------|---------|-----------|
| **TinyBDD** | [![NuGet](https://img.shields.io/nuget/v/TinyBDD.svg)](https://www.nuget.org/packages/TinyBDD/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/TinyBDD.svg)](https://www.nuget.org/packages/TinyBDD/) |
| **TinyBDD.MSTest** | [![NuGet](https://img.shields.io/nuget/v/TinyBDD.MSTest.svg)](https://www.nuget.org/packages/TinyBDD.MSTest/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/TinyBDD.MSTest.svg)](https://www.nuget.org/packages/TinyBDD.MSTest/) |
| **TinyBDD.Xunit** | [![NuGet](https://img.shields.io/nuget/v/TinyBDD.Xunit.svg)](https://www.nuget.org/packages/TinyBDD.Xunit/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/TinyBDD.Xunit.svg)](https://www.nuget.org/packages/TinyBDD.Xunit/) |
| **TinyBDD.NUnit** | [![NuGet](https://img.shields.io/nuget/v/TinyBDD.NUnit.svg)](https://www.nuget.org/packages/TinyBDD.NUnit/) | [![NuGet Downloads](https://img.shields.io/nuget/dt/TinyBDD.NUnit.svg)](https://www.nuget.org/packages/TinyBDD.NUnit/) |

---

**TinyBDD** is a minimal, fluent **Behavior-Driven Development** library for .NET.  
It provides a lightweight `Given` / `When` / `Then` syntax with optional `And` / `But` chaining, supporting both **sync** and **async** steps.

It is designed to:

- Be **framework-agnostic** (works with MSTest, xUnit, NUnit, etc.).
- Keep scenarios **clear and concise** without heavy DSLs or external tooling.
- Support **async and sync predicates** for maximum flexibility.
- Integrate with existing test runners’ output for easy step visibility.


---

## Features

- **Readable BDD syntax**:
  ```csharp
    await Given("a number", () => 5)
        .When("doubled", x => x * 2)
        .Then(">= 10", v => v >= 10)
        .And("<= 20", v => v <= 20)
        .But("!= 15", v => v != 15)
        .AssertPassed();
  ```
- **Sync & Async Support**:

    * `Func<T>` / `Func<T, bool>`
    * `Func<Task<T>>` / `Func<T, Task<bool>>`
    * Token-aware variants for advanced control.

- **`And` / `But` chaining** with correct step names in output:

  ```
  Given start [OK]
  When double [OK]
  Then >= 10 [OK]
  And <= 20 (async) [OK]
  But != 11 [OK]
  ```

- **Test framework adapters**:

    * **MSTest**: `TinyBddMsTestBase`, `MSTestBddReporter`, `MSTestTraitBridge`
    * **xUnit**:  `TinyBddXunitBase`, `XunitTraitBridge`, `XunitBddReporter`
    * **NUnit**: `TinyBddNUnitBase`, `NUnitTraitBridge`, `NUnitBddReporter`
    * Automatically logs steps and tags to the test output.

---

## Installation

Add TinyBDD via NuGet:

```powershell
dotnet add package TinyBDD
```

For MSTest:

```powershell
dotnet add package TinyBDD.MSTest
```

For NUnit:

```powershell
dotnet add package TinyBDD.NUnit
```

For xUnit:

```powershell
dotnet add package TinyBDD.Xunit
```

---

## Basic Usage

### MSTest Example

```csharp
using TinyBDD.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[Feature("Math")]
[TestClass]
public class MathTests : TinyBddMsTestBase
{
    [Scenario("Doubling numbers")]
    [TestMethod]
    public async Task DoublingScenario()
    {
        await Given("start with 5", () => 5)
             .When("doubled", x => x * 2)
             .Then("should be 10", v => v == 10)
             .AssertPassed();
    }
}
```

---

### NUnit Example

```csharp
using TinyBDD.NUnit;
using NUnit.Framework;

[Feature("Math")]
public class MathTests : TinyBddNUnitBase
{
    [Scenario("Doubling numbers")]
    [Test]
    public async Task DoublingScenario()
    {
        await Given("start with 5", () => 5)
             .When("doubled", x => x * 2)
             .Then("should be 10", v => v == 10)
             .AssertPassed();
    }
}
```

---

### xUnit Example

```csharp
using TinyBDD.Xunit;
using Xunit;

[Feature("Math")]
public class MathTests : TinyBddXunitBase
{
    [Scenario("Doubling numbers")]
    [Fact]
    public async Task DoublingScenario()
    {
        await Given("start with 5", () => 5) 
             .When("doubled", x => x * 2)
             .Then("should be 10", v => v == 10)
             .AssertPassed();
    }
}
```

---

## Step Types

| Step    | Purpose                                     | Example                        |
|---------|---------------------------------------------|--------------------------------|
| `Given` | Initial state / setup                       | `.Given("start", () => 5)`     |
| `When`  | Action / event                              | `.When("doubled", x => x * 2)` |
| `Then`  | Assertion                                   | `.Then(">= 10", v => v >= 10)` |
| `And`   | Additional assertion after `Then` or `When` | `.And("<= 20", v => v <= 20)`  |
| `But`   | Additional assertion phrased negatively     | `.But("!= 15", v => v != 15)`  |

All step types have **sync** and **async** overloads.

---

## Cleanup with Finally

`Finally` registers cleanup handlers that execute after all steps complete, even if steps throw exceptions. This is useful for resource cleanup like disposing objects:

```csharp
await Given("a database connection", () => new SqlConnection(connectionString))
    .Finally("close connection", conn => conn.Dispose())
    .When("query data", conn => conn.Query<User>("SELECT * FROM Users"))
    .Then("results returned", users => users.Any())
    .AssertPassed();

// Connection is automatically disposed after all steps complete
```

**Key Features:**
- Finally handlers execute in registration order after all other steps
- They execute even when steps throw exceptions
- Multiple Finally handlers can be registered at different points in the chain
- Each Finally handler receives the state value at the point where it was registered
- The chain passes through the upstream value unchanged (tap semantics)

```csharp
await Given("resource A", () => new ResourceA())
    .Finally("cleanup A", a => a.Dispose())
    .When("create resource B", a => new ResourceB(a))
    .Finally("cleanup B", b => b.Dispose())
    .Then("verify", b => b.IsValid)
    .AssertPassed();

// Execution order: Given → When → Then → Finally cleanup A → Finally cleanup B
```

---

## Tags

Tags can be added for reporting and filtering:

```csharp
ctx.AddTag("smoke");
ctx.AddTag("fast");
```

In xUnit, tags are logged to the test output:

```
[TinyBDD] Tag: smoke
[TinyBDD] Tag: fast
```

---

## Asserting Pass/Fail

TinyBDD tracks step results internally. At the end of the scenario, call one of the following methods:

```csharp
Scenario.AssertPassed();

Scenario.AssertFailed();

// or use the fluent syntax:
await Given("one", () => 1)
    .When("add one", x => x + 1)
    .Then("equals two", v => v == 2)
    .AssertPassed();

await Given("one", () => 1)
    .When("add one", x => x + 1)
    .Then("equals elevent", v => v == 11)
    .AssertFailed();
```

This ensures that all steps passed and throws if any failed.

---

## Philosophy

TinyBDD was created with a few guiding principles:

1. **Focus on readability, not ceremony**  
   Steps should read like plain English and map directly to Gherkin-style thinking, but without requiring `.feature`
   files, extra compilers, or DSL preprocessors.

2. **Code is the spec**  
   Instead of writing a separate Gherkin text file, you write directly in C# using a fluent API that mirrors `Given` →
   `When` → `Then` → `And` → `But`.  
   Your unit test runner output **is** the human-readable spec.

3. **Stay out of your way**  
   TinyBDD is not an opinionated test framework; it’s a syntax layer that integrates with MSTest, xUnit, or NUnit and
   leaves assertions, test discovery, and reporting to them.

---

## Gherkin-Style Output

When running a scenario, TinyBDD prints structured step output similar to Gherkin formatting.  
For example:

```csharp
await Given("start", () => 5)
    .When("double", x => x * 2)
    .Then(">= 10", v => v >= 10)
    .And("<= 20 (async)", v => Task.FromResult(v <= 20))
    .But("!= 11", v => v != 11)
    .AssertPassed();
```

Test output:

```
Feature: Math
Scenario: Doubling numbers
  Given start [OK] 0 ms
  When double [OK] 0 ms
  Then >= 10 [OK] 0 ms
  And <= 20 (async) [OK] 0 ms
  But != 11 [OK] 0 ms
```

If a step fails, you’ll see exactly which step failed, how long it took, and the exception message.

---

## Why Not Use SpecFlow / Cucumber?

SpecFlow, Cucumber, and similar tools are powerful for large-scale BDD, but they:

* Require separate `.feature` files and a parser/runner.
* Often introduce a disconnect between the feature file and the code that actually runs.
* Come with heavier setup and slower test discovery.

TinyBDD keeps **everything in one place**—your test class—while still producing clear, human-readable steps.

---

## Minimal Example

For the smallest possible test:

```csharp
await Given("one", () => 1)
    .When("add one", x => x + 1)
    .Then("equals two", v => v == 2)
    .AssertPassed();
```

Output:

```
Given one [OK]
When add one [OK]
Then equals two [OK]
```

---

## Async Philosophy

In TinyBDD, sync and async steps are **equally first-class** citizens.

* If your step is synchronous, write it synchronously:

  ```csharp
  .When("double", x => x * 2)
  .Then("is 10", v => v == 10)
  ```

* If your step needs async work:

  ```csharp
  .When("fetch from DB", async x => await db.GetAsync(x))
  .Then("result exists", async v => Assert.NotNull(v))
  ```

You can even mix sync and async steps freely in the same scenario.

---

## Output Style

TinyBDD always prints the BDD keyword for the step type (`Given`, `When`, `Then`, `And`, `But`), the step title, the
result `[OK]` / `[FAIL]`, and the elapsed time in milliseconds.

For failed steps, TinyBDD stops the scenario immediately and prints the exception:

```
Then equals two [FAIL] 1 ms
Expected: 2
Actual:   3
```

---

## Recommended Usage

* One **scenario** per test method.
* Keep each step **single-purpose**—avoid hiding multiple unrelated actions in one step.
* Prefer creating functions, even local ones, to avoid unnecessary allocations, closure creation, garbage collection, and code cleanliness.
* Use **`Scenario.AssertPassed()`** or the fluent **`ThenChain.AssertPassed()`**  at the end of each test to ensure every step was explicitly checked.
* Use **tags** to group and filter tests.

----

## License

[MIT License](LICENSE)
