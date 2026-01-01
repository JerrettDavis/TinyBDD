# Best Practices

This guide covers recommended patterns, anti-patterns, and optimization strategies for using PatternKit effectively.

## Workflow Design

### Keep Workflows Focused

Each workflow should have a single, clear purpose:

```csharp
// Good - focused workflow
public class CreateOrderWorkflow : IWorkflowDefinition<Order>
{
    public string Name => "CreateOrder";

    public async ValueTask<Order> ExecuteAsync(WorkflowContext context, CancellationToken ct)
    {
        return await Workflow
            .Given(context, "order request", () => _request)
            .When("validate", r => Validate(r))
            .When("create order", r => CreateOrder(r))
            .Then("created", o => o.Id != null)
            .GetResultAsync(ct);
    }
}

// Bad - workflow doing too much
public class DoEverythingWorkflow : IWorkflowDefinition
{
    public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken ct)
    {
        // Creates orders, processes payments, sends emails,
        // updates inventory, generates reports...
    }
}
```

### Use Descriptive Step Titles

Step titles should clearly describe what happens:

```csharp
// Good - descriptive titles
await Workflow
    .Given(context, "a user with valid credentials", () => user)
    .When("the user submits login request", u => AuthService.Login(u))
    .Then("authentication succeeds", result => result.Success)
    .And("a session token is returned", result => result.Token != null);

// Bad - vague titles
await Workflow
    .Given(context, "user", () => user)
    .When("process", u => AuthService.Login(u))
    .Then("check", result => result.Success);
```

### Follow BDD Conventions

Maintain consistent language patterns:

| Phase | Convention | Examples |
|-------|------------|----------|
| Given | Past tense or state description | "a registered user", "the database contains records" |
| When | Action in present tense | "the user logs in", "the API is called" |
| Then | Expected outcome | "the response is successful", "the order is created" |

## Performance

### Avoid Closure Allocations

Use state-passing overloads in performance-critical code:

```csharp
// Allocates closure for each captured variable
var config = GetConfig();
var logger = GetLogger();
await Workflow
    .Given(context, "data", () => data)
    .When("process", d => d.Process(config))  // Captures config
    .When("log", d => { logger.Log(d); return d; });  // Captures logger

// Zero allocations with state-passing
var state = (config: GetConfig(), logger: GetLogger());
await Workflow
    .Given(context, "data", state, (s) => data)
    .When("process", state, (d, s) => d.Process(s.config))
    .When("log", state, (d, s) => { s.logger.Log(d); return d; });
```

### Use ValueTask Appropriately

```csharp
// Good - synchronous path completes without allocation
.When("check cache", key =>
{
    if (_cache.TryGetValue(key, out var value))
        return new ValueTask<Data>(value);  // No Task allocation
    return new ValueTask<Data>(LoadFromDbAsync(key));  // Async when needed
})

// Unnecessary - always creates Task
.When("check cache", async key =>
{
    if (_cache.TryGetValue(key, out var value))
        return value;
    return await LoadFromDbAsync(key);
})
```

### Batch Operations

```csharp
// Good - batch database operations
await Workflow
    .Given(context, "items", () => items)
    .When("save all", async items =>
    {
        await _db.BulkInsertAsync(items);  // Single DB call
        return items;
    })
    .Then("saved", items => items.All(i => i.Id != null));

// Bad - N+1 database calls
await Workflow
    .Given(context, "items", () => items)
    .When("save each", async items =>
    {
        foreach (var item in items)
        {
            await _db.InsertAsync(item);  // N database calls!
        }
        return items;
    });
```

## Error Handling

### Use Appropriate Exception Types

```csharp
// Domain exceptions for business rule violations
public class InsufficientInventoryException : Exception { }
public class PaymentDeclinedException : Exception { }

// Use in workflows
await Workflow
    .Given(context, "order", () => order)
    .When("check inventory", order =>
    {
        if (!HasInventory(order))
            throw new InsufficientInventoryException();
        return order;
    })
    .When("process payment", async order =>
    {
        var result = await _payment.ChargeAsync(order);
        if (!result.Success)
            throw new PaymentDeclinedException(result.Error);
        return order;
    });
```

### Handle Expected Failures Gracefully

```csharp
// Return result objects instead of throwing for expected cases
public record ValidationResult(bool IsValid, List<string> Errors);

await Workflow
    .Given(context, "request", () => request)
    .When("validate", request =>
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(request.Email))
            errors.Add("Email is required");
        if (request.Age < 18)
            errors.Add("Must be 18 or older");
        return new ValidationResult(errors.Count == 0, errors);
    })
    .Then("is valid", result =>
    {
        if (!result.IsValid)
            context.SetMetadata("validationErrors", result.Errors);
        return result.IsValid;
    });
```

### Configure Behavior for Your Use Case

```csharp
// Testing - see all failures
var testOptions = new WorkflowOptions
{
    ContinueOnError = true,
    HaltOnFailedAssertion = false,
    MarkRemainingAsSkippedOnFailure = true
};

// Production - fail fast
var prodOptions = new WorkflowOptions
{
    ContinueOnError = false,
    HaltOnFailedAssertion = true,
    StepTimeout = TimeSpan.FromSeconds(30)
};
```

## Resource Management

### Always Dispose Contexts

```csharp
// Good - proper disposal
var context = _factory.Create("Workflow");
await using (context)
{
    await Workflow
        .Given(context, "data", () => data)
        .When("process", d => Process(d))
        .Then("done", _ => true);
}

// Or with try/finally
var context = _factory.Create("Workflow");
try
{
    await Workflow...;
}
finally
{
    await context.DisposeAsync();
}
```

### Use Finally for Cleanup

```csharp
await Workflow
    .Given(context, "connection", () => OpenConnection())
    .When("execute", conn => conn.Execute(sql))
    .Then("success", _ => true)
    .Finally("close", conn => conn.Close());  // Always runs

// Register cleanup on context
context.OnDispose(async () =>
{
    await CleanupResourcesAsync();
});
```

### Scope Dependencies Properly

```csharp
public async ValueTask ExecuteAsync(WorkflowContext context, CancellationToken ct)
{
    // Get scoped service provider
    var sp = context.GetExtension<ServiceProviderExtension>()?.ServiceProvider;
    using var scope = sp?.CreateScope();
    var dbContext = scope?.ServiceProvider.GetRequiredService<AppDbContext>();

    await Workflow
        .Given(context, "data", () => LoadData(dbContext))
        .When("save", data => dbContext.SaveChangesAsync())
        .Then("saved", _ => true);
}
```

## Dependency Injection

### Register Handlers with Appropriate Lifetime

```csharp
// Stateless handlers - Singleton or Transient
services.AddStepHandler<ValidateRequest, Result, ValidateHandler>(ServiceLifetime.Singleton);

// Handlers with scoped dependencies (DbContext, etc.)
services.AddStepHandler<SaveRequest, Entity, SaveHandler>(ServiceLifetime.Scoped);

// Handlers with per-request state
services.AddStepHandler<ProcessRequest, Result, ProcessHandler>(ServiceLifetime.Transient);
```

### Use Factory for Complex Initialization

```csharp
services.AddStepHandler<ComplexRequest, Result>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ComplexHandler>>();
    var cache = sp.GetRequiredService<IDistributedCache>();

    return new ComplexHandler(
        connectionString: config.GetConnectionString("Main"),
        timeout: TimeSpan.FromSeconds(config.GetValue<int>("Timeout")),
        logger: logger,
        cache: cache);
});
```

## Behavior Design

### Keep Behaviors Composable

```csharp
// Good - focused, composable behaviors
services
    .AddBehavior<Order, LoggingBehavior<Order>>()
    .AddBehavior<Order, MetricsBehavior<Order>>()
    .AddBehavior<Order, RetryBehavior<Order>>()
    .AddBehavior<Order, CircuitBreakerBehavior<Order>>();

// Bad - monolithic behavior
services.AddBehavior<Order, DoEverythingBehavior<Order>>();
```

### Order Behaviors Correctly

```csharp
// Order matters! Outer behaviors wrap inner ones
services
    .AddBehavior<T, LoggingBehavior<T>>()        // 1st - logs all attempts
    .AddBehavior<T, MetricsBehavior<T>>()        // 2nd - records metrics
    .AddBehavior<T, RetryBehavior<T>>()          // 3rd - retries wrap circuit
    .AddBehavior<T, CircuitBreakerBehavior<T>>(); // 4th - innermost
```

## Testing

### Use Descriptive Test Names

```csharp
// Good - describes scenario and expectation
[Fact]
public async Task CreateOrder_WithValidData_ReturnsOrderWithId()

[Fact]
public async Task CreateOrder_WithInvalidEmail_ThrowsValidationException()

// Bad - vague names
[Fact]
public async Task Test1()

[Fact]
public async Task CreateOrderTest()
```

### Test Edge Cases

```csharp
[Theory]
[InlineData(null, "Email is required")]
[InlineData("", "Email is required")]
[InlineData("not-an-email", "Invalid email format")]
[InlineData("test@", "Invalid email format")]
public async Task Validate_InvalidEmail_ReturnsError(string email, string expectedError)
{
    var context = new WorkflowContext { WorkflowName = "ValidateEmailTest" };

    await Workflow
        .Given(context, "invalid email", () => email)
        .When("validate", e => ValidateEmail(e))
        .Then("returns expected error", result =>
            result.Errors.Contains(expectedError))
        .AssertPassed();
}
```

### Mock External Dependencies

```csharp
[Fact]
public async Task ProcessPayment_GatewayError_RecordsFailure()
{
    var gateway = new Mock<IPaymentGateway>();
    gateway
        .Setup(g => g.ChargeAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new PaymentGatewayException("Gateway unavailable"));

    var context = new WorkflowContext { WorkflowName = "PaymentFailure" };
    var processor = new PaymentProcessor(gateway.Object);

    try
    {
        await processor.ProcessAsync(context, new Order { Total = 100 });
    }
    catch (PaymentGatewayException)
    {
        // Expected
    }

    Assert.False(context.AllPassed);
    Assert.Contains(context.Steps, s => s.Kind == "When" && !s.Passed);
}
```

## Anti-Patterns to Avoid

### Don't Use Workflows for Simple Operations

```csharp
// Overkill - simple operation doesn't need workflow
var context = new WorkflowContext { WorkflowName = "Add" };
var result = await Workflow
    .Given(context, "numbers", () => (1, 2))
    .When("add", n => n.Item1 + n.Item2)
    .Then("check", r => r == 3)
    .GetResultAsync();

// Just do it directly
var result = 1 + 2;
```

### Don't Nest Workflows Unnecessarily

```csharp
// Bad - unnecessary nesting
await Workflow
    .Given(context, "data", () => data)
    .When("process", async d =>
    {
        var innerContext = new WorkflowContext { WorkflowName = "Inner" };
        await Workflow
            .Given(innerContext, "inner", () => d)
            .When("transform", x => Transform(x))
            .Then("valid", x => x.IsValid);
        return d;
    });

// Good - use composition or sequential steps
await Workflow
    .Given(context, "data", () => data)
    .When("transform", d => Transform(d))
    .Then("valid", d => d.IsValid);
```

### Don't Ignore Context Disposal

```csharp
// Bad - context never disposed, cleanup handlers never run
public async Task ProcessAsync()
{
    var context = _factory.Create("Process");
    await Workflow.Given(context, "data", () => data)...;
    // Context leaked!
}

// Good - proper disposal
public async Task ProcessAsync()
{
    var context = _factory.Create("Process");
    await using (context)
    {
        await Workflow.Given(context, "data", () => data)...;
    }
}
```

### Don't Mix Concerns in Step Handlers

```csharp
// Bad - handler does too much
public class DoEverythingHandler : IStepHandler<CreateOrderRequest, Order>
{
    public async ValueTask<Order> HandleAsync(CreateOrderRequest request, CancellationToken ct)
    {
        // Validates, creates, processes payment, sends email, updates inventory...
    }
}

// Good - focused handlers
public class CreateOrderHandler : IStepHandler<CreateOrderRequest, Order> { }
public class ProcessPaymentHandler : IStepHandler<ProcessPaymentRequest, PaymentResult> { }
public class SendEmailHandler : IStepHandler<SendEmailRequest, Unit> { }
```

## Summary

| Area | Do | Don't |
|------|-----|-------|
| **Design** | Keep workflows focused | Create god-workflows |
| **Titles** | Use descriptive, consistent titles | Use vague or cryptic titles |
| **Performance** | Use state-passing for hot paths | Capture unnecessary closures |
| **Errors** | Use appropriate exception types | Swallow exceptions silently |
| **Resources** | Always dispose contexts | Leak resources |
| **DI** | Match lifetime to dependency needs | Use wrong lifetimes |
| **Behaviors** | Keep focused and composable | Create monolithic behaviors |
| **Testing** | Test edge cases | Only test happy path |
