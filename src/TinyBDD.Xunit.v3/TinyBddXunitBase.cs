using Xunit;

namespace TinyBDD.Xunit.v3;

/// <summary>
/// Base class for xUnit.v3 that initializes TinyBDD ambient context in the constructor
/// and writes a Gherkin report when disposed at the end of the test.
/// </summary>
/// <remarks>
/// <para>
/// The constructor sets <see cref="Ambient.Current"/> and wires an <see cref="XunitTraitBridge"/>.
/// <see cref="IAsyncLifetime.InitializeAsync"/> executes any configured background steps.
/// <see cref="DisposeAsync"/> emits a Gherkin report and clears the ambient context.
/// </para>
/// <para>
/// To configure background steps, override <see cref="TestBase.ConfigureBackground"/>.
/// </para>
/// </remarks>
[Feature("Unnamed Feature")]
[UseTinyBdd]
public abstract class TinyBddXunitBase : TestBase, IAsyncLifetime
{
    private readonly ITestOutputHelper _output;

    protected override IBddReporter Reporter => new XunitBddReporter(_output);

    /// <summary>Initializes the base with xUnit's <see cref="ITestOutputHelper"/> and sets up TinyBDD context.</summary>
    protected TinyBddXunitBase(ITestOutputHelper output)
    {
        _output = output;
        var traits = new XunitTraitBridge(output);
        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

    /// <summary>Executes background steps if configured.</summary>
    public virtual ValueTask InitializeAsync()
    {
        return new ValueTask(ExecuteBackgroundAsync());
    }

    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    public virtual ValueTask DisposeAsync()
    {
        CleanUp();
        return default;
    }
}