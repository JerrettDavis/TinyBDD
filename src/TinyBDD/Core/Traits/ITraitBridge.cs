namespace TinyBDD;

/// <summary>
/// Abstraction for mapping TinyBDD scenario tags to the host test framework's
/// notion of categories/traits.
/// </summary>
public interface ITraitBridge
{
    /// <summary>
    /// Adds a tag to the current test using the host framework's capabilities, or logs it when
    /// runtime assignment is not supported.
    /// </summary>
    void AddTag(string tag); // map to Traits/Categories as supported by framework
}
