# Basic Workflows

This guide covers fundamental workflow patterns and the core fluent API.

## Creating Your First Workflow

### Minimal Example

```csharp
using PatternKit.Core;

// Create context
var context = new WorkflowContext { WorkflowName = "HelloWorld" };

// Build and execute workflow
await Workflow
    .Given(context, "a greeting", () => "Hello")
    .When("appended with world", greeting => $"{greeting}, World!")
    .Then("contains world", message => message.Contains("World"));

// Check results
Console.WriteLine($"Passed: {context.AllPassed}");
```

### With Description and Options

```csharp
var context = new WorkflowContext
{
    WorkflowName = "Calculator",
    Description = "Tests basic arithmetic operations",
    Options = new WorkflowOptions
    {
        ContinueOnError = false,
        HaltOnFailedAssertion = true
    }
};
```

## Value Transformations

### Type-Changing Transforms

```csharp
await Workflow
    .Given(context, "a number string", () => "42")
    .When("parsed to int", s => int.Parse(s))           // string → int
    .When("converted to double", i => (double)i)        // int → double
    .When("formatted", d => $"Value: {d:F2}")           // double → string
    .Then("has expected format", s => s == "Value: 42.00");
```

### Chained Transforms

```csharp
await Workflow
    .Given(context, "raw data", () => "  hello world  ")
    .When("trimmed", s => s.Trim())
    .And("uppercased", s => s.ToUpperInvariant())
    .And("split", s => s.Split(' '))
    .Then("has two words", words => words.Length == 2);
```

## Side Effects

### Action-Based Steps

```csharp
var logs = new List<string>();

await Workflow
    .Given(context, "initial value", () => 10)
    .When("log value", value =>
    {
        logs.Add($"Processing: {value}");
    })
    .When("double", value => value * 2)
    .When("log result", value =>
    {
        logs.Add($"Result: {value}");
    })
    .Then("logged twice", _ => logs.Count == 2);
```

### Async Side Effects

```csharp
await Workflow
    .Given(context, "order", () => order)
    .When("save to database", async order =>
    {
        await _repository.SaveAsync(order);
    })
    .And("send notification", async order =>
    {
        await _notificationService.NotifyAsync(order.CustomerId, "Order saved");
    })
    .Then("persisted", _ => true);
```

## Assertion Patterns

### Predicate Assertions

```csharp
await Workflow
    .Given(context, "user", () => new User { Age = 25, Email = "test@example.com" })
    .When("validate", user => user)
    .Then("is adult", user => user.Age >= 18)
    .And("has valid email", user => user.Email.Contains("@"))
    .And("is not premium", user => !user.IsPremium);
```

### Action Assertions

```csharp
await Workflow
    .Given(context, "numbers", () => new[] { 1, 2, 3 })
    .When("calculate sum", nums => nums.Sum())
    .Then("sum is 6", sum =>
    {
        if (sum != 6)
            throw new AssertionException($"Expected 6 but got {sum}");
    });
```

### Multiple Assertions

```csharp
await Workflow
    .Given(context, "order", () => CreateOrder())
    .When("process", order => ProcessOrder(order))
    .Then("status is complete", order => order.Status == "Complete")
    .And("has order id", order => order.Id != null)
    .And("has timestamp", order => order.ProcessedAt != default)
    .And("items preserved", order => order.Items.Count > 0);
```

## Contrasting Cases with But

```csharp
await Workflow
    .Given(context, "valid order", () => new Order { Total = 100 })
    .When("apply discount", order =>
    {
        order.Discount = 10;
        order.Total -= order.Discount;
        return order;
    })
    .But("tax is added", order =>
    {
        order.Tax = order.Total * 0.08m;
        order.Total += order.Tax;
        return order;
    })
    .Then("final total correct", order =>
        order.Total == 97.20m); // (100 - 10) * 1.08 = 97.20
```

## Async Operations

### Task-Based Async

```csharp
await Workflow
    .Given(context, "user id", () => userId)
    .When("fetch user", async id => await _userRepo.GetByIdAsync(id))
    .And("fetch orders", async user =>
    {
        user.Orders = await _orderRepo.GetByUserIdAsync(user.Id);
        return user;
    })
    .And("fetch preferences", async user =>
    {
        user.Preferences = await _prefRepo.GetByUserIdAsync(user.Id);
        return user;
    })
    .Then("fully loaded", user =>
        user.Orders != null && user.Preferences != null);
```

### With Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await Workflow
    .Given(context, "data source", () => dataSource)
    .When("fetch data", async (source, ct) =>
    {
        return await source.FetchAsync(ct);
    })
    .When("process data", async (data, ct) =>
    {
        return await ProcessAsync(data, ct);
    })
    .Then("completed", result => result.Success)
    .GetResultAsync(cts.Token);
```

### ValueTask Optimization

```csharp
// Use ValueTask for potentially synchronous completions
await Workflow
    .Given(context, "cached key", () => cacheKey)
    .When("check cache", key =>
    {
        // May complete synchronously if cached
        if (_cache.TryGetValue(key, out var value))
            return new ValueTask<Data>(value);
        return new ValueTask<Data>(FetchFromDbAsync(key));
    })
    .Then("has data", data => data != null);
```

## Getting Results

### Awaiting the Chain

```csharp
// Simply await - executes and returns final value
var result = await Workflow
    .Given(context, "input", () => 5)
    .When("process", x => x * 2)
    .Then("positive", x => x > 0);

Console.WriteLine($"Result: {result}"); // Result: 10
```

### Explicit GetResultAsync

```csharp
var chain = Workflow
    .Given(context, "input", () => 5)
    .When("process", x => x * 2)
    .Then("positive", x => x > 0);

// Explicit method call
var result = await chain.GetResultAsync();
```

### AssertPassed and AssertFailed

```csharp
// Throws if any step failed
await Workflow
    .Given(context, "data", () => data)
    .When("validate", d => Validate(d))
    .Then("valid", d => d.IsValid)
    .AssertPassed();

// Throws if all steps passed (for negative testing)
await Workflow
    .Given(context, "invalid data", () => invalidData)
    .When("validate", d => Validate(d))
    .Then("should fail", d => d.IsValid)
    .AssertFailed();
```

## Cleanup with Finally

### Basic Cleanup

```csharp
await Workflow
    .Given(context, "connection", () => OpenConnection())
    .When("query", conn => conn.Query("SELECT * FROM users"))
    .Then("has results", results => results.Any())
    .Finally("close connection", conn => conn.Close());
```

### Async Cleanup

```csharp
await Workflow
    .Given(context, "temp file", () => CreateTempFile())
    .When("write data", file =>
    {
        File.WriteAllText(file, "test data");
        return file;
    })
    .Then("file exists", file => File.Exists(file))
    .Finally("delete file", async file =>
    {
        await Task.Run(() => File.Delete(file));
    });
```

### Multiple Cleanup Steps

```csharp
await Workflow
    .Given(context, "resources", () => AcquireResources())
    .When("process", resources => Process(resources))
    .Then("success", result => result.Success)
    .Finally("release resource A", res => res.A.Dispose())
    .Finally("release resource B", res => res.B.Dispose())
    .Finally("log completion", _ => _logger.Log("Workflow complete"));
```

## State-Passing (Performance Optimization)

### Avoiding Closures

```csharp
// With closure (allocates)
var config = GetConfig();
await Workflow
    .Given(context, "input", () => input)
    .When("process", value => value * config.Multiplier)  // Captures 'config'
    .Then("valid", value => value < config.MaxValue);     // Captures 'config'

// Without closure (zero allocation)
var config = GetConfig();
await Workflow
    .Given(context, "input", config, cfg => input)
    .When("process", config, (value, cfg) => value * cfg.Multiplier)
    .Then("valid", config, (value, cfg) => value < cfg.MaxValue);
```

### State Object Pattern

```csharp
var state = new ProcessingState
{
    Config = GetConfig(),
    Logger = GetLogger(),
    Metrics = GetMetrics()
};

await Workflow
    .Given(context, "data", state, (s) => LoadData())
    .When("transform", state, (data, s) =>
    {
        s.Logger.Log("Transforming");
        return Transform(data, s.Config);
    })
    .When("validate", state, (data, s) =>
    {
        s.Metrics.RecordValidation();
        return Validate(data, s.Config);
    })
    .Then("success", state, (result, s) =>
    {
        s.Logger.Log($"Result: {result.Status}");
        return result.IsValid;
    });
```

## Inspecting Results

### Step Results

```csharp
await Workflow
    .Given(context, "start", () => 1)
    .When("double", x => x * 2)
    .Then("check", x => x == 2);

foreach (var step in context.Steps)
{
    Console.WriteLine($"[{step.Kind}] {step.Title}");
    Console.WriteLine($"  Elapsed: {step.Elapsed.TotalMilliseconds:F2}ms");
    Console.WriteLine($"  Passed: {step.Passed}");
}
```

### Input/Output Tracking

```csharp
foreach (var io in context.IO)
{
    Console.WriteLine($"[{io.Kind}] {io.Title}");
    Console.WriteLine($"  Input:  {io.Input}");
    Console.WriteLine($"  Output: {io.Output}");
}
```

### Metadata Access

```csharp
// Store during execution
await Workflow
    .Given(context, "data", () =>
    {
        context.SetMetadata("startTime", DateTime.UtcNow);
        return LoadData();
    })
    .When("process", data =>
    {
        context.SetMetadata("itemCount", data.Count);
        return Process(data);
    })
    .Then("complete", result =>
    {
        context.SetMetadata("endTime", DateTime.UtcNow);
        return result.Success;
    });

// Access after execution
var startTime = context.GetMetadata<DateTime>("startTime");
var itemCount = context.GetMetadata<int>("itemCount");
var duration = context.GetMetadata<DateTime>("endTime") - startTime;
Console.WriteLine($"Processed {itemCount} items in {duration}");
```

## Continuing After Then

```csharp
await Workflow
    .Given(context, "order", () => CreateOrder())
    .When("validate", order => Validate(order))
    .Then("is valid", order => order.IsValid)
    // Continue with more steps
    .When("process payment", order => ProcessPayment(order))
    .Then("payment successful", order => order.PaymentStatus == "Success")
    // And more
    .When("ship", order => Ship(order))
    .Then("shipped", order => order.ShipmentId != null);
```

## Error Scenarios

### Handling Step Exceptions

```csharp
var context = new WorkflowContext
{
    WorkflowName = "ErrorHandling",
    Options = new WorkflowOptions { ContinueOnError = true }
};

try
{
    await Workflow
        .Given(context, "data", () => data)
        .When("might fail", data =>
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return data;
        })
        .Then("processed", _ => true);
}
catch (WorkflowStepException ex)
{
    Console.WriteLine($"Step failed: {ex.Message}");
    Console.WriteLine($"Context: {ex.Context.WorkflowName}");
}
```

### Inspecting Failures

```csharp
await Workflow
    .Given(context, "data", () => data)
    .When("step 1", _ => { throw new Exception("Step 1 failed"); return 1; })
    .When("step 2", _ => 2)  // Skipped if HaltOnFailedAssertion
    .Then("done", _ => true);

if (!context.AllPassed)
{
    var failure = context.FirstFailure;
    Console.WriteLine($"First failure: {failure?.Title}");
    Console.WriteLine($"Error: {failure?.Error?.Message}");

    foreach (var failed in context.GetFailedSteps())
    {
        Console.WriteLine($"Failed: {failed.Kind} {failed.Title}");
    }
}
```
