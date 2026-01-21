using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.SourceGenerators.Tests;

/// <summary>
/// Outer class to test nested type scenario - should trigger TBDD011 warning.
/// </summary>
public partial class OuterTestClass
{
    /// <summary>
    /// Nested test class - should trigger TBDD011 warning even if partial.
    /// Generated optimized code should NOT be produced.
    /// </summary>
    public partial class NestedTypeScenarioTests : TinyBddXunitBase
    {
        public NestedTypeScenarioTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task ScenarioInNestedType()
        {
            // This scenario should run normally but optimization should be skipped
            await Given("a number", () => 10)
                 .When("multiply by 2", x => x * 2)
                 .Then("equals 20", x => x == 20);
        }
    }
}
