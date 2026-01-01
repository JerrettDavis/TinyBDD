# Testing Patterns

This guide covers patterns for using PatternKit in unit tests, integration tests, and test-driven development.

## Unit Testing

### xUnit Integration

```csharp
using PatternKit.Core;
using Xunit;

public class CalculatorTests
{
    [Fact]
    public async Task Add_ReturnsSumOfTwoNumbers()
    {
        var context = new WorkflowContext
        {
            WorkflowName = nameof(Add_ReturnsSumOfTwoNumbers)
        };

        await Workflow
            .Given(context, "two numbers", () => (a: 5, b: 3))
            .When("added", nums => Calculator.Add(nums.a, nums.b))
            .Then("result is 8", result => result == 8)
            .AssertPassed();
    }

    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public async Task Add_VariousInputs(int a, int b, int expected)
    {
        var context = new WorkflowContext
        {
            WorkflowName = $"Add_{a}_{b}"
        };

        var state = (a, b, expected);

        await Workflow
            .Given(context, "inputs", state, s => (s.a, s.b))
            .When("added", state, (nums, s) => Calculator.Add(nums.Item1, nums.Item2))
            .Then("equals expected", state, (result, s) => result == s.expected)
            .AssertPassed();
    }
}
```

### NUnit Integration

```csharp
using PatternKit.Core;
using NUnit.Framework;

[TestFixture]
public class UserServiceTests
{
    private IUserService _userService;

    [SetUp]
    public void Setup()
    {
        _userService = new UserService(new InMemoryUserRepository());
    }

    [Test]
    public async Task CreateUser_WithValidData_CreatesUser()
    {
        var context = new WorkflowContext
        {
            WorkflowName = nameof(CreateUser_WithValidData_CreatesUser)
        };

        await Workflow
            .Given(context, "valid user data", () => new CreateUserDto
            {
                Email = "test@example.com",
                Name = "Test User"
            })
            .When("user is created", async dto =>
                await _userService.CreateAsync(dto))
            .Then("user has id", user => user.Id != null)
            .And("email matches", user => user.Email == "test@example.com")
            .And("name matches", user => user.Name == "Test User")
            .AssertPassed();
    }

    [Test]
    public async Task CreateUser_WithInvalidEmail_Fails()
    {
        var context = new WorkflowContext
        {
            WorkflowName = nameof(CreateUser_WithInvalidEmail_Fails)
        };

        await Workflow
            .Given(context, "invalid email", () => new CreateUserDto
            {
                Email = "not-an-email",
                Name = "Test"
            })
            .When("creation attempted", async dto =>
            {
                try
                {
                    await _userService.CreateAsync(dto);
                    return (Success: true, Error: null as string);
                }
                catch (ValidationException ex)
                {
                    return (Success: false, Error: ex.Message);
                }
            })
            .Then("creation failed", result => !result.Success)
            .And("error mentions email", result =>
                result.Error?.Contains("email") == true)
            .AssertPassed();
    }
}
```

### MSTest Integration

```csharp
using PatternKit.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class OrderProcessorTests
{
    [TestMethod]
    public async Task ProcessOrder_ValidOrder_Succeeds()
    {
        var context = new WorkflowContext
        {
            WorkflowName = nameof(ProcessOrder_ValidOrder_Succeeds)
        };

        await Workflow
            .Given(context, "a valid order", () => CreateValidOrder())
            .When("processed", order => _processor.Process(order))
            .Then("status is complete", result => result.Status == "Complete")
            .AssertPassed();
    }
}
```

## Mocking Dependencies

### With Moq

```csharp
[Fact]
public async Task OrderService_ProcessesPayment()
{
    // Arrange
    var mockPaymentService = new Mock<IPaymentService>();
    mockPaymentService
        .Setup(p => p.ChargeAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "txn-123" });

    var service = new OrderService(mockPaymentService.Object);
    var context = new WorkflowContext { WorkflowName = "OrderPaymentTest" };
    var order = new Order { Id = "order-1", Total = 100m };

    // Act & Assert
    await Workflow
        .Given(context, "an order", () => order)
        .When("payment is processed", async o =>
            await service.ProcessPaymentAsync(o))
        .Then("payment succeeded", result => result.Success)
        .And("has transaction id", result => result.TransactionId != null)
        .AssertPassed();

    // Verify mock was called
    mockPaymentService.Verify(
        p => p.ChargeAsync(It.Is<Order>(o => o.Id == "order-1"), It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### With NSubstitute

```csharp
[Fact]
public async Task EmailService_SendsConfirmation()
{
    var emailService = Substitute.For<IEmailService>();
    emailService
        .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(Task.CompletedTask);

    var context = new WorkflowContext { WorkflowName = "EmailTest" };

    await Workflow
        .Given(context, "order confirmation", () => new OrderConfirmation
        {
            OrderId = "123",
            CustomerEmail = "test@example.com"
        })
        .When("email is sent", async conf =>
        {
            await emailService.SendAsync(conf.CustomerEmail, $"Order {conf.OrderId} confirmed");
            return conf;
        })
        .Then("completed", _ => true)
        .AssertPassed();

    await emailService.Received(1)
        .SendAsync("test@example.com", Arg.Is<string>(s => s.Contains("123")));
}
```

## Integration Testing

### With WebApplicationFactory

```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_ReturnsCreatedUser()
    {
        var context = new WorkflowContext
        {
            WorkflowName = "API_CreateUser"
        };

        await Workflow
            .Given(context, "user request", () => new
            {
                email = "newuser@example.com",
                name = "New User"
            })
            .When("POST to /api/users", async request =>
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                return await _client.PostAsync("/api/users", content);
            })
            .Then("returns 201 Created", response =>
                response.StatusCode == HttpStatusCode.Created)
            .And("has location header", response =>
                response.Headers.Location != null)
            .When("response is parsed", async response =>
            {
                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserDto>(body);
            })
            .Then("has user id", user => user.Id != null)
            .And("email matches", user => user.Email == "newuser@example.com")
            .AssertPassed();
    }
}
```

### Database Integration

```csharp
public class DatabaseIntegrationTests : IAsyncLifetime
{
    private SqliteConnection _connection;
    private AppDbContext _context;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task UserRepository_CreatesAndRetrievesUser()
    {
        var repo = new UserRepository(_context);
        var context = new WorkflowContext { WorkflowName = "DbIntegration" };

        await Workflow
            .Given(context, "a new user", () => new User
            {
                Email = "db@test.com",
                Name = "DB User"
            })
            .When("saved to database", async user =>
            {
                await repo.AddAsync(user);
                await _context.SaveChangesAsync();
                return user.Id;
            })
            .When("retrieved by id", async id =>
                await repo.GetByIdAsync(id))
            .Then("user found", user => user != null)
            .And("email matches", user => user.Email == "db@test.com")
            .AssertPassed();
    }
}
```

## Test Fixtures and Shared Context

### Shared Test Context

```csharp
public class TestFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; }
    public IWorkflowContextFactory ContextFactory { get; private set; }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddPatternKit(options =>
        {
            options.UseScopedContexts = false;
            options.DefaultWorkflowOptions = new WorkflowOptions
            {
                ContinueOnError = true
            };
        });

        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddTransient<UserService>();

        Services = services.BuildServiceProvider();
        ContextFactory = Services.GetRequiredService<IWorkflowContextFactory>();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }
}

public class UserServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public UserServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUser_Success()
    {
        var context = _fixture.ContextFactory.Create("CreateUser");
        var service = _fixture.Services.GetRequiredService<UserService>();

        await Workflow
            .Given(context, "user data", () => new CreateUserDto { Email = "test@test.com" })
            .When("created", dto => service.CreateAsync(dto))
            .Then("has id", user => user.Id != null)
            .AssertPassed();
    }
}
```

## Behavior-Driven Development

### Feature File Style

```csharp
public class ShoppingCartFeature
{
    private readonly IShoppingCartService _cartService;

    public ShoppingCartFeature()
    {
        _cartService = new ShoppingCartService();
    }

    [Fact]
    public async Task AddItemToEmptyCart()
    {
        // Feature: Shopping Cart
        // Scenario: Add item to empty cart

        var context = new WorkflowContext
        {
            WorkflowName = "Add Item to Empty Cart",
            Description = "As a customer, I want to add items to my cart"
        };

        await Workflow
            // Given an empty shopping cart
            .Given(context, "an empty shopping cart", () =>
                _cartService.CreateCart())

            // When I add a product with quantity 2
            .When("I add a product with quantity 2", cart =>
            {
                _cartService.AddItem(cart.Id, "PROD-001", quantity: 2);
                return _cartService.GetCart(cart.Id);
            })

            // Then the cart should have 1 item
            .Then("the cart should have 1 item", cart =>
                cart.Items.Count == 1)

            // And the item quantity should be 2
            .And("the item quantity should be 2", cart =>
                cart.Items[0].Quantity == 2)

            // And the cart should not be empty
            .And("the cart should not be empty", cart =>
                !cart.IsEmpty)

            .AssertPassed();
    }

    [Fact]
    public async Task RemoveLastItemEmptiesCart()
    {
        var context = new WorkflowContext
        {
            WorkflowName = "Remove Last Item Empties Cart"
        };

        await Workflow
            .Given(context, "a cart with one item", () =>
            {
                var cart = _cartService.CreateCart();
                _cartService.AddItem(cart.Id, "PROD-001", 1);
                return _cartService.GetCart(cart.Id);
            })

            .When("I remove the item", cart =>
            {
                _cartService.RemoveItem(cart.Id, "PROD-001");
                return _cartService.GetCart(cart.Id);
            })

            .Then("the cart should be empty", cart => cart.IsEmpty)
            .And("the cart should have no items", cart => cart.Items.Count == 0)

            .AssertPassed();
    }
}
```

### Scenario Outlines

```csharp
public class DiscountScenarios
{
    public static IEnumerable<object[]> DiscountTestCases =>
        new List<object[]>
        {
            new object[] { 100m, "SAVE10", 90m },
            new object[] { 100m, "SAVE20", 80m },
            new object[] { 50m, "SAVE10", 45m },
            new object[] { 200m, "HALFOFF", 100m },
        };

    [Theory]
    [MemberData(nameof(DiscountTestCases))]
    public async Task ApplyDiscount_CalculatesCorrectTotal(
        decimal originalPrice,
        string couponCode,
        decimal expectedTotal)
    {
        var context = new WorkflowContext
        {
            WorkflowName = $"Discount_{couponCode}_{originalPrice}"
        };

        var testData = (originalPrice, couponCode, expectedTotal);

        await Workflow
            .Given(context, "an order with original price", testData,
                d => new Order { Total = d.originalPrice })

            .When("discount coupon is applied", testData,
                (order, d) => _discountService.ApplyCoupon(order, d.couponCode))

            .Then("total equals expected", testData,
                (order, d) => order.Total == d.expectedTotal)

            .AssertPassed();
    }
}
```

## Test Organization

### Base Test Class

```csharp
public abstract class WorkflowTestBase
{
    protected WorkflowContext CreateContext([CallerMemberName] string testName = "")
    {
        return new WorkflowContext
        {
            WorkflowName = $"{GetType().Name}.{testName}",
            Options = new WorkflowOptions
            {
                ContinueOnError = true,
                MarkRemainingAsSkippedOnFailure = true
            }
        };
    }

    protected void LogWorkflowResults(WorkflowContext context)
    {
        Console.WriteLine($"Workflow: {context.WorkflowName}");
        Console.WriteLine($"Status: {(context.AllPassed ? "PASSED" : "FAILED")}");
        Console.WriteLine($"Steps:");

        foreach (var step in context.Steps)
        {
            var status = step.Passed ? "✓" : "✗";
            Console.WriteLine($"  {status} [{step.Kind}] {step.Title} ({step.Elapsed.TotalMilliseconds:F2}ms)");
            if (step.Error != null)
                Console.WriteLine($"      Error: {step.Error.Message}");
        }
    }
}

public class MyTests : WorkflowTestBase
{
    [Fact]
    public async Task MyTest()
    {
        var context = CreateContext();

        await Workflow
            .Given(context, "data", () => "test")
            .When("process", s => s.ToUpper())
            .Then("uppercase", s => s == "TEST");

        LogWorkflowResults(context);

        await context.AssertPassed();
    }
}
```

## Debugging Tests

### Verbose Output

```csharp
[Fact]
public async Task DebugTest()
{
    var context = new WorkflowContext
    {
        WorkflowName = "DebugTest",
        Options = new WorkflowOptions
        {
            ContinueOnError = true,
            MarkRemainingAsSkippedOnFailure = true
        }
    };

    await Workflow
        .Given(context, "data", () =>
        {
            var data = LoadTestData();
            _output.WriteLine($"Loaded {data.Count} items");
            return data;
        })
        .When("process", data =>
        {
            _output.WriteLine($"Processing...");
            var result = Process(data);
            _output.WriteLine($"Processed: {result.Status}");
            return result;
        })
        .Then("success", result =>
        {
            _output.WriteLine($"Checking result: {result}");
            return result.Success;
        });

    // Always log results for debugging
    foreach (var step in context.Steps)
    {
        _output.WriteLine($"{step.Kind} {step.Title}: {step.Elapsed}");
        if (step.Error != null)
            _output.WriteLine($"  ERROR: {step.Error}");
    }

    foreach (var io in context.IO)
    {
        _output.WriteLine($"IO: {io.Title}");
        _output.WriteLine($"  In:  {io.Input}");
        _output.WriteLine($"  Out: {io.Output}");
    }
}
```
