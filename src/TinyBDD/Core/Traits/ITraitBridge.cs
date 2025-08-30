namespace TinyBDD;

/// <summary>
/// Abstraction for mapping TinyBDD scenario tags to the host test framework's
/// notion of categories/traits.
/// </summary>
/// <remarks>
/// Implementations are provided by adapters such as TinyBDD.Xunit, TinyBDD.NUnit, and TinyBDD.MSTest.
/// They receive tags via <see cref="ScenarioContext.AddTag(string)"/> and forward them to the underlying
/// framework where possible (e.g., categories), or log them in the framework reporter.
/// </remarks>
/// <seealso href="xref:TinyBDD.ScenarioContext"/>
/// <seealso href="xref:TinyBDD.MSTest.MsTestTraitBridge"/>
/// <seealso href="xref:TinyBDD.NUnit.NUnitTraitBridge"/>
/// <seealso href="xref:TinyBDD.Xunit.XunitTraitBridge"/>
public interface ITraitBridge
{
    /// <summary>
    /// Adds a tag to the current test using the host framework's capabilities, or logs it when
    /// runtime assignment is not supported.
    /// </summary>
    /// <param name="tag">The tag name to associate with the current scenario.</param>
    void AddTag(string tag); // map to Traits/Categories as supported by framework
}
