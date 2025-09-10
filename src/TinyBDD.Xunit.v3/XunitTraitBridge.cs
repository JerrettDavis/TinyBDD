
using Xunit;

namespace TinyBDD.Xunit.v3;

/// <summary>
/// Bridges TinyBDD tags to xUnit by writing them to <see cref="ITestOutputHelper"/>.
/// xUnit traits are attribute-based and cannot be added at runtime; this bridge logs tags for visibility.
/// </summary>
public sealed class XunitTraitBridge : ITraitBridge
{
    private readonly ITestOutputHelper? _output;

    /// <summary>Creates a bridge that logs tags to the provided output sink.</summary>
    public XunitTraitBridge(ITestOutputHelper? output = null) => _output = output;

    /// <summary>Logs a tag to xUnit’s output sink when available.</summary>
    public void AddTag(string tag)
    {
        // xUnit has Traits via custom attributes, not settable at runtime.
        // We log tags for reporting, and users can add [Trait] manually if they want discovery filtering.
        _output?.WriteLine($"[TinyBDD] Tag: {tag}");
    }
}