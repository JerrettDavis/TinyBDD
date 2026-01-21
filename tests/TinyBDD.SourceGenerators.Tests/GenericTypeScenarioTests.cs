using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.SourceGenerators.Tests;

/// <summary>
/// Generic test class - should trigger TBDD012 warning.
/// Generated optimized code should NOT be produced.
/// </summary>
public partial class GenericTypeScenarioTests<T> : TinyBddXunitBase
{
    public GenericTypeScenarioTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task ScenarioInGenericType()
    {
        // This scenario should run normally but optimization should be skipped
        await Given("a value", () => 42)
             .When("add 8", x => x + 8)
             .Then("equals 50", x => x == 50);
    }
}

/// <summary>
/// Concrete instantiation for xUnit to discover the test.
/// </summary>
public class ConcreteGenericTypeScenarioTests : GenericTypeScenarioTests<int>
{
    public ConcreteGenericTypeScenarioTests(ITestOutputHelper output) : base(output)
    {
    }
}
