# Troubleshooting & FAQ

This guide addresses common issues, configuration pitfalls, and debugging techniques for TinyBDD scenarios.

## Common Errors

### "TinyBDD ambient ScenarioContext not set"

**Symptom**: Exception thrown when calling `Given()`, `When()`, or `Then()` without context.

**Cause**: Using the ambient API (Flow methods) without setting up the ambient context.

**Solutions**:

1. **Inherit from a base class** (recommended):

```csharp
// Good: Base class sets up ambient context
public class MyTests : TinyBddXunitBase
{
    public MyTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Works with ambient"), Fact]
    public async Task WorksWithAmbient()
    {
        await Given(() => 1) // Ambient context is set
            .When("double", x => x * 2)
            .Then("equals 2", x => x == 2)
            .AssertPassed();
    }
}
```

2. **Set ambient context manually**:

```csharp
// Alternative: Manually set ambient context
public class MyTests
{
    [Scenario("Manual ambient setup"), Fact]
    public async Task ManualAmbientSetup()
    {
        var ctx = Bdd.CreateContext(this);
        Ambient.Current.Value = ctx;
        
        await Given(() => 1)
            .When("double", x => x * 2)
            .Then("equals 2", x => x == 2)
            .AssertPassed();
    }
}
```

3. **Use explicit context** (no ambient):

```csharp
// Alternative: Use explicit context API
public class MyTests
{
    [Scenario("Explicit context"), Fact]
    public async Task ExplicitContext()
    {
        var ctx = Bdd.CreateContext(this);
        
        await Bdd.Given(ctx, "number", () => 1)
            .When("double", x => x * 2)
            .Then("equals 2", x => x == 2)
            .AssertPassed();
    }
}
```

### "Assertion failed" with No Details

**Symptom**: Assertion fails but error message lacks context.

**Cause**: Using boolean predicate without descriptive title.

**Solution**: Use fluent expectations for better error messages:

```csharp
// Avoid: Minimal error context
await Given(() => user)
    .Then("user valid", u => u.Email.Contains("@"))
    .AssertPassed();
// Error: "Assertion failed: user valid"

// Better: Fluent expectation with context
await Given(() => user)
    .Then("email is valid", u => 
        Expect.For(u.Email, "user email")
            .Because("all users must have valid email addresses")
            .ToSatisfy(email => email.Contains("@"), "contain @ symbol"))
    .AssertPassed();
// Error: "expected user email to contain @ symbol, but was 'invalidemail' because all users must have valid email addresses"
```

### Scenario Steps Not Appearing in Test Output

**Symptom**: Test passes/fails but BDD steps don't show in output.

**Cause**: Reporter not configured or base class not used.

**Solutions**:

1. **Inherit from framework base class**:

```csharp
// Ensures reporter is configured
public class MyTests : TinyBddXunitBase
{
    public MyTests(ITestOutputHelper output) : base(output) { }
}
```

2. **Manual reporter setup**:

```csharp
[Fact]
public async Task ManualReporting()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.Given(ctx, "number", () => 1)
        .When("double", x => x * 2)
        .Then("equals 2", x => x == 2)
        .AssertPassed();
    
    // Manually write report
    var reporter = new StringBddReporter();
    GherkinFormatter.Write(ctx, reporter);
    _output.WriteLine(reporter.ToString());
}
```

### Tags Don't Appear as Traits/Categories

**Symptom**: `[Tag]` attributes don't show up in test framework's trait/category filters.

**Cause**: Most frameworks require native attributes for discovery-time filtering.

**Solution**: Use both TinyBDD tags and framework-specific attributes:

```csharp
// xUnit
[Scenario("Smoke test")]
[Tag("smoke")] // For TinyBDD reporting
[Trait("Category", "smoke")] // For xUnit filtering
[Fact]
public async Task SmokeTest() { /* ... */ }

// NUnit
[Scenario("Smoke test")]
[Tag("smoke")] // For TinyBDD reporting
[Category("smoke")] // For NUnit filtering
[Test]
public async Task SmokeTest() { /* ... */ }

// MSTest
[Scenario("Smoke test")]
[Tag("smoke")] // For TinyBDD reporting
[TestCategory("smoke")] // For MSTest filtering
[TestMethod]
public async Task SmokeTest() { /* ... */ }
```

### Parallel Test Failures

**Symptom**: Tests pass individually but fail when run in parallel.

**Cause**: Shared mutable state or ambient context conflicts.

**Solutions**:

1. **Avoid shared mutable state**:

```csharp
// Bad: Shared mutable state
private static int _counter = 0;

[Fact]
public async Task Test1()
{
    _counter++; // Race condition!
    await Given(() => _counter)
        .Then("equals 1", x => x == 1) // May fail
        .AssertPassed();
}

// Good: Isolated state
[Fact]
public async Task Test1()
{
    var counter = 0;
    counter++;
    await Given(() => counter)
        .Then("equals 1", x => x == 1)
        .AssertPassed();
}
```

2. **Use AsyncLocal for ambient context**: TinyBDD's `Ambient.Current` uses `AsyncLocal`, so each test has isolated context.

3. **Configure parallel execution**:

```xml
<!-- xUnit: Disable parallelization if needed -->
<ItemGroup>
  <None Update="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

```json
{
  "parallelizeTestCollections": false
}
```

### Finally Blocks Not Executing

**Symptom**: Finally handlers don't run or resources aren't cleaned up.

**Cause**: Exception thrown before Finally handlers are registered or pipeline doesn't complete.

**Solution**: Ensure Finally is registered early:

```csharp
// Bad: Finally might not register if Given throws
await Given("resource", () => CreateResourceThatMightThrow())
    .Finally("cleanup", r => r.Dispose())
    .When("use", r => r.DoSomething())
    .Then("works", r => true)
    .AssertPassed();

// Better: Register Finally immediately after resource creation
await Given("resource", () => 
    {
        var resource = CreateResource();
        return resource;
    })
    .Finally("cleanup", r => r.Dispose()) // Registered before any potentially failing steps
    .When("use", r => r.DoSomething())
    .Then("works", r => true)
    .AssertPassed();
```

## Configuration Issues

### Test Discovery Problems

**Symptom**: Tests don't appear in Test Explorer.

**Causes and Solutions**:

1. **Missing test framework packages**:

```bash
# Ensure test adapter is installed
dotnet add package xunit.runner.visualstudio
# or
dotnet add package NUnit3TestAdapter
# or
dotnet add package MSTest.TestAdapter
```

2. **Incorrect test attributes**:

```csharp
// xUnit requires [Fact] or [Theory]
[Scenario("Test"), Fact]
public async Task Test() { /* ... */ }

// NUnit requires [Test]
[Scenario("Test"), Test]
public async Task Test() { /* ... */ }

// MSTest requires [TestMethod] and class must have [TestClass]
[TestClass]
public class Tests : TinyBddMsTestBase
{
    [Scenario("Test"), TestMethod]
    public async Task Test() { /* ... */ }
}
```

3. **Build issues**: Clean and rebuild the solution:

```bash
dotnet clean
dotnet build
```

### Output Not Showing in CI/CD

**Symptom**: BDD output visible locally but not in CI logs.

**Solution**: Use appropriate verbosity and loggers:

```bash
# GitHub Actions
dotnet test --verbosity normal --logger "console;verbosity=detailed"

# Azure DevOps
dotnet test --logger "trx" --logger "console;verbosity=normal"

# GitLab CI
dotnet test --logger "junit;LogFilePath=test-results.xml"
```

### Scenario Outline Examples Not Running

**Symptom**: `ScenarioOutline` doesn't execute examples or all examples fail.

**Common Causes**:

1. **Forgot to call `AssertAllPassedAsync()`**:

```csharp
// Bad: Examples won't run
var outline = await Bdd.ScenarioOutline<int>(ctx, "Test")
    .Given("number", ex => ex)
    .Then("positive", n => n > 0)
    .Examples(1, 2, 3);
    // Missing AssertAllPassedAsync() or RunAsync()

// Good: Examples execute
await Bdd.ScenarioOutline<int>(ctx, "Test")
    .Given("number", ex => ex)
    .Then("positive", n => n > 0)
    .Examples(1, 2, 3)
    .AssertAllPassedAsync(); // Executes all examples
```

2. **No examples provided**:

```csharp
// Error: InvalidOperationException
await Bdd.ScenarioOutline<int>(ctx, "Test")
    .Given("number", ex => ex)
    .Then("positive", n => n > 0)
    .AssertAllPassedAsync(); // Throws: No examples provided
```

## Debugging Techniques

### Enable Diagnostic Output

Add diagnostic logging to understand execution flow:

```csharp
[Scenario("Debug scenario"), Fact]
public async Task DebugScenario()
{
    var ctx = Bdd.CreateContext(this);
    
    // Log context state
    _output.WriteLine($"Feature: {ctx.FeatureName}");
    _output.WriteLine($"Scenario: {ctx.ScenarioName}");
    
    await Bdd.Given(ctx, "value", () =>
    {
        var value = 1;
        _output.WriteLine($"Given: value={value}");
        return value;
    })
    .When("doubled", x =>
    {
        var result = x * 2;
        _output.WriteLine($"When: {x} * 2 = {result}");
        return result;
    })
    .Then("equals 2", x =>
    {
        _output.WriteLine($"Then: checking {x} == 2");
        return x == 2;
    })
    .AssertPassed();
    
    // Log steps
    foreach (var step in ctx.Steps)
    {
        _output.WriteLine($"{step.Kind} {step.Title} [{(step.Error == null ? "OK" : "FAIL")}] {step.Elapsed.TotalMilliseconds}ms");
    }
}
```

### Inspect Step IO

Examine data flow through scenario:

```csharp
[Scenario("Inspect data flow"), Fact]
public async Task InspectDataFlow()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.Given(ctx, "initial", () => 1)
        .When("transform", x => x * 2)
        .Then("verify", x => x == 2)
        .AssertPassed();
    
    // Examine IO lineage
    foreach (var io in ctx.IO)
    {
        _output.WriteLine($"{io.Kind} {io.Title}:");
        _output.WriteLine($"  Input:  {io.Input ?? "<null>"}");
        _output.WriteLine($"  Output: {io.Output ?? "<null>"}");
    }
    
    // Check current item
    _output.WriteLine($"Current Item: {ctx.CurrentItem}");
}
```

### Use Breakpoints Effectively

Set breakpoints in step lambdas:

```csharp
[Scenario("Debug with breakpoints"), Fact]
public async Task DebugWithBreakpoints()
{
    await Given("value", () => 
    {
        var x = 1; // Breakpoint here
        return x;
    })
    .When("transform", x => 
    {
        var result = x * 2; // Breakpoint here
        return result;
    })
    .Then("verify", x => 
    {
        var isValid = x == 2; // Breakpoint here
        return isValid;
    })
    .AssertPassed();
}
```

### Isolate Failing Steps

Comment out steps to isolate failures:

```csharp
[Scenario("Isolate failure"), Fact]
public async Task IsolateFailure()
{
    await Given("setup", () => Setup())
        .And("more setup", (state, ct) => MoreSetup(state))
        // .When("action that fails", x => FailingAction(x)) // Commented out
        .Then("verify", x => x != null)
        .AssertPassed();
}
```

### Capture Exceptions

Understand exception details:

```csharp
[Scenario("Exception details"), Fact]
public async Task ExceptionDetails()
{
    var ctx = Bdd.CreateContext(this);
    
    try
    {
        await Bdd.Given(ctx, "setup", () => 1)
            .When("failing step", x => throw new InvalidOperationException("Test error"))
            .Then("never reached", x => x == 1)
            .AssertPassed();
    }
    catch (BddStepException ex)
    {
        _output.WriteLine($"Step failed: {ex.Message}");
        _output.WriteLine($"Inner exception: {ex.InnerException?.Message}");
        _output.WriteLine($"Context: Feature={ctx.FeatureName}, Scenario={ctx.ScenarioName}");
        
        // Examine steps
        var failedStep = ctx.Steps.FirstOrDefault(s => s.Error != null);
        if (failedStep != null)
        {
            _output.WriteLine($"Failed step: {failedStep.Kind} {failedStep.Title}");
            _output.WriteLine($"Error: {failedStep.Error.Message}");
        }
    }
}
```

## Performance Issues

### Slow Test Execution

**Symptom**: Tests take longer than expected.

**Diagnosis**:

1. **Check step timing**:

```csharp
var slowSteps = ctx.Steps.Where(s => s.Elapsed.TotalMilliseconds > 100);
foreach (var step in slowSteps)
{
    _output.WriteLine($"Slow step: {step.Kind} {step.Title} took {step.Elapsed.TotalMilliseconds}ms");
}
```

2. **Profile setup/teardown**:

```csharp
private readonly Stopwatch _setupTimer = new();

[SetUp]
public void Setup()
{
    _setupTimer.Restart();
    // Setup code
    _setupTimer.Stop();
    Console.WriteLine($"Setup took {_setupTimer.ElapsedMilliseconds}ms");
}
```

**Solutions**:

1. **Optimize expensive operations**:

```csharp
// Bad: Expensive operation in every test
[SetUp]
public void Setup()
{
    _db = new TestDatabase();
    _db.SeedWithMillionsOfRecords(); // Very slow
}

// Good: Use cached or minimal data
private static TestDatabase _sharedDb;

[OneTimeSetUp]
public void OneTimeSetup()
{
    _sharedDb = new TestDatabase();
    _sharedDb.SeedWithMinimalData();
}
```

2. **Use parallel execution** (see Configuration Issues above)

3. **Tag slow tests**:

```csharp
[Scenario("Slow integration test")]
[Tag("slow")]
[Tag("integration")]
[Fact]
public async Task SlowIntegrationTest() { /* ... */ }
```

```bash
# Run fast tests only
dotnet test --filter "Category!=slow"
```

### High Memory Usage

**Symptom**: Tests consume excessive memory.

**Causes and Solutions**:

1. **Large test data**: Use smaller data sets or streaming:

```csharp
// Bad: Loading huge dataset
var data = LoadMillionRecords();

// Good: Use representative subset
var data = LoadSampleRecords(100);
```

2. **Resource leaks**: Ensure proper disposal:

```csharp
// Always use Finally for cleanup
await Given("resource", () => new ExpensiveResource())
    .Finally("dispose", r => r.Dispose())
    .When("use", r => r.DoWork())
    .Then("succeeds", r => true)
    .AssertPassed();
```

## Framework-Specific Issues

### xUnit: ITestOutputHelper is Null

**Symptom**: `NullReferenceException` when writing to `ITestOutputHelper`.

**Cause**: Base class constructor not called or parameter not passed.

**Solution**:

```csharp
public class MyTests : TinyBddXunitBase
{
    // Good: Pass output to base constructor
    public MyTests(ITestOutputHelper output) : base(output)
    {
    }
}
```

### NUnit: TestContext is Null

**Symptom**: `NullReferenceException` when accessing `TestContext`.

**Cause**: Accessing TestContext outside of test execution.

**Solution**: Only access TestContext within test methods or SetUp/TearDown:

```csharp
[SetUp]
public void Setup()
{
    var context = TestContext.CurrentContext; // OK here
}

[Test]
public async Task MyTest()
{
    var context = TestContext.CurrentContext; // OK here
}

// Avoid accessing TestContext in constructor or OneTimeSetUp
```

### MSTest: TestContext Not Available

**Symptom**: TestContext is null or not set.

**Cause**: TestContext property not defined or not public.

**Solution**:

```csharp
[TestClass]
public class MyTests : TinyBddMsTestBase
{
    // TestContext must be a public property named exactly "TestContext"
    public TestContext TestContext { get; set; }
    
    [TestMethod]
    public async Task MyTest()
    {
        // TestContext.WriteLine available here
    }
}
```

## FAQ

### Q: Can I use TinyBDD without inheriting a base class?

**A**: Yes, use the explicit context API:

```csharp
public class MyTests
{
    [Fact]
    public async Task ExplicitContext()
    {
        var ctx = Bdd.CreateContext(this);
        await Bdd.Given(ctx, "value", () => 1)
            .When("double", x => x * 2)
            .Then("equals 2", x => x == 2)
            .AssertPassed();
    }
}
```

### Q: How do I test async code?

**A**: TinyBDD supports async natively. All step methods have async overloads:

```csharp
await Given("async setup", async () => await LoadDataAsync())
    .When("async action", async data => await ProcessAsync(data))
    .Then("async verify", async result => await VerifyAsync(result))
    .AssertPassed();
```

### Q: Can I mix TinyBDD with regular assertions?

**A**: Yes, use action-based Then steps:

```csharp
await Given(() => user)
    .Then("user is valid", u => 
    {
        Assert.NotNull(u); // Regular assertion
        Assert.True(u.IsActive);
        u.Email.Should().Contain("@"); // FluentAssertions
    })
    .AssertPassed();
```

### Q: How do I handle expected exceptions?

**A**: Use `AssertFailed()` or catch the exception:

```csharp
// Expect the scenario to fail
await Given(() => 0)
    .When("divide", x => 10 / x) // Throws DivideByZeroException
    .Then("never reached", x => true)
    .AssertFailed();
```

### Q: Can I reuse step definitions?

**A**: Yes, create helper methods:

```csharp
private async Task<ScenarioChain<User>> GivenAuthenticatedUser()
{
    return await Given("authenticated user", async () =>
    {
        var user = await _authService.CreateUserAsync();
        await _authService.LoginAsync(user);
        return user;
    });
}

[Scenario("User can edit profile"), Fact]
public async Task UserCanEditProfile()
{
    await GivenAuthenticatedUser()
        .When("updating profile", u => _profileService.UpdateAsync(u, newData))
        .Then("profile updated", result => result.Success)
        .AssertPassed();
}
```

### Q: How do I test multiple scenarios with shared setup?

**A**: Use test class initialization:

```csharp
public class SharedSetupTests : TinyBddXunitBase, IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    
    public SharedSetupTests(ITestOutputHelper output, DatabaseFixture fixture) 
        : base(output)
    {
        _fixture = fixture;
    }
    
    [Scenario("Test 1"), Fact]
    public async Task Test1()
    {
        await Given("shared db", () => _fixture.Database)
            .When("query", db => db.Query<User>())
            .Then("users exist", users => users.Any())
            .AssertPassed();
    }
    
    [Scenario("Test 2"), Fact]
    public async Task Test2()
    {
        await Given("shared db", () => _fixture.Database)
            .When("query", db => db.Query<Order>())
            .Then("orders exist", orders => orders.Any())
            .AssertPassed();
    }
}
```

## Getting Help

If you encounter an issue not covered here:

1. **Check the GitHub repository**: Search existing issues and discussions
2. **Review sample projects**: Look at test projects in the repository
3. **Read the API documentation**: Explore the generated API docs
4. **Create an issue**: Provide a minimal reproducible example

## Next Steps

- Explore [Advanced Usage](advanced-usage.md) for advanced patterns
- See [Samples Index](samples-index.md) for working examples
- Review [Writing Scenarios](writing-scenarios.md) for best practices

