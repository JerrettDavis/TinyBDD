using System.Text;
using Xunit.Abstractions;

namespace TinyBDD.Xunit.Tests;

public sealed class FakeOutput : ITestOutputHelper
{
    private readonly StringBuilder _sb = new();
    public void WriteLine(string message) => _sb.AppendLine(message);

    public void WriteLine(string format, params object[] args) =>
        _sb.AppendLine(string.Format(format, args));

    public override string ToString() => _sb.ToString();
}

[Feature("Xunit adapter")]
public class XunitTraitBridgeTests(ITestOutputHelper output)
{
    [Scenario("Trait bridge logs tags")]
    [Fact]
    public async Task AddTag_Writes_To_Output()
    {
        var tee = new TeeOutput(output);
        var traits = new XunitTraitBridge(tee);
        var ctx = Bdd.CreateContext(this, traits: traits);

        ctx.AddTag("smoke");
        ctx.AddTag("fast");

        await Bdd.Given(ctx, "wire", () => 1)
            .When("noop", (x, _) => Task.CompletedTask)
            .Then("ok", _ => Task.CompletedTask);

        var log = tee.ToString(); // assert against this
        Assert.Contains("[TinyBDD] Tag: smoke", log);
        Assert.Contains("[TinyBDD] Tag: fast", log);
    }


    [Scenario("Reporter writes")]
    [Fact]
    public void Reporter_Writes_Lines()
    {
        var outFx = new TeeOutput(output);
        var reporter = new XunitBddReporter(outFx);

        reporter.WriteLine("hello");
        reporter.WriteLine("world");

        Assert.Contains("hello", outFx.ToString());
        Assert.Contains("world", outFx.ToString());
    }
}