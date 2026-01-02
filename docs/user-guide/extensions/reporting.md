# Reporting Extension

**TinyBDD.Extensions.Reporting** provides structured JSON reporting for TinyBDD scenarios through the observer pattern, enabling integration with CI/CD systems, trend analysis, and production diagnostics.

## Installation

```bash
dotnet add package TinyBDD.Extensions.Reporting
```

## Quick Start

```csharp
using TinyBDD;
using TinyBDD.Extensions.Reporting;

// Configure JSON reporting
var options = TinyBdd.Configure(builder => builder
    .AddJsonReport("artifacts/tinybdd-report.json"));

// Use in your scenarios
var ctx = Bdd.CreateContext(this, options: options);

await Bdd.Given(ctx, "start", () => 1)
    .When("add one", x => x + 1)
    .Then("equals two", x => x == 2);

// JSON report is automatically written after scenario completes
```

## JSON Report Format

The `JsonReportObserver` generates structured JSON containing:

```json
{
  "scenarios": [
    {
      "featureName": "Calculator",
      "scenarioName": "Addition",
      "featureDescription": null,
      "tags": ["smoke", "fast"],
      "startTime": "2024-01-15T10:30:00.000Z",
      "endTime": "2024-01-15T10:30:00.123Z",
      "durationMs": 123.45,
      "passed": true,
      "steps": [
        {
          "kind": "Given",
          "title": "start",
          "durationMs": 1.23,
          "passed": true,
          "error": null
        },
        {
          "kind": "When",
          "title": "add one",
          "durationMs": 2.34,
          "passed": true,
          "error": null
        },
        {
          "kind": "Then",
          "title": "equals two",
          "durationMs": 3.45,
          "passed": true,
          "error": null
        }
      ]
    }
  ]
}
```

## Configuration Options

### Custom JSON Serialization

```csharp
using System.Text.Json;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = false,          // Compact output
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var options = TinyBdd.Configure(builder => builder
    .AddJsonReport("report.json", jsonOptions));
```

### Multiple Reports

Generate multiple report files:

```csharp
var options = TinyBdd.Configure(builder => builder
    .AddJsonReport("artifacts/detailed-report.json")
    .AddJsonReport("artifacts/summary-report.json", compactOptions));
```

## Observer Pattern Integration

`JsonReportObserver` implements both `IScenarioObserver` and `IStepObserver`, hooking into TinyBDD's extensibility layer:

```csharp
public class CustomObserver : IScenarioObserver, IStepObserver
{
    public ValueTask OnScenarioStarting(ScenarioContext context)
    {
        // Called when scenario starts
        Console.WriteLine($"Starting: {context.ScenarioName}");
        return default;
    }

    public ValueTask OnScenarioFinished(ScenarioContext context)
    {
        // Called when scenario completes
        Console.WriteLine($"Finished: {context.ScenarioName}");
        return default;
    }

    public ValueTask OnStepStarting(ScenarioContext context, StepInfo step)
    {
        // Called before each step
        Console.WriteLine($"Step starting: {step.Kind} {step.Title}");
        return default;
    }

    public ValueTask OnStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io)
    {
        // Called after each step
        Console.WriteLine($"Step finished: {step.Kind} {step.Title} in {result.Elapsed.TotalMilliseconds}ms");
        return default;
    }
}

// Register custom observer
var options = TinyBdd.Configure(builder => builder
    .AddObserver(new CustomObserver()));
```

## Usage Patterns

### CI/CD Integration

Generate reports for CI artifacts:

```csharp
// In test setup or fixture
var artifactsPath = Environment.GetEnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY")
    ?? "artifacts";

Directory.CreateDirectory(artifactsPath);

var options = TinyBdd.Configure(builder => builder
    .AddJsonReport(Path.Combine(artifactsPath, $"test-results-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json")));

// Use throughout tests
public class TestBase
{
    protected TinyBddExtensibilityOptions Options { get; } = options;

    protected ScenarioContext CreateContext()
    {
        return Bdd.CreateContext(this, options: Options);
    }
}
```

### Test Framework Integration

#### xUnit Example

```csharp
using TinyBDD.Xunit;
using TinyBDD.Extensions.Reporting;
using Xunit;

[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    private static readonly TinyBddExtensibilityOptions Options = TinyBdd.Configure(
        builder => builder.AddJsonReport("artifacts/calculator-tests.json"));

    public CalculatorTests(ITestOutputHelper output) : base(output)
    {
        // Override ambient context with reporting-enabled options
        Ambient.Current.Value = Bdd.CreateContext(this, options: Options);
    }

    [Scenario("Addition"), Fact]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
    }
}
```

#### NUnit Example

```csharp
using TinyBDD.NUnit;
using TinyBDD.Extensions.Reporting;
using NUnit.Framework;

[Feature("Calculator")]
public class CalculatorTests : TinyBddNUnitBase
{
    private static readonly TinyBddExtensibilityOptions Options = TinyBdd.Configure(
        builder => builder.AddJsonReport("artifacts/calculator-tests.json"));

    [SetUp]
    public void Setup()
    {
        Ambient.Current.Value = Bdd.CreateContext(this, options: Options);
    }

    [Scenario("Addition"), Test]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
    }
}
```

#### MSTest Example

```csharp
using TinyBDD.MSTest;
using TinyBDD.Extensions.Reporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
[Feature("Calculator")]
public class CalculatorTests : TinyBddMsTestBase
{
    private static readonly TinyBddExtensibilityOptions Options = TinyBdd.Configure(
        builder => builder.AddJsonReport("artifacts/calculator-tests.json"));

    [TestInitialize]
    public void TestInitialize()
    {
        Ambient.Current.Value = Bdd.CreateContext(this, options: Options);
    }

    [Scenario("Addition"), TestMethod]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
    }
}
```

### Production Workflow Reporting

Combine with hosting extensions for production observability:

```csharp
using TinyBDD.Extensions.DependencyInjection;
using TinyBDD.Extensions.Hosting;
using TinyBDD.Extensions.Reporting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTinyBdd(options =>
{
    options.DefaultScenarioOptions = new ScenarioOptions
    {
        ContinueOnError = true,
        StepTimeout = TimeSpan.FromSeconds(30)
    };
});

// Add JSON reporting for production workflows
var reportPath = Path.Combine(
    builder.Configuration["LogDirectory"] ?? "/var/log/workflows",
    $"workflow-{DateTime.UtcNow:yyyyMMdd}.json");

builder.Services.AddTinyBddHosting(hostingOptions =>
{
    hostingOptions.StopHostOnFailure = true;
});

// Configure reporting through options builder
var workflowOptions = TinyBdd.Configure(opts => opts.AddJsonReport(reportPath));

builder.Services.AddSingleton(workflowOptions);

var host = builder.Build();
await host.RunAsync();
```

## Trend Analysis

Parse JSON reports for historical analysis:

```csharp
using System.Text.Json;

public class TrendAnalyzer
{
    public async Task<TrendReport> AnalyzeAsync(string reportDirectory)
    {
        var reports = Directory.GetFiles(reportDirectory, "*.json")
            .Select(async file =>
            {
                var json = await File.ReadAllTextAsync(file);
                return JsonSerializer.Deserialize<ReportRoot>(json);
            })
            .ToList();

        var allReports = await Task.WhenAll(reports);

        return new TrendReport
        {
            TotalScenarios = allReports.SelectMany(r => r.Scenarios).Count(),
            AverageDuration = allReports
                .SelectMany(r => r.Scenarios)
                .Average(s => s.DurationMs),
            PassRate = allReports
                .SelectMany(r => r.Scenarios)
                .Count(s => s.Passed) / (double)allReports.SelectMany(r => r.Scenarios).Count(),
            SlowestSteps = allReports
                .SelectMany(r => r.Scenarios)
                .SelectMany(s => s.Steps)
                .OrderByDescending(st => st.DurationMs)
                .Take(10)
                .ToList()
        };
    }
}
```

## Combining with Other Observers

Create comprehensive observability by combining multiple observers:

```csharp
var options = TinyBdd.Configure(builder => builder
    // JSON reporting for persistence
    .AddJsonReport("artifacts/report.json")
    
    // Custom logging observer
    .AddObserver(new LoggingObserver(logger))
    
    // Telemetry observer
    .AddObserver(new TelemetryObserver(telemetryClient))
    
    // Metrics observer
    .AddObserver(new MetricsObserver(metricsCollector)));
```

## Best Practices

1. **Separate reports per test class**: Use class-specific report files to avoid conflicts
2. **Include timestamps**: Use date/time in filenames for historical tracking
3. **Configure serialization**: Customize JSON options for compact or detailed output
4. **Parse for analysis**: Use structured JSON for automated trend analysis
5. **Combine observers**: Layer multiple observers for comprehensive observability
6. **Handle file I/O carefully**: Ensure report directories exist and are writable
7. **Clean old reports**: Implement retention policies for report files
8. **Use in CI/CD**: Publish reports as build artifacts for visibility

## Troubleshooting

### Report Not Generated

Check that the output directory exists:

```csharp
var reportPath = "artifacts/report.json";
Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

var options = TinyBdd.Configure(builder => builder.AddJsonReport(reportPath));
```

### Concurrent Write Issues

Use unique filenames when running tests in parallel:

```csharp
var reportPath = $"artifacts/report-{Guid.NewGuid()}.json";
var options = TinyBdd.Configure(builder => builder.AddJsonReport(reportPath));
```

### Large Reports

Use compact JSON for production:

```csharp
var compactOptions = new JsonSerializerOptions
{
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var options = TinyBdd.Configure(builder => builder
    .AddJsonReport("report.json", compactOptions));
```

## Next Steps

- [Dependency Injection Guide](dependency-injection.md) - DI integration
- [Hosting Guide](hosting.md) - Background workflow integration
- [Observer Pattern](../../advanced-usage.md#observer-pattern) - Custom observers
- [Orchestrator Patterns](../orchestrator-patterns.md) - Production workflows

Return to: [Extensions Index](index.md) | [User Guide](../index.md)
