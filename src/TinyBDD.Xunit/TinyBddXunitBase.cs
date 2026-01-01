using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Xunit;

/// <summary>
/// Base class for xUnit that initializes TinyBDD ambient context in the constructor
/// and writes a Gherkin report when disposed at the end of the test.
/// </summary>
/// <remarks>
/// <para>
/// The constructor sets <see cref="Ambient.Current"/> and wires an <see cref="XunitTraitBridge"/>.
/// <see cref="IAsyncLifetime.InitializeAsync"/> executes any configured background steps.
/// <see cref="IAsyncLifetime.DisposeAsync"/> emits a Gherkin report and clears the ambient context.
/// </para>
/// <para>
/// To configure background steps, override <see cref="TestBase.ConfigureBackground"/>.
/// </para>
/// <para>
/// To configure feature-level setup/teardown, override <see cref="TestBase.ConfigureFeatureSetup"/>
/// and <see cref="TestBase.ConfigureFeatureTeardown"/>. Feature setup runs once before the first test
/// in the class, and feature teardown can be triggered manually via <see cref="ExecuteFeatureTeardownExplicitlyAsync"/>.
/// </para>
/// </remarks>
[Feature("Unnamed Feature")]
[UseTinyBdd]
public abstract class TinyBddXunitBase : TestBase, IAsyncLifetime
{
    private static readonly ConcurrentDictionary<Type, object?> _featureStates = new();
    private static readonly ConcurrentDictionary<Type, bool> _featureSetupComplete = new();
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> _setupLocks = new();

    private readonly ITestOutputHelper _output;
    private bool _disposed;

    protected override IBddReporter Reporter => new XunitBddReporter(_output);

    /// <summary>Initializes the base with xUnit's <see cref="ITestOutputHelper"/> and sets up TinyBDD context.</summary>
    protected TinyBddXunitBase(ITestOutputHelper output)
    {
        _output = output;
        var traits = new XunitTraitBridge(output);

        var ctx = Bdd.CreateContext(this, traits: traits);
        Ambient.Current.Value = ctx;
    }

    /// <summary>Executes feature setup (once per class) and background steps (per test).</summary>
    public virtual async Task InitializeAsync()
    {
        // Execute feature setup once per test class type
        await EnsureFeatureSetupAsync();

        // Execute background steps for this scenario
        await ExecuteBackgroundAsync();
    }

    /// <summary>Writes a Gherkin report and clears the ambient context.</summary>
    public virtual Task DisposeAsync()
    {
        CleanUp();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ensures feature setup runs exactly once for this test class type.
    /// </summary>
    private async Task EnsureFeatureSetupAsync()
    {
        var type = GetType();
        var setupLock = _setupLocks.GetOrAdd(type, _ => new SemaphoreSlim(1, 1));

        if (_featureSetupComplete.ContainsKey(type))
        {
            // Feature setup already complete, restore state
            if (_featureStates.TryGetValue(type, out var state))
            {
                FeatureState = state;
                FeatureSetupExecuted = true;
            }
            return;
        }

        await setupLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_featureSetupComplete.ContainsKey(type))
            {
                if (_featureStates.TryGetValue(type, out var state))
                {
                    FeatureState = state;
                    FeatureSetupExecuted = true;
                }
                return;
            }

            // Execute feature setup
            await ExecuteFeatureSetupAsync();

            // Store state for subsequent tests
            _featureStates[type] = FeatureState;
            _featureSetupComplete[type] = true;
        }
        finally
        {
            setupLock.Release();
        }
    }

    /// <summary>
    /// Explicitly executes feature teardown. Call this from a manual cleanup method if needed.
    /// </summary>
    /// <remarks>
    /// Due to xUnit's per-test instance model, feature teardown cannot be automatically triggered.
    /// If you need guaranteed cleanup, consider using IClassFixture or manually calling this method
    /// from a static cleanup mechanism.
    /// </remarks>
    protected async Task ExecuteFeatureTeardownExplicitlyAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        var type = GetType();
        if (_featureSetupComplete.ContainsKey(type))
        {
            await ExecuteFeatureTeardownAsync();
            _featureSetupComplete.TryRemove(type, out _);
            _featureStates.TryRemove(type, out _);
        }
    }
}