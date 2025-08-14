namespace TinyBDD.MSTest;

public sealed class MsTestTraitBridge : ITraitBridge
{
    public void AddTag(string tag)
    {
        // MSTest categories are attributes; log at runtime.
        TestContext!.WriteLine($"[TinyBDD] TestCategory: {tag}");
    }

    // Helper to inject TestContext
    public static TestContext? TestContext { get; set; }
}