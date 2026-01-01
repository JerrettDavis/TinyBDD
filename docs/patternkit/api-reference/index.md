# API Reference

This section provides comprehensive API documentation for all PatternKit packages.

## Packages

| Package | Description |
|---------|-------------|
| [PatternKit.Core](core.md) | Core workflow engine, fluent API, behaviors, and handlers |
| [PatternKit.Extensions.DependencyInjection](dependency-injection.md) | Microsoft.Extensions.DependencyInjection integration |
| [PatternKit.Extensions.Hosting](hosting.md) | Microsoft.Extensions.Hosting integration |

## Quick Reference

### Core Types

```csharp
// Workflow entry point
Workflow.Given(context, title, () => value)

// Workflow context
var context = new WorkflowContext { WorkflowName = "MyWorkflow" };

// Workflow options
var options = new WorkflowOptions
{
    ContinueOnError = false,
    HaltOnFailedAssertion = true,
    StepTimeout = TimeSpan.FromSeconds(30)
};

// Step results
StepResult result = context.Steps[0];
bool passed = result.Passed;
TimeSpan elapsed = result.Elapsed;
```

### Fluent API

```csharp
await Workflow
    // Given phase (setup)
    .Given(context, "title", () => value)
    .And("more setup", v => transform(v))

    // When phase (action)
    .When("action", v => process(v))
    .And("more actions", v => process2(v))
    .But("exception case", v => handleException(v))

    // Then phase (assertions)
    .Then("verify", v => v.IsValid)
    .And("more checks", v => v.Property == expected)

    // Cleanup
    .Finally("cleanup", v => cleanup(v));
```

### Behaviors

```csharp
// Built-in behaviors
services.AddTimingBehavior<T>();
services.AddRetryBehavior<T>(maxRetries: 3);
services.AddCircuitBreakerBehavior<T>(failureThreshold: 5);

// Custom behavior
public class MyBehavior<T> : IBehavior<T>
{
    public ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken ct) => next(ct);
}
```

### Step Handlers

```csharp
// Define request
public record MyRequest(string Data) : IStep<MyResponse>;

// Implement handler
public class MyHandler : IStepHandler<MyRequest, MyResponse>
{
    public ValueTask<MyResponse> HandleAsync(
        MyRequest request,
        CancellationToken ct) => new(new MyResponse());
}

// Register
services.AddStepHandler<MyRequest, MyResponse, MyHandler>();

// Use in workflow
await chain.Handle("step", factory, v => new MyRequest(v.Data));
```

### Dependency Injection

```csharp
// Register services
services.AddPatternKit(options => { /* configure */ });
services.AddStepHandler<Req, Res, Handler>();
services.AddBehavior<T, MyBehavior<T>>();

// Inject and use
public class MyService
{
    private readonly IWorkflowContextFactory _factory;

    public MyService(IWorkflowContextFactory factory)
    {
        _factory = factory;
    }

    public async Task DoWorkAsync()
    {
        var context = _factory.Create("MyWorkflow");
        await Workflow.Given(context, "start", () => data)
            // ...
    }
}
```

### Hosting

```csharp
// Configure hosting
services.AddPatternKitHosting(
    configurePatternKit: opts => { },
    configureHosting: opts => { });

// Register workflows
services.AddWorkflow<MyWorkflow>();
services.AddRecurringWorkflow<MyRecurringWorkflow>();

// Run on-demand
public class MyController
{
    private readonly IWorkflowRunner _runner;

    public async Task<IActionResult> Run()
    {
        var context = await _runner.RunAsync("MyWorkflow");
        return Ok(context.AllPassed);
    }
}
```

## Namespace Reference

### PatternKit.Core

| Type | Description |
|------|-------------|
| `Workflow` | Static entry point for fluent API |
| `WorkflowContext` | Execution state container |
| `WorkflowChain<T>` | Fluent builder chain |
| `ResultChain<T>` | Terminal awaitable chain |
| `WorkflowOptions` | Execution configuration |
| `StepResult` | Step execution result |
| `StepMetadata` | Step information for hooks |
| `StepIO` | Input/output tracking |
| `StepPhase` | Given/When/Then enum |
| `StepWord` | Primary/And/But enum |
| `IBehavior<T>` | Behavior interface |
| `TimingBehavior<T>` | Timing behavior |
| `RetryBehavior<T>` | Retry behavior |
| `CircuitBreakerBehavior<T>` | Circuit breaker |
| `IStepHandler<TReq, TRes>` | Handler interface |
| `IStep<TRes>` | Request marker |
| `Unit` | Void return type |
| `IStepHandlerFactory` | Handler factory |
| `DefaultStepHandlerFactory` | Dictionary-based factory |
| `IWorkflowExtension` | Extension marker |
| `WorkflowStepException` | Step failure exception |
| `WorkflowAssertionException` | Assertion failure |
| `CircuitBreakerOpenException` | Circuit open exception |

### PatternKit.Extensions.DependencyInjection

| Type | Description |
|------|-------------|
| `ServiceCollectionExtensions` | DI registration extensions |
| `PatternKitOptions` | DI configuration options |
| `IWorkflowContextFactory` | Context factory interface |
| `DefaultWorkflowContextFactory` | Default factory implementation |
| `ServiceProviderExtension` | Service provider extension |
| `ServiceProviderStepHandlerFactory` | DI handler factory |

### PatternKit.Extensions.Hosting

| Type | Description |
|------|-------------|
| `HostBuilderExtensions` | Host builder extensions |
| `WorkflowHostingOptions` | Hosting configuration |
| `IWorkflowDefinition` | Workflow definition interface |
| `IWorkflowDefinition<T>` | Typed result workflow |
| `IRecurringWorkflowDefinition` | Recurring workflow |
| `IWorkflowRunner` | On-demand runner interface |
| `WorkflowRunner` | Default runner implementation |
| `WorkflowHostedService` | Background service |
