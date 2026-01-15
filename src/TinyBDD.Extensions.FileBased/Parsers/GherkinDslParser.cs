using System.Text.RegularExpressions;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Parsers;

/// <summary>
/// Parses Gherkin .feature files containing scenario definitions.
/// </summary>
/// <remarks>
/// Supports standard Gherkin syntax:
/// <code>
/// Feature: Feature Name
///   Optional feature description
/// 
/// Scenario: Scenario Name
///   Given the application is running
///   When I register a user with email "test@example.com"
///   Then the user should exist
/// 
/// Scenario Outline: Parameterized Scenario
///   Given a value of &lt;input&gt;
///   When I process it
///   Then the result should be &lt;output&gt;
/// 
/// Examples:
///   | input | output |
///   | 5     | 10     |
///   | 3     | 6      |
/// </code>
/// </remarks>
public sealed class GherkinDslParser : IDslParser
{
    private static readonly Regex FeatureRegex = new(@"^\s*Feature:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex ScenarioRegex = new(@"^\s*Scenario:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex ScenarioOutlineRegex = new(@"^\s*Scenario Outline:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex StepRegex = new(@"^\s*(Given|When|Then|And|But)\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex TagRegex = new(@"@(\w+)", RegexOptions.Compiled);
    private static readonly Regex ExamplesRegex = new(@"^\s*Examples:\s*$", RegexOptions.Compiled);
    private static readonly Regex TableRowRegex = new(@"^\s*\|(.+)\|\s*$", RegexOptions.Compiled);
    
    /// <inheritdoc />
    public async Task<FeatureDefinition> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Feature file not found: {filePath}", filePath);
        }

#if NETSTANDARD2_0 || NET462 || NET47 || NET471 || NET472 || NET48 || NET481
        // Use synchronous file reading for older frameworks to avoid thread pool overhead
        string content;
        using (var reader = new StreamReader(filePath))
        {
            content = reader.ReadToEnd();
        }
        await Task.CompletedTask.ConfigureAwait(false); // Make method async-compatible
#else
        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
#endif

        try
        {
            return ParseFeature(content);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to parse Gherkin file '{filePath}': {ex.Message}", ex);
        }
    }

    private static FeatureDefinition ParseFeature(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var feature = new FeatureDefinition();
        var currentTags = new List<string>();
        var currentScenario = default(ScenarioDefinition);
        var featureDescriptionLines = new List<string>();
        var parsingFeatureDescription = false;
        var parsingExamples = false;
        var exampleHeaders = new List<string>();
        var exampleRows = new List<List<string>>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.TrimStart();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                continue;
            }

            // Parse tags (can be multiple on one line)
            var tagMatches = TagRegex.Matches(line);
            if (tagMatches.Count > 0)
            {
                // Tags after feature description mean we're done with feature description
                if (parsingFeatureDescription && featureDescriptionLines.Count > 0)
                {
                    feature.Description = string.Join(" ", featureDescriptionLines).Trim();
                    featureDescriptionLines.Clear();
                    parsingFeatureDescription = false;
                }
                
                foreach (Match tagMatch in tagMatches)
                {
                    currentTags.Add(tagMatch.Groups[1].Value);
                }
                continue;
            }

            // Parse Feature
            var featureMatch = FeatureRegex.Match(line);
            if (featureMatch.Success)
            {
                feature.Name = featureMatch.Groups[1].Value.Trim();
                feature.Tags.AddRange(currentTags);
                currentTags.Clear();
                parsingFeatureDescription = true;
                continue;
            }

            // Parse Scenario or Scenario Outline
            var scenarioMatch = ScenarioRegex.Match(line);
            var scenarioOutlineMatch = ScenarioOutlineRegex.Match(line);
            
            if (scenarioMatch.Success || scenarioOutlineMatch.Success)
            {
                parsingFeatureDescription = false;
                parsingExamples = false;
                
                if (!string.IsNullOrWhiteSpace(string.Join(" ", featureDescriptionLines)))
                {
                    feature.Description = string.Join(" ", featureDescriptionLines).Trim();
                    featureDescriptionLines.Clear();
                }

                currentScenario = new ScenarioDefinition
                {
                    Name = (scenarioMatch.Success ? scenarioMatch.Groups[1] : scenarioOutlineMatch.Groups[1]).Value.Trim(),
                    Tags = new List<string>(currentTags)
                };
                
                feature.Scenarios.Add(currentScenario);
                currentTags.Clear();
                continue;
            }

            // Parse Examples
            var examplesMatch = ExamplesRegex.Match(line);
            if (examplesMatch.Success)
            {
                parsingExamples = true;
                exampleHeaders.Clear();
                exampleRows.Clear();
                continue;
            }

            // Parse table rows (for Examples)
            var tableMatch = TableRowRegex.Match(line);
            if (tableMatch.Success && parsingExamples)
            {
                var cells = tableMatch.Groups[1].Value
                    .Split('|')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                if (exampleHeaders.Count == 0)
                {
                    exampleHeaders.AddRange(cells);
                }
                else
                {
                    exampleRows.Add(cells);
                }
                continue;
            }

            // Parse Steps
            var stepMatch = StepRegex.Match(line);
            if (stepMatch.Success && currentScenario != null)
            {
                var keyword = stepMatch.Groups[1].Value;
                var text = stepMatch.Groups[2].Value.Trim();
                
                // If we have example data and this is a scenario outline, expand it
                if (exampleRows.Count > 0)
                {
                    // For scenario outlines with examples, we'll create multiple scenarios
                    // Remove the current scenario and replace with expanded versions
                    feature.Scenarios.Remove(currentScenario);
                    
                    foreach (var exampleRow in exampleRows)
                    {
                        var expandedScenario = new ScenarioDefinition
                        {
                            Name = $"{currentScenario.Name} (Example: {string.Join(", ", exampleRow)})",
                            Tags = new List<string>(currentScenario.Tags)
                        };

                        // Clone and expand all previous steps
                        foreach (var prevStep in currentScenario.Steps)
                        {
                            expandedScenario.Steps.Add(ExpandStep(prevStep, exampleHeaders, exampleRow));
                        }

                        // Add current step
                        expandedScenario.Steps.Add(CreateStep(keyword, text, exampleHeaders, exampleRow));
                        
                        feature.Scenarios.Add(expandedScenario);
                    }
                    
                    currentScenario = feature.Scenarios.Last();
                }
                else
                {
                    currentScenario.Steps.Add(CreateStep(keyword, text, null, null));
                }
                continue;
            }

            // Collect feature description lines (but NOT scenario or step lines)
            if (parsingFeatureDescription && !string.IsNullOrWhiteSpace(trimmedLine))
            {
                featureDescriptionLines.Add(trimmedLine);
            }
        }

        // Finalize feature description
        if (featureDescriptionLines.Count > 0 && string.IsNullOrWhiteSpace(feature.Description))
        {
            feature.Description = string.Join(" ", featureDescriptionLines).Trim();
        }

        return feature;
    }

    private static StepDefinition CreateStep(string keyword, string text, List<string>? headers, List<string>? values)
    {
        var expandedText = text;
        var parameters = new Dictionary<string, object?>();

        // Expand placeholders if we have example data
        if (headers != null && values != null)
        {
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                var placeholder = $"<{headers[i]}>";
                if (expandedText.Contains(placeholder))
                {
                    expandedText = expandedText.Replace(placeholder, values[i]);
                }
                parameters[headers[i]] = values[i];
            }
        }

        // Extract quoted string parameters and remove quotes from text
        // This allows the regex pattern matching to work with {paramName} placeholders
        var quotedStrings = Regex.Matches(expandedText, @"""([^""]+)""");
        foreach (Match match in quotedStrings)
        {
            var value = match.Groups[1].Value;
            // Remove the quotes from the text so pattern matching works
            expandedText = expandedText.Replace($"\"{value}\"", value);
        }

        return new StepDefinition
        {
            Keyword = keyword,
            Text = expandedText,
            Parameters = parameters
        };
    }

    private static StepDefinition ExpandStep(StepDefinition step, List<string> headers, List<string> values)
    {
        var expandedText = step.Text;
        var parameters = new Dictionary<string, object?>(step.Parameters);

        // Expand placeholders
        for (int i = 0; i < headers.Count && i < values.Count; i++)
        {
            var placeholder = $"<{headers[i]}>";
            if (expandedText.Contains(placeholder))
            {
                expandedText = expandedText.Replace(placeholder, values[i]);
            }
            parameters[headers[i]] = values[i];
        }

        return new StepDefinition
        {
            Keyword = step.Keyword,
            Text = expandedText,
            Parameters = parameters
        };
    }
}
