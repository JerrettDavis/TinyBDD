# PatternKit Documentation

PatternKit is a high-performance, AoT-compatible fluent workflow DSL for .NET that enables you to express domain requirements as executable code. Built on the proven Given-When-Then pattern from behavior-driven development, PatternKit serves as general-purpose application plumbing for both testing and production scenarios.

## What is PatternKit?

PatternKit transforms the familiar BDD (Behavior-Driven Development) syntax into a powerful workflow orchestration engine:

```csharp
await Workflow.Given(context, "a user account", () => new User("alice@example.com"))
    .When("the user logs in", user => authService.Authenticate(user))
    .Then("the session is created", session => session.IsValid);
```

Unlike traditional BDD frameworks that are tightly coupled to testing, PatternKit is designed as **architectural infrastructure** that works equally well in:

- **Unit and Integration Tests** - Express test cases as readable specifications
- **API Request Pipelines** - Build validation and processing workflows
- **Background Services** - Define scheduled or event-driven workflows
- **Business Process Automation** - Model complex domain workflows

## Key Features

### Fluent Given-When-Then API

Write workflows that read like natural language specifications:

```csharp
await Workflow.Given(ctx, "an order with items", () => CreateOrder())
    .And("a valid payment method", order => order.WithPayment(visa))
    .When("the order is submitted", order => orderService.Submit(order))
    .And("payment is processed", order => paymentService.Charge(order))
    .Then("confirmation email is sent", order => emailService.WasCalled())
    .And("inventory is updated", order => inventory.Reserved(order.Items));
```

### AoT (Ahead-of-Time) Compilation Compatible

PatternKit is designed from the ground up to support Native AOT compilation:

- No reflection-based APIs
- Trimmer-safe with full annotation support
- Source generator friendly architecture
- `CallerMemberName` attributes for automatic naming

### High Performance

Optimized for minimal allocations and maximum throughput:

- `ValueTask`-first async implementation
- State-passing overloads to avoid closure allocations
- Struct-based chains where possible
- Aggressive inlining hints for hot paths

### Enterprise Integration

First-class support for Microsoft.Extensions ecosystem:

```csharp
// Dependency Injection
services.AddPatternKit()
    .AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>()
    .AddRetryBehavior<Order>(maxRetries: 3);

// Hosted Services
services.AddPatternKitHosting()
    .AddWorkflow<OrderProcessingWorkflow>()
    .AddRecurringWorkflow<InventorySyncWorkflow>();
```

### Extensible Behavior System

Cross-cutting concerns via composable behaviors:

- **Timing** - Automatic performance measurement
- **Retry** - Exponential backoff retry logic
- **Circuit Breaker** - Fault tolerance patterns
- **Custom Behaviors** - Implement `IBehavior<T>` for any cross-cutting concern

### Rich Execution Context

Full visibility into workflow execution:

```csharp
// Access step results
foreach (var step in context.Steps)
{
    Console.WriteLine($"{step.Kind} {step.Title}: {step.Elapsed}");
}

// Check execution status
if (!context.AllPassed)
{
    var failure = context.FirstFailure;
    logger.LogError("Workflow failed at: {Step}", failure?.Title);
}

// Store and retrieve metadata
context.SetMetadata("correlationId", Guid.NewGuid());
var id = context.GetMetadata<Guid>("correlationId");
```

## Documentation Structure

| Section | Description |
|---------|-------------|
| [Getting Started](getting-started.md) | Quick introduction and first workflow |
| [Core Concepts](concepts/index.md) | In-depth explanation of PatternKit fundamentals |
| [API Reference](api-reference/index.md) | Complete API documentation for all packages |
| [Samples & Tutorials](samples/index.md) | Practical examples and step-by-step guides |
| [Best Practices](best-practices.md) | Recommendations for production usage |

## Package Overview

PatternKit is distributed as multiple NuGet packages for modular adoption:

| Package | Description | Dependencies |
|---------|-------------|--------------|
| `PatternKit.Core` | Core workflow engine and fluent API | None (zero dependencies) |
| `PatternKit.Extensions.DependencyInjection` | Microsoft.Extensions.DependencyInjection integration | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `PatternKit.Extensions.Hosting` | `IHostBuilder` and `BackgroundService` integration | `Microsoft.Extensions.Hosting.Abstractions` |

## Requirements

- **.NET 8.0** or later (for full AoT support)
- **.NET Standard 2.1** compatible for broader reach
- **C# 12** or later recommended

## Quick Example

Here's a complete example showing PatternKit in action:

```csharp
using PatternKit.Core;

// Create a workflow context
var context = new WorkflowContext { WorkflowName = "CalculatorTest" };

// Define and execute a workflow
var result = await Workflow
    .Given(context, "two numbers", () => (a: 10, b: 5))
    .When("they are added", nums => nums.a + nums.b)
    .Then("the sum is correct", sum => sum == 15)
    .GetResultAsync();

// Check results
Console.WriteLine($"Result: {result}");  // Result: 15
Console.WriteLine($"All Passed: {context.AllPassed}");  // All Passed: True

// Inspect step execution
foreach (var step in context.Steps)
{
    Console.WriteLine($"  [{step.Kind}] {step.Title} - {step.Elapsed.TotalMilliseconds:F2}ms");
}
```

Output:
```
Result: 15
All Passed: True
  [Given] two numbers - 0.05ms
  [When] they are added - 0.02ms
  [Then] the sum is correct - 0.01ms
```

## Getting Help

- **GitHub Issues**: Report bugs or request features at the project repository
- **Discussions**: Ask questions and share ideas in GitHub Discussions
- **API Reference**: Comprehensive documentation for all public APIs

## Next Steps

- [Get Started with PatternKit](getting-started.md) - Create your first workflow
- [Learn Core Concepts](concepts/index.md) - Understand the fundamentals
- [Explore Samples](samples/index.md) - See practical examples
