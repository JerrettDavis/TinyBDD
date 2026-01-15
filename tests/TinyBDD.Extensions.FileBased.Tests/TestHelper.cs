using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

internal static class TestHelper
{
    public static FeatureDefinition CreateTestFeature()
    {
        return new FeatureDefinition
        {
            Name = "Test Feature",
            Description = "A test feature for unit testing",
            Tags = new List<string> { "@test" },
            Scenarios = new List<ScenarioDefinition>
            {
                new ScenarioDefinition
                {
                    Name = "Test Scenario",
                    Tags = new List<string> { "@smoke" },
                    Steps = new List<StepDefinition>
                    {
                        new StepDefinition
                        {
                            Keyword = "Given",
                            Text = "a calculator"
                        },
                        new StepDefinition
                        {
                            Keyword = "When",
                            Text = "I add 5 and 3",
                            Parameters = new Dictionary<string, object?>
                            {
                                { "a", 5 },
                                { "b", 3 }
                            }
                        },
                        new StepDefinition
                        {
                            Keyword = "Then",
                            Text = "the result should be 8",
                            Parameters = new Dictionary<string, object?>
                            {
                                { "expected", 8 }
                            }
                        }
                    }
                }
            }
        };
    }
}
