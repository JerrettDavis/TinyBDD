# Steps and Phases

PatternKit workflows consist of discrete steps organized into phases. This document explains how steps work and how to use them effectively.

## Step Phases

Every step belongs to one of three phases:

### Given (Setup Phase)

The **Given** phase establishes preconditions and initial state:

```csharp
Workflow.Given(context, "a user account", () => new User("alice"))
    .And("with premium membership", user => user.AddMembership("premium"))
    .And("with 1000 points", user => { user.Points = 1000; return user; })
```

Use Given for:
- Creating test data
- Setting up dependencies
- Establishing initial state
- Configuring the system under test

### When (Action Phase)

The **When** phase performs the primary action or behavior:

```csharp
.When("the user makes a purchase", user => store.Purchase(user, item))
.And("points are calculated", order => calculator.Calculate(order))
.But("discount is not applied", order => { order.Discount = 0; return order; })
```

Use When for:
- Invoking the behavior under test
- Executing business logic
- Performing transformations
- Triggering side effects

### Then (Assertion Phase)

The **Then** phase verifies expected outcomes:

```csharp
.Then("the order is created", order => order.Id != null)
.And("points were deducted", order => order.User.Points < 1000)
.And("confirmation email was sent", order => emailService.WasSent(order.User))
```

Use Then for:
- Verifying state changes
- Checking return values
- Validating side effects
- Asserting invariants

## Step Words

Within each phase, steps are connected with keywords:

### Primary

The first step in each phase uses the primary keyword (`Given`, `When`, or `Then`):

```csharp
.Given(...)  // Primary Given
.When(...)   // Primary When
.Then(...)   // Primary Then
```

### And

Adds additional steps in the same phase:

```csharp
.Given("first", () => value)
.And("second", v => v.WithMore())    // Also in Given phase
.And("third", v => v.WithEvenMore()) // Also in Given phase
```

### But

Indicates a contrasting or exceptional case:

```csharp
.When("order submitted", o => Submit(o))
.But("inventory check fails", o =>
{
    o.InventoryStatus = "Unavailable";
    return o;
})
```

`But` is semantically equivalent to `And` but communicates intent better when expressing exceptions or contrasts.

## StepResult

Each executed step produces a `StepResult`:

```csharp
public readonly record struct StepResult
{
    public required string Kind { get; init; }    // "Given", "When", "Then", "And", "But"
    public required string Title { get; init; }   // Human-readable description
    public TimeSpan Elapsed { get; init; }        // Execution duration
    public Exception? Error { get; init; }        // Exception if failed
    public bool Passed => Error is null;          // Computed success status
}
```

### Accessing Results

```csharp
await workflow;

foreach (var step in context.Steps)
{
    Console.WriteLine($"[{step.Kind}] {step.Title}");
    Console.WriteLine($"  Duration: {step.Elapsed.TotalMilliseconds:F2}ms");
    Console.WriteLine($"  Status: {(step.Passed ? "Passed" : "Failed")}");

    if (step.Error != null)
    {
        Console.WriteLine($"  Error: {step.Error.Message}");
    }
}
```

### Filtering Results

```csharp
// Get only failed steps
var failures = context.GetFailedSteps().ToList();

// Get steps by phase
var givenSteps = context.Steps.Where(s => s.Kind == "Given");
var whenSteps = context.Steps.Where(s => s.Kind == "When" || s.Kind == "And" || s.Kind == "But");
var thenSteps = context.Steps.Where(s => s.Kind == "Then");

// Calculate total time
var totalTime = context.TotalElapsed();
```

## StepIO

The `StepIO` record tracks input/output for each step:

```csharp
public readonly record struct StepIO
{
    public string Kind { get; init; }    // Display keyword
    public string Title { get; init; }   // Step title
    public object? Input { get; init; }  // Value received
    public object? Output { get; init; } // Value produced
}
```

### Tracing Data Flow

```csharp
await Workflow
    .Given(context, "number", () => 5)
    .When("doubled", x => x * 2)
    .When("as string", x => $"Value: {x}")
    .Then("not empty", s => s.Length > 0);

foreach (var io in context.IO)
{
    Console.WriteLine($"[{io.Kind}] {io.Title}");
    Console.WriteLine($"  Input:  {io.Input}");
    Console.WriteLine($"  Output: {io.Output}");
}
```

Output:
```
[Given] number
  Input:  (null)
  Output: 5
[When] doubled
  Input:  5
  Output: 10
[When] as string
  Input:  10
  Output: Value: 10
[Then] not empty
  Input:  Value: 10
  Output: Value: 10
```

## StepMetadata

The `StepMetadata` record is passed to hooks before step execution:

```csharp
public readonly record struct StepMetadata
{
    public string Kind { get; init; }     // Display keyword string
    public string Title { get; init; }    // Step title
    public StepPhase Phase { get; init; } // Given/When/Then enum
    public StepWord Word { get; init; }   // Primary/And/But enum
}
```

### Using in Hooks

```csharp
pipeline.BeforeStep = (context, metadata) =>
{
    var isAssertion = metadata.Phase == StepPhase.Then;
    Console.WriteLine($"Executing {metadata.Kind} {metadata.Title}");
    Console.WriteLine($"  Phase: {metadata.Phase}");
    Console.WriteLine($"  Is assertion: {isAssertion}");
};
```

## Assertion Steps

### Predicate Assertions

Return `bool` to indicate pass/fail:

```csharp
.Then("value is positive", x => x > 0)
.And("value is even", x => x % 2 == 0)
.And("value in range", x => x >= 0 && x <= 100)
```

When a predicate returns `false`:
- The step is recorded with a `WorkflowAssertionException`
- If `HaltOnFailedAssertion` is true (default), execution stops
- The context's `AllPassed` becomes `false`

### Action Assertions

Throw exceptions on failure:

```csharp
.Then("value matches expected", value =>
{
    if (value != expected)
        throw new AssertionException($"Expected {expected} but got {value}");
})
```

Use action assertions for:
- Complex validation logic
- Framework-specific assertions (NUnit's `Assert.That`, xUnit's `Assert.Equal`)
- Custom error messages

### Framework Integration

```csharp
// xUnit
.Then("equals expected", value =>
{
    Assert.Equal(expected, value);
})

// NUnit
.Then("equals expected", value =>
{
    Assert.That(value, Is.EqualTo(expected));
})

// FluentAssertions
.Then("equals expected", value =>
{
    value.Should().Be(expected);
})
```

## Transformation Steps

Steps that transform the value:

```csharp
// Type-changing transform
.When("parse", (string s) => int.Parse(s))          // string → int

// Same-type transform
.When("increment", (int x) => x + 1)                // int → int

// Complex transform
.When("create order", (Cart cart) => new Order      // Cart → Order
{
    Items = cart.Items.ToList(),
    Total = cart.Total
})
```

## Effect Steps

Steps that perform side effects without changing the value:

```csharp
.When("log entry", (Order order) =>
{
    logger.Log($"Processing order {order.Id}");
})  // Returns void, order value continues unchanged

.And("notify", async (Order order) =>
{
    await notificationService.NotifyAsync(order);
})  // Async void variant
```

## Finally Steps

Cleanup steps that always execute:

```csharp
.Finally("cleanup", value =>
{
    CleanupResources(value);
})

.Finally("async cleanup", async value =>
{
    await CleanupAsync(value);
})

// With state-passing
.Finally("cleanup with state", state, (value, state) =>
{
    state.Cleanup(value);
})
```

Finally steps:
- Always execute, even if previous steps failed
- Execute in the order they were added
- Don't affect pass/fail status
- Can be async

## Step Titles

### Explicit Titles

```csharp
.Given("a configured service", () => new Service())
.When("the method is called", service => service.Call())
.Then("the result is valid", result => result.IsValid)
```

### Auto-Generated Titles

Some overloads generate titles automatically:

```csharp
// Title generated from type: "Service"
.Given(context, () => new Service())

// Title generated from expression (if supported)
.When(x => x.Process())  // Title: "Process"
```

### Title Best Practices

1. **Use present tense for Given**: "a user exists", "the database contains records"
2. **Use active voice for When**: "the service processes the request", "user submits form"
3. **Use declarative statements for Then**: "the result is valid", "an email was sent"
4. **Be specific but concise**: "user with admin role" not just "user"
5. **Avoid implementation details**: "order is placed" not "OrderService.PlaceOrder called"

## Step Timeout

Configure per-step timeouts:

```csharp
var context = new WorkflowContext
{
    WorkflowName = "TimedWorkflow",
    Options = new WorkflowOptions
    {
        StepTimeout = TimeSpan.FromSeconds(10)
    }
};

await Workflow
    .Given(context, "data", () => data)
    .When("long operation", async data =>
    {
        // This will timeout after 10 seconds
        return await LongRunningOperation(data);
    })
    .Then("completed", result => result != null);
```

When a step times out:
- A `TimeoutException` is recorded as the step's error
- The step is marked as failed
- Execution behavior depends on `ContinueOnError` setting

## Phase Inheritance

`And` and `But` inherit the phase from the previous step:

```csharp
await Workflow
    .Given(context, "setup", () => 1)      // Phase: Given
    .And("more setup", x => x + 1)         // Phase: Given (inherited)
    .When("action", x => x * 2)            // Phase: When
    .And("more action", x => x + 1)        // Phase: When (inherited)
    .But("exception", x => x - 1)          // Phase: When (inherited)
    .Then("check", x => x > 0)             // Phase: Then
    .And("more checks", x => x < 100);     // Phase: Then (inherited)
```

This allows natural chaining while maintaining phase semantics.
