# Workflows

A workflow in PatternKit is a sequence of steps that transform data and verify outcomes. This document covers everything you need to know about creating and executing workflows.

## WorkflowContext

Every workflow operates within a `WorkflowContext`, which serves as the central container for:

- Workflow identification and metadata
- Step execution results
- Input/output tracking
- Extensions and custom data
- Configuration options

### Creating a Context

```csharp
// Minimal context
var context = new WorkflowContext { WorkflowName = "MyWorkflow" };

// Full configuration
var context = new WorkflowContext
{
    WorkflowName = "OrderProcessing",
    Description = "Processes and validates customer orders",
    Options = new WorkflowOptions
    {
        ContinueOnError = false,
        HaltOnFailedAssertion = true,
        StepTimeout = TimeSpan.FromSeconds(30)
    }
};
```

### Context Properties

| Property | Type | Description |
|----------|------|-------------|
| `WorkflowName` | `string` | Required. Unique identifier for the workflow |
| `Description` | `string?` | Optional human-readable description |
| `ExecutionId` | `string` | Auto-generated unique execution ID (8 chars) |
| `Options` | `WorkflowOptions` | Execution behavior configuration |
| `Steps` | `IReadOnlyList<StepResult>` | Results of executed steps |
| `IO` | `IReadOnlyList<StepIO>` | Input/output log per step |
| `CurrentValue` | `object?` | Current value in the pipeline |
| `AllPassed` | `bool` | Whether all steps succeeded |
| `FirstFailure` | `StepResult?` | First failed step, if any |

### Metadata Storage

Store and retrieve custom data:

```csharp
// Store metadata
context.SetMetadata("correlationId", Guid.NewGuid());
context.SetMetadata("userId", "user-123");
context.SetMetadata("startTime", DateTime.UtcNow);

// Retrieve metadata
var correlationId = context.GetMetadata<Guid>("correlationId");

// Safe retrieval
if (context.TryGetMetadata<string>("userId", out var userId))
{
    Console.WriteLine($"User: {userId}");
}
```

### Extensions

Attach framework-specific extensions:

```csharp
// Custom extension
public class LoggingExtension : IWorkflowExtension
{
    public ILogger Logger { get; init; }
}

// Attach to context
context.SetExtension(new LoggingExtension { Logger = logger });

// Retrieve later
var ext = context.GetExtension<LoggingExtension>();
ext?.Logger.LogInformation("Workflow step completed");
```

### Cleanup Registration

Register cleanup handlers:

```csharp
var connection = OpenConnection();

// Register cleanup that runs on context disposal
context.OnDispose(async () =>
{
    await connection.CloseAsync();
});

// Use the context...
await using (context)
{
    await Workflow
        .Given(context, "connection", () => connection)
        .When("query", conn => conn.Query("..."))
        .Then("has results", data => data.Any());
}
// Connection is automatically closed
```

## The Fluent Chain API

### Starting a Workflow

All workflows begin with `Workflow.Given()`:

```csharp
// With explicit title
Workflow.Given(context, "a user account", () => new User())

// With value
Workflow.Given(context, "an existing order", existingOrder)

// With state-passing (avoids closures)
Workflow.Given(context, "config value", config, cfg => cfg.Value)

// Auto-title from type name
Workflow.Given(context, () => new ShoppingCart())  // Title: "ShoppingCart"
```

### Chaining Steps

#### And - Additive Continuation

Use `And` to add more steps in the same phase:

```csharp
await Workflow
    .Given(context, "a customer", () => new Customer())
    .And("with shipping address", customer =>
    {
        customer.Address = new Address("123 Main St");
        return customer;
    })
    .And("with payment method", customer =>
    {
        customer.PaymentMethod = new CreditCard();
        return customer;
    })
    .When("order is placed", customer => PlaceOrder(customer))
    .Then("order is confirmed", order => order.Status == "Confirmed");
```

#### But - Contrasting Continuation

Use `But` to indicate a contrasting or exception case:

```csharp
await Workflow
    .Given(context, "a valid order", () => CreateOrder())
    .When("submitted", order => Submit(order))
    .But("payment fails", order =>
    {
        order.PaymentStatus = "Declined";
        return order;
    })
    .Then("order is pending", order => order.Status == "Pending");
```

### WorkflowChain<T>

The `WorkflowChain<T>` class provides the fluent API. Key characteristics:

- **Generic type `T`** - The current value type flowing through the chain
- **Immutable** - Each method returns a new chain instance
- **Deferred execution** - Steps are queued until awaited

#### Transformation Methods

```csharp
// Synchronous transform
.When("process", (T value) => newValue)

// Async Task transform
.When("fetch", async (T value) => await FetchAsync(value))

// Async ValueTask transform
.When("process", (T value) => new ValueTask<TOut>(result))

// With cancellation token
.When("fetch", async (T value, CancellationToken ct) =>
    await FetchAsync(value, ct))

// State-passing (avoids closure allocation)
.When("process", state, (T value, TState state) =>
    Process(value, state))
```

#### Effect Methods (Side Effects)

```csharp
// Synchronous effect (returns same value)
.When("log", (T value) => { Log(value); })

// Async effect
.When("save", async (T value) => { await SaveAsync(value); })
```

### ResultChain<T>

The `ResultChain<T>` is returned by `Then` methods and represents a terminal chain:

```csharp
ResultChain<int> chain = Workflow
    .Given(context, "start", () => 10)
    .When("double", x => x * 2)
    .Then("positive", x => x > 0);
```

#### Awaiting Results

```csharp
// Simply await (executes and returns value)
int result = await chain;

// Execute and assert all passed
await chain.AssertPassed();

// Execute and assert at least one failed
await chain.AssertFailed();

// Execute and get the final value explicitly
int result = await chain.GetResultAsync();
```

#### Continuing After Then

You can continue building after `Then`:

```csharp
await Workflow
    .Given(context, "order", () => order)
    .When("submitted", o => Submit(o))
    .Then("is pending", o => o.Status == "Pending")
    .And("has order id", o => o.Id != null)           // More assertions
    .When("payment processed", o => ProcessPayment(o)) // Back to When
    .Then("is complete", o => o.Status == "Complete");
```

## Step Execution

### ExecutionPipeline

The `ExecutionPipeline` manages step execution:

1. Steps are queued in order
2. Each step receives the output of the previous step
3. Timing is recorded for each step
4. Errors are captured and can halt execution
5. Hooks are invoked before/after each step

### Execution Flow

```
Queue Step 1    Queue Step 2    Queue Step 3
     │               │               │
     ▼               ▼               ▼
┌─────────────────────────────────────────┐
│            ExecutionPipeline             │
│  ┌─────┐   ┌─────┐   ┌─────┐            │
│  │ S1  │ → │ S2  │ → │ S3  │            │
│  └─────┘   └─────┘   └─────┘            │
└─────────────────────────────────────────┘
                    │
                    ▼
            ┌──────────────┐
            │  RunAsync()  │
            └──────────────┘
                    │
     ┌──────────────┼──────────────┐
     ▼              ▼              ▼
   Step 1        Step 2        Step 3
  Execute       Execute       Execute
```

### Hooks

Register hooks for step lifecycle events:

```csharp
// Access through the pipeline (advanced usage)
pipeline.BeforeStep = (context, metadata) =>
{
    Console.WriteLine($"Starting: {metadata.Kind} {metadata.Title}");
};

pipeline.AfterStep = (context, result) =>
{
    var status = result.Passed ? "✓" : "✗";
    Console.WriteLine($"{status} {result.Kind} {result.Title} ({result.Elapsed})");
};
```

## WorkflowOptions

Configure execution behavior:

```csharp
var options = new WorkflowOptions
{
    // Continue executing after non-assertion exceptions
    ContinueOnError = false,  // Default: false

    // Stop immediately when a Then predicate returns false
    HaltOnFailedAssertion = true,  // Default: true

    // Maximum time per step (null = no timeout)
    StepTimeout = TimeSpan.FromSeconds(30),  // Default: null

    // Mark remaining steps as "Skipped" when stopping
    MarkRemainingAsSkippedOnFailure = false  // Default: false
};

context.Options = options;
// Or use the default:
// context.Options = WorkflowOptions.Default;
```

### Option Effects

| Option | `true` | `false` |
|--------|--------|---------|
| `ContinueOnError` | Continue after exceptions | Stop on first exception |
| `HaltOnFailedAssertion` | Stop when Then returns false | Continue after failed assertions |
| `MarkRemainingAsSkippedOnFailure` | Record skipped steps | Don't record skipped steps |

## Finally Steps

Add cleanup that always runs:

```csharp
await Workflow
    .Given(context, "resource", () => AcquireResource())
    .When("process", resource => Process(resource))
    .Then("completed", result => result.Success)
    .Finally("release resource", resource =>
    {
        resource.Dispose();
    });
```

`Finally` steps:
- Run regardless of success or failure
- Execute in order they're added
- Don't affect the workflow pass/fail status
- Receive the current value in the chain

## Composing Workflows

### Merging Contexts

Combine results from multiple workflows:

```csharp
var mainContext = new WorkflowContext { WorkflowName = "Main" };
var subContext = new WorkflowContext { WorkflowName = "Sub" };

// Execute sub-workflow
await Workflow.Given(subContext, "data", () => data)
    .When("process", d => Process(d))
    .Then("valid", d => d.IsValid);

// Merge into main workflow
mainContext.MergeSteps(subContext);

// mainContext.Steps now contains steps from both workflows
```

### Nested Workflows

Execute workflows within workflows:

```csharp
await Workflow.Given(mainContext, "order", () => order)
    .When("validate", async order =>
    {
        var validationContext = new WorkflowContext
        {
            WorkflowName = "OrderValidation"
        };

        await Workflow
            .Given(validationContext, "order", () => order)
            .When("check inventory", o => CheckInventory(o))
            .And("check payment", o => CheckPayment(o))
            .Then("is valid", o => o.IsValid)
            .AssertPassed();

        // Merge validation steps into main
        mainContext.MergeSteps(validationContext);

        return order;
    })
    .Then("order validated", o => o.IsValid);
```

## Exception Handling

### WorkflowStepException

Thrown when a step fails with an exception:

```csharp
try
{
    await workflow;
}
catch (WorkflowStepException ex)
{
    Console.WriteLine($"Step failed: {ex.Message}");
    Console.WriteLine($"Workflow: {ex.Context.WorkflowName}");
    Console.WriteLine($"Failed step: {ex.Context.FirstFailure?.Title}");
    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
}
```

### WorkflowAssertionException

Thrown when assertions fail:

```csharp
try
{
    await workflow.AssertPassed();
}
catch (WorkflowAssertionException ex)
{
    Console.WriteLine($"Assertion failed: {ex.Message}");
}
```

### Examining Failures

```csharp
await workflow;

if (!context.AllPassed)
{
    foreach (var failure in context.GetFailedSteps())
    {
        Console.WriteLine($"Failed: {failure.Kind} {failure.Title}");
        Console.WriteLine($"  Error: {failure.Error?.Message}");
        Console.WriteLine($"  Duration: {failure.Elapsed}");
    }
}
```

## Performance Considerations

### Avoiding Closures

Use state-passing overloads for hot paths:

```csharp
// Creates closure (allocation)
var config = GetConfig();
.When("process", x => x * config.Multiplier)

// No closure (zero allocation)
.When("process", config, (x, cfg) => x * cfg.Multiplier)
```

### ValueTask

All async operations use `ValueTask` for optimal performance:

```csharp
// Synchronous completion - no Task allocation
.When("sync", x => new ValueTask<int>(x * 2))

// Async completion when needed
.When("async", async x => await FetchAsync(x))
```

### Struct-Based Chains

`ResultChain<T>` is a `readonly struct`, minimizing allocations for terminal operations.
