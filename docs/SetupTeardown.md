# Setup and Teardown Strategy

TinyBDD provides a comprehensive, multi-layered setup and teardown strategy for managing test resources at different scopes. This document explains each layer and provides real-world examples.

## Overview

TinyBDD supports **seven lifecycle layers**, from broadest to narrowest scope:

1. **Assembly Setup** - Runs once per test assembly
2. **Assembly Teardown** - Runs once after all tests complete
3. **Feature Setup** - Runs once per test class
4. **Feature Teardown** - Runs once after all tests in a class complete
5. **Scenario Background** - Runs before each test method
6. **Scenario Steps** - The actual test logic (Given/When/Then)
7. **Scenario Teardown (Finally)** - Runs after each test method (even on failure)

### Execution Order

```
Assembly Setup
├─ Feature Setup (Test Class 1)
│  ├─ Background → Scenario 1 → Finally
│  ├─ Background → Scenario 2 → Finally
│  └─ Background → Scenario 3 → Finally
├─ Feature Teardown (Test Class 1)
├─ Feature Setup (Test Class 2)
│  └─ Background → Scenario 4 → Finally
└─ Feature Teardown (Test Class 2)
Assembly Teardown
```

---

## 1. Assembly Setup and Teardown

**Purpose**: Initialize expensive, globally-shared resources once for the entire test assembly (e.g., test databases, external services, DI containers).

**Scope**: Entire test assembly

**Execution**: Once before any tests, once after all tests

### How to Use

#### Step 1: Create an Assembly Fixture

```csharp
using TinyBDD;

public class DatabaseFixture : AssemblyFixture
{
    private TestDatabase? _database;

    public TestDatabase Database => _database
        ?? throw new InvalidOperationException("Database not initialized");

    protected override async Task SetupAsync(CancellationToken ct)
    {
        // Initialize expensive resource
        _database = new TestDatabase();
        await _database.StartAsync(ct);
        await _database.MigrateAsync(ct);
        await _database.SeedTestDataAsync(ct);
    }

    protected override async Task TeardownAsync(CancellationToken ct)
    {
        // Cleanup
        if (_database is not null)
        {
            await _database.StopAsync(ct);
            await _database.DisposeAsync();
        }
    }
}
```

#### Step 2: Register the Fixture

Add this to any file in your test project (typically `AssemblyInfo.cs` or at the top of a test file):

```csharp
[assembly: AssemblySetup(typeof(DatabaseFixture))]
```

#### Step 3: Access the Fixture in Tests

```csharp
public class UserRepositoryTests : TinyBddXunitBase
{
    [Fact]
    public async Task CanQueryUsers()
    {
        var db = AssemblyFixture.Get<DatabaseFixture>().Database;

        await Given("a user repository", () => new UserRepository(db))
            .When("querying all users", repo => repo.GetAllAsync())
            .Then("users are returned", users => users.Any())
            .AssertPassed();
    }
}
```

### Real-World Examples

#### Example 1: Test Web Server

```csharp
[assembly: AssemblySetup(typeof(WebServerFixture))]

public class WebServerFixture : AssemblyFixture
{
    private WebApplication? _app;
    private HttpClient? _client;

    public HttpClient Client => _client!;
    public string BaseUrl { get; private set; } = string.Empty;

    protected override async Task SetupAsync(CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:5555");

        _app = builder.Build();
        _app.MapGet("/health", () => "OK");
        await _app.StartAsync(ct);

        BaseUrl = "http://localhost:5555";
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    protected override async Task TeardownAsync(CancellationToken ct)
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.StopAsync(ct);
        _app?.Dispose();
    }
}
```

#### Example 2: Docker Container Setup

```csharp
[assembly: AssemblySetup(typeof(DockerContainerFixture))]

public class DockerContainerFixture : AssemblyFixture
{
    private string? _containerId;
    public string ConnectionString { get; private set; } = string.Empty;

    protected override async Task SetupAsync(CancellationToken ct)
    {
        // Start PostgreSQL container
        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "run -d -p 5432:5432 -e POSTGRES_PASSWORD=test postgres:15",
            RedirectStandardOutput = true
        };

        using var process = Process.Start(processInfo)!;
        _containerId = (await process.StandardOutput.ReadToEndAsync()).Trim();

        // Wait for container to be ready
        await Task.Delay(5000, ct);

        ConnectionString = "Host=localhost;Port=5432;Database=test;Username=postgres;Password=test";
    }

    protected override async Task TeardownAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_containerId))
        {
            var stopInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"rm -f {_containerId}"
            };
            using var process = Process.Start(stopInfo);
            await process!.WaitForExitAsync(ct);
        }
    }
}
```

#### Example 3: Multiple Assembly Fixtures

You can register multiple fixtures. They execute in registration order:

```csharp
[assembly: AssemblySetup(typeof(ConfigurationFixture))]
[assembly: AssemblySetup(typeof(DatabaseFixture))]
[assembly: AssemblySetup(typeof(CacheFixture))]
[assembly: AssemblySetup(typeof(WebServerFixture))]
```

---

## 2. Feature Setup and Teardown

**Purpose**: Initialize resources shared across all tests in a single test class (feature).

**Scope**: Test class

**Execution**: Once per test class

### How to Use

Override `ConfigureFeatureSetup()` and `ConfigureFeatureTeardown()` in your test class:

```csharp
public class ShoppingCartFeatureTests : TinyBddXunitBase
{
    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("test data seeded", async () =>
            {
                var db = AssemblyFixture.Get<DatabaseFixture>().Database;
                await db.SeedProductsAsync();
                return new { Database = db, ProductCount = 50 };
            })
            .And("shopping cart service initialized", async state =>
            {
                var service = new ShoppingCartService(state.Database);
                return new { state.Database, Service = service };
            });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("cleaning up test data", async () =>
            {
                var db = AssemblyFixture.Get<DatabaseFixture>().Database;
                await db.CleanupProductsAsync();
                return new object();
            });
    }

    [Fact]
    public async Task CanAddItemToCart()
    {
        await GivenFeature<dynamic>("the shopping cart service")
            .When("adding item to cart", async svc =>
                await svc.Service.AddItemAsync("product-1", 2))
            .Then("cart contains item", cart => cart.Items.Count == 1)
            .AssertPassed();
    }
}
```

### Real-World Examples

#### Example 1: API Client Feature

```csharp
[Feature("User Management API")]
public class UserApiTests : TinyBddXunitBase
{
    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("API client configured", () =>
            {
                var baseUrl = AssemblyFixture.Get<WebServerFixture>().BaseUrl;
                var client = new ApiClient(baseUrl);
                return client;
            })
            .And("authentication token obtained", async client =>
            {
                var token = await client.LoginAsync("admin", "password");
                client.SetToken(token);
                return client;
            });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("logging out", async () =>
        {
            if (FeatureState is ApiClient client)
                await client.LogoutAsync();
            return new object();
        });
    }

    [Fact]
    public async Task CanCreateUser()
    {
        await GivenFeature<ApiClient>("authenticated API client")
            .When("creating a user", async client =>
                await client.CreateUserAsync(new User { Name = "John" }))
            .Then("user is created", user => user.Id > 0)
            .AssertPassed();
    }

    [Fact]
    public async Task CanDeleteUser()
    {
        await GivenFeature<ApiClient>("authenticated API client")
            .When("deleting a user", async client =>
                await client.DeleteUserAsync(123))
            .Then("deletion succeeds", result => result)
            .AssertPassed();
    }
}
```

#### Example 2: Test Data Setup

```csharp
[Feature("Order Processing")]
public class OrderProcessingTests : TinyBddNUnitBase
{
    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("test customers created", async () =>
            {
                var db = AssemblyFixture.Get<DatabaseFixture>().Database;
                var customer1 = await db.CreateCustomerAsync("Alice");
                var customer2 = await db.CreateCustomerAsync("Bob");
                return new { Database = db, Customer1 = customer1, Customer2 = customer2 };
            })
            .And("test products created", async state =>
            {
                var product1 = await state.Database.CreateProductAsync("Widget", 9.99m);
                var product2 = await state.Database.CreateProductAsync("Gadget", 19.99m);
                return new
                {
                    state.Database,
                    state.Customer1,
                    state.Customer2,
                    Product1 = product1,
                    Product2 = product2
                };
            });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("cleaning test data", async () =>
        {
            if (FeatureState is dynamic state)
            {
                await state.Database.DeleteAllOrdersAsync();
                await state.Database.DeleteAllProductsAsync();
                await state.Database.DeleteAllCustomersAsync();
            }
            return new object();
        });
    }

    [Test]
    public async Task CanPlaceOrder()
    {
        await GivenFeature<dynamic>("test data")
            .When("placing an order", async data =>
            {
                var order = new Order
                {
                    CustomerId = data.Customer1.Id,
                    Items = { new OrderItem { ProductId = data.Product1.Id, Quantity = 2 } }
                };
                return await data.Database.CreateOrderAsync(order);
            })
            .Then("order is created", (Order order) => order.Id > 0)
            .AssertPassed();
    }
}
```

---

## 3. Scenario Background

**Purpose**: Setup context that runs before each test method, providing fresh state per scenario.

**Scope**: Per test method

**Execution**: Before each test

### How to Use

Override `ConfigureBackground()` in your test class:

```csharp
public class UserServiceTests : TinyBddXunitBase
{
    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("a database connection", () =>
            {
                var db = AssemblyFixture.Get<DatabaseFixture>().Database;
                return db;
            })
            .And("a user service", db => new UserService(db));
    }

    [Fact]
    public async Task CanGetUserById()
    {
        await GivenBackground<UserService>("the user service")
            .When("getting user by id", svc => svc.GetByIdAsync(1))
            .Then("user is found", user => user is not null)
            .AssertPassed();
    }
}
```

### Real-World Examples

#### Example 1: Fresh Database Transaction

```csharp
public class TransactionTests : TinyBddXunitBase
{
    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("a database transaction", async () =>
            {
                var db = AssemblyFixture.Get<DatabaseFixture>().Database;
                var transaction = await db.BeginTransactionAsync();
                return new { Database = db, Transaction = transaction };
            })
            .Finally("rollback transaction", async state =>
            {
                // Cleanup: rollback to ensure test isolation
                await state.Transaction.RollbackAsync();
                await state.Transaction.DisposeAsync();
            });
    }

    [Fact]
    public async Task InsertDoesNotAffectOtherTests()
    {
        await GivenBackground<dynamic>("a transaction")
            .When("inserting a record", async state =>
            {
                await state.Database.InsertAsync(new User { Name = "Test" });
                return state;
            })
            .Then("record exists in transaction", async state =>
                await state.Database.CountUsersAsync() == 1)
            .AssertPassed();

        // After test: transaction is rolled back, no data persisted
    }
}
```

#### Example 2: Fresh Test Context

```csharp
public class CalculatorTests : TinyBddMsTestBase
{
    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("a calculator instance", () => new Calculator())
            .And("initial state reset", calc =>
            {
                calc.Clear();
                return calc;
            });
    }

    [TestMethod]
    public async Task CanAdd()
    {
        await GivenBackground<Calculator>("a calculator")
            .When("adding 2 + 3", calc => calc.Add(2, 3))
            .Then("result is 5", result => result == 5)
            .AssertPassed();
    }

    [TestMethod]
    public async Task CanSubtract()
    {
        await GivenBackground<Calculator>("a calculator")
            .When("subtracting 5 - 3", calc => calc.Subtract(5, 3))
            .Then("result is 2", result => result == 2)
            .AssertPassed();
    }
}
```

---

## 4. Scenario Teardown (Finally)

**Purpose**: Cleanup resources created during the scenario, even if the test fails.

**Scope**: Per scenario chain

**Execution**: After all steps in the chain, even on failure

### How to Use

Use the `.Finally()` method anywhere in your scenario chain:

```csharp
[Fact]
public async Task FileHandlingTest()
{
    await Given("a temp file", () => File.CreateText("temp.txt"))
        .Finally("cleanup file", file =>
        {
            file.Close();
            File.Delete("temp.txt");
        })
        .When("writing data", file =>
        {
            file.WriteLine("test data");
            file.Flush();
            return file;
        })
        .Then("file has content", file =>
        {
            file.Close();
            return File.ReadAllText("temp.txt").Contains("test data");
        })
        .AssertPassed();

    // File is deleted even if test fails
}
```

### Real-World Examples

#### Example 1: HTTP Request Lifecycle

```csharp
[Fact]
public async Task ApiRequestWithCleanup()
{
    await Given("an HTTP request", () => new HttpRequestMessage(HttpMethod.Get, "/api/users"))
        .Finally("dispose request", req => req.Dispose())
        .When("sending request", async req =>
        {
            var client = AssemblyFixture.Get<WebServerFixture>().Client;
            return await client.SendAsync(req);
        })
        .Finally("dispose response", resp => resp.Dispose())
        .Then("response is successful", resp => resp.IsSuccessStatusCode)
        .AssertPassed();
}
```

#### Example 2: Database Connection

```csharp
[Fact]
public async Task DatabaseQueryWithConnection()
{
    await Given("a database connection", async () =>
        {
            var conn = new NpgsqlConnection("...");
            await conn.OpenAsync();
            return conn;
        })
        .Finally("close connection", async conn => await conn.DisposeAsync())
        .When("executing query", async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM users";
            return await cmd.ExecuteScalarAsync();
        })
        .Then("count is positive", count => (long)count! > 0)
        .AssertPassed();
}
```

#### Example 3: Multiple Cleanup Handlers

```csharp
[Fact]
public async Task MultipleCleanupHandlers()
{
    var log = new List<string>();

    await Given("resource 1", () => "file.txt")
        .Finally("cleanup 1", _ => log.Add("cleanup-1"))
        .When("resource 2", r1 => (r1, "socket"))
        .Finally("cleanup 2", _ => log.Add("cleanup-2"))
        .Then("resources ready", resources => resources != default)
        .Finally("cleanup 3", _ => log.Add("cleanup-3"))
        .AssertPassed();

    // Cleanup order: ["cleanup-1", "cleanup-2", "cleanup-3"]
    Assert.Equal(new[] { "cleanup-1", "cleanup-2", "cleanup-3" }, log);
}
```

---

## Complete Example: E-Commerce Test Suite

Here's a complete example showing all layers working together:

```csharp
// Assembly-level fixtures
[assembly: AssemblySetup(typeof(PostgresFixture))]
[assembly: AssemblySetup(typeof(RedisFixture))]
[assembly: AssemblySetup(typeof(MessageQueueFixture))]

// Fixtures
public class PostgresFixture : AssemblyFixture
{
    public string ConnectionString { get; private set; } = string.Empty;

    protected override async Task SetupAsync(CancellationToken ct)
    {
        // Start PostgreSQL container
        ConnectionString = await StartPostgresContainerAsync(ct);
        await MigrateDatabaseAsync(ct);
    }

    protected override async Task TeardownAsync(CancellationToken ct)
    {
        await StopPostgresContainerAsync(ct);
    }
}

// Feature-level setup
[Feature("Order Processing", "Tests for order creation and fulfillment")]
public class OrderProcessingFeature : TinyBddXunitBase
{
    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("test customers seeded", async () =>
            {
                var connStr = AssemblyFixture.Get<PostgresFixture>().ConnectionString;
                var db = new Database(connStr);

                var customers = new[]
                {
                    await db.CreateCustomerAsync("Alice", "alice@test.com"),
                    await db.CreateCustomerAsync("Bob", "bob@test.com")
                };

                return new { Database = db, Customers = customers };
            })
            .And("test products seeded", async state =>
            {
                var products = new[]
                {
                    await state.Database.CreateProductAsync("Widget", 9.99m, stock: 100),
                    await state.Database.CreateProductAsync("Gadget", 19.99m, stock: 50)
                };

                return new
                {
                    state.Database,
                    state.Customers,
                    Products = products
                };
            });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("cleaning test data", async () =>
        {
            if (FeatureState is dynamic state)
            {
                await state.Database.DeleteAllOrdersAsync();
                await state.Database.DeleteAllProductsAsync();
                await state.Database.DeleteAllCustomersAsync();
            }
            return new object();
        });
    }

    // Background for each scenario
    protected override ScenarioChain<object>? ConfigureBackground()
    {
        return Given("an order service", () =>
        {
            var db = (FeatureState as dynamic)!.Database;
            var mq = AssemblyFixture.Get<MessageQueueFixture>().Queue;
            return new OrderService(db, mq);
        });
    }

    [Scenario("Customer can place an order")]
    [Fact]
    public async Task Customer_Can_Place_Order()
    {
        await GivenBackground<OrderService>("the order service")
            .And("a customer", _ => (FeatureState as dynamic)!.Customers[0])
            .And("a product", _ => (FeatureState as dynamic)!.Products[0])
            .When("placing an order", async data =>
            {
                var (service, customer, product) = data;
                return await service.CreateOrderAsync(customer.Id, product.Id, quantity: 2);
            })
            .Then("order is created", (Order order) => order.Id > 0)
            .And("order total is correct", order => order.Total == 19.98m)
            .And("inventory is decremented", async order =>
            {
                var product = (FeatureState as dynamic)!.Products[0];
                var updated = await ((dynamic)FeatureState).Database.GetProductAsync(product.Id);
                return updated.Stock == 98;
            })
            .Finally("cancel order for cleanup", async order =>
            {
                await GivenBackground<OrderService>().CancelOrderAsync(order.Id);
            })
            .AssertPassed();
    }

    [Scenario("Order processing sends notification")]
    [Fact]
    public async Task Order_Processing_Sends_Notification()
    {
        await GivenBackground<OrderService>("the order service")
            .When("an order is processed", async service =>
            {
                var customer = (FeatureState as dynamic)!.Customers[0];
                var product = (FeatureState as dynamic)!.Products[0];
                return await service.CreateOrderAsync(customer.Id, product.Id, 1);
            })
            .Then("notification is queued", async order =>
            {
                var mq = AssemblyFixture.Get<MessageQueueFixture>().Queue;
                var messages = await mq.GetMessagesAsync("order-notifications");
                return messages.Any(m => m.OrderId == order.Id);
            })
            .AssertPassed();
    }
}
```

---

## Framework-Specific Notes

### xUnit

- **Feature Setup/Teardown**: Managed with static state and semaphores due to xUnit's per-test instance model
- **Assembly Fixtures**: Not built-in; must be managed manually or via collection fixtures
- **Recommendation**: Use `IClassFixture<T>` for more complex feature-level state

### MSTest

- **Feature Setup/Teardown**: Uses `[ClassInitialize]` and `[ClassCleanup]`
- **Assembly Fixtures**: Uses `[AssemblyInitialize]` and `[AssemblyCleanup]`
- **Limitation**: Class-level methods are static, limiting access to instance members

### NUnit

- **Feature Setup/Teardown**: Uses `[OneTimeSetUp]` and `[OneTimeTearDown]`
- **Assembly Fixtures**: Uses `[SetUpFixture]` at assembly or namespace level
- **Best Support**: NUnit has the most natural fit for all lifecycle layers

---

## Best Practices

### 1. **Choose the Right Layer**

- **Assembly**: Database containers, web servers, expensive global resources
- **Feature**: Test data for a feature, authenticated clients, feature-specific setup
- **Background**: Fresh instances, transactions, scenario-specific context
- **Finally**: Resource cleanup, file deletion, connection disposal

### 2. **Minimize Assembly Fixtures**

Assembly fixtures slow down test startup. Only use them for truly expensive resources that can't be recreated quickly.

### 3. **Ensure Cleanup**

Always pair setup with teardown:
- Assembly: Implement `TeardownAsync()` to stop containers, close connections
- Feature: Implement `ConfigureFeatureTeardown()` to clean test data
- Scenario: Use `.Finally()` to dispose resources

### 4. **Avoid State Pollution**

- Background runs per-test, providing fresh state
- Feature state is shared; be careful about mutations
- Assembly state is global; design it to be read-only or thread-safe

### 5. **Use Finally for Deterministic Cleanup**

```csharp
// Good: Guaranteed cleanup
await Given("resource", () => CreateResource())
    .Finally("cleanup", r => r.Dispose())
    .When("use resource", r => r.DoWork())
    .AssertPassed();

// Bad: Cleanup may not run if test fails
await Given("resource", () => CreateResource())
    .When("use resource", r => r.DoWork())
    .Then("cleanup", r => { r.Dispose(); return true; })
    .AssertPassed();
```

### 6. **Document Your Lifecycle**

Add XML comments to your setup methods explaining what resources are initialized and why:

```csharp
/// <summary>
/// Feature setup: Seeds test customers and products that are shared across
/// all order processing scenarios. Cleanup removes all test data.
/// </summary>
protected override ScenarioChain<object>? ConfigureFeatureSetup() { ... }
```

---

## Troubleshooting

### "Feature setup has not been executed"

- **Cause**: Calling `GivenFeature<T>()` before feature setup runs
- **Solution**: Ensure your test framework adapter calls `ExecuteFeatureSetupAsync()` in the right hook

### "Assembly fixture not registered"

- **Cause**: Missing `[assembly: AssemblySetup(typeof(...))]` attribute
- **Solution**: Add the attribute to a file in your test project

### Feature state is null

- **Cause**: `ConfigureFeatureSetup()` returned `null` or didn't set state
- **Solution**: Ensure your setup chain has a `Then()` step that captures state

### Background not executing

- **Cause**: Framework adapter not calling `ExecuteBackgroundAsync()`
- **Solution**: Check that your test base class properly initializes

---

## Summary

TinyBDD's multi-layered setup/teardown strategy provides:

✅ **Declarative** - Express setup as BDD steps
✅ **Framework-agnostic** - Works with xUnit, NUnit, MSTest
✅ **Performant** - Share expensive resources, isolate cheap ones
✅ **Ubiquitous** - Seven layers cover all testing needs
✅ **Clean** - Automatic cleanup with `Finally()`
✅ **Well-tested** - Comprehensive test coverage
✅ **Production-ready** - Used in real-world scenarios

Choose the right layer for your needs, and enjoy clean, maintainable tests!
