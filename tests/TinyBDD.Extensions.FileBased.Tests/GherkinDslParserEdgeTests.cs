using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Tests;

/// <summary>
/// Edge-case parser tests targeting branches not covered by the existing
/// <see cref="GherkinDslParserAdditionalTests"/> and <see cref="GherkinDslParserTests"/>.
/// </summary>
public class GherkinDslParserEdgeTests
{
    private static async Task<string> WriteTempAsync(string content, string suffix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{suffix}-{Guid.NewGuid()}.feature");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [Fact]
    public async Task ParseAsync_FeatureWithDescriptionButNoScenarios_FinalizesDescription()
    {
        var parser = new GherkinDslParser();
        var content = @"Feature: Lonely Feature
  This feature has a description
  spanning multiple lines
  but no scenarios at all
";
        var path = await WriteTempAsync(content, "lonely-feature");

        try
        {
            var feature = await parser.ParseAsync(path);
            Assert.Equal("Lonely Feature", feature.Name);
            Assert.NotNull(feature.Description);
            Assert.Contains("description", feature.Description);
            Assert.Contains("spanning", feature.Description);
            Assert.Empty(feature.Scenarios);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsync_FeatureWithDescriptionEndsAtTags_FinalizesAtTagBoundary()
    {
        var parser = new GherkinDslParser();
        // Description lines collected, then a tag line. The tag-boundary branch
        // finalizes the description before the scenario tag is collected.
        var content = @"Feature: Tagged
  some description text here

@scn
Scenario: One
  Given x
";
        var path = await WriteTempAsync(content, "tag-boundary");

        try
        {
            var feature = await parser.ParseAsync(path);
            Assert.Equal("Tagged", feature.Name);
            Assert.NotNull(feature.Description);
            Assert.Contains("some description text", feature.Description);
            Assert.Single(feature.Scenarios);
            Assert.Contains("scn", feature.Scenarios[0].Tags);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsync_StepWithQuotedString_StripsQuotes()
    {
        // Verifies the quoted-string pre-processing branch in CreateStep.
        var parser = new GherkinDslParser();
        var content = @"Feature: Quoted
Scenario: With quotes
  Given a user named ""alice"" exists
";
        var path = await WriteTempAsync(content, "quoted-string");

        try
        {
            var feature = await parser.ParseAsync(path);
            Assert.Single(feature.Scenarios);
            var step = feature.Scenarios[0].Steps[0];
            // Quotes should be stripped from the canonical text:
            Assert.Equal("a user named alice exists", step.Text);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseAsync_SkipsCommentsAndBlankLines()
    {
        var parser = new GherkinDslParser();
        var content = @"# This is a top-level comment
Feature: Commented Feature

# A comment inside
Scenario: One
  Given a step
  # an inner comment
";
        var path = await WriteTempAsync(content, "commented");

        try
        {
            var feature = await parser.ParseAsync(path);
            Assert.Equal("Commented Feature", feature.Name);
            Assert.Single(feature.Scenarios);
            Assert.Single(feature.Scenarios[0].Steps);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
