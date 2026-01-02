using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinyBDD.Extensions.Reporting;

/// <summary>
/// Observer that collects scenario execution data and writes it to JSON format.
/// </summary>
/// <remarks>
/// This observer captures scenario metadata, steps, timings, and results into a structured
/// JSON report suitable for CI artifacts, trend analysis, and diagnostics.
/// </remarks>
public sealed class JsonReportObserver : IScenarioObserver, IStepObserver
{
    private readonly string _outputPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ScenarioReport> _scenarios = new();
    private ScenarioReport? _currentScenario;

    /// <summary>
    /// Creates a new JSON report observer.
    /// </summary>
    /// <param name="outputPath">Path where the JSON report will be written.</param>
    /// <param name="jsonOptions">Optional JSON serialization options.</param>
    public JsonReportObserver(string outputPath, JsonSerializerOptions? jsonOptions = null)
    {
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc/>
    public ValueTask OnScenarioStarting(ScenarioContext context)
    {
        _currentScenario = new ScenarioReport
        {
            FeatureName = context.FeatureName,
            FeatureDescription = context.FeatureDescription,
            ScenarioName = context.ScenarioName,
            Tags = context.Tags.ToArray(),
            StartTime = DateTimeOffset.UtcNow,
            Steps = new List<StepReport>()
        };

        return default;
    }

    /// <inheritdoc/>
    public ValueTask OnScenarioFinished(ScenarioContext context)
    {
        if (_currentScenario is null)
            return default;

        _currentScenario.EndTime = DateTimeOffset.UtcNow;
        _currentScenario.Duration = _currentScenario.EndTime - _currentScenario.StartTime;
        _currentScenario.Passed = context.Steps.All(s => s.Error is null);
        _currentScenario.Failed = context.Steps.Any(s => s.Error is not null);

        _scenarios.Add(_currentScenario);
        _currentScenario = null;

        // Write report after each scenario
        WriteReport();

        return default;
    }

    /// <inheritdoc/>
    public ValueTask OnStepStarting(ScenarioContext context, StepInfo step)
    {
        // Step start tracking is handled by OnStepFinished
        return default;
    }

    /// <inheritdoc/>
    public ValueTask OnStepFinished(ScenarioContext context, StepInfo step, StepResult result, StepIO io)
    {
        if (_currentScenario?.Steps is null)
            return default;

        _currentScenario.Steps.Add(new StepReport
        {
            Kind = step.Kind,
            Title = step.Title,
            Phase = step.Phase.ToString(),
            Duration = result.Elapsed,
            Passed = result.Error is null,
            Error = result.Error?.Message,
            Input = io.Input,
            Output = io.Output
        });

        return default;
    }

    private void WriteReport()
    {
        try
        {
            var directory = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var report = new JsonReport
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                Scenarios = _scenarios.ToArray()
            };

            var json = JsonSerializer.Serialize(report, _jsonOptions);
            File.WriteAllText(_outputPath, json);
        }
        catch
        {
            // Suppress write failures to prevent masking test failures
        }
    }
}

/// <summary>
/// Root JSON report structure.
/// </summary>
public sealed class JsonReport
{
    public DateTimeOffset GeneratedAt { get; set; }
    public ScenarioReport[] Scenarios { get; set; } = Array.Empty<ScenarioReport>();
}

/// <summary>
/// Represents a single scenario in the JSON report.
/// </summary>
public sealed class ScenarioReport
{
    public string FeatureName { get; set; } = "";
    public string? FeatureDescription { get; set; }
    public string ScenarioName { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Passed { get; set; }
    public bool Failed { get; set; }
    public List<StepReport> Steps { get; set; } = new();
}

/// <summary>
/// Represents a single step in the JSON report.
/// </summary>
public sealed class StepReport
{
    public string Kind { get; set; } = "";
    public string Title { get; set; } = "";
    public string Phase { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public bool Passed { get; set; }
    public string? Error { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Input { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Output { get; set; }
}
