using TinyBDD.Extensions.FileBased.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TinyBDD.Extensions.FileBased.Parsers;

/// <summary>
/// Parses YAML files containing scenario definitions.
/// </summary>
/// <remarks>
/// Expected YAML format:
/// <code>
/// feature: Feature Name
/// description: Optional description
/// tags:
///   - tag1
///   - tag2
/// scenarios:
///   - name: Scenario Name
///     description: Optional description
///     tags:
///       - scenario-tag
///     steps:
///       - keyword: Given
///         text: the application is running
///       - keyword: When
///         text: I register a user with email {email}
///         parameters:
///           email: test@example.com
///       - keyword: Then
///         text: the user should exist
/// </code>
/// </remarks>
public sealed class YamlDslParser : IDslParser
{
    private readonly IDeserializer _deserializer;

    public YamlDslParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <inheritdoc />
    public async Task<FeatureDefinition> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Scenario file not found: {filePath}", filePath);
        }

#if NETSTANDARD2_0 || NET462 || NET47 || NET471 || NET472 || NET48 || NET481
        // Use synchronous file reading for older frameworks to avoid thread pool overhead
        string yaml;
        using (var reader = new StreamReader(filePath))
        {
            yaml = reader.ReadToEnd();
        }
        await Task.CompletedTask.ConfigureAwait(false); // Make method async-compatible
#else
        var yaml = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
#endif
        
        try
        {
            var yamlModel = _deserializer.Deserialize<YamlFeatureModel>(yaml);
            return MapToFeatureDefinition(yamlModel);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse YAML file '{filePath}': {ex.Message}", ex);
        }
    }

    private static FeatureDefinition MapToFeatureDefinition(YamlFeatureModel yaml)
    {
        return new FeatureDefinition
        {
            Name = yaml.Feature ?? "Unnamed Feature",
            Description = yaml.Description,
            Tags = yaml.Tags ?? new List<string>(),
            Scenarios = yaml.Scenarios?.Select(MapToScenarioDefinition).ToList() ?? new List<ScenarioDefinition>()
        };
    }

    private static ScenarioDefinition MapToScenarioDefinition(YamlScenarioModel yaml)
    {
        return new ScenarioDefinition
        {
            Name = yaml.Name ?? "Unnamed Scenario",
            Description = yaml.Description,
            Tags = yaml.Tags ?? new List<string>(),
            Steps = yaml.Steps?.Select(MapToStepDefinition).ToList() ?? new List<StepDefinition>()
        };
    }

    private static StepDefinition MapToStepDefinition(YamlStepModel yaml)
    {
        return new StepDefinition
        {
            Keyword = yaml.Keyword ?? "Given",
            Text = yaml.Text ?? string.Empty,
            Parameters = yaml.Parameters ?? new Dictionary<string, object?>()
        };
    }

    // Internal YAML models
    private sealed class YamlFeatureModel
    {
        public string? Feature { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
        public List<YamlScenarioModel>? Scenarios { get; set; }
    }

    private sealed class YamlScenarioModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
        public List<YamlStepModel>? Steps { get; set; }
    }

    private sealed class YamlStepModel
    {
        public string? Keyword { get; set; }
        public string? Text { get; set; }
        public Dictionary<string, object?>? Parameters { get; set; }
    }
}
