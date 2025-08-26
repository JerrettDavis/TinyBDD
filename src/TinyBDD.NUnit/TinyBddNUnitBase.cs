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
public abstract class TinyBddNUnitBase
{
    /// <summary>The current scenario context for the running test.</summary>
    protected ScenarioContext Scenario => Ambient.Current.Value!;

    /// <summary>Initializes the TinyBDD ambient context and trait bridge.</summary>
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
    {
        var ctx = Ambient.Current.Value;
        if (ctx is not null)
        {
            var reporter = new NUnitBddReporter();
            GherkinFormatter.Write(ctx, reporter);
        }
        Ambient.Current.Value = null;
    }
}
