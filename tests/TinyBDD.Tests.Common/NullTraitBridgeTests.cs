namespace TinyBDD.Tests.Common;

public class NullTraitBridgeTests
{
    [Fact]
    public void AddTag_Does_Not_Throw_And_Tags_Are_Still_In_Context()
    {
        var ctx = new ScenarioContext("F", null, "S", new NullTraitBridge());
        ctx.AddTag("a");
        ctx.AddTag("b");

        Assert.Contains("a", ctx.Tags);
        Assert.Contains("b", ctx.Tags);
    }
}