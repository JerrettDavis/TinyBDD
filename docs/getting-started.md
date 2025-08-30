# Getting Started


This guide gets you productive with TinyBDD in minutes: install the package, pick your style (explicit vs ambient), and write a few scenarios.

## Prerequisites
- .NET 8 or 9 (TinyBDD targets net9.0 in this repo; it works with net8.0 too)
- A unit test framework: xUnit, NUnit, or MSTest

## Install
<img src="images/tinyBDD.png" alt="TinyBDD" width="110" align="right" />

Pick your framework adapter plus the core library.
- xUnit
  ```bash
  dotnet add package TinyBDD
  dotnet add package TinyBDD.Xunit
  ```
- NUnit
  ```bash
  dotnet add package TinyBDD
  dotnet add package TinyBDD.NUnit
  ```
- MSTest
  ```bash
  dotnet add package TinyBDD
  dotnet add package TinyBDD.MSTest
  ```

## Two ways to use TinyBDD
1) Explicit (no base class)
- Create a ScenarioContext and pass it to Bdd.Given/When/Then.
  ```csharp
  [Feature("Math")]
  public class CalculatorTests
  {
      [Scenario("Add numbers"), Fact] // or [Test] / [TestMethod]
      public async Task Add()
      {
          var ctx = Bdd.CreateContext(this);

          await Bdd.Given(ctx, "numbers", () => new[]{1,2,3})
                   .When("sum", (arr, _) => Task.FromResult(arr.Sum()))
                   .Then("> 0", sum => sum > 0);

          ctx.AssertPassed();
      }
  }
  ```

2) Ambient (leaner syntax)
- Set Ambient.Current once per test and call Flow.Given/When/Then, or inherit a base class that does it for you.
  ```csharp
  await Flow.Given(() => 1)
            .When("double", x => x * 2)
            .Then("== 2", v => v == 2);
  ```

## Adapter quick starts
xUnit
- Inherit TinyBddXunitBase and use the ambient Flow API.
  ```csharp
  using TinyBDD.Xunit;
  using Xunit;
  using Xunit.Abstractions;

  [Feature("Math")]
  public class CalculatorTests : TinyBddXunitBase
  {
      public CalculatorTests(ITestOutputHelper output) : base(output) { }

      [Scenario("Double value"), Fact]
      public async Task Doubles()
      {
          await Flow.Given(() => 2)
                    .When("double", x => x * 2)
                    .Then("== 4", v => v == 4);

          Scenario.AssertPassed();
      }
  }
  ```

NUnit
- Inherit TinyBddNUnitBase and use the ambient Flow API.
  ```csharp
  using NUnit.Framework;
  using TinyBDD.NUnit;

  [Feature("Math")]
  public class CalculatorTests : TinyBddNUnitBase
  {
      [Scenario("Double value")]
      [Test]
      public async Task Doubles()
      {
          await Flow.Given(() => 2)
                    .When("double", x => x * 2)
                    .Then("== 4", v => v == 4);

          Scenario.AssertPassed();
      }
  }
  ```

MSTest
- Inherit TinyBddMsTestBase and use the ambient Flow API.
  ```csharp
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using TinyBDD.MSTest;

  [TestClass]
  [Feature("Math")]
  public class CalculatorTests : TinyBddMsTestBase
  {
      [Scenario("Double value")]
      [TestMethod]
      public async Task Doubles()
      {
          await Flow.Given(() => 2)
                    .When("double", x => x * 2)
                    .Then("== 4", v => v == 4);

          Scenario.AssertPassed();
      }
  }
  ```

## Fluent basics
- Given produces a value (or not), When can transform or perform a side effect, Then asserts.
- All steps have async and sync overloads; CancellationToken variants exist where useful.
- Predicates and actions:
  - Action-based assertions: Then("ok", () => Task.CompletedTask)
  - Predicate-based assertions: Then("x == 2", v => v == 2)
  - If a predicate returns false, TinyBDD throws BddAssertException for you
- Default titles:
  - Many overloads provide sensible default titles (e.g., "When action", "Then assertion"). You can always supply explicit titles.

## Attributes and tags
- [Feature(name, description?)] on a class names the feature and optional description
- [Scenario(name?, params string[] tags?)] on a method names the scenario and adds inline tags
- [Tag("smoke")] can be applied to the feature class or scenario methods
- Tags are recorded in ScenarioContext.Tags and forwarded to your test framework via an ITraitBridge

## Reporting
- Base classes emit a Gherkin-style summary to the test output automatically:
  ```
  Feature: Math
  Scenario: Double value
    Given numbers [OK] 0 ms
    When double [OK] 0 ms
    Then == 4 [OK] 0 ms
  ```
- Manual reporting:
  ```csharp
  var ctx = Bdd.CreateContext(this);
  // run steps...
  var reporter = new StringBddReporter();
  GherkinFormatter.Write(ctx, reporter);
  Console.WriteLine(reporter.ToString());
  ```

## Troubleshooting
- Error: "TinyBDD ambient ScenarioContext not set"
  - You called Flow.Given/When/Then without setting Ambient.Current. Inherit the adapter base class or set Ambient.Current.Value = Bdd.CreateContext(this) at the start of the test.
- Tags don’t show as Traits/Categories
  - Most frameworks require attributes for discovery-time traits; bridges log tags to output at runtime. For discovery filtering, also add native [Trait]/[Category] attributes.
- Parallel tests
  - Ambient.Current uses AsyncLocal, so each async flow has its own context. It’s safe to run tests in parallel.

## Next steps
- Read the Introduction for philosophy and design notes
- Explore the API reference for full overloads and XML examples
- Check adapters (TinyBDD.Xunit/NUnit/MSTest) for base classes and logging bridges
