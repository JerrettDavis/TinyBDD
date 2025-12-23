using NUnit.Framework;

namespace TinyBDD.NUnit;

/// <summary>
/// Base class for NUnit that initializes TinyBDD ambient context before each test
/// and writes a Gherkin report after each test.
/// </summary>
/// <remarks>
/// In <see cref="TinyBdd_SetUp"/>, this class creates a <see cref="ScenarioContext"/>, sets
/// <see cref="Ambient.Current"/>, and wires an <see cref="NUnitTraitBridge"/>. In
/// <see cref="TinyBdd_TearDown"/>, it emits a Gherkin report and clears the ambient context.
/// </remarks>
[Feature("Unnamed Feature")]
[UseTinyBdd]
public abstract class TinyBddNUnitBase : TestBase
{
    protected override IBddReporter Reporter => new NUnitBddReporter();
    
    /// <summary>Initializes the TinyBDD ambient context and trait bridge.</summary>
    /// <remarks>
    /// If you override <see cref="TestBase.ConfigureBackground"/>, call <see cref="TestBase.ExecuteBackgroundAsync"/>
    /// at the start of your test or in a derived <c>[SetUp]</c> method.
    /// </remarks>
    [SetUp]
    public void TinyBdd_SetUp()
    {
        var traits = new NUnitTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    [TearDown]
    public void TinyBdd_TearDown()
        => CleanUp();
}
