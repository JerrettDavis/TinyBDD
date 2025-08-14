using TinyBDD;
using Xunit.Abstractions;

namespace TinyBDD.Xunit;

public sealed class XunitTraitBridge : ITraitBridge
{
    private readonly ITestOutputHelper? _output;
    public XunitTraitBridge(ITestOutputHelper? output = null) => _output = output;
    public void AddTag(string tag)
    {
        // xUnit has Traits via custom attributes, not settable at runtime.
        // We log tags for reporting, and users can add [Trait] manually if they want discovery filtering.
        _output?.WriteLine($"[TinyBDD] Tag: {tag}");
    }
}