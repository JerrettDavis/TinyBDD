namespace TinyBDD;

/// <summary>
/// No-op implementation of <see cref="ITraitBridge"/> used when no host test framework
/// integration is desired. Tags are ignored.
/// </summary>
public sealed class NullTraitBridge : ITraitBridge
{
    /// <summary>Does nothing.</summary>
    public void AddTag(string tag) { }
}
