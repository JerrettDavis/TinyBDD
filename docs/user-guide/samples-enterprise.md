# Enterprise Integration Samples

This page provides production-ready code samples demonstrating TinyBDD as an application orchestrator in enterprise scenarios.

## E-Commerce Order Processing

A complete order fulfillment workflow with payment, inventory, and shipping coordination.

### Workflow Definition

```csharp
public class OrderFulfillmentWorkflow : IWorkflowDefinition
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _payments;
    private readonly IInventoryService _inventory;
    private readonly IShippingService _shipping;
    private readonly INotificationService _notifications;
    private readonly ILogger<OrderFulfillmentWorkflow> _logger;

    public OrderFulfillmentWorkflow(
        IOrderRepository orders,
        IPaymentGateway payments,
        IInventoryService inventory,
        IShippingService shipping,
        INotificationService notifications,
        ILogger<OrderFulfillmentWorkflow> logger)
    {
        _orders = orders;
        _payments = payments;
        _inventory = inventory;
        _shipping = shipping;
        _notifications = notifications;
        _logger = logger;
    }

    public string FeatureName => "Order Fulfillment";
    public string ScenarioName => "Process customer order";
    public string? FeatureDescription => "End-to-end order processing workflow";

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "pending orders retrieved",
                () => _orders.GetPendingAsync(ct))
            .When("each order validated", orders =>
                ValidateOrdersAsync(orders, ct))
            .When("payments processed", orders =>
                ProcessPaymentsAsync(orders, ct))
            .And("inventory reserved", orders =>
                ReserveInventoryAsync(orders, ct))
            .When("shipments created", orders =>
                CreateShipmentsAsync(orders, ct))
            .Then("customers notified", orders =>
                NotifyCustomersAsync(orders, ct));

        LogWorkflowResults(context);
    }

    private async Task<IList<Order>> ValidateOrdersAsync(
        IList<Order> orders, CancellationToken ct)
    {
        var validOrders = new List<Order>();
        foreach (var order in orders)
        {
            if (await _orders.ValidateAsync(order, ct))
            {
                validOrders.Add(order);
            }
            else
            {
                _logger.LogWarning("Order {OrderId} failed validation", order.Id);
            }
        }
        return validOrders;
    }

    private async Task<IList<Order>> ProcessPaymentsAsync(
        IList<Order> orders, CancellationToken ct)
    {
        foreach (var order in orders)
        {
            var result = await _payments.ChargeAsync(
                order.PaymentMethod,
                order.Total,
                ct);

            order.PaymentConfirmation = result.ConfirmationId;
            order.Status = OrderStatus.Paid;
            await _orders.UpdateAsync(order, ct);
        }
        return orders;
    }

    private async Task<IList<Order>> ReserveInventoryAsync(
        IList<Order> orders, CancellationToken ct)
    {
        foreach (var order in orders)
        {
            foreach (var item in order.Items)
            {
                await _inventory.ReserveAsync(item.Sku, item.Quantity, ct);
            }
            order.InventoryReserved = true;
        }
        return orders;
    }

    private async Task<IList<Order>> CreateShipmentsAsync(
        IList<Order> orders, CancellationToken ct)
    {
        foreach (var order in orders)
        {
            var shipment = await _shipping.CreateShipmentAsync(order, ct);
            order.TrackingNumber = shipment.TrackingNumber;
            order.Status = OrderStatus.Shipped;
            await _orders.UpdateAsync(order, ct);
        }
        return orders;
    }

    private async Task<bool> NotifyCustomersAsync(
        IList<Order> orders, CancellationToken ct)
    {
        foreach (var order in orders)
        {
            await _notifications.SendOrderConfirmationAsync(order, ct);
        }
        return true;
    }

    private void LogWorkflowResults(ScenarioContext context)
    {
        foreach (var step in context.Steps)
        {
            _logger.LogInformation(
                "[{Kind}] {Title}: {Status} ({Duration}ms)",
                step.Kind,
                step.Title,
                step.Error == null ? "OK" : "FAILED",
                step.Elapsed.TotalMilliseconds);
        }
    }
}
```

### Service Registration

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Add infrastructure services
builder.Services.AddDbContext<OrderDbContext>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddHttpClient<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IShippingService, ShippoShippingService>();
builder.Services.AddScoped<INotificationService, SendGridNotificationService>();

// Add TinyBDD hosting
builder.Services.AddTinyBddHosting(options =>
{
    options.StopHostOnFailure = false; // Continue processing other orders
});

// Register the workflow as a hosted service
builder.Services.AddWorkflowHostedService<OrderFulfillmentWorkflow>();

var host = builder.Build();
await host.RunAsync();
```

---

## Data Pipeline / ETL Workflow

An ETL workflow for synchronizing data between systems.

```csharp
public class DataSyncWorkflow : IWorkflowDefinition
{
    private readonly ISourceSystem _source;
    private readonly ITargetSystem _target;
    private readonly ITransformationEngine _transformer;
    private readonly ILogger<DataSyncWorkflow> _logger;

    public DataSyncWorkflow(
        ISourceSystem source,
        ITargetSystem target,
        ITransformationEngine transformer,
        ILogger<DataSyncWorkflow> logger)
    {
        _source = source;
        _target = target;
        _transformer = transformer;
        _logger = logger;
    }

    public string FeatureName => "Data Synchronization";
    public string ScenarioName => "ETL Pipeline";
    public string? FeatureDescription => "Extract, transform, and load data between systems";

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "sync window determined",
                () => GetSyncWindow())
            .When("source data extracted", window =>
                ExtractAsync(window, ct))
            .And("data validated", records =>
                ValidateAsync(records))
            .When("transformations applied", records =>
                TransformAsync(records, ct))
            .And("duplicates removed", records =>
                DeduplicateAsync(records))
            .When("data loaded to target", records =>
                LoadAsync(records, ct))
            .Then("sync completed", result =>
                result.SuccessCount > 0);

        LogSyncSummary(context);
    }

    private SyncWindow GetSyncWindow()
    {
        return new SyncWindow
        {
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow
        };
    }

    private async Task<IList<SourceRecord>> ExtractAsync(
        SyncWindow window, CancellationToken ct)
    {
        _logger.LogInformation(
            "Extracting records from {Start} to {End}",
            window.StartTime, window.EndTime);

        return await _source.GetRecordsAsync(window, ct);
    }

    private Task<IList<SourceRecord>> ValidateAsync(IList<SourceRecord> records)
    {
        var valid = records.Where(r => r.IsValid).ToList();
        _logger.LogInformation(
            "Validated {Valid}/{Total} records",
            valid.Count, records.Count);
        return Task.FromResult<IList<SourceRecord>>(valid);
    }

    private async Task<IList<TargetRecord>> TransformAsync(
        IList<SourceRecord> records, CancellationToken ct)
    {
        var transformed = new List<TargetRecord>();
        foreach (var record in records)
        {
            transformed.Add(await _transformer.TransformAsync(record, ct));
        }
        return transformed;
    }

    private Task<IList<TargetRecord>> DeduplicateAsync(IList<TargetRecord> records)
    {
        var unique = records
            .GroupBy(r => r.UniqueKey)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToList();
        return Task.FromResult<IList<TargetRecord>>(unique);
    }

    private async Task<LoadResult> LoadAsync(
        IList<TargetRecord> records, CancellationToken ct)
    {
        return await _target.BulkInsertAsync(records, ct);
    }

    private void LogSyncSummary(ScenarioContext context)
    {
        var totalDuration = context.Steps.Sum(s => s.Elapsed.TotalMilliseconds);
        _logger.LogInformation(
            "Sync completed in {Duration}ms across {Steps} steps",
            totalDuration, context.Steps.Count);
    }
}
```

---

## API Health Check Workflow

A comprehensive health check workflow for microservices.

```csharp
public class HealthCheckWorkflow : IWorkflowDefinition
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IDbConnection _database;
    private readonly IDistributedCache _cache;
    private readonly IMessageBus _messageBus;

    public string FeatureName => "System Health Check";
    public string ScenarioName => "Verify all dependencies";
    public string? FeatureDescription => null;

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "health check initiated",
                () => new HealthReport())
            .When("database connectivity verified", report =>
                CheckDatabaseAsync(report, ct))
            .And("cache connectivity verified", report =>
                CheckCacheAsync(report, ct))
            .And("message bus connectivity verified", report =>
                CheckMessageBusAsync(report, ct))
            .And("external APIs verified", report =>
                CheckExternalApisAsync(report, ct))
            .Then("all systems operational", report =>
                report.IsHealthy);
    }

    private async Task<HealthReport> CheckDatabaseAsync(
        HealthReport report, CancellationToken ct)
    {
        var check = new HealthCheck { Name = "Database" };
        try
        {
            await _database.ExecuteAsync("SELECT 1", ct);
            check.Status = HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            check.Status = HealthStatus.Unhealthy;
            check.Error = ex.Message;
        }
        report.Checks.Add(check);
        return report;
    }

    private async Task<HealthReport> CheckCacheAsync(
        HealthReport report, CancellationToken ct)
    {
        var check = new HealthCheck { Name = "Cache" };
        try
        {
            var key = $"health-check-{Guid.NewGuid()}";
            await _cache.SetAsync(key, "test", TimeSpan.FromSeconds(5), ct);
            await _cache.RemoveAsync(key, ct);
            check.Status = HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            check.Status = HealthStatus.Unhealthy;
            check.Error = ex.Message;
        }
        report.Checks.Add(check);
        return report;
    }

    private async Task<HealthReport> CheckMessageBusAsync(
        HealthReport report, CancellationToken ct)
    {
        var check = new HealthCheck { Name = "MessageBus" };
        try
        {
            await _messageBus.PingAsync(ct);
            check.Status = HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            check.Status = HealthStatus.Unhealthy;
            check.Error = ex.Message;
        }
        report.Checks.Add(check);
        return report;
    }

    private async Task<HealthReport> CheckExternalApisAsync(
        HealthReport report, CancellationToken ct)
    {
        var endpoints = new[]
        {
            ("PaymentAPI", "https://api.stripe.com/health"),
            ("ShippingAPI", "https://api.shippo.com/health"),
            ("NotificationAPI", "https://api.sendgrid.com/health")
        };

        foreach (var (name, url) in endpoints)
        {
            var check = new HealthCheck { Name = name };
            try
            {
                var client = _httpFactory.CreateClient();
                var response = await client.GetAsync(url, ct);
                check.Status = response.IsSuccessStatusCode
                    ? HealthStatus.Healthy
                    : HealthStatus.Degraded;
            }
            catch (Exception ex)
            {
                check.Status = HealthStatus.Unhealthy;
                check.Error = ex.Message;
            }
            report.Checks.Add(check);
        }

        return report;
    }
}

public class HealthReport
{
    public List<HealthCheck> Checks { get; } = new();
    public bool IsHealthy => Checks.All(c => c.Status == HealthStatus.Healthy);
}

public class HealthCheck
{
    public string Name { get; set; } = "";
    public HealthStatus Status { get; set; }
    public string? Error { get; set; }
}

public enum HealthStatus { Healthy, Degraded, Unhealthy }
```

---

## Batch Processing Workflow

Process large datasets in batches with progress tracking.

```csharp
public class BatchProcessingWorkflow
{
    private readonly IScenarioContextFactory _factory;
    private readonly IBatchProcessor _processor;
    private readonly IProgressReporter _progress;

    public async Task<BatchResult> ProcessLargeDataset(
        IAsyncEnumerable<DataRecord> records,
        int batchSize,
        CancellationToken ct)
    {
        var context = _factory.Create("Batch Processing", "Large dataset");
        var batches = new List<BatchSummary>();
        var currentBatch = new List<DataRecord>();
        var batchNumber = 0;

        await foreach (var record in records.WithCancellation(ct))
        {
            currentBatch.Add(record);

            if (currentBatch.Count >= batchSize)
            {
                batchNumber++;
                var summary = await ProcessBatchAsync(
                    context, batchNumber, currentBatch, ct);
                batches.Add(summary);
                currentBatch.Clear();

                _progress.Report(batchNumber, batches.Sum(b => b.ProcessedCount));
            }
        }

        // Process remaining records
        if (currentBatch.Count > 0)
        {
            batchNumber++;
            var summary = await ProcessBatchAsync(
                context, batchNumber, currentBatch, ct);
            batches.Add(summary);
        }

        return new BatchResult
        {
            TotalBatches = batchNumber,
            TotalProcessed = batches.Sum(b => b.ProcessedCount),
            TotalFailed = batches.Sum(b => b.FailedCount),
            Context = context
        };
    }

    private async Task<BatchSummary> ProcessBatchAsync(
        ScenarioContext context,
        int batchNumber,
        List<DataRecord> batch,
        CancellationToken ct)
    {
        var summary = new BatchSummary { BatchNumber = batchNumber };

        await Bdd.Given(context, $"batch {batchNumber} loaded ({batch.Count} records)",
                () => batch)
            .When("records validated", records =>
                _processor.ValidateAsync(records, ct))
            .When("records transformed", records =>
                _processor.TransformAsync(records, ct))
            .When("records persisted", records =>
                _processor.PersistAsync(records, ct))
            .Then("batch complete", result =>
            {
                summary.ProcessedCount = result.SuccessCount;
                summary.FailedCount = result.FailedCount;
                return result.SuccessCount > 0;
            });

        return summary;
    }
}
```

---

## Scheduled Job Workflow

A workflow that runs on a schedule with idempotency.

```csharp
public class ScheduledReportWorkflow : BackgroundService
{
    private readonly IWorkflowRunner _runner;
    private readonly IReportGenerator _reports;
    private readonly IEmailService _email;
    private readonly IDistributedLock _lock;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until next scheduled time (e.g., 6 AM)
            await WaitUntilNextRunAsync(stoppingToken);

            // Acquire distributed lock for idempotency
            await using var lockHandle = await _lock.TryAcquireAsync(
                "daily-report-workflow",
                TimeSpan.FromMinutes(30),
                stoppingToken);

            if (lockHandle == null)
            {
                // Another instance is running
                continue;
            }

            await _runner.RunAsync(
                "Daily Reports",
                "Generate and distribute reports",
                async (context, ct) =>
                {
                    await Bdd.Given(context, "report date determined",
                            () => DateTime.UtcNow.Date.AddDays(-1))
                        .When("sales report generated", date =>
                            _reports.GenerateSalesReportAsync(date, ct))
                        .And("inventory report generated", date =>
                            _reports.GenerateInventoryReportAsync(date, ct))
                        .And("financial summary generated", date =>
                            _reports.GenerateFinancialSummaryAsync(date, ct))
                        .When("reports bundled", reports =>
                            BundleReportsAsync(reports))
                        .Then("reports distributed", bundle =>
                            DistributeReportsAsync(bundle, ct));
                },
                stoppingToken);
        }
    }

    private async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddDays(1).AddHours(6); // 6 AM UTC
        if (now.Hour < 6)
            nextRun = now.Date.AddHours(6);

        var delay = nextRun - now;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
    }
}
```

---

## Testing Enterprise Workflows

```csharp
public class OrderFulfillmentWorkflowTests
{
    private ServiceProvider BuildServiceProvider(
        Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTinyBddHosting();

        // Default fakes
        services.AddSingleton<IOrderRepository>(new FakeOrderRepository());
        services.AddSingleton<IPaymentGateway>(new FakePaymentGateway());
        services.AddSingleton<IInventoryService>(new FakeInventoryService());
        services.AddSingleton<IShippingService>(new FakeShippingService());
        services.AddSingleton<INotificationService>(new FakeNotificationService());
        services.AddScoped<OrderFulfillmentWorkflow>();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessOrders_WithValidOrders_CompletesSuccessfully()
    {
        // Arrange
        var provider = BuildServiceProvider(services =>
        {
            var repo = new FakeOrderRepository();
            repo.AddPendingOrder(new Order { Id = Guid.NewGuid(), Total = 100 });
            services.AddSingleton<IOrderRepository>(repo);
        });

        var runner = provider.GetRequiredService<IWorkflowRunner>();
        var workflow = provider.GetRequiredService<OrderFulfillmentWorkflow>();

        // Act
        var context = await runner.RunAsync(workflow);

        // Assert
        Assert.True(context.Steps.All(s => s.Error == null));
        Assert.Equal(6, context.Steps.Count);
    }

    [Fact]
    public async Task ProcessOrders_WithPaymentFailure_RecordsErrorAndContinues()
    {
        // Arrange
        var provider = BuildServiceProvider(services =>
        {
            services.AddSingleton<IPaymentGateway>(
                new FakePaymentGateway { ShouldFail = true });
        });

        var runner = provider.GetRequiredService<IWorkflowRunner>();
        var workflow = provider.GetRequiredService<OrderFulfillmentWorkflow>();

        // Act & Assert
        await Assert.ThrowsAsync<BddStepException>(
            () => runner.RunAsync(workflow));
    }

    [Fact]
    public async Task ProcessOrders_MeasuresStepPerformance()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var runner = provider.GetRequiredService<IWorkflowRunner>();
        var workflow = provider.GetRequiredService<OrderFulfillmentWorkflow>();

        // Act
        var context = await runner.RunAsync(workflow);

        // Assert
        Assert.All(context.Steps, step =>
        {
            Assert.True(step.Elapsed.TotalMilliseconds >= 0);
            Assert.True(step.Elapsed.TotalMilliseconds < 5000); // No step over 5s
        });
    }
}
```

## Next Steps

- [Orchestrator Patterns](orchestrator-patterns.md) - Design patterns for workflows
- [Dependency Injection](extensions/dependency-injection.md) - DI integration
- [Hosting](extensions/hosting.md) - Background service patterns

Return to: [User Guide](index.md)
