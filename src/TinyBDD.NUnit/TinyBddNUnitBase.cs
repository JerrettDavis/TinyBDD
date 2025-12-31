using NUnit.Framework;

namespace TinyBDD.NUnit;

/// <summary>
/// Base class for NUnit that initializes TinyBDD ambient context before each test
/// and writes a Gherkin report after each test.
/// </summary>
/// <remarks>
/// <para>
/// In <see cref="TinyBdd_SetUp"/>, this class creates a <see cref="ScenarioContext"/>, sets
/// <see cref="Ambient.Current"/>, and wires an <see cref="NUnitTraitBridge"/>. In
/// <see cref="TinyBdd_TearDown"/>, it emits a Gherkin report and clears the ambient context.
/// </para>
/// <para>
/// Feature-level setup/teardown is supported via <see cref="ConfigureFeatureSetup"/> and
/// <see cref="ConfigureFeatureTeardown"/>. These run once per test fixture via
/// <see cref="TinyBdd_OneTimeSetUp"/> and <see cref="TinyBdd_OneTimeTearDown"/>.
/// </para>
/// </remarks>
[Feature("Unnamed Feature")]
[UseTinyBdd]
public abstract class TinyBddNUnitBase : TestBase
{
    protected override IBddReporter Reporter => new NUnitBddReporter();

    /// <summary>Executes feature setup once before any tests in the fixture.</summary>
    /// <remarks>
    /// This method is called by NUnit before any test methods run in the fixture.
    /// Override <see cref="TestBase.ConfigureFeatureSetup"/> to define feature-level setup steps.
    /// </remarks>
    [OneTimeSetUp]
    public async Task TinyBdd_OneTimeSetUp()
    {
        // Create a temporary context for feature setup
        var traits = new NUnitTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        var previousContext = Ambient.Current.Value;
        Ambient.Current.Value = ctx;

        try
        {
            await ExecuteFeatureSetupAsync();
        }
        finally
        {
            Ambient.Current.Value = previousContext;
        }
    }

    /// <summary>Executes feature teardown once after all tests in the fixture complete.</summary>
    /// <remarks>
    /// This method is called by NUnit after all test methods in the fixture complete.
    /// Override <see cref="TestBase.ConfigureFeatureTeardown"/> to define feature-level teardown steps.
    /// </remarks>
    [OneTimeTearDown]
    public async Task TinyBdd_OneTimeTearDown()
    {
        // Create a temporary context for feature teardown
        var traits = new NUnitTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        var previousContext = Ambient.Current.Value;
        Ambient.Current.Value = ctx;

        try
        {
            await ExecuteFeatureTeardownAsync();
        }
        finally
        {
            Ambient.Current.Value = previousContext;
        }
    }

    /// <summary>Initializes the TinyBDD ambient context and trait bridge.</summary>
    /// <remarks>
    /// If you override <see cref="TestBase.ConfigureBackground"/>, call <see cref="TestBase.ExecuteBackgroundAsync"/>
    /// at the start of your test or in a derived <c>[SetUp]</c> method.
    /// </remarks>
    [SetUp]
    public async Task TinyBdd_SetUp()
    {
        var traits = new NUnitTraitBridge();
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;

        // Execute background for each test
        await ExecuteBackgroundAsync();
    }

    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    [TearDown]
    public void TinyBdd_TearDown()
        => CleanUp();
}
