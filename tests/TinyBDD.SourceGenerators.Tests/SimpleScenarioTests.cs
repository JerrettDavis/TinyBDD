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
    [GenerateOptimized]
    public async Task SimpleAddition()
    {
        await Given("a number", () => 5)
             .When("add 3", x => x + 3)
             .Then("equals 8", x => x == 8);
    }

    [Fact]
    [GenerateOptimized]
    public async Task TypeTransition()
    {
        await Given("a number", () => 42)
             .When("convert to string", x => x.ToString())
             .Then("is correct", str => str == "42");
    }

    [Fact]
    public async Task WithoutOptimization()
    {
        // No [GenerateOptimized] - uses standard pipeline
        await Given("start", () => 10)
             .When("double", x => x * 2)
             .Then("equals 20", x => x == 20);
    }
}

