# Samples Index

This page provides links to runnable sample projects and code snippets demonstrating TinyBDD usage across different test frameworks and scenarios.

## Quick Start Samples

### Minimal Example

The simplest possible TinyBDD scenario:

```csharp
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

[Feature("Quick Start")]
public class QuickStartTests : TinyBddXunitBase
{
    public QuickStartTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Basic arithmetic"), Fact]
    public async Task BasicArithmetic()
    {
        await Given("number", () => 5)
            .When("doubled", x => x * 2)
            .Then("equals 10", x => x == 10)
            .AssertPassed();
    }
}
```

### Hello World for Each Framework

#### xUnit v2

```csharp
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

[Feature("Hello World")]
public class HelloWorldTests : TinyBddXunitBase
{
    public HelloWorldTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Greeting message"), Fact]
    public async Task GreetingMessage()
    {
        await Given("name", () => "World")
            .When("creating greeting", name => $"Hello, {name}!")
            .Then("greeting is correct", greeting => greeting == "Hello, World!")
            .AssertPassed();
    }
}
```

#### xUnit v3

```csharp
using TinyBDD.Xunit.v3;
using Xunit;

[Feature("Hello World")]
public class HelloWorldTests : TinyBddXunitBase
{
    [Scenario("Greeting message"), Fact]
    public async Task GreetingMessage()
    {
        await Given("name", () => "World")
            .When("creating greeting", name => $"Hello, {name}!")
            .Then("greeting is correct", greeting => greeting == "Hello, World!")
            .AssertPassed();
    }
}
```

#### NUnit

```csharp
using NUnit.Framework;
using TinyBDD.NUnit;

[Feature("Hello World")]
public class HelloWorldTests : TinyBddNUnitBase
{
    [Scenario("Greeting message"), Test]
    public async Task GreetingMessage()
    {
        await Given("name", () => "World")
            .When("creating greeting", name => $"Hello, {name}!")
            .Then("greeting is correct", greeting => greeting == "Hello, World!")
            .AssertPassed();
    }
}
```

#### MSTest

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinyBDD.MSTest;

[TestClass]
[Feature("Hello World")]
public class HelloWorldTests : TinyBddMsTestBase
{
    [Scenario("Greeting message"), TestMethod]
    public async Task GreetingMessage()
    {
        await Given("name", () => "World")
            .When("creating greeting", name => $"Hello, {name}!")
            .Then("greeting is correct", greeting => greeting == "Hello, World!")
            .AssertPassed();
    }
}
```

## Domain Examples

### E-Commerce Shopping Cart

```csharp
[Feature("Shopping Cart")]
public class ShoppingCartTests : TinyBddXunitBase
{
    public ShoppingCartTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Adding items to cart"), Fact]
    public async Task AddingItemsToCart()
    {
        await Given("empty cart", () => new ShoppingCart())
            .When("adding widget", cart => cart.AddItem("Widget", 10.00m, 2))
            .And("adding gadget", cart => cart.AddItem("Gadget", 5.00m, 1))
            .Then("cart has 2 items", cart => cart.ItemCount == 2)
            .And("total is correct", cart => cart.Total == 25.00m)
            .AssertPassed();
    }
    
    [Scenario("Applying discount code"), Fact]
    public async Task ApplyingDiscountCode()
    {
        await Given("cart with items", () =>
            {
                var cart = new ShoppingCart();
                cart.AddItem("Widget", 100.00m, 1);
                return cart;
            })
            .When("applying 20% discount", cart => cart.ApplyDiscount("SAVE20", 0.20m))
            .Then("total reflects discount", cart => cart.Total == 80.00m)
            .AssertPassed();
    }
    
    [Scenario("Removing items"), Fact]
    public async Task RemovingItems()
    {
        await Given("cart with items", () =>
            {
                var cart = new ShoppingCart();
                cart.AddItem("Widget", 10.00m, 2);
                cart.AddItem("Gadget", 5.00m, 1);
                return cart;
            })
            .When("removing widget", cart => cart.RemoveItem("Widget"))
            .Then("cart has 1 item", cart => cart.ItemCount == 1)
            .And("total updated", cart => cart.Total == 5.00m)
            .AssertPassed();
    }
}
```

### User Authentication

```csharp
[Feature("User Authentication")]
public class AuthenticationTests : TinyBddXunitBase
{
    private readonly IAuthenticationService _authService;
    
    public AuthenticationTests(ITestOutputHelper output) : base(output)
    {
        _authService = new TestAuthenticationService();
    }
    
    [Scenario("Successful login with valid credentials"), Fact]
    public async Task SuccessfulLogin()
    {
        await Given("valid credentials", () => 
                new LoginRequest("user@example.com", "correct-password"))
            .When("attempting login", creds => _authService.LoginAsync(creds))
            .Then("login succeeds", result => result.IsSuccess)
            .And("user token is present", result => !string.IsNullOrEmpty(result.Token))
            .AssertPassed();
    }
    
    [Scenario("Failed login with invalid password"), Fact]
    public async Task FailedLoginInvalidPassword()
    {
        await Given("invalid credentials", () => 
                new LoginRequest("user@example.com", "wrong-password"))
            .When("attempting login", creds => _authService.LoginAsync(creds))
            .Then("login fails", result => !result.IsSuccess)
            .And("error message is clear", result => 
                result.ErrorMessage == "Invalid credentials")
            .AssertPassed();
    }
    
    [Scenario("Account lockout after failed attempts"), Fact]
    public async Task AccountLockout()
    {
        var credentials = new LoginRequest("user@example.com", "wrong");
        
        await Given("three failed login attempts", async () =>
            {
                await _authService.LoginAsync(credentials);
                await _authService.LoginAsync(credentials);
                await _authService.LoginAsync(credentials);
                return credentials;
            })
            .When("attempting fourth login", creds => _authService.LoginAsync(creds))
            .Then("account is locked", result => 
                result.ErrorMessage.Contains("locked"))
            .AssertPassed();
    }
}
```

### API Integration

```csharp
[Feature("Weather API")]
public class WeatherApiTests : TinyBddXunitBase
{
    private readonly HttpClient _client;
    
    public WeatherApiTests(ITestOutputHelper output) : base(output)
    {
        _client = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
    }
    
    [Scenario("Fetch current weather"), Fact]
    public async Task FetchCurrentWeather()
    {
        await Given("city name", () => "London")
            .When("requesting weather", async city =>
            {
                var response = await _client.GetAsync($"/weather?city={city}");
                return await response.Content.ReadAsStringAsync();
            })
            .Then("response contains temperature", json => json.Contains("temperature"))
            .And("response contains conditions", json => json.Contains("conditions"))
            .AssertPassed();
    }
    
    [Scenario("Handle invalid city"), Fact]
    public async Task HandleInvalidCity()
    {
        await Given("invalid city", () => "InvalidCityName123")
            .When("requesting weather", async city =>
            {
                var response = await _client.GetAsync($"/weather?city={city}");
                return response;
            })
            .Then("returns 404", response => response.StatusCode == System.Net.HttpStatusCode.NotFound)
            .AssertPassed();
    }
}
```

## Data-Driven Examples

### Scenario Outline: Calculator

```csharp
[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    public CalculatorTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Addition with multiple values"), Fact]
    public async Task AdditionWithMultipleValues()
    {
        var ctx = Bdd.CreateContext(this);
        
        await Bdd.ScenarioOutline<(int a, int b, int expected)>(ctx, "Addition")
            .Given("first number", ex => ex.a)
            .And("second number", (_, ex) => ex.b)
            .When("added together", (a, b) => a + b)
            .Then("result matches", (sum, ex) => sum == ex.expected)
            .Examples(
                (a: 1, b: 2, expected: 3),
                (a: 5, b: 5, expected: 10),
                (a: -1, b: 1, expected: 0),
                (a: 100, b: 200, expected: 300))
            .AssertAllPassedAsync();
    }
}
```

### Password Validation Matrix

```csharp
public record PasswordTest(string Password, bool IsValid, string Reason);

[Feature("Password Validation")]
public class PasswordValidationTests : TinyBddXunitBase
{
    private readonly IPasswordValidator _validator;
    
    public PasswordValidationTests(ITestOutputHelper output) : base(output)
    {
        _validator = new PasswordValidator();
    }
    
    [Scenario("Password strength validation"), Fact]
    public async Task PasswordStrengthValidation()
    {
        var ctx = Bdd.CreateContext(this);
        
        await Bdd.ScenarioOutline<PasswordTest>(ctx, "Password validation rules")
            .Given("password", ex => ex.Password)
            .When("validating", pwd => _validator.Validate(pwd))
            .Then("result matches expected", (result, ex) => 
                result.IsValid == ex.IsValid)
            .Examples(
                new PasswordTest("Str0ng!Pass", true, "valid strong password"),
                new PasswordTest("weak", false, "too short"),
                new PasswordTest("NoNumbers!", false, "missing numbers"),
                new PasswordTest("nonumber123", false, "missing special chars"),
                new PasswordTest("NOLOWERCASE1!", false, "missing lowercase"),
                new PasswordTest("nouppercase1!", false, "missing uppercase"),
                new PasswordTest("P@ssw0rd", true, "valid with all requirements"))
            .AssertAllPassedAsync();
    }
}
```

## Advanced Examples

### Resource Management with Finally

```csharp
[Feature("File Operations")]
public class FileOperationTests : TinyBddXunitBase
{
    public FileOperationTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Write and read file"), Fact]
    public async Task WriteAndReadFile()
    {
        var testFile = "test-file.txt";
        
        await Given("file stream", () => File.Create(testFile))
            .Finally("cleanup file", async stream =>
            {
                stream.Dispose();
                await Task.Delay(10); // Ensure file is released
                if (File.Exists(testFile))
                    File.Delete(testFile);
            })
            .When("writing data", async stream =>
            {
                var data = Encoding.UTF8.GetBytes("Test content");
                await stream.WriteAsync(data);
                stream.Position = 0;
                return stream;
            })
            .Then("data is written", stream => stream.Length > 0)
            .AssertPassed();
    }
}
```

### Database Transaction Management

```csharp
[Feature("Order Processing")]
public class OrderProcessingTests : TinyBddXunitBase
{
    private readonly TestDatabase _db;
    
    public OrderProcessingTests(ITestOutputHelper output) : base(output)
    {
        _db = new TestDatabase();
    }
    
    [Scenario("Process order with rollback"), Fact]
    public async Task ProcessOrderWithRollback()
    {
        await Given("database connection", () => _db.OpenConnection())
            .Finally("close connection", conn => conn.Dispose())
            .And("transaction", (conn, ct) =>
            {
                var transaction = conn.BeginTransaction();
                return Task.FromResult((conn, transaction));
            })
            .Finally("rollback transaction", state => state.transaction.Rollback())
            .When("creating order", async state =>
            {
                var order = new Order { CustomerId = 123, Total = 99.99m };
                await _db.InsertAsync(order, state.transaction);
                return order;
            })
            .Then("order exists in transaction", async order =>
            {
                var found = await _db.FindAsync<Order>(order.Id);
                return found != null;
            })
            .AssertPassed();
    }
}
```

### Custom Reporter Example

```csharp
[Feature("Custom Reporting")]
public class CustomReportingTests : TinyBddXunitBase
{
    public CustomReportingTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Generate JSON report"), Fact]
    public async Task GenerateJsonReport()
    {
        var ctx = Bdd.CreateContext(this);
        
        await Bdd.Given(ctx, "data", () => new { Name = "Test", Value = 42 })
            .When("processing", data => data.Value * 2)
            .Then("result is correct", result => result == 84)
            .AssertPassed();
        
        // Generate JSON report
        var reporter = new JsonBddReporter();
        GherkinFormatter.Write(ctx, reporter);
        
        var json = reporter.ToJson();
        _output.WriteLine(json);
        
        // Optionally save to file
        await File.WriteAllTextAsync("report.json", json);
    }
}

public class JsonBddReporter : IBddReporter
{
    private readonly List<string> _lines = new();
    
    public void WriteLine(string message)
    {
        _lines.Add(message);
    }
    
    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(
            new { timestamp = DateTime.UtcNow, lines = _lines },
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
```

## Test Framework Examples

### xUnit Theory with TinyBDD

```csharp
[Feature("Parameterized Tests")]
public class ParameterizedTests : TinyBddXunitBase
{
    public ParameterizedTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("String operations"), Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("world", "WORLD")]
    [InlineData("test", "TEST")]
    public async Task StringOperations(string input, string expected)
    {
        await Given("input string", () => input)
            .When("converting to uppercase", s => s.ToUpper())
            .Then("matches expected", result => result == expected)
            .AssertPassed();
    }
}
```

### NUnit TestCase with TinyBDD

```csharp
[Feature("Parameterized Tests")]
public class ParameterizedTests : TinyBddNUnitBase
{
    [Scenario("String operations"), TestCase("hello", "HELLO")]
    [TestCase("world", "WORLD")]
    [TestCase("test", "TEST")]
    public async Task StringOperations(string input, string expected)
    {
        await Given("input string", () => input)
            .When("converting to uppercase", s => s.ToUpper())
            .Then("matches expected", result => result == expected)
            .AssertPassed();
    }
}
```

### MSTest DataRow with TinyBDD

```csharp
[TestClass]
[Feature("Parameterized Tests")]
public class ParameterizedTests : TinyBddMsTestBase
{
    [Scenario("String operations")]
    [DataRow("hello", "HELLO")]
    [DataRow("world", "WORLD")]
    [DataRow("test", "TEST")]
    [TestMethod]
    public async Task StringOperations(string input, string expected)
    {
        await Given("input string", () => input)
            .When("converting to uppercase", s => s.ToUpper())
            .Then("matches expected", result => result == expected)
            .AssertPassed();
    }
}
```

## Repository Examples

The TinyBDD repository contains comprehensive test suites demonstrating real-world usage:

### Test Projects in Repository

1. **TinyBDD.Xunit.Tests** - xUnit v2 examples
   - Location: `tests/TinyBDD.Xunit.Tests/`
   - Examples: Basic scenarios, fixtures, parallel tests

2. **TinyBDD.Xunit.v3.Tests** - xUnit v3 examples
   - Location: `tests/TinyBDD.Xunit.v3.Tests/`
   - Examples: Modern xUnit patterns, enhanced extensibility

3. **TinyBDD.NUnit.Tests** - NUnit examples
   - Location: `tests/TinyBDD.NUnit.Tests/`
   - Examples: SetUp/TearDown, TestCase, Categories

4. **TinyBDD.MSTest.Tests** - MSTest examples
   - Location: `tests/TinyBDD.MSTest.Tests/`
   - Examples: TestInitialize/TestCleanup, DataRow, TestCategory

### Example Test Classes

Browse these test classes for patterns:

- **LoginTests** - User authentication scenarios
- **InventoryTests** - E-commerce inventory management
- **OrderTests** - Order processing workflows
- **ValidationTests** - Input validation scenarios

## Sample Project Templates

### Creating a New Project with TinyBDD

```bash
# Create new xUnit project
dotnet new xunit -n MyApp.Tests
cd MyApp.Tests

# Add TinyBDD
dotnet add package TinyBDD
dotnet add package TinyBDD.Xunit

# Create first test
cat > CalculatorTests.cs << 'EOF'
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    public CalculatorTests(ITestOutputHelper output) : base(output) { }
    
    [Scenario("Addition"), Fact]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
    }
}
EOF

# Run tests
dotnet test
```

## Additional Resources

### Documentation

- [Getting Started](getting-started.md) - Installation and basic usage
- [Writing Scenarios](writing-scenarios.md) - Best practices for scenarios
- [Data and Tables](data-and-tables.md) - Data-driven testing
- [Hooks and Lifecycle](hooks-and-lifecycle.md) - Setup and teardown
- [Running with Test Frameworks](running-with-test-frameworks.md) - Framework-specific guidance
- [Reporting](reporting.md) - Custom reporters and output
- [Troubleshooting & FAQ](troubleshooting-faq.md) - Common issues and solutions

### Community Examples

Check the GitHub repository for:
- Issue discussions with example code
- Pull requests demonstrating new features
- Community-contributed samples

### Contributing Examples

To contribute your own examples:

1. Fork the repository
2. Add your example to the appropriate test project
3. Include clear documentation
4. Submit a pull request

## Tips for Learning

1. **Start simple**: Begin with the quick start examples
2. **Explore domains**: Review domain examples relevant to your work
3. **Study test projects**: Browse the repository test projects
4. **Experiment**: Modify examples to understand behavior
5. **Read documentation**: Reference detailed guides for deeper understanding
6. **Ask questions**: Open discussions on GitHub for clarification

## Next Steps

- Try the [Quick Start samples](#quick-start-samples) in your test project
- Explore [Domain Examples](#domain-examples) for real-world patterns
- Review [Test Framework Examples](#test-framework-examples) for your framework
- Check the [Repository Examples](#repository-examples) for comprehensive coverage

