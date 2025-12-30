# Writing Scenarios

This guide covers the essential techniques for writing clear, maintainable BDD scenarios in TinyBDD, including background steps, scenario outlines for data-driven tests, and tagging for organization and filtering.

## Anatomy of a Well-Written Scenario

A well-crafted scenario tells a story that anyone on your team can understand. Each step should be:
- **Single-purpose**: One action or assertion per step
- **Business-focused**: Use domain language, not implementation details
- **Self-contained**: The scenario should be understandable without reading code

### Example: A Clear Scenario

```csharp
[Feature("Shopping Cart")]
public class ShoppingCartTests : TinyBddXunitBase
{
    [Scenario("Adding items updates total"), Fact]
    public async Task AddingItemsUpdatesTotal()
    {
        await Given("an empty cart", () => new ShoppingCart())
            .When("adding a $10 item", cart => cart.AddItem("Widget", 10m))
            .And("adding a $5 item", cart => cart.AddItem("Gadget", 5m))
            .Then("total is $15", cart => cart.Total == 15m)
            .AssertPassed();
    }
}
```

## Background Steps

Background steps represent setup that's common across multiple scenarios in a feature. While Gherkin has a dedicated `Background:` section, TinyBDD handles this through standard test framework features.

### Using Test Fixtures

For shared setup, use your test framework's initialization mechanisms:

#### xUnit: Constructor and IDisposable

```csharp
[Feature("User Authentication")]
public class AuthenticationTests : TinyBddXunitBase, IDisposable
{
    private readonly TestDatabase _db;
    private readonly AuthService _authService;
    
    public AuthenticationTests(ITestOutputHelper output) : base(output)
    {
        // Background: Initialize test database and services
        _db = new TestDatabase();
        _db.Seed();
        _authService = new AuthService(_db);
    }
    
    [Scenario("Valid credentials allow login"), Fact]
    public async Task ValidCredentialsAllowLogin()
    {
        await Given("valid credentials", () => new Credentials("user@example.com", "correct"))
            .When("attempting login", creds => _authService.LoginAsync(creds))
            .Then("login succeeds", result => result.IsSuccess)
            .AssertPassed();
    }
    
    [Scenario("Invalid credentials deny login"), Fact]
    public async Task InvalidCredentialsDenyLogin()
    {
        await Given("invalid credentials", () => new Credentials("user@example.com", "wrong"))
            .When("attempting login", creds => _authService.LoginAsync(creds))
            .Then("login fails", result => !result.IsSuccess)
            .AssertPassed();
    }
    
    public void Dispose()
    {
        // Cleanup after each scenario
        _db?.Dispose();
    }
}
```

#### NUnit: SetUp and TearDown

```csharp
[Feature("User Authentication")]
public class AuthenticationTests : TinyBddNUnitBase
{
    private TestDatabase _db;
    private AuthService _authService;
    
    [SetUp]
    public void BackgroundSetup()
    {
        // Background: Initialize test database and services
        _db = new TestDatabase();
        _db.Seed();
        _authService = new AuthService(_db);
    }
    
    [Scenario("Valid credentials allow login"), Test]
    public async Task ValidCredentialsAllowLogin()
    {
        await Given("valid credentials", () => new Credentials("user@example.com", "correct"))
            .When("attempting login", creds => _authService.LoginAsync(creds))
            .Then("login succeeds", result => result.IsSuccess)
            .AssertPassed();
    }
    
    [TearDown]
    public void BackgroundCleanup()
    {
        _db?.Dispose();
    }
}
```

#### MSTest: TestInitialize and TestCleanup

```csharp
[TestClass]
[Feature("User Authentication")]
public class AuthenticationTests : TinyBddMsTestBase
{
    private TestDatabase _db;
    private AuthService _authService;
    
    [TestInitialize]
    public void BackgroundSetup()
    {
        // Background: Initialize test database and services
        _db = new TestDatabase();
        _db.Seed();
        _authService = new AuthService(_db);
    }
    
    [Scenario("Valid credentials allow login"), TestMethod]
    public async Task ValidCredentialsAllowLogin()
    {
        await Given("valid credentials", () => new Credentials("user@example.com", "correct"))
            .When("attempting login", creds => _authService.LoginAsync(creds))
            .Then("login succeeds", result => result.IsSuccess)
            .AssertPassed();
    }
    
    [TestCleanup]
    public void BackgroundCleanup()
    {
        _db?.Dispose();
    }
}
```

### Explicit Background Steps in Scenarios

For scenarios requiring visible setup steps, include them in your Given chain:

```csharp
[Scenario("Transfer between accounts"), Fact]
public async Task TransferBetweenAccounts()
{
    await Given("account A with $100", () => new Account("A", 100m))
        .And("account B with $50", (accountA, ct) => 
        {
            var accountB = new Account("B", 50m);
            return Task.FromResult((accountA, accountB));
        })
        .When("transferring $30 from A to B", accounts => 
        {
            accounts.accountA.Transfer(accounts.accountB, 30m);
            return accounts;
        })
        .Then("account A has $70", accounts => accounts.accountA.Balance == 70m)
        .And("account B has $80", accounts => accounts.accountB.Balance == 80m)
        .AssertPassed();
}
```

## Scenario Outlines: Data-Driven Tests

Scenario outlines let you run the same test logic with multiple sets of data. This is ideal for testing boundary conditions, equivalence classes, or multiple valid/invalid inputs.

### Basic Scenario Outline

```csharp
[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
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

### Complex Data Examples

For more complex scenarios with multiple parameters:

```csharp
[Scenario("Password validation"), Fact]
public async Task PasswordValidation()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.ScenarioOutline<(string password, bool shouldBeValid, string reason)>(ctx, "Password rules")
        .Given("a password", ex => ex.password)
        .When("validating", pwd => _validator.Validate(pwd))
        .Then("validation result matches expected", (result, ex) => 
            result.IsValid == ex.shouldBeValid)
        .Examples(
            ("Str0ng!Pass", true, "valid strong password"),
            ("weak", false, "too short"),
            ("NoNumbers!", false, "missing numbers"),
            ("nonumber123", false, "missing special character"),
            ("NOLOWERCASE1!", false, "missing lowercase"))
        .AssertAllPassedAsync();
}
```

### Accessing Example Data in Steps

Each step can access the example data through the `ex` parameter:

```csharp
await Bdd.ScenarioOutline<(decimal price, decimal discount, decimal expectedTotal)>(ctx, "Discount calculation")
    .Given("item with price", ex => new CartItem { Price = ex.price })
    .When("applying discount", (item, ex) => 
    {
        item.ApplyDiscount(ex.discount);
        return item;
    })
    .Then("final price matches", (item, ex) => 
        Math.Abs(item.FinalPrice - ex.expectedTotal) < 0.01m)
    .Examples(
        (price: 100m, discount: 0.10m, expectedTotal: 90m),
        (price: 50m, discount: 0.20m, expectedTotal: 40m),
        (price: 25m, discount: 0m, expectedTotal: 25m))
    .AssertAllPassedAsync();
```

### Handling Example Failures

When an example fails, the exception indicates which example row failed:

```csharp
// If the second example fails:
// Example 1 (a: 1, b: 2, expected: 3) [OK]
// Example 2 (a: 5, b: 5, expected: 10) [FAIL]
//   Expected: 10, Actual: 9
```

Use `RunAsync()` instead of `AssertAllPassedAsync()` to capture results without throwing:

```csharp
var results = await outline.Examples(...).RunAsync();

// Inspect individual results
foreach (var result in results.Items)
{
    Console.WriteLine($"Example {result.Index}: {(result.Passed ? "PASS" : "FAIL")}");
    if (!result.Passed)
    {
        Console.WriteLine($"  Error: {result.Exception?.Message}");
    }
}

// Or assert all passed
results.AssertAllPassed(); // Throws aggregate exception with all failures
```

## Tags: Organization and Filtering

Tags help categorize scenarios for selective execution, reporting, and organization. Tags can be applied at both the feature (class) and scenario (method) levels.

### Applying Tags

#### Single Tag per Attribute

```csharp
[Feature("Payment Processing")]
[Tag("integration")]
public class PaymentTests : TinyBddXunitBase
{
    [Scenario("Credit card payment"), Fact]
    [Tag("smoke")]
    public async Task CreditCardPayment() { /* ... */ }
    
    [Scenario("PayPal payment"), Fact]
    [Tag("slow")]
    public async Task PayPalPayment() { /* ... */ }
}
```

#### Multiple Tags via Scenario Attribute

The `[Scenario]` attribute accepts tags as additional parameters:

```csharp
[Feature("User Registration")]
public class RegistrationTests : TinyBddXunitBase
{
    [Scenario("Email registration", "smoke", "fast")]
    [Fact]
    public async Task EmailRegistration() { /* ... */ }
    
    [Scenario("Social login", "integration", "slow")]
    [Fact]
    public async Task SocialLogin() { /* ... */ }
}
```

#### Programmatic Tags

Add tags dynamically within scenarios:

```csharp
[Scenario("Dynamic tagging example"), Fact]
public async Task DynamicTagging()
{
    var ctx = Ambient.Current.Value;
    ctx.AddTag("runtime-tag");
    
    await Given(() => 1)
        .When("double", x => x * 2)
        .Then("equals 2", x => x == 2)
        .AssertPassed();
}
```

### Common Tag Conventions

| Tag | Purpose | Example Use |
|-----|---------|-------------|
| `smoke` | Critical path tests | Quick validation of core functionality |
| `regression` | Full regression suite | Comprehensive testing before release |
| `integration` | External dependencies | Tests requiring databases, APIs, etc. |
| `unit` | Pure unit tests | Fast, isolated tests |
| `slow` | Long-running tests | Tests taking more than a few seconds |
| `wip` | Work in progress | Features under development |
| `bug-{id}` | Bug verification | Tests for specific bug fixes |

### Filtering Tests by Tag

Each test framework provides mechanisms for filtering by tags:

#### xUnit Trait Filtering

TinyBDD bridges tags to xUnit traits:

```bash
# Run only smoke tests
dotnet test --filter "Category=smoke"

# Exclude slow tests
dotnet test --filter "Category!=slow"

# Multiple conditions
dotnet test --filter "(Category=smoke)|(Category=regression)"
```

#### NUnit Category Filtering

```bash
# Run specific category
dotnet test --filter "TestCategory=smoke"

# Exclude category
dotnet test --filter "TestCategory!=integration"
```

#### MSTest TestCategory Filtering

```bash
# Run specific category
dotnet test --filter "TestCategory=smoke"

# Complex filters
dotnet test --filter "(TestCategory=smoke)&(TestCategory!=slow)"
```

### Tag Reporting

Tags appear in test output for documentation and traceability:

```
Feature: Payment Processing
Scenario: Credit card payment
  Tags: smoke, integration
  Given a valid credit card [OK] 2 ms
  When processing payment [OK] 45 ms
  Then payment succeeds [OK] 1 ms
```

### Best Practices for Tags

1. **Use consistent naming**: Establish tag conventions across your team
2. **Keep tags simple**: Short, lowercase, hyphen-separated
3. **Apply liberally**: Better to over-tag than under-tag
4. **Document your tags**: Maintain a list of standard tags and their meanings
5. **Use tag hierarchies**: Consider prefixes like `api-`, `ui-`, `db-` for clarity

## Scenario Organization Tips

### One Scenario Per Test Method

Each test method should contain exactly one scenario. This provides:
- Clear test isolation
- Better failure reporting
- Easier debugging

```csharp
// Good: One scenario per method
[Scenario("Adding items"), Fact]
public async Task AddingItems() { /* one scenario */ }

[Scenario("Removing items"), Fact]
public async Task RemovingItems() { /* another scenario */ }

// Avoid: Multiple scenarios in one method
[Scenario("Cart operations"), Fact]
public async Task CartOperations()
{
    // Multiple unrelated scenarios here - harder to understand and debug
}
```

### Group Related Scenarios in a Feature Class

Use feature classes to group related scenarios:

```csharp
[Feature("Shopping Cart")]
public class ShoppingCartTests : TinyBddXunitBase
{
    [Scenario("Adding items"), Fact]
    public async Task AddingItems() { /* ... */ }
    
    [Scenario("Removing items"), Fact]
    public async Task RemovingItems() { /* ... */ }
    
    [Scenario("Calculating totals"), Fact]
    public async Task CalculatingTotals() { /* ... */ }
}
```

### Use Descriptive Names

Scenario names should describe the behavior being tested:

```csharp
// Good: Describes the behavior
[Scenario("Empty cart shows zero total")]

// Avoid: Implementation-focused
[Scenario("GetTotal returns 0")]

// Good: Business-focused
[Scenario("Discount applies to eligible items")]

// Avoid: Technical details
[Scenario("DiscountCalculator.Calculate() works")]
```

## Next Steps

- Learn about [Data and Tables](data-and-tables.md) for working with complex data structures
- Explore [Hooks and Lifecycle](hooks-and-lifecycle.md) for setup and teardown patterns
- See [Running with Test Frameworks](running-with-test-frameworks.md) for framework-specific guidance

