using Xunit.Abstractions;

namespace TinyBDD.Xunit;

/// <summary>
/// Base class for xUnit that initializes TinyBDD ambient context in the constructor
/// and writes a Gherkin report when disposed at the end of the test.
/// </summary>
/// <remarks>
/// The constructor sets <see cref="Ambient.Current"/> and wires an <see cref="XunitTraitBridge"/>.
/// <see cref="Dispose"/> emits a Gherkin report and clears the ambient context.
/// </remarks>
[Feature("Unnamed Feature")]
[UseTinyBdd]
public abstract class TinyBddXunitBase : TestBase, IDisposable
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
    
    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    public void Dispose()
    {
        CleanUp();
        
        GC.SuppressFinalize(this);
    }
}