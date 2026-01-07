using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.SourceGenerators.Tests;

public partial class SimpleScenarioTests : TinyBddXunitBase
{
    public SimpleScenarioTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task SimpleAddition()
    {
        // Auto-optimized by source generator (no attribute needed!)
        await Given("a number", () => 5)
             .When("add 3", x => x + 3)
             .Then("equals 8", x => x == 8);
    }

    [Fact]
    public async Task TypeTransition()
    {
        // Auto-optimized - type transitions (int -> string) handled automatically
        await Given("a number", () => 42)
             .When("convert to string", x => x.ToString())
             .Then("is correct", str => str == "42");
    }

    [Fact]
    [DisableOptimization]
    public async Task WithoutOptimization()
    {
        // Opt-out: uses standard pipeline
        await Given("start", () => 10)
             .When("double", x => x * 2)
             .Then("equals 20", x => x == 20);
    }

    [Fact]
    [GenerateOptimized]  // Optional - explicit opt-in (redundant but allowed)
    public async Task ExplicitOptIn()
    {
        await Given("start", () => 1)
             .When("increment", x => x + 1)
             .Then("equals 2", x => x == 2);
    }
}

