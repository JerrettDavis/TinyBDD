using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.SourceGenerators.Tests;

/// <summary>
/// Test class WITHOUT partial modifier - should trigger TBDD010 warning.
/// Generated optimized code should NOT be produced.
/// </summary>
public class NonPartialScenarioTests : TinyBddXunitBase
{
    public NonPartialScenarioTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task ScenarioInNonPartialClass()
    {
        // This scenario should run normally but optimization should be skipped
        await Given("a number", () => 5)
             .When("add 3", x => x + 3)
             .Then("equals 8", x => x == 8);
    }

    [Fact]
    public async Task AnotherScenarioInNonPartialClass()
    {
        // Multiple methods in the same non-partial class should only emit one warning (spam control)
        await Given("a string", () => "hello")
             .When("uppercase", s => s.ToUpper())
             .Then("equals HELLO", s => s == "HELLO");
    }
}
