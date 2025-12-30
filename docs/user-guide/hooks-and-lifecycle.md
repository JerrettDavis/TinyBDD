# Hooks and Lifecycle

This guide covers TinyBDD's lifecycle hooks, setup and teardown patterns, dependency injection strategies, and techniques for managing shared context across scenarios.

## Understanding the Test Lifecycle

Every TinyBDD scenario follows a predictable lifecycle:

1. **Test fixture creation** (framework-specific: constructor, SetUp, TestInitialize)
2. **Scenario context initialization** (via `Bdd.CreateContext` or base class)
3. **Step execution** (Given → When → Then → And → But)
4. **Finally handlers** (cleanup, resource disposal)
5. **Test fixture disposal** (framework-specific: Dispose, TearDown, TestCleanup)

## Framework-Specific Setup and Teardown

### xUnit Lifecycle

xUnit creates a new test class instance for each test method. Use the constructor for setup and `IDisposable` for cleanup.

```csharp
[Feature("Resource Management")]
public class ResourceTests : TinyBddXunitBase, IDisposable
{
    private readonly TestDatabase _db;
    private readonly ILogger _logger;
    
    // Setup: Runs before each test method
    public ResourceTests(ITestOutputHelper output) : base(output)
    {
        _logger = CreateLogger();
        _logger.LogInformation("Test starting");
        
        _db = new TestDatabase();
        _db.Initialize();
    }
    
    [Scenario("Database operations"), Fact]
    public async Task DatabaseOperations()
    {
        await Given("database connection", () => _db)
            .When("executing query", db => db.Query<User>("SELECT * FROM Users"))
            .Then("query succeeds", users => users != null)
            .AssertPassed();
    }
    
    // Cleanup: Runs after each test method
    public void Dispose()
    {
        _logger?.LogInformation("Test completed");
        _db?.Dispose();
    }
}
```

#### xUnit Class Fixtures (Shared Setup)

For expensive setup shared across multiple tests in a class:

```csharp
public class DatabaseFixture : IDisposable
{
    public TestDatabase Database { get; }
    
    public DatabaseFixture()
    {
        Database = new TestDatabase();
        Database.Initialize();
        Database.Seed();
    }
    
    public void Dispose()
    {
        Database?.Dispose();
    }
}

[Feature("Shared Database Tests")]
public class SharedDatabaseTests : TinyBddXunitBase, IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    
    public SharedDatabaseTests(ITestOutputHelper output, DatabaseFixture fixture) 
        : base(output)
    {
        _fixture = fixture;
    }
    
    [Scenario("Query shared database"), Fact]
    public async Task QuerySharedDatabase()
    {
        await Given("shared database", () => _fixture.Database)
            .When("querying users", db => db.Query<User>("SELECT * FROM Users"))
            .Then("users exist", users => users.Any())
            .AssertPassed();
    }
}
```

#### xUnit Collection Fixtures (Shared Across Classes)

For setup shared across multiple test classes:

```csharp
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, just the collection definition
}

[Feature("Inventory Tests")]
[Collection("Database collection")]
public class InventoryTests : TinyBddXunitBase
{
    private readonly DatabaseFixture _fixture;
    
    public InventoryTests(ITestOutputHelper output, DatabaseFixture fixture) 
        : base(output)
    {
        _fixture = fixture;
    }
    
    [Scenario("Check inventory"), Fact]
    public async Task CheckInventory()
    {
        await Given("database", () => _fixture.Database)
            .When("querying inventory", db => db.Query<Item>("SELECT * FROM Inventory"))
            .Then("items exist", items => items.Any())
            .AssertPassed();
    }
}

[Feature("Order Tests")]
[Collection("Database collection")]
public class OrderTests : TinyBddXunitBase
{
    private readonly DatabaseFixture _fixture;
    
    public OrderTests(ITestOutputHelper output, DatabaseFixture fixture) 
        : base(output)
    {
        _fixture = fixture;
    }
    
    [Scenario("Check orders"), Fact]
    public async Task CheckOrders()
    {
        await Given("database", () => _fixture.Database)
            .When("querying orders", db => db.Query<Order>("SELECT * FROM Orders"))
            .Then("orders exist", orders => orders.Any())
            .AssertPassed();
    }
}
```

### NUnit Lifecycle

NUnit provides flexible setup and teardown options at multiple levels.

```csharp
[Feature("Resource Management")]
public class ResourceTests : TinyBddNUnitBase
{
    private TestDatabase _db;
    private ILogger _logger;
    
    // Runs once before any tests in the class
    [OneTimeSetUp]
    public void ClassSetup()
    {
        _logger = CreateLogger();
        _logger.LogInformation("Test class starting");
    }
    
    // Runs before each test method
    [SetUp]
    public void TestSetup()
    {
        _db = new TestDatabase();
        _db.Initialize();
    }
    
    [Scenario("Database operations"), Test]
    public async Task DatabaseOperations()
    {
        await Given("database connection", () => _db)
            .When("executing query", db => db.Query<User>("SELECT * FROM Users"))
            .Then("query succeeds", users => users != null)
            .AssertPassed();
    }
    
    // Runs after each test method
    [TearDown]
    public void TestCleanup()
    {
        _db?.Dispose();
    }
    
    // Runs once after all tests in the class
    [OneTimeTearDown]
    public void ClassCleanup()
    {
        _logger?.LogInformation("Test class completed");
    }
}
```

#### NUnit Fixtures

Share expensive resources across tests:

```csharp
[SetUpFixture]
public class DatabaseFixture
{
    public static TestDatabase SharedDatabase { get; private set; }
    
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        SharedDatabase = new TestDatabase();
        SharedDatabase.Initialize();
        SharedDatabase.Seed();
    }
    
    [OneTimeTearDown]
    public void GlobalCleanup()
    {
        SharedDatabase?.Dispose();
    }
}

[Feature("Shared Database Tests")]
public class SharedDatabaseTests : TinyBddNUnitBase
{
    [Scenario("Query shared database"), Test]
    public async Task QuerySharedDatabase()
    {
        await Given("shared database", () => DatabaseFixture.SharedDatabase)
            .When("querying users", db => db.Query<User>("SELECT * FROM Users"))
            .Then("users exist", users => users.Any())
            .AssertPassed();
    }
}
```

### MSTest Lifecycle

MSTest provides assembly, class, and method-level initialization.

```csharp
[TestClass]
[Feature("Resource Management")]
public class ResourceTests : TinyBddMsTestBase
{
    private TestDatabase _db;
    private static ILogger _logger;
    
    // Runs once before any tests in the assembly
    [AssemblyInitialize]
    public static void AssemblySetup(TestContext context)
    {
        _logger = CreateLogger();
        _logger.LogInformation("Test assembly starting");
    }
    
    // Runs once before any tests in the class
    [ClassInitialize]
    public static void ClassSetup(TestContext context)
    {
        _logger?.LogInformation("Test class starting");
    }
    
    // Runs before each test method
    [TestInitialize]
    public void TestSetup()
    {
        _db = new TestDatabase();
        _db.Initialize();
    }
    
    [Scenario("Database operations"), TestMethod]
    public async Task DatabaseOperations()
    {
        await Given("database connection", () => _db)
            .When("executing query", db => db.Query<User>("SELECT * FROM Users"))
            .Then("query succeeds", users => users != null)
            .AssertPassed();
    }
    
    // Runs after each test method
    [TestCleanup]
    public void TestCleanup()
    {
        _db?.Dispose();
    }
    
    // Runs once after all tests in the class
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _logger?.LogInformation("Test class completed");
    }
    
    // Runs once after all tests in the assembly
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        _logger?.LogInformation("Test assembly completed");
    }
}
```

## Pipeline Hooks

TinyBDD provides hooks that execute before and after each step within a scenario, enabling lightweight instrumentation for logging, tracing, and timing.

### BeforeStep and AfterStep

These hooks are set on the `Pipeline` object within a scenario context:

```csharp
[Scenario("Instrumented scenario"), Fact]
public async Task InstrumentedScenario()
{
    var ctx = Bdd.CreateContext(this);
    
    // Get access to the pipeline (via reflection helper or exposed API)
    var pipeline = GetPipeline(ctx);
    
    pipeline.BeforeStep = (context, meta) =>
    {
        Console.WriteLine($"Starting: {meta.Kind} {meta.Title}");
    };
    
    pipeline.AfterStep = (context, result) =>
    {
        var status = result.Error == null ? "OK" : "FAIL";
        Console.WriteLine($"Completed: {result.Kind} {result.Title} [{status}] {result.Elapsed.TotalMilliseconds}ms");
    };
    
    await Bdd.Given(ctx, "number", () => 5)
        .When("double", x => x * 2)
        .Then("equals 10", x => x == 10)
        .AssertPassed();
}

// Helper to access pipeline (implementation-specific)
private Pipeline GetPipeline(ScenarioContext ctx)
{
    // Access via reflection or exposed API
    var field = ctx.GetType().GetField("_pipeline", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    return (Pipeline)field?.GetValue(ctx);
}
```

### Custom Timing and Tracing

Create a reusable hook pattern for timing:

```csharp
public abstract class InstrumentedTestBase : TinyBddXunitBase
{
    private readonly List<StepTiming> _timings = new();
    
    protected InstrumentedTestBase(ITestOutputHelper output) : base(output)
    {
    }
    
    protected void EnableInstrumentation(ScenarioContext ctx)
    {
        var pipeline = GetPipeline(ctx);
        
        pipeline.BeforeStep = (context, meta) =>
        {
            _timings.Add(new StepTiming 
            { 
                Step = $"{meta.Kind} {meta.Title}",
                StartTime = DateTime.UtcNow
            });
        };
        
        pipeline.AfterStep = (context, result) =>
        {
            var timing = _timings.Last();
            timing.Duration = result.Elapsed;
            timing.Success = result.Error == null;
        };
    }
    
    protected void DumpTimings()
    {
        foreach (var timing in _timings)
        {
            Console.WriteLine($"{timing.Step}: {timing.Duration.TotalMilliseconds}ms [{(timing.Success ? "OK" : "FAIL")}]");
        }
    }
}

public class StepTiming
{
    public string Step { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}
```

## Finally Blocks for Cleanup

The `Finally` method registers cleanup handlers that execute after all steps complete, even if steps throw exceptions.

### Basic Finally Usage

```csharp
[Scenario("Resource cleanup"), Fact]
public async Task ResourceCleanup()
{
    await Given("file stream", () => File.OpenWrite("test.txt"))
        .Finally("close stream", stream => stream.Dispose())
        .When("writing data", stream =>
        {
            stream.Write(Encoding.UTF8.GetBytes("Hello"));
            return stream;
        })
        .Then("data written", stream => stream.Position > 0)
        .AssertPassed();
    
    // Stream is automatically disposed after scenario completes
}
```

### Multiple Finally Handlers

Register multiple cleanup handlers at different points:

```csharp
[Scenario("Multiple resource cleanup"), Fact]
public async Task MultipleResourceCleanup()
{
    await Given("database connection", () => new SqlConnection(connectionString))
        .Finally("close connection", conn => conn.Dispose())
        .And("transaction", (conn, ct) =>
        {
            conn.Open();
            var transaction = conn.BeginTransaction();
            return Task.FromResult((conn, transaction));
        })
        .Finally("rollback transaction", state => state.transaction.Rollback())
        .When("executing query", state =>
        {
            var cmd = new SqlCommand("INSERT INTO Users VALUES (@name)", 
                state.conn, state.transaction);
            cmd.Parameters.AddWithValue("@name", "Test User");
            return cmd.ExecuteNonQuery();
        })
        .Then("query executed", rowsAffected => rowsAffected == 1)
        .AssertPassed();
    
    // Execution order:
    // 1. All scenario steps complete
    // 2. Transaction is rolled back
    // 3. Connection is closed
}
```

### Async Finally Handlers

Finally handlers support async operations:

```csharp
[Scenario("Async cleanup"), Fact]
public async Task AsyncCleanup()
{
    await Given("HTTP client", () => new HttpClient())
        .Finally("dispose client", async (client, ct) =>
        {
            // Drain any pending requests
            await Task.Delay(10, ct);
            client.Dispose();
        })
        .When("making request", client => 
            client.GetStringAsync("https://api.example.com/data"))
        .Then("response received", response => !string.IsNullOrEmpty(response))
        .AssertPassed();
}
```

### Finally Handler Error Handling

Finally handlers suppress exceptions to prevent masking original exceptions:

```csharp
[Scenario("Finally error handling"), Fact]
public async Task FinallyErrorHandling()
{
    var cleanupAttempted = false;
    
    try
    {
        await Given("resource", () => new TestResource())
            .Finally("cleanup", resource =>
            {
                cleanupAttempted = true;
                // If this throws, it won't mask the original step exception
                resource.Dispose();
            })
            .When("failing operation", resource => throw new InvalidOperationException("Step failed"))
            .Then("never reached", resource => true)
            .AssertPassed();
    }
    catch (BddStepException)
    {
        // Original exception is preserved
        Assert.True(cleanupAttempted);
    }
}
```

## Dependency Injection Patterns

### Constructor Injection

Pass dependencies through test class constructors:

```csharp
[Feature("Payment Processing")]
public class PaymentTests : TinyBddXunitBase
{
    private readonly IPaymentGateway _gateway;
    private readonly ILogger _logger;
    
    public PaymentTests(ITestOutputHelper output) : base(output)
    {
        // Setup real or mock dependencies
        _logger = new TestLogger();
        _gateway = new TestPaymentGateway(_logger);
    }
    
    [Scenario("Process payment"), Fact]
    public async Task ProcessPayment()
    {
        await Given("payment request", () => new PaymentRequest(100m))
            .When("processing", request => _gateway.ProcessAsync(request))
            .Then("payment succeeds", result => result.IsSuccess)
            .AssertPassed();
    }
}
```

### Service Locator Pattern

For complex dependency graphs:

```csharp
public class TestServices
{
    private readonly IServiceProvider _services;
    
    public TestServices()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, TestEmailService>();
        services.AddScoped<IDatabase, TestDatabase>();
    }
    
    public T GetService<T>() => _services.GetRequiredService<T>();
}

[Feature("User Management")]
public class UserManagementTests : TinyBddXunitBase
{
    private readonly TestServices _services = new();
    
    public UserManagementTests(ITestOutputHelper output) : base(output)
    {
    }
    
    [Scenario("Create user"), Fact]
    public async Task CreateUser()
    {
        var userService = _services.GetService<IUserService>();
        
        await Given("user registration data", () => new UserRegistration("user@example.com", "password"))
            .When("registering", data => userService.RegisterAsync(data))
            .Then("registration succeeds", result => result.IsSuccess)
            .AssertPassed();
    }
}
```

### Test Doubles and Mocking

Integrate mocking libraries for test isolation:

```csharp
[Feature("Order Processing")]
public class OrderProcessingTests : TinyBddXunitBase
{
    public OrderProcessingTests(ITestOutputHelper output) : base(output)
    {
    }
    
    [Scenario("Send order confirmation"), Fact]
    public async Task SendOrderConfirmation()
    {
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        var orderService = new OrderService(mockEmailService.Object);
        
        await Given("completed order", () => new Order { Id = "123", CustomerEmail = "customer@example.com" })
            .When("confirming order", order => orderService.ConfirmAsync(order))
            .Then("confirmation sent", result => result.EmailSent)
            .AssertPassed();
        
        // Verify email was sent
        mockEmailService.Verify(
            e => e.SendAsync("customer@example.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }
}
```

## Shared Context Patterns

### Instance Fields for Scenario State

Share state across multiple scenarios in a test class:

```csharp
[Feature("Shopping Cart")]
public class ShoppingCartTests : TinyBddXunitBase
{
    private readonly ShoppingCart _cart = new();
    
    public ShoppingCartTests(ITestOutputHelper output) : base(output)
    {
    }
    
    [Scenario("Add first item"), Fact]
    public async Task AddFirstItem()
    {
        await Given("empty cart", () => _cart)
            .When("adding item", cart => cart.AddItem("Widget", 10m))
            .Then("cart has one item", cart => cart.ItemCount == 1)
            .AssertPassed();
    }
    
    [Scenario("Calculate total"), Fact]
    public async Task CalculateTotal()
    {
        _cart.AddItem("Widget", 10m);
        _cart.AddItem("Gadget", 20m);
        
        await Given("cart with items", () => _cart)
            .When("calculating total", cart => cart.CalculateTotal())
            .Then("total is correct", total => total == 30m)
            .AssertPassed();
    }
}
```

### Context Objects

Encapsulate shared state in a context object:

```csharp
public class TestContext
{
    public TestDatabase Database { get; set; }
    public IUserService UserService { get; set; }
    public User CurrentUser { get; set; }
    public List<string> LogMessages { get; } = new();
}

[Feature("User Operations")]
public class UserOperationTests : TinyBddXunitBase
{
    private TestContext _context;
    
    public UserOperationTests(ITestOutputHelper output) : base(output)
    {
        _context = new TestContext
        {
            Database = new TestDatabase(),
            UserService = new UserService()
        };
        _context.Database.Initialize();
    }
    
    [Scenario("Create and retrieve user"), Fact]
    public async Task CreateAndRetrieveUser()
    {
        await Given("user creation data", () => new CreateUserRequest("test@example.com"))
            .When("creating user", async request =>
            {
                _context.CurrentUser = await _context.UserService.CreateAsync(request);
                return _context.CurrentUser;
            })
            .And("retrieving user", user => 
                _context.Database.Users.Find(user.Id))
            .Then("user exists in database", retrievedUser => retrievedUser != null)
            .And("email matches", retrievedUser => retrievedUser.Email == "test@example.com")
            .AssertPassed();
    }
}
```

## Best Practices

### 1. Minimize Shared State

Prefer scenario isolation over shared state:

```csharp
// Good: Each scenario is isolated
[Scenario("Add item"), Fact]
public async Task AddItem()
{
    var cart = new ShoppingCart(); // Fresh instance per scenario
    await Given("cart", () => cart)
        .When("adding item", c => c.AddItem("Widget", 10m))
        .Then("item added", c => c.ItemCount == 1)
        .AssertPassed();
}

// Avoid: Shared state can cause test interdependence
private readonly ShoppingCart _sharedCart = new();

[Scenario("Add item"), Fact]
public async Task AddItem()
{
    // State from previous scenarios may affect this one
    await Given("cart", () => _sharedCart)
        .When("adding item", c => c.AddItem("Widget", 10m))
        .Then("item added", c => c.ItemCount == 1) // May fail if cart already has items
        .AssertPassed();
}
```

### 2. Use Finally for Guaranteed Cleanup

Always clean up resources using Finally:

```csharp
await Given("file", () => File.Create("temp.txt"))
    .Finally("delete file", file =>
    {
        file.Dispose();
        File.Delete("temp.txt");
    })
    .When("writing", file => file.Write(Encoding.UTF8.GetBytes("data")))
    .Then("written", file => file.Position > 0)
    .AssertPassed();
```

### 3. Keep Setup Lightweight

Minimize expensive operations in setup methods:

```csharp
// Good: Lazy initialization
private TestDatabase _db;
private TestDatabase Database => _db ??= CreateAndInitializeDatabase();

// Avoid: Expensive setup for every test
[SetUp]
public void Setup()
{
    _db = new TestDatabase();
    _db.Initialize(); // Expensive
    _db.SeedWithMillionsOfRecords(); // Very expensive
}
```

### 4. Document Lifecycle Dependencies

Make lifecycle dependencies explicit:

```csharp
[Feature("Integration Tests")]
// Requires: Database connection available
// Setup: Seeds test data in SetUp
// Cleanup: Clears test data in TearDown
public class IntegrationTests : TinyBddNUnitBase
{
    // ...
}
```

### 5. Use Appropriate Fixture Scope

Choose the right scope for your fixtures:

- **Per-test**: Fresh state for each scenario (constructor/SetUp)
- **Per-class**: Shared expensive resources (ClassFixture/OneTimeSetUp)
- **Per-assembly**: Global resources (AssemblyInitialize/SetUpFixture)

## Next Steps

- Learn about [Reporting](reporting.md) for custom output and CI integration
- Explore [Running with Test Frameworks](running-with-test-frameworks.md) for execution details
- See [Troubleshooting & FAQ](troubleshooting-faq.md) for common issues

