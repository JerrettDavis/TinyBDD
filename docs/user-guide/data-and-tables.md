# Data and Tables

This guide covers working with data in TinyBDD scenarios, from simple parameterized steps to complex table-driven tests and data validation patterns.

## Data-Driven Tests: Two Approaches

TinyBDD provides two powerful approaches for data-driven testing: `Bdd.Scenario()` with `ForEachAsync()` for flexible iteration, and `Bdd.ScenarioOutline<T>()` for type-safe, Gherkin-style examples.

### Approach 1: Bdd.Scenario() with ForEachAsync()

The `Bdd.Scenario()` approach provides a flexible way to iterate over examples with full control over the scenario for each row:

```csharp
[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    public CalculatorTests(ITestOutputHelper output) : base(output) { }

    [Scenario("Adding numbers with examples"), Fact]
    public async Task AddingNumbersWithExamples()
    {
        var ctx = Bdd.CreateContext(this);
        
        var result = await Bdd.Scenario(ctx, "Addition examples", 
                (a: 1, b: 2, expected: 3),
                (a: 5, b: 5, expected: 10),
                (a: -1, b: 1, expected: 0),
                (a: 0, b: 0, expected: 0))
            .ForEachAsync(row =>
                Bdd.Given(ctx, $"numbers {row.Data.a} and {row.Data.b}", () => (row.Data.a, row.Data.b))
                    .When("added together", nums => nums.Item1 + nums.Item2)
                    .Then($"equals {row.Data.expected}", sum => sum == row.Data.expected));
        
        // All examples pass
        Assert.True(result.AllPassed);
        Assert.Equal(4, result.TotalCount);
    }
}
```

**Key Features:**
- Access `row.Data` for the current example's data
- Access `row.Index` for the zero-based index (0, 1, 2, ...)
- Returns `ExamplesResult` with detailed execution results
- Use `ForEachAsync()` for full scenario control per row
- Use `AssertAllPassedAsync()` to assert and throw on first failure

**Example with Row Index:**

```csharp
[Scenario("Track execution order"), Fact]
public async Task TrackExecutionOrder()
{
    var ctx = Bdd.CreateContext(this);
    var executionOrder = new List<int>();
    
    await Bdd.Scenario(ctx, "Row indices", "first", "second", "third")
        .ForEachAsync(row =>
        {
            executionOrder.Add(row.Index);
            return Bdd.Given(ctx, $"row {row.Index}", () => row.Data)
                .Then("not empty", s => !string.IsNullOrEmpty(s));
        });
    
    // Indices are 0, 1, 2
    Assert.Equal(new[] { 0, 1, 2 }, executionOrder);
}
```

**Example with Anonymous Types:**

```csharp
[Scenario("String operations"), Fact]
public async Task StringOperations()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.Scenario(ctx, "String transformations",
            new { input = "hello", operation = "upper", expected = "HELLO" },
            new { input = "WORLD", operation = "lower", expected = "world" },
            new { input = "  trim  ", operation = "trim", expected = "trim" })
        .ForEachAsync(row =>
            Bdd.Given(ctx, "input string", () => row.Data.input)
                .When($"apply {row.Data.operation}", s => row.Data.operation switch
                {
                    "upper" => s.ToUpper(),
                    "lower" => s.ToLower(),
                    "trim" => s.Trim(),
                    _ => s
                })
                .Then("matches expected", s => s == row.Data.expected));
}
```

**Handling Results:**

```csharp
// ForEachAsync returns ExamplesResult
var result = await Bdd.Scenario(ctx, "Test", 1, 2, 3)
    .ForEachAsync(row => /* ... */);

// Check results
Console.WriteLine($"Total: {result.TotalCount}");
Console.WriteLine($"Passed: {result.PassedCount}");
Console.WriteLine($"Failed: {result.FailedCount}");
Console.WriteLine($"All passed: {result.AllPassed}");

// Or assert all passed (throws if any failed)
result.AssertAllPassed();

// Alternative: AssertAllPassedAsync immediately throws on failure
await Bdd.Scenario(ctx, "Test", 1, 2, 3)
    .AssertAllPassedAsync(row => /* ... */);
```

### Approach 2: ScenarioOutline<T>()

The `ScenarioOutline<T>()` approach provides a more Gherkin-aligned, type-safe API where example data is accessible throughout the chain:

```csharp
[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    public CalculatorTests(ITestOutputHelper output) : base(output) { }

    [Scenario("Adding two numbers"), Fact]
    public async Task AddingTwoNumbers()
    {
        var ctx = Bdd.CreateContext(this);
        
        await Bdd.ScenarioOutline<(int a, int b, int expected)>(ctx, "Addition examples")
            .Given("first number", ex => ex.a)
            .And("second number", (_, ex) => ex.b)
            .When("added together", (a, b) => a + b)
            .Then("result equals expected", (sum, ex) => sum == ex.expected)
            .Examples(
                (a: 1, b: 2, expected: 3),
                (a: 5, b: 5, expected: 10),
                (a: -1, b: 1, expected: 0),
                (a: 0, b: 0, expected: 0))
            .AssertAllPassedAsync();
    }
}
```

**Key Features:**
- Type-safe access to example data via `ex` parameter
- Steps can access example data: `(value, ex) => ...`
- Cleaner syntax for scenarios following Gherkin structure
- Use `RunAsync()` for detailed results or `AssertAllPassedAsync()` to assert

### Choosing Between Approaches

| Use Case | Recommended Approach |
|----------|---------------------|
| Need row index or custom logic per example | `Bdd.Scenario()` + `ForEachAsync()` |
| Different scenario structure per example | `Bdd.Scenario()` + `ForEachAsync()` |
| Dynamic step titles based on example | `Bdd.Scenario()` + `ForEachAsync()` |
| Gherkin-style examples with fixed structure | `ScenarioOutline<T>()` |
| Type-safe access to example fields | `ScenarioOutline<T>()` |
| Complex conditions based on example data | `ScenarioOutline<T>()` |

## Parameterized Steps

The simplest form of data-driven testing is passing parameters through your scenario chain. TinyBDD naturally supports this through its fluent API.

### Basic Parameter Passing

```csharp
[Scenario("Calculate order total"), Fact]
public async Task CalculateOrderTotal()
{
    await Given("items with prices", () => new[] { 10m, 20m, 30m })
        .When("calculating total", prices => prices.Sum())
        .Then("total is correct", total => total == 60m)
        .AssertPassed();
}
```

### Complex Object Parameters

```csharp
public record OrderItem(string Name, decimal Price, int Quantity);

[Scenario("Calculate line item total"), Fact]
public async Task CalculateLineItemTotal()
{
    await Given("an order item", () => new OrderItem("Widget", 15.99m, 3))
        .When("calculating line total", item => item.Price * item.Quantity)
        .Then("total is correct", total => total == 47.97m)
        .AssertPassed();
}
```

### Transforming Parameters

Use When steps to transform data as it flows through the scenario:

```csharp
[Scenario("Apply discount to cart"), Fact]
public async Task ApplyDiscountToCart()
{
    await Given("cart with items", () => new Cart
        {
            Items = new[] 
            {
                new CartItem("Widget", 100m),
                new CartItem("Gadget", 50m)
            }
        })
        .And("discount code", (cart, ct) => 
        {
            var discount = new DiscountCode("SAVE20", 0.20m);
            return Task.FromResult((cart, discount));
        })
        .When("applying discount", state => 
        {
            state.cart.ApplyDiscount(state.discount);
            return state.cart;
        })
        .Then("total reflects discount", cart => cart.Total == 120m)
        .AssertPassed();
}
```

## Table Data Binding

When working with multiple related data sets, table-like structures make tests more readable and maintainable.

### List of Records

Use C# records or classes to represent table rows:

```csharp
public record TestUser(string Email, string Role, bool IsActive);

[Scenario("Validate user permissions"), Fact]
public async Task ValidateUserPermissions()
{
    var users = new[]
    {
        new TestUser("admin@example.com", "Admin", true),
        new TestUser("user@example.com", "User", true),
        new TestUser("guest@example.com", "Guest", true),
        new TestUser("inactive@example.com", "User", false)
    };
    
    await Given("test users", () => users)
        .When("checking admin access", userList => 
            userList.Where(u => u.Role == "Admin" && u.IsActive))
        .Then("only active admins have access", admins => admins.Count() == 1)
        .And("admin email is correct", admins => 
            admins.First().Email == "admin@example.com")
        .AssertPassed();
}
```

### Dictionary-Based Tables

For flexible key-value data:

```csharp
[Scenario("Configuration validation"), Fact]
public async Task ConfigurationValidation()
{
    var config = new Dictionary<string, string>
    {
        ["ApiUrl"] = "https://api.example.com",
        ["Timeout"] = "30",
        ["RetryCount"] = "3",
        ["EnableLogging"] = "true"
    };
    
    await Given("application config", () => config)
        .Then("API URL is valid", cfg => 
            Uri.TryCreate(cfg["ApiUrl"], UriKind.Absolute, out _))
        .And("timeout is numeric", cfg => int.TryParse(cfg["Timeout"], out _))
        .And("retry count is reasonable", cfg => 
            int.Parse(cfg["RetryCount"]) is >= 0 and <= 10)
        .AssertPassed();
}
```

### Tuple-Based Tables

For lightweight, related data without defining classes:

```csharp
[Scenario("Currency conversion"), Fact]
public async Task CurrencyConversion()
{
    var rates = new[]
    {
        (From: "USD", To: "EUR", Rate: 0.85m),
        (From: "USD", To: "GBP", Rate: 0.73m),
        (From: "EUR", To: "GBP", Rate: 0.86m)
    };
    
    await Given("exchange rates", () => rates)
        .And("amount to convert", (rateTable, ct) => 
            Task.FromResult((rateTable, amount: 100m)))
        .When("converting USD to EUR", state =>
        {
            var rate = state.rateTable.First(r => r.From == "USD" && r.To == "EUR");
            return state.amount * rate.Rate;
        })
        .Then("converted amount is correct", result => result == 85m)
        .AssertPassed();
}
```

## Scenario Outlines with Tables

Scenario outlines are ideal for table-driven tests where you run the same logic against multiple data rows.

### Data Matrix Testing

```csharp
public record LoginAttempt(string Username, string Password, bool ShouldSucceed, string Reason);

[Scenario("Login validation matrix"), Fact]
public async Task LoginValidationMatrix()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.ScenarioOutline<LoginAttempt>(ctx, "Login attempts")
        .Given("credentials", ex => new Credentials(ex.Username, ex.Password))
        .When("attempting login", creds => _authService.LoginAsync(creds))
        .Then("result matches expected", (result, ex) => 
            result.IsSuccess == ex.ShouldSucceed)
        .Examples(
            new LoginAttempt("valid@example.com", "correct", true, "valid credentials"),
            new LoginAttempt("valid@example.com", "wrong", false, "wrong password"),
            new LoginAttempt("invalid@example.com", "any", false, "unknown user"),
            new LoginAttempt("", "any", false, "empty username"),
            new LoginAttempt("valid@example.com", "", false, "empty password"))
        .AssertAllPassedAsync();
}
```

### Boundary Value Testing

Test edge cases and boundaries systematically:

```csharp
[Scenario("Age validation boundaries"), Fact]
public async Task AgeValidationBoundaries()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.ScenarioOutline<(int age, bool isValid, string description)>(ctx, "Age boundaries")
        .Given("user age", ex => ex.age)
        .When("validating", age => _validator.ValidateAge(age))
        .Then("validation result is correct", (result, ex) => 
            result.IsValid == ex.isValid)
        .Examples(
            (-1, false, "negative age"),
            (0, false, "zero age"),
            (1, true, "minimum valid age"),
            (17, false, "under 18"),
            (18, true, "exactly 18"),
            (19, true, "over 18"),
            (120, true, "maximum reasonable age"),
            (121, false, "over maximum age"))
        .AssertAllPassedAsync();
}
```

### Equivalence Class Testing

Group similar inputs that should behave the same way:

```csharp
public record EmailTest(string Email, bool IsValid, string EquivalenceClass);

[Scenario("Email format validation"), Fact]
public async Task EmailFormatValidation()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.ScenarioOutline<EmailTest>(ctx, "Email validation")
        .Given("email address", ex => ex.Email)
        .When("validating format", email => _validator.ValidateEmail(email))
        .Then("validation matches expected", (result, ex) => 
            result.IsValid == ex.IsValid)
        .Examples(
            new EmailTest("user@example.com", true, "valid standard"),
            new EmailTest("user.name@example.com", true, "valid with dot"),
            new EmailTest("user+tag@example.com", true, "valid with plus"),
            new EmailTest("user@sub.example.com", true, "valid subdomain"),
            new EmailTest("invalid", false, "missing @"),
            new EmailTest("@example.com", false, "missing local part"),
            new EmailTest("user@", false, "missing domain"),
            new EmailTest("user @example.com", false, "contains space"),
            new EmailTest("", false, "empty string"))
        .AssertAllPassedAsync();
}
```

## Data Validation Patterns

### Multi-Field Validation

Validate multiple fields on the same object:

```csharp
public record Address(string Street, string City, string State, string ZipCode);

[Scenario("Address validation"), Fact]
public async Task AddressValidation()
{
    await Given("address to validate", () => new Address(
            "123 Main St", 
            "Springfield", 
            "IL", 
            "62701"))
        .Then("street is not empty", addr => !string.IsNullOrWhiteSpace(addr.Street))
        .And("city is not empty", addr => !string.IsNullOrWhiteSpace(addr.City))
        .And("state is two letters", addr => 
            addr.State.Length == 2 && addr.State.All(char.IsLetter))
        .And("zip code is five digits", addr => 
            addr.ZipCode.Length == 5 && addr.ZipCode.All(char.IsDigit))
        .AssertPassed();
}
```

### Collection Validation

Work with collections of data:

```csharp
[Scenario("Order items validation"), Fact]
public async Task OrderItemsValidation()
{
    var items = new[]
    {
        new OrderItem("Widget", 10m, 2),
        new OrderItem("Gadget", 20m, 1),
        new OrderItem("Doohickey", 15m, 3)
    };
    
    await Given("order with items", () => new Order { Items = items })
        .Then("all items have positive prices", order => 
            order.Items.All(item => item.Price > 0))
        .And("all items have positive quantities", order => 
            order.Items.All(item => item.Quantity > 0))
        .And("total quantity is correct", order => 
            order.Items.Sum(item => item.Quantity) == 6)
        .And("total amount is correct", order => 
            order.Items.Sum(item => item.Price * item.Quantity) == 85m)
        .AssertPassed();
}
```

### Conditional Validation

Apply different validation based on data values:

```csharp
public record PaymentRequest(string Type, decimal Amount, string? CardNumber, string? AccountNumber);

[Scenario("Payment validation by type"), Fact]
public async Task PaymentValidationByType()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.ScenarioOutline<PaymentRequest>(ctx, "Payment types")
        .Given("payment request", ex => ex)
        .When("validating", payment => _validator.ValidatePayment(payment))
        .Then("validation succeeds", (result, ex) => result.IsValid)
        .Examples(
            new PaymentRequest("CreditCard", 100m, "4111111111111111", null),
            new PaymentRequest("BankTransfer", 500m, null, "123456789"),
            new PaymentRequest("Cash", 50m, null, null))
        .AssertAllPassedAsync();
}
```

## Working with External Data Sources

### Loading Data from Files

```csharp
[Scenario("Process batch from CSV"), Fact]
public async Task ProcessBatchFromCsv()
{
    await Given("CSV file content", () => File.ReadAllLines("testdata/orders.csv"))
        .When("parsing CSV", lines => 
        {
            return lines.Skip(1) // Skip header
                .Select(line => line.Split(','))
                .Select(parts => new Order
                {
                    Id = parts[0],
                    Amount = decimal.Parse(parts[1]),
                    Date = DateTime.Parse(parts[2])
                })
                .ToList();
        })
        .Then("all orders parsed", orders => orders.Count > 0)
        .And("all amounts are positive", orders => 
            orders.All(o => o.Amount > 0))
        .AssertPassed();
}
```

### Database Test Data

```csharp
[Scenario("Query database records"), Fact]
public async Task QueryDatabaseRecords()
{
    await Given("database connection", () => new TestDatabase())
        .And("seeded test data", (db, ct) =>
        {
            db.Seed(new[]
            {
                new User("Alice", "alice@example.com"),
                new User("Bob", "bob@example.com")
            });
            return Task.FromResult(db);
        })
        .When("querying active users", db => 
            db.Query<User>("SELECT * FROM Users WHERE IsActive = 1"))
        .Then("users are returned", users => users.Count() == 2)
        .And("Alice is included", users => 
            users.Any(u => u.Name == "Alice"))
        .Finally("dispose connection", db => db.Dispose())
        .AssertPassed();
}
```

### JSON Test Data

```csharp
public record TestCase(string Name, Dictionary<string, object> Input, object Expected);

[Scenario("Process JSON test cases"), Fact]
public async Task ProcessJsonTestCases()
{
    var json = @"
    [
        {
            ""name"": ""case1"",
            ""input"": { ""x"": 1, ""y"": 2 },
            ""expected"": 3
        },
        {
            ""name"": ""case2"",
            ""input"": { ""x"": 5, ""y"": 5 },
            ""expected"": 10
        }
    ]";
    
    await Given("test cases from JSON", () => 
            JsonSerializer.Deserialize<TestCase[]>(json))
        .When("executing first case", cases => 
        {
            var testCase = cases[0];
            var x = Convert.ToInt32(testCase.Input["x"]);
            var y = Convert.ToInt32(testCase.Input["y"]);
            return x + y;
        })
        .Then("result matches expected", result => result == 3)
        .AssertPassed();
}
```

## Data Builder Pattern

For complex test data setup, use the builder pattern:

```csharp
public class OrderBuilder
{
    private List<OrderItem> _items = new();
    private string _customerEmail = "test@example.com";
    private string _shippingAddress = "123 Test St";
    
    public OrderBuilder WithItem(string name, decimal price, int qty = 1)
    {
        _items.Add(new OrderItem(name, price, qty));
        return this;
    }
    
    public OrderBuilder ForCustomer(string email)
    {
        _customerEmail = email;
        return this;
    }
    
    public OrderBuilder ShipTo(string address)
    {
        _shippingAddress = address;
        return this;
    }
    
    public Order Build() => new Order
    {
        Items = _items,
        CustomerEmail = _customerEmail,
        ShippingAddress = _shippingAddress
    };
}

[Scenario("Complex order processing"), Fact]
public async Task ComplexOrderProcessing()
{
    await Given("order with multiple items", () => 
            new OrderBuilder()
                .ForCustomer("customer@example.com")
                .WithItem("Widget", 10m, 2)
                .WithItem("Gadget", 20m, 1)
                .ShipTo("456 Main St")
                .Build())
        .When("calculating shipping", order => 
        {
            order.ShippingCost = order.Items.Count * 5m;
            return order;
        })
        .Then("shipping cost is correct", order => order.ShippingCost == 10m)
        .AssertPassed();
}
```

## Data Cleanup and Isolation

### Using Finally for Data Cleanup

```csharp
[Scenario("Create and cleanup test data"), Fact]
public async Task CreateAndCleanupTestData()
{
    await Given("test database", () => new TestDatabase())
        .And("test user", (db, ct) =>
        {
            var user = new User { Id = Guid.NewGuid(), Name = "TestUser" };
            db.Users.Add(user);
            return Task.FromResult((db, userId: user.Id));
        })
        .When("querying user", state => 
            state.db.Users.Find(state.userId))
        .Then("user exists", user => user != null)
        .Finally("cleanup test user", state => 
        {
            state.db.Users.Remove(state.userId);
            state.db.SaveChanges();
        })
        .Finally("dispose database", state => state.db.Dispose())
        .AssertPassed();
}
```

### Test Data Isolation

Ensure each scenario uses isolated data:

```csharp
[Feature("User Management")]
public class UserManagementTests : TinyBddXunitBase
{
    private readonly string _testRunId = Guid.NewGuid().ToString("N")[..8];
    
    private string TestEmail(string user) => $"{user}-{_testRunId}@test.example.com";
    
    [Scenario("Create unique users"), Fact]
    public async Task CreateUniqueUsers()
    {
        // Each test run gets unique emails
        await Given("unique email", () => TestEmail("user1"))
            .When("creating user", email => _userService.CreateAsync(email))
            .Then("user created", result => result.IsSuccess)
            .AssertPassed();
    }
}
```

## Best Practices for Data-Driven Tests

1. **Keep data close to tests**: Define test data in the same file as the tests that use it
2. **Use meaningful names**: Give test data variables descriptive names
3. **Minimize test data**: Use the smallest data set that validates the behavior
4. **Make data explicit**: Avoid hidden setup; make test data visible in the scenario
5. **Isolate test data**: Ensure each scenario uses independent data
6. **Clean up resources**: Use Finally blocks to dispose of resources
7. **Document example purposes**: Use the reason/description parameter in scenario outlines
8. **Test boundaries**: Include edge cases and boundary values in your data sets
9. **Use builders for complex data**: Simplify complex object construction with builders
10. **Version test data files**: Keep external test data files in source control

## Next Steps

- Learn about [Hooks and Lifecycle](hooks-and-lifecycle.md) for managing test setup and teardown
- Explore [Writing Scenarios](writing-scenarios.md) for scenario organization techniques
- See [Running with Test Frameworks](running-with-test-frameworks.md) for execution options

