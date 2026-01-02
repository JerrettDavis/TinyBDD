# Reporting

This guide covers TinyBDD's reporting capabilities, from built-in reporters and output configuration to the JSON reporting extension and creating custom reporters for CI/CD integration.

## Overview

TinyBDD provides multiple reporting mechanisms:

1. **Built-in framework reporters** - Console output for test frameworks
2. **JSON reporting extension** - Structured reports via `TinyBDD.Extensions.Reporting`
3. **Custom reporters** - Implement `IBddReporter` for specialized formats
4. **Observer pattern** - Hook into scenario/step lifecycle for telemetry

## JSON Reporting Extension

For structured reporting with JSON output, see the **[Reporting Extension](extensions/reporting.md)** documentation. This extension provides:

- Structured JSON reports for CI/CD artifacts
- Observer pattern integration for scenario and step lifecycle
- Configurable serialization options
- Support for trend analysis and diagnostics

Quick example:

```csharp
using TinyBDD.Extensions.Reporting;

var options = TinyBdd.Configure(builder => builder
    .AddJsonReport("artifacts/report.json"));

var ctx = Bdd.CreateContext(this, options: options);

await Bdd.Given(ctx, "start", () => 1)
    .When("add one", x => x + 1)
    .Then("equals two", x => x == 2);
```

For complete details, see **[Reporting Extension Guide](extensions/reporting.md)**.

---

## Built-In Reporters

TinyBDD provides several built-in reporters for different scenarios and test frameworks.

### StringBddReporter

The simplest reporter that captures output as a string. Ideal for testing and custom formatting.

```csharp
[Scenario("Using string reporter"), Fact]
public async Task UsingStringReporter()
{
    var ctx = Bdd.CreateContext(this);
    
    await Bdd.Given(ctx, "number", () => 5)
        .When("doubled", x => x * 2)
        .Then("equals 10", x => x == 10)
        .AssertPassed();
    
    // Generate report
    var reporter = new StringBddReporter();
    GherkinFormatter.Write(ctx, reporter);
    
    // Use the output
    Console.WriteLine(reporter.ToString());
    File.WriteAllText("scenario-report.txt", reporter.ToString());
}
```

Example output:

```
Feature: Calculator
Scenario: Using string reporter
  Given number [OK] 0 ms
  When doubled [OK] 0 ms
  Then equals 10 [OK] 1 ms
```

### Framework-Specific Reporters

Each test framework adapter includes a dedicated reporter that integrates with the framework's output mechanisms.

#### XunitBddReporter

Writes to xUnit's `ITestOutputHelper`:

```csharp
[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    public CalculatorTests(ITestOutputHelper output) : base(output)
    {
        // XunitBddReporter is automatically configured by the base class
    }
    
    [Scenario("Addition"), Fact]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
        
        // Output automatically appears in xUnit test output
    }
}
```

#### NUnitBddReporter

Writes to NUnit's `TestContext`:

```csharp
[Feature("Calculator")]
public class CalculatorTests : TinyBddNUnitBase
{
    // NUnitBddReporter is automatically configured by the base class
    
    [Scenario("Addition"), Test]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
        
        // Output appears in NUnit test output
    }
}
```

#### MsTestBddReporter

Writes to MSTest's `TestContext`:

```csharp
[TestClass]
[Feature("Calculator")]
public class CalculatorTests : TinyBddMsTestBase
{
    // MsTestBddReporter is automatically configured by the base class
    
    [Scenario("Addition"), TestMethod]
    public async Task Addition()
    {
        await Given("numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("equals 5", sum => sum == 5)
            .AssertPassed();
        
        // Output appears in MSTest test output via TestContext
    }
}
```

## Gherkin Formatter

The `GherkinFormatter` class converts a `ScenarioContext` into formatted Gherkin-style output using any `IBddReporter`.

### Basic Usage

```csharp
var ctx = Bdd.CreateContext(this);

await Bdd.Given(ctx, "initial state", () => 1)
    .When("action", x => x + 1)
    .Then("expected result", x => x == 2)
    .AssertPassed();

var reporter = new StringBddReporter();
GherkinFormatter.Write(ctx, reporter);

Console.WriteLine(reporter.ToString());
```

### Output Format

The formatter produces output following Gherkin conventions:

```
Feature: Feature Name
Scenario: Scenario Name
  Tags: tag1, tag2
  Given initial state [OK] 2 ms
  When action [OK] 1 ms
  Then expected result [OK] 0 ms
```

For failed steps:

```
Feature: Feature Name
Scenario: Scenario Name
  Given initial state [OK] 2 ms
  When action [OK] 1 ms
  Then expected result [FAIL] 1 ms
    Expected: 3
    Actual: 2
```

## Creating Custom Reporters

Implement the `IBddReporter` interface to create custom reporters for specialized output formats or destinations.

### IBddReporter Interface

```csharp
public interface IBddReporter
{
    void WriteLine(string message);
}
```

### Example: JSON Reporter

Create a reporter that outputs JSON for structured logging:

```csharp
using System.Text.Json;

public class JsonBddReporter : IBddReporter
{
    private readonly List<string> _lines = new();
    
    public void WriteLine(string message)
    {
        _lines.Add(message);
    }
    
    public string ToJson()
    {
        var report = new
        {
            timestamp = DateTime.UtcNow,
            lines = _lines
        };
        return JsonSerializer.Serialize(report, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }
}

// Usage
var reporter = new JsonBddReporter();
GherkinFormatter.Write(ctx, reporter);
File.WriteAllText("scenario-report.json", reporter.ToJson());
```

### Example: Markdown Reporter

Generate markdown documentation from scenarios:

```csharp
public class MarkdownBddReporter : IBddReporter
{
    private readonly StringBuilder _content = new();
    private bool _firstLine = true;
    
    public void WriteLine(string message)
    {
        if (_firstLine)
        {
            _content.AppendLine($"# {message}");
            _firstLine = false;
        }
        else if (message.StartsWith("Feature:"))
        {
            _content.AppendLine();
            _content.AppendLine($"## {message}");
        }
        else if (message.StartsWith("Scenario:"))
        {
            _content.AppendLine();
            _content.AppendLine($"### {message}");
        }
        else if (message.StartsWith("  "))
        {
            _content.AppendLine($"- {message.Trim()}");
        }
        else
        {
            _content.AppendLine(message);
        }
    }
    
    public override string ToString() => _content.ToString();
}

// Usage
var reporter = new MarkdownBddReporter();
GherkinFormatter.Write(ctx, reporter);
File.WriteAllText("scenarios.md", reporter.ToString());
```

### Example: HTML Reporter

Generate HTML reports with styling:

```csharp
public class HtmlBddReporter : IBddReporter
{
    private readonly StringBuilder _html = new();
    private bool _inScenario = false;
    
    public HtmlBddReporter()
    {
        _html.AppendLine("<!DOCTYPE html>");
        _html.AppendLine("<html>");
        _html.AppendLine("<head>");
        _html.AppendLine("<style>");
        _html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        _html.AppendLine(".feature { color: #2c3e50; margin-top: 20px; }");
        _html.AppendLine(".scenario { color: #34495e; margin-top: 15px; }");
        _html.AppendLine(".step { margin-left: 20px; padding: 5px; }");
        _html.AppendLine(".pass { color: green; }");
        _html.AppendLine(".fail { color: red; }");
        _html.AppendLine("</style>");
        _html.AppendLine("</head>");
        _html.AppendLine("<body>");
    }
    
    public void WriteLine(string message)
    {
        if (message.StartsWith("Feature:"))
        {
            _html.AppendLine($"<h2 class='feature'>{message}</h2>");
        }
        else if (message.StartsWith("Scenario:"))
        {
            if (_inScenario)
                _html.AppendLine("</div>");
            _html.AppendLine($"<h3 class='scenario'>{message}</h3>");
            _html.AppendLine("<div class='steps'>");
            _inScenario = true;
        }
        else if (message.Contains("[OK]"))
        {
            _html.AppendLine($"<div class='step pass'>{message}</div>");
        }
        else if (message.Contains("[FAIL]"))
        {
            _html.AppendLine($"<div class='step fail'>{message}</div>");
        }
        else
        {
            _html.AppendLine($"<div class='step'>{message}</div>");
        }
    }
    
    public string ToHtml()
    {
        if (_inScenario)
            _html.AppendLine("</div>");
        _html.AppendLine("</body>");
        _html.AppendLine("</html>");
        return _html.ToString();
    }
}

// Usage
var reporter = new HtmlBddReporter();
GherkinFormatter.Write(ctx, reporter);
File.WriteAllText("scenarios.html", reporter.ToHtml());
```

### Example: CI/CD Reporter

Create structured output for CI/CD systems:

```csharp
public class CiBddReporter : IBddReporter
{
    private readonly StringBuilder _output = new();
    
    public void WriteLine(string message)
    {
        // GitHub Actions annotations
        if (message.Contains("[FAIL]"))
        {
            _output.AppendLine($"::error::{message}");
        }
        else if (message.Contains("[OK]"))
        {
            _output.AppendLine($"::notice::{message}");
        }
        else
        {
            _output.AppendLine(message);
        }
    }
    
    public override string ToString() => _output.ToString();
}
```

### Example: Structured Reporter

Capture structured data for analysis:

```csharp
public class StructuredBddReporter : IBddReporter
{
    public string FeatureName { get; private set; }
    public string ScenarioName { get; private set; }
    public List<StepInfo> Steps { get; } = new();
    
    public void WriteLine(string message)
    {
        if (message.StartsWith("Feature:"))
        {
            FeatureName = message.Replace("Feature:", "").Trim();
        }
        else if (message.StartsWith("Scenario:"))
        {
            ScenarioName = message.Replace("Scenario:", "").Trim();
        }
        else if (message.Trim().StartsWith("Given") || 
                 message.Trim().StartsWith("When") || 
                 message.Trim().StartsWith("Then") ||
                 message.Trim().StartsWith("And") ||
                 message.Trim().StartsWith("But"))
        {
            var parts = message.Trim().Split(new[] { "[OK]", "[FAIL]" }, StringSplitOptions.None);
            var stepText = parts[0].Trim();
            var passed = message.Contains("[OK]");
            
            Steps.Add(new StepInfo 
            { 
                Text = stepText, 
                Passed = passed 
            });
        }
    }
    
    public ScenarioReport ToReport()
    {
        return new ScenarioReport
        {
            Feature = FeatureName,
            Scenario = ScenarioName,
            Steps = Steps,
            TotalSteps = Steps.Count,
            PassedSteps = Steps.Count(s => s.Passed),
            FailedSteps = Steps.Count(s => !s.Passed)
        };
    }
}

public class StepInfo
{
    public string Text { get; set; }
    public bool Passed { get; set; }
}

public class ScenarioReport
{
    public string Feature { get; set; }
    public string Scenario { get; set; }
    public List<StepInfo> Steps { get; set; }
    public int TotalSteps { get; set; }
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
}
```

## Reporter Lifecycle Callbacks

Custom reporters can track the complete lifecycle by parsing the formatted output or by creating a more advanced integration.

### Advanced Reporter Pattern

```csharp
public abstract class LifecycleAwareBddReporter : IBddReporter
{
    private string _currentFeature;
    private string _currentScenario;
    
    protected virtual void OnFeatureStart(string featureName) { }
    protected virtual void OnScenarioStart(string scenarioName) { }
    protected virtual void OnStep(string stepText, bool passed, TimeSpan duration) { }
    protected virtual void OnScenarioComplete(string scenarioName, bool allPassed) { }
    
    public void WriteLine(string message)
    {
        if (message.StartsWith("Feature:"))
        {
            _currentFeature = message.Replace("Feature:", "").Trim();
            OnFeatureStart(_currentFeature);
        }
        else if (message.StartsWith("Scenario:"))
        {
            _currentScenario = message.Replace("Scenario:", "").Trim();
            OnScenarioStart(_currentScenario);
        }
        else if (message.Trim().Length > 0)
        {
            var passed = message.Contains("[OK]");
            var failed = message.Contains("[FAIL]");
            
            if (passed || failed)
            {
                var duration = ExtractDuration(message);
                OnStep(message, passed, duration);
            }
        }
    }
    
    private TimeSpan ExtractDuration(string message)
    {
        // Parse duration from message like "Given step [OK] 15 ms"
        var match = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)\s*ms");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var ms))
        {
            return TimeSpan.FromMilliseconds(ms);
        }
        return TimeSpan.Zero;
    }
}

// Example implementation
public class TimingReporter : LifecycleAwareBddReporter
{
    private readonly List<double> _stepDurations = new();
    
    protected override void OnStep(string stepText, bool passed, TimeSpan duration)
    {
        _stepDurations.Add(duration.TotalMilliseconds);
        Console.WriteLine($"Step took {duration.TotalMilliseconds}ms");
    }
    
    protected override void OnScenarioComplete(string scenarioName, bool allPassed)
    {
        var total = _stepDurations.Sum();
        var avg = _stepDurations.Average();
        Console.WriteLine($"Total: {total}ms, Average: {avg:F2}ms per step");
    }
}
```

## Configuring Reporter Output

### Controlling Output Verbosity

Base classes automatically write reports at the end of each scenario. To customize this behavior:

```csharp
public class CustomXunitBase : TinyBddXunitBase
{
    protected bool EnableDetailedOutput { get; set; } = true;
    
    public CustomXunitBase(ITestOutputHelper output) : base(output)
    {
    }
    
    protected void ConfigureReporting(ScenarioContext ctx)
    {
        if (!EnableDetailedOutput)
        {
            // Disable automatic reporting
            // Implementation depends on base class internals
        }
    }
}
```

### Multiple Reporters

Use multiple reporters simultaneously:

```csharp
public class MultiReporter : IBddReporter
{
    private readonly List<IBddReporter> _reporters;
    
    public MultiReporter(params IBddReporter[] reporters)
    {
        _reporters = new List<IBddReporter>(reporters);
    }
    
    public void WriteLine(string message)
    {
        foreach (var reporter in _reporters)
        {
            reporter.WriteLine(message);
        }
    }
}

// Usage
var consoleReporter = new StringBddReporter();
var fileReporter = new FileReporter("scenarios.log");
var multiReporter = new MultiReporter(consoleReporter, fileReporter);

GherkinFormatter.Write(ctx, multiReporter);
```

## CI/CD Integration

### GitHub Actions

Output formatted for GitHub Actions annotations:

```csharp
public class GitHubActionsBddReporter : IBddReporter
{
    public void WriteLine(string message)
    {
        if (message.Contains("[FAIL]"))
        {
            // Extract step information
            var stepName = message.Split('[')[0].Trim();
            Console.WriteLine($"::error title=Step Failed::{stepName}");
        }
        else if (message.StartsWith("Feature:") || message.StartsWith("Scenario:"))
        {
            Console.WriteLine($"::group::{message}");
        }
        
        Console.WriteLine(message);
    }
}
```

### Azure DevOps

Format for Azure Pipelines logging:

```csharp
public class AzureDevOpsBddReporter : IBddReporter
{
    public void WriteLine(string message)
    {
        if (message.Contains("[FAIL]"))
        {
            Console.WriteLine($"##vso[task.logissue type=error]{message}");
        }
        else if (message.StartsWith("Scenario:"))
        {
            Console.WriteLine($"##[section]{message}");
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
```

### TeamCity

TeamCity service messages:

```csharp
public class TeamCityBddReporter : IBddReporter
{
    private string _currentScenario;
    
    public void WriteLine(string message)
    {
        if (message.StartsWith("Scenario:"))
        {
            _currentScenario = message.Replace("Scenario:", "").Trim();
            Console.WriteLine($"##teamcity[testStarted name='{_currentScenario}']");
        }
        else if (message.Contains("[FAIL]"))
        {
            Console.WriteLine($"##teamcity[testFailed name='{_currentScenario}' message='{message}']");
        }
        
        Console.WriteLine(message);
    }
}
```

## Performance Monitoring

Track and report scenario performance:

```csharp
public class PerformanceBddReporter : IBddReporter
{
    private readonly List<ScenarioTiming> _timings = new();
    private ScenarioTiming _current;
    
    public void WriteLine(string message)
    {
        if (message.StartsWith("Scenario:"))
        {
            _current = new ScenarioTiming 
            { 
                Name = message.Replace("Scenario:", "").Trim(),
                StartTime = DateTime.UtcNow 
            };
        }
        else if (_current != null && (message.Contains("[OK]") || message.Contains("[FAIL]")))
        {
            var duration = ExtractDuration(message);
            _current.StepDurations.Add(duration);
        }
        
        // Store when scenario completes
        if (_current != null && message.Trim().Length == 0)
        {
            _current.EndTime = DateTime.UtcNow;
            _timings.Add(_current);
            _current = null;
        }
    }
    
    public void GeneratePerformanceReport()
    {
        foreach (var timing in _timings)
        {
            var total = timing.EndTime - timing.StartTime;
            var stepTotal = timing.StepDurations.Sum();
            
            Console.WriteLine($"Scenario: {timing.Name}");
            Console.WriteLine($"  Total Time: {total.TotalMilliseconds}ms");
            Console.WriteLine($"  Step Time: {stepTotal.TotalMilliseconds}ms");
            Console.WriteLine($"  Overhead: {(total - stepTotal).TotalMilliseconds}ms");
        }
    }
    
    private TimeSpan ExtractDuration(string message)
    {
        // Implementation as shown earlier
        return TimeSpan.Zero;
    }
}

public class ScenarioTiming
{
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<TimeSpan> StepDurations { get; } = new();
}
```

## Best Practices

1. **Use JSON extension for structured reporting**: For CI/CD and trend analysis, use `TinyBDD.Extensions.Reporting`
2. **Use framework reporters for test output**: Start with built-in reporters for standard scenarios
3. **Implement IBddReporter for custom formats**: Create custom reporters for specialized needs
4. **Use observer pattern for telemetry**: Implement `IScenarioObserver` and `IStepObserver` for cross-cutting concerns
5. **Keep reporters simple**: Focus on output formatting, not business logic
6. **Test your reporters**: Ensure custom reporters handle edge cases
7. **Consider performance**: Avoid expensive operations in WriteLine
8. **Support multiple outputs**: Use MultiReporter pattern when needed
9. **Document formats**: Provide examples of reporter output
10. **Handle errors gracefully**: Catch and log exceptions in custom reporters
11. **Version control output**: Store sample outputs for regression testing
12. **Integrate with CI/CD**: Use appropriate reporter for your build system

## Observer Pattern for Advanced Reporting

For advanced scenarios requiring lifecycle hooks, implement the observer interfaces:

### IScenarioObserver

```csharp
public class TelemetryScenarioObserver : IScenarioObserver
{
    private readonly TelemetryClient _telemetry;

    public TelemetryScenarioObserver(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }

    public ValueTask OnScenarioStarting(ScenarioContext context)
    {
        _telemetry.TrackEvent("ScenarioStarted", new Dictionary<string, string>
        {
            ["Feature"] = context.FeatureName,
            ["Scenario"] = context.ScenarioName
        });
        return default;
    }

    public ValueTask OnScenarioFinished(ScenarioContext context)
    {
        var passed = context.Steps.All(s => s.Error == null);
        _telemetry.TrackEvent("ScenarioFinished", new Dictionary<string, string>
        {
            ["Feature"] = context.FeatureName,
            ["Scenario"] = context.ScenarioName,
            ["Passed"] = passed.ToString()
        });
        return default;
    }
}
```

### IStepObserver

```csharp
public class StepTimingObserver : IStepObserver
{
    private readonly ILogger _logger;

    public StepTimingObserver(ILogger logger)
    {
        _logger = logger;
    }

    public ValueTask OnStepStarting(ScenarioContext context, StepInfo step)
    {
        _logger.LogInformation("Step starting: {Kind} {Title}", step.Kind, step.Title);
        return default;
    }

    public ValueTask OnStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io)
    {
        _logger.LogInformation(
            "Step finished: {Kind} {Title} in {Duration}ms, Passed: {Passed}",
            step.Kind, step.Title, result.Elapsed.TotalMilliseconds, result.Error == null);
        return default;
    }
}
```

### Registering Observers

```csharp
var options = TinyBdd.Configure(builder => builder
    .AddObserver(new TelemetryScenarioObserver(telemetryClient))
    .AddObserver(new StepTimingObserver(logger)));

var ctx = Bdd.CreateContext(this, options: options);
```

For more details on the observer pattern and JSON reporting, see **[Reporting Extension](extensions/reporting.md)**.

---

## Next Steps

- **[Reporting Extension Guide](extensions/reporting.md)** - Complete JSON reporting documentation
- [Troubleshooting & FAQ](troubleshooting-faq.md) - Common issues and solutions
- [Extensibility & Advanced](advanced-usage.md) - Advanced patterns and extensibility
- [Samples Index](samples-index.md) - Example implementations

