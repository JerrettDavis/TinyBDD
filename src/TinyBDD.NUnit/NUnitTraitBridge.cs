using NUnit.Framework;

namespace TinyBDD.NUnit;

/// <summary>
/// Bridges TinyBDD tags to NUnit by writing them to <see cref="TestContext"/> output.
/// NUnit categories are attribute-based and cannot be added at runtime.
/// </summary>
public sealed class NUnitTraitBridge : ITraitBridge
{
    /// <summary>Logs a tag to NUnit's test output.</summary>
    public void AddTag(string tag)
    {
        // NUnit categories are attributes. At runtime we cannot add them to the test,
        // but we can push them into TestContext for logs.
        TestContext.Out.WriteLine($"[{nameof(TinyBDD)}] Category: {tag}");
    }
}