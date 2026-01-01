# Enterprise Integration

This guide covers production-ready patterns for using PatternKit in enterprise applications with dependency injection, hosting, and observability.

## ASP.NET Core Web API

### Complete Setup

```csharp
// Program.cs
using PatternKit.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add PatternKit with hosting
builder.Services.AddPatternKitHosting(
    configurePatternKit: options =>
    {
        options.DefaultWorkflowOptions = new WorkflowOptions
        {
            StepTimeout = TimeSpan.FromSeconds(30),
            ContinueOnError = false
        };
        options.EnableMetrics = true;
    },
    configureHosting: options =>
    {
        options.EnableParallelExecution = true;
        options.ContinueOnFailure = true;
    });

// Register step handlers
builder.Services
    .AddStepHandler<CreateOrderRequest, Order, CreateOrderHandler>()
    .AddStepHandler<ProcessPaymentRequest, PaymentResult, ProcessPaymentHandler>()
    .AddStepHandler<SendNotificationRequest, Unit, SendNotificationHandler>();

// Register behaviors
builder.Services
    .AddTimingBehavior<Order>()
    .AddRetryBehavior<PaymentResult>(maxRetries: 3)
    .AddCircuitBreakerBehavior<ApiResponse>(failureThreshold: 5);

// Register workflows
builder.Services
    .AddWorkflow<OrderProcessingWorkflow>()
    .AddRecurringWorkflow<HealthCheckWorkflow>();

var app = builder.Build();

// API endpoints
app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    IWorkflowContextFactory contextFactory,
    IStepHandlerFactory handlerFactory,
    ILogger<Program> logger) =>
{
    var context = contextFactory.Create(
        "CreateOrder",
        $"Creating order for customer {request.CustomerId}");

    await using (context)
    {
        try
        {
            var order = await Workflow
                .Given(context, "order request", () => request)
                .Handle("create order", handlerFactory, r =>
                    new CreateOrderRequest(r.CustomerId, r.Items))
                .Handle("process payment", handlerFactory, order =>
                    new ProcessPaymentRequest(order, request.PaymentMethod))
                .Handle("send confirmation", handlerFactory, order =>
                    new SendNotificationRequest(order.CustomerId, $"Order {order.Id} confirmed"))
                .Then("completed", order => order.Status == "Confirmed")
                .GetResultAsync();

            logger.LogInformation(
                "Order {OrderId} created in {Duration}ms",
                order.Id,
                context.TotalElapsed().TotalMilliseconds);

            return Results.Created($"/api/orders/{order.Id}", order);
        }
        catch (WorkflowStepException ex)
        {
            logger.LogError(ex,
                "Order creation failed at step: {Step}",
                context.FirstFailure?.Title);

            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }
});

app.Run();
```

## Worker Service

### Background Processing

```csharp
// Program.cs
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Infrastructure
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<IPaymentGateway, PaymentGateway>();
        services.AddSingleton<INotificationService, NotificationService>();

        // PatternKit
        services.AddPatternKitHosting(
            configureHosting: options =>
            {
                options.StartAutomatically = true;
                options.EnableParallelExecution = true;
                options.MaxDegreeOfParallelism = 4;
            })
            .AddRecurringWorkflow<PendingOrderProcessor>()
            .AddRecurringWorkflow<HealthMonitor>()
            .AddRecurringWorkflow<MetricsCollector>();
    })
    .Build();

await host.RunAsync();

// Workflows
public class PendingOrderProcessor : IRecurringWorkflowDefinition
{
    private readonly IOrderRepository _orders;
    private readonly IPaymentGateway _payment;
    private readonly ILogger<PendingOrderProcessor> _logger;

    public string Name => "PendingOrderProcessor";
    public string? Description => "Processes orders awaiting payment";
    public TimeSpan Interval => TimeSpan.FromSeconds(30);
    public bool RunImmediately => true;

    public PendingOrderProcessor(
        IOrderRepository orders,
        IPaymentGateway payment,
        ILogger<PendingOrderProcessor> logger)
    {
        _orders = orders;
        _payment = payment;
        _logger = logger;
    }

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        var pendingOrders = await _orders.GetPendingAsync(cancellationToken);
        _logger.LogInformation("Processing {Count} pending orders", pendingOrders.Count);

        foreach (var order in pendingOrders)
        {
            try
            {
                await Workflow
                    .Given(context, "pending order", () => order)
                    .When("process payment", async o =>
                    {
                        var result = await _payment.ChargeAsync(o, cancellationToken);
                        if (!result.Success)
                            throw new PaymentException(result.ErrorMessage);
                        o.PaymentId = result.TransactionId;
                        o.Status = "Paid";
                        return o;
                    })
                    .When("update status", async o =>
                    {
                        await _orders.UpdateAsync(o, cancellationToken);
                        return o;
                    })
                    .Then("processed", o => o.Status == "Paid");

                _logger.LogInformation("Order {OrderId} processed", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process order {OrderId}", order.Id);
            }
        }
    }
}
```

## Microservices Communication

### With HTTP Client

```csharp
public class ExternalApiWorkflow : IWorkflowDefinition<ApiResponse>
{
    private readonly HttpClient _httpClient;
    private readonly CircuitBreakerBehavior<ApiResponse> _circuitBreaker;

    public string Name => "ExternalApiCall";

    public ExternalApiWorkflow(
        IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ExternalApi");
        _circuitBreaker = new CircuitBreakerBehavior<ApiResponse>(
            failureThreshold: 3,
            openDuration: TimeSpan.FromMinutes(1));
    }

    public async ValueTask<ApiResponse> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        return await Workflow
            .Given(context, "api request", () => new ApiRequest { /* ... */ })
            .When("call external api", async request =>
            {
                // Circuit breaker wraps the call
                return await _circuitBreaker.WrapAsync(
                    async ct =>
                    {
                        var response = await _httpClient.PostAsJsonAsync(
                            "/api/process",
                            request,
                            ct);

                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadFromJsonAsync<ApiResponse>(ct);
                    },
                    context,
                    new StepMetadata { Title = "call external api" },
                    cancellationToken);
            })
            .Then("successful", response => response?.Success == true)
            .GetResultAsync(cancellationToken);
    }
}
```

### With Message Queue

```csharp
public class MessageProcessor : IWorkflowDefinition
{
    private readonly IMessageQueue _queue;
    private readonly IMessageHandler _handler;

    public string Name => "MessageProcessor";

    public async ValueTask ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        await Workflow
            .Given(context, "message queue", () => _queue)
            .When("receive message", async queue =>
            {
                var message = await queue.ReceiveAsync(cancellationToken);
                context.SetMetadata("messageId", message?.Id);
                return message;
            })
            .When("process message", async message =>
            {
                if (message == null) return false;

                await _handler.HandleAsync(message, cancellationToken);
                return true;
            })
            .When("acknowledge", async processed =>
            {
                if (processed)
                {
                    var messageId = context.GetMetadata<string>("messageId");
                    await _queue.AcknowledgeAsync(messageId, cancellationToken);
                }
                return processed;
            })
            .Then("completed", success => success);
    }
}
```

## Observability

### Structured Logging

```csharp
public class LoggingBehavior<T> : IBehavior<T>
{
    private readonly ILogger _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<T>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["WorkflowName"] = context.WorkflowName,
            ["ExecutionId"] = context.ExecutionId,
            ["StepKind"] = step.Kind,
            ["StepTitle"] = step.Title,
            ["CorrelationId"] = context.GetMetadata<string>("correlationId") ?? ""
        });

        _logger.LogDebug("Starting step {StepTitle}", step.Title);
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await next(cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Completed step {StepTitle} in {ElapsedMs}ms",
                step.Title,
                sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex,
                "Failed step {StepTitle} after {ElapsedMs}ms",
                step.Title,
                sw.ElapsedMilliseconds);

            throw;
        }
    }
}
```

### Metrics with OpenTelemetry

```csharp
public class MetricsBehavior<T> : IBehavior<T>
{
    private static readonly Meter Meter = new("PatternKit.Workflows", "1.0");
    private static readonly Counter<long> StepCounter = Meter.CreateCounter<long>("workflow_steps_total");
    private static readonly Histogram<double> StepDuration = Meter.CreateHistogram<double>("workflow_step_duration_ms");

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        var tags = new TagList
        {
            { "workflow", context.WorkflowName },
            { "step", step.Title },
            { "phase", step.Phase.ToString() }
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next(cancellationToken);
            sw.Stop();

            tags.Add("status", "success");
            StepCounter.Add(1, tags);
            StepDuration.Record(sw.Elapsed.TotalMilliseconds, tags);

            return result;
        }
        catch
        {
            sw.Stop();
            tags.Add("status", "error");
            StepCounter.Add(1, tags);
            StepDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
            throw;
        }
    }
}

// Registration
services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("PatternKit.Workflows");
    });

services.AddBehavior<Order, MetricsBehavior<Order>>();
```

### Distributed Tracing

```csharp
public class TracingBehavior<T> : IBehavior<T>
{
    private static readonly ActivitySource ActivitySource = new("PatternKit.Workflows");

    public async ValueTask<T> WrapAsync(
        Func<CancellationToken, ValueTask<T>> next,
        WorkflowContext context,
        StepMetadata step,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(
            $"{context.WorkflowName}.{step.Title}",
            ActivityKind.Internal);

        activity?.SetTag("workflow.name", context.WorkflowName);
        activity?.SetTag("workflow.execution_id", context.ExecutionId);
        activity?.SetTag("step.kind", step.Kind);
        activity?.SetTag("step.title", step.Title);
        activity?.SetTag("step.phase", step.Phase.ToString());

        try
        {
            var result = await next(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Health Checks

```csharp
public class WorkflowHealthCheck : IHealthCheck
{
    private readonly IWorkflowRunner _runner;

    public WorkflowHealthCheck(IWorkflowRunner runner)
    {
        _runner = runner;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _runner.RunAsync(
                "HealthCheck",
                async (ctx, ct) =>
                {
                    return await Workflow
                        .Given(ctx, "health check", () => DateTime.UtcNow)
                        .When("check database", async _ =>
                            await CheckDatabaseAsync(ct))
                        .And("check cache", async _ =>
                            await CheckCacheAsync(ct))
                        .Then("healthy", result => result.AllHealthy)
                        .GetResultAsync(ct);
                },
                cancellationToken);

            return result.AllHealthy
                ? HealthCheckResult.Healthy("All systems operational")
                : HealthCheckResult.Degraded("Some systems degraded");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}

// Registration
builder.Services.AddHealthChecks()
    .AddCheck<WorkflowHealthCheck>("workflow_health");
```

## Multi-Tenancy

```csharp
public class TenantAwareWorkflowFactory
{
    private readonly IWorkflowContextFactory _contextFactory;
    private readonly ITenantProvider _tenantProvider;

    public TenantAwareWorkflowFactory(
        IWorkflowContextFactory contextFactory,
        ITenantProvider tenantProvider)
    {
        _contextFactory = contextFactory;
        _tenantProvider = tenantProvider;
    }

    public WorkflowContext CreateContext(string workflowName, string? description = null)
    {
        var context = _contextFactory.Create(workflowName, description);
        var tenant = _tenantProvider.GetCurrentTenant();

        context.SetMetadata("tenantId", tenant.Id);
        context.SetMetadata("tenantName", tenant.Name);

        return context;
    }
}

// Usage
public class TenantOrderProcessor
{
    private readonly TenantAwareWorkflowFactory _factory;

    public async Task ProcessAsync(Order order)
    {
        var context = _factory.CreateContext("ProcessOrder");

        await Workflow
            .Given(context, "order", () => order)
            .When("validate tenant", o =>
            {
                var tenantId = context.GetMetadata<string>("tenantId");
                if (o.TenantId != tenantId)
                    throw new UnauthorizedException("Order belongs to different tenant");
                return o;
            })
            .When("process", o => Process(o))
            .Then("completed", o => o.Status == "Complete");
    }
}
```

## Transaction Management

```csharp
public class TransactionalWorkflow<T>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public async Task<T> ExecuteAsync(
        WorkflowContext context,
        Func<AppDbContext, WorkflowContext, ValueTask<T>> workflowBuilder)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var result = await workflowBuilder(dbContext, context);

            if (context.AllPassed)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

// Usage
var result = await _transactionalWorkflow.ExecuteAsync(context, async (db, ctx) =>
{
    return await Workflow
        .Given(ctx, "order", () => order)
        .When("save order", async o =>
        {
            db.Orders.Add(o);
            await db.SaveChangesAsync();
            return o;
        })
        .When("update inventory", async o =>
        {
            foreach (var item in o.Items)
            {
                await db.Inventory.Where(i => i.ProductId == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        i => i.Quantity,
                        i => i.Quantity - item.Quantity));
            }
            return o;
        })
        .Then("committed", o => o.Status == "Saved")
        .GetResultAsync();
});
```
