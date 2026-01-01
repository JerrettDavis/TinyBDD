# Getting Started with PatternKit

This guide walks you through installing PatternKit and creating your first workflow. By the end, you'll understand the basic concepts and be ready to explore more advanced features.

## Prerequisites

- .NET 8.0 SDK or later
- A code editor (Visual Studio, VS Code, or Rider recommended)

## Installation

### Package Manager Console

```powershell
# Core package only
Install-Package PatternKit.Core

# With DI support
Install-Package PatternKit.Extensions.DependencyInjection

# With hosting support (includes DI)
Install-Package PatternKit.Extensions.Hosting
```

### .NET CLI

```bash
# Core package only
dotnet add package PatternKit.Core

# With DI support
dotnet add package PatternKit.Extensions.DependencyInjection

# With hosting support (includes DI)
dotnet add package PatternKit.Extensions.Hosting
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="PatternKit.Core" Version="1.0.0" />
  <!-- Optional: Add if using dependency injection -->
  <PackageReference Include="PatternKit.Extensions.DependencyInjection" Version="1.0.0" />
  <!-- Optional: Add if using hosted services -->
  <PackageReference Include="PatternKit.Extensions.Hosting" Version="1.0.0" />
</ItemGroup>
```

## Your First Workflow

Let's create a simple workflow that validates a user registration:

### Step 1: Create a Console Application

```bash
dotnet new console -n PatternKitDemo
cd PatternKitDemo
dotnet add package PatternKit.Core
```

### Step 2: Write Your First Workflow

Replace the contents of `Program.cs`:

```csharp
using PatternKit.Core;

// Create the workflow context
var context = new WorkflowContext
{
    WorkflowName = "UserRegistration",
    Description = "Validates user registration data"
};

// Define the user data
var registrationData = new
{
    Email = "alice@example.com",
    Password = "SecureP@ss123",
    Age = 25
};

// Build and execute the workflow
await Workflow
    .Given(context, "registration data", () => registrationData)
    .When("email is validated", data =>
    {
        if (!data.Email.Contains('@'))
            throw new InvalidOperationException("Invalid email format");
        return data;
    })
    .And("password strength is checked", data =>
    {
        if (data.Password.Length < 8)
            throw new InvalidOperationException("Password too weak");
        return data;
    })
    .And("age is verified", data =>
    {
        if (data.Age < 18)
            throw new InvalidOperationException("Must be 18 or older");
        return data;
    })
    .Then("all validations pass", data => true);

// Report results
Console.WriteLine($"Workflow: {context.WorkflowName}");
Console.WriteLine($"Status: {(context.AllPassed ? "PASSED" : "FAILED")}");
Console.WriteLine($"Total Time: {context.Steps.Sum(s => s.Elapsed.TotalMilliseconds):F2}ms");
Console.WriteLine();
Console.WriteLine("Steps:");
foreach (var step in context.Steps)
{
    var status = step.Passed ? "✓" : "✗";
    Console.WriteLine($"  {status} [{step.Kind}] {step.Title} ({step.Elapsed.TotalMilliseconds:F2}ms)");
}
```

### Step 3: Run the Workflow

```bash
dotnet run
```

Expected output:
```
Workflow: UserRegistration
Status: PASSED
Total Time: 0.15ms

Steps:
  ✓ [Given] registration data (0.05ms)
  ✓ [When] email is validated (0.03ms)
  ✓ [And] password strength is checked (0.02ms)
  ✓ [And] age is verified (0.02ms)
  ✓ [Then] all validations pass (0.03ms)
```

## Understanding the Workflow Structure

PatternKit workflows follow the **Given-When-Then** pattern:

### Given - Setup Phase

The `Given` step establishes preconditions and initial state:

```csharp
Workflow.Given(context, "title", () => initialValue)
```

- Creates the starting value for the workflow
- Multiple `Given` steps can be chained with `And`
- Think of this as the "Arrange" in AAA (Arrange-Act-Assert)

### When - Action Phase

The `When` step performs the primary action or transformation:

```csharp
.When("title", previousValue => transformedValue)
```

- Transforms or processes the current value
- Chain multiple actions with `And` or `But`
- Think of this as the "Act" in AAA

### Then - Assertion Phase

The `Then` step verifies the expected outcome:

```csharp
.Then("title", value => value.SomeProperty == expected)
```

- Returns `true` if the assertion passes
- Can also use action-based assertions that throw on failure
- Think of this as the "Assert" in AAA

## Handling Async Operations

PatternKit has first-class support for async operations:

```csharp
await Workflow
    .Given(context, "a user id", () => userId)
    .When("user is fetched from database", async id =>
    {
        return await userRepository.GetByIdAsync(id);
    })
    .And("user profile is loaded", async user =>
    {
        user.Profile = await profileService.LoadAsync(user.Id);
        return user;
    })
    .Then("user has a profile", user => user.Profile != null);
```

All step types support:
- Synchronous lambdas: `x => result`
- `Task<T>` returning lambdas: `async x => await ...`
- `ValueTask<T>` returning lambdas: `x => new ValueTask<T>(result)`
- Cancellation token support: `(x, ct) => ...`

## Using WorkflowOptions

Configure workflow behavior with `WorkflowOptions`:

```csharp
var context = new WorkflowContext
{
    WorkflowName = "ConfiguredWorkflow",
    Options = new WorkflowOptions
    {
        ContinueOnError = true,        // Continue after non-assertion errors
        HaltOnFailedAssertion = false, // Continue after assertion failures
        StepTimeout = TimeSpan.FromSeconds(30), // Per-step timeout
        MarkRemainingAsSkippedOnFailure = true  // Mark skipped steps
    }
};
```

## Accessing Step Results

After workflow execution, inspect the results:

```csharp
// Check overall status
if (context.AllPassed)
{
    Console.WriteLine("All steps passed!");
}
else
{
    var failure = context.FirstFailure;
    Console.WriteLine($"Failed at: {failure?.Title}");
    Console.WriteLine($"Error: {failure?.Error?.Message}");
}

// Iterate through all steps
foreach (var step in context.Steps)
{
    Console.WriteLine($"{step.Kind} {step.Title}");
    Console.WriteLine($"  Duration: {step.Elapsed}");
    Console.WriteLine($"  Passed: {step.Passed}");
    if (step.Error != null)
    {
        Console.WriteLine($"  Error: {step.Error.Message}");
    }
}

// Calculate total elapsed time
var totalTime = context.TotalElapsed();
Console.WriteLine($"Total execution time: {totalTime}");
```

## Using with Test Frameworks

PatternKit works seamlessly with any test framework:

### xUnit

```csharp
public class CalculatorTests
{
    [Fact]
    public async Task Addition_ReturnsCorrectSum()
    {
        var context = new WorkflowContext { WorkflowName = nameof(Addition_ReturnsCorrectSum) };

        await Workflow
            .Given(context, "two numbers", () => (a: 5, b: 3))
            .When("added together", nums => nums.a + nums.b)
            .Then("result is 8", result => result == 8)
            .AssertPassed();
    }
}
```

### NUnit

```csharp
[TestFixture]
public class CalculatorTests
{
    [Test]
    public async Task Subtraction_ReturnsCorrectDifference()
    {
        var context = new WorkflowContext { WorkflowName = nameof(Subtraction_ReturnsCorrectDifference) };

        await Workflow
            .Given(context, "two numbers", () => (a: 10, b: 4))
            .When("subtracted", nums => nums.a - nums.b)
            .Then("result is 6", result => result == 6)
            .AssertPassed();
    }
}
```

### MSTest

```csharp
[TestClass]
public class CalculatorTests
{
    [TestMethod]
    public async Task Multiplication_ReturnsCorrectProduct()
    {
        var context = new WorkflowContext { WorkflowName = nameof(Multiplication_ReturnsCorrectProduct) };

        await Workflow
            .Given(context, "two numbers", () => (a: 6, b: 7))
            .When("multiplied", nums => nums.a * nums.b)
            .Then("result is 42", result => result == 42)
            .AssertPassed();
    }
}
```

## Next Steps

Now that you understand the basics, explore these topics:

1. **[Core Concepts](concepts/index.md)** - Deep dive into workflows, behaviors, and handlers
2. **[Dependency Injection Integration](concepts/dependency-injection.md)** - Use with Microsoft.Extensions.DependencyInjection
3. **[Hosting Integration](concepts/hosting.md)** - Run workflows as background services
4. **[Samples](samples/index.md)** - Real-world examples and patterns
5. **[API Reference](api-reference/index.md)** - Complete API documentation

## Common Patterns

### Avoiding Closures with State-Passing

For performance-critical code, use state-passing overloads:

```csharp
var config = new Configuration();

await Workflow
    .Given(context, "data", config, (cfg) => cfg.InitialValue)
    .When("processed", config, (value, cfg) => value * cfg.Multiplier)
    .Then("within range", config, (value, cfg) => value < cfg.MaxValue);
```

### Extracting the Final Value

Get the workflow result after execution:

```csharp
var result = await Workflow
    .Given(context, "input", () => 10)
    .When("doubled", x => x * 2)
    .Then("positive", x => x > 0)
    .GetResultAsync();

Console.WriteLine($"Final value: {result}"); // Final value: 20
```

### Using Cleanup Steps

Add cleanup logic that runs regardless of success:

```csharp
await Workflow
    .Given(context, "a connection", () => OpenConnection())
    .When("data is fetched", conn => conn.Query("SELECT * FROM users"))
    .Then("results returned", data => data.Any())
    .Finally("connection closed", conn => conn.Dispose());
```
