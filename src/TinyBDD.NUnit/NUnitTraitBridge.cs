using NUnit.Framework;

namespace TinyBDD.NUnit;

public sealed class NUnitTraitBridge : ITraitBridge
{
    public void AddTag(string tag)
    {
        // NUnit categories are attributes. At runtime we cannot add them to the test,
        // but we can push them into TestContext for logs.
        TestContext.Out.WriteLine($"[{nameof(TinyBDD)}] Category: {tag}");
    }
}