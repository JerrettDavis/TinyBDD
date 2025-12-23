using Xunit.Abstractions;

namespace TinyBDD.Xunit.Tests
{
    // --- Test doubles ---

    /// <summary>Captures lines written by TinyBDD's XunitBddReporter and XunitTraitBridge.</summary>
    internal sealed class CapturingOutput : ITestOutputHelper
    {
        public List<string> Lines { get; } = new();

        public void WriteLine(string message) => Lines.Add(message);

        // xUnit v2 ITestOutputHelper includes this overload; keep it to be safe.
        public void WriteLine(string format, params object[] args) =>
            Lines.Add(string.Format(format, args));
    }

    // --- A demo test class that uses the real base class under test ---

    // Give the feature a name and description, so we can assert both show up.
    [Feature("Inventory", "Basic stock operations")]
    public sealed class DemoTinyBddXunit(ITestOutputHelper output) : TinyBddXunitBase(output)
    {
        /// <summary>
        /// Runs a minimal scenario so the base class emits a Gherkin report on Dispose().
        /// </summary>
        public static Task RunScenarioAsync() =>
            Flow.Given("wire", () => 1)
                .When("act", x => x + 0)
                .Then("assert", v => v == 1)
                .AssertPassed();
    }

    public class TinyBddXunitBaseOutputTests
    {
        [Fact]
        [Scenario("A cool scenario with all the whistles", "Tag1", "Tag2")]
        public async Task Emits_Feature_Description_Scenario_Tags_And_Steps()
        {
            // Arrange
            var output = new CapturingOutput();
            var demo = new DemoTinyBddXunit(output);

            // Act
            await demo.InitializeAsync(); // execute any background steps
            await DemoTinyBddXunit.RunScenarioAsync();
            await demo.DisposeAsync(); // triggers Gherkin emission via the base class

            var text = string.Join(Environment.NewLine, output.Lines);

            // Assert: tags logged by XunitTraitBridge
            Assert.Contains("[TinyBDD] Tag: Tag1", text);
            Assert.Contains("[TinyBDD] Tag: Tag2", text);

            // Assert: feature + description
            Assert.Contains("Feature: Inventory", text);
            Assert.Contains("Basic stock operations", text);

            // Assert: scenario name (from [Scenario] on THIS test method)
            Assert.Contains("Scenario: A cool scenario with all the whistles", text);

            // Assert: step lines (status and durations may vary; just check the prefix)
            Assert.Contains("  Given wire [OK]", text);
            Assert.Contains("  When act [OK]", text);
            Assert.Contains("  Then assert [OK]", text);
        }

        [Fact]
        public async Task Emits_MethodName_As_Scenario_When_ScenarioAttribute_Missing()
        {
            // Arrange
            var output = new CapturingOutput();
            var demo = new DemoTinyBddXunit(output);

            // Act
            await demo.InitializeAsync(); // execute any background steps
            await DemoTinyBddXunit.RunScenarioAsync();
            await demo.DisposeAsync();

            var text = string.Join(Environment.NewLine, output.Lines);

            // Assert: still shows feature name
            Assert.Contains("Feature: Inventory", text);

            // Scenario falls back to the test method name
            Assert.Contains("Scenario: Emits_MethodName_As_Scenario_When_ScenarioAttribute_Missing", text);

            // Steps present
            Assert.Contains("  Given wire [OK]", text);
            Assert.Contains("  When act [OK]", text);
            Assert.Contains("  Then assert [OK]", text);
        }
    }
}