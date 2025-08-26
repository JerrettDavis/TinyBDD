namespace TinyBDD.MSTest;

/// <summary>
/// Bridges TinyBDD tags to MSTest by writing them to <see cref="TestContext"/> output.
/// MSTest categories are attribute-based and cannot be added at runtime, so this bridge logs tags for visibility.
/// </summary>
public sealed class MsTestTraitBridge : ITraitBridge
{
    /// <summary>Maps a TinyBDD tag to MSTest by writing it to the current <see cref="TestContext"/>.</summary>
    public void AddTag(string tag)
    {
        // MSTest categories are attributes; log at runtime.
        TestContext!.WriteLine($"[TinyBDD] TestCategory: {tag}");
    }

    /// <summary>The current MSTest context, injected by TinyBDD during test initialization.</summary>
    public static TestContext? TestContext { get; set; }
}