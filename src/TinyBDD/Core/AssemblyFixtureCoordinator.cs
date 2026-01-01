using System.Collections.Concurrent;
using System.Reflection;

namespace TinyBDD;

/// <summary>
/// Coordinates the lifecycle of assembly-wide fixtures across the entire test assembly.
/// </summary>
/// <remarks>
/// <para>
/// This coordinator discovers all <see cref="AssemblySetupAttribute"/> declarations,
/// instantiates the corresponding <see cref="AssemblyFixture"/> types, and manages
/// their setup and teardown lifecycle.
/// </para>
/// <para>
/// The coordinator ensures that:
/// <list type="bullet">
/// <item><description>Fixtures are initialized exactly once before any tests run.</description></item>
/// <item><description>Fixtures are torn down exactly once after all tests complete.</description></item>
/// <item><description>Multiple fixtures execute in registration order.</description></item>
/// <item><description>Fixture state is accessible via <see cref="GetFixture{T}"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// This is a framework-agnostic singleton that integrates with xUnit, NUnit, and MSTest
/// via their respective assembly fixture mechanisms.
/// </para>
/// </remarks>
/// <seealso cref="AssemblyFixture"/>
/// <seealso cref="AssemblySetupAttribute"/>
public sealed class AssemblyFixtureCoordinator
{
    private static readonly object _lock = new();
    private static AssemblyFixtureCoordinator? _instance;
    private static bool _isInitialized;

    private readonly ConcurrentDictionary<Type, AssemblyFixture> _fixtures = new();
    private readonly List<AssemblyFixture> _fixtureOrder = new();
    private bool _setupComplete;
    private bool _teardownComplete;

    private AssemblyFixtureCoordinator()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the coordinator.
    /// </summary>
    public static AssemblyFixtureCoordinator Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new AssemblyFixtureCoordinator();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Discovers and initializes all assembly fixtures from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for <see cref="AssemblySetupAttribute"/> declarations.</param>
    /// <param name="reporter">Optional reporter for Gherkin-style logging.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <remarks>
    /// This method is idempotent - calling it multiple times has no effect after the first call.
    /// </remarks>
    public async Task InitializeAsync(Assembly assembly, IBddReporter? reporter = null, CancellationToken ct = default)
    {
        if (_isInitialized)
            return;

        lock (_lock)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
        }

        // Discover all AssemblySetupAttribute declarations
        var attributes = assembly.GetCustomAttributes<AssemblySetupAttribute>().ToList();

        if (attributes.Count == 0)
            return;

        // Instantiate and register fixtures
        foreach (var attr in attributes)
        {
            var fixtureType = attr.FixtureType;

            // Create instance
            var fixture = (AssemblyFixture)Activator.CreateInstance(fixtureType)!;
            fixture.Reporter = reporter;

            _fixtures[fixtureType] = fixture;
            _fixtureOrder.Add(fixture);
        }

        // Execute setup for all fixtures in order
        foreach (var fixture in _fixtureOrder)
        {
            await fixture.InternalSetupAsync(ct);
        }

        _setupComplete = true;
    }

    /// <summary>
    /// Tears down all initialized assembly fixtures.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous teardown operation.</returns>
    /// <remarks>
    /// Fixtures are torn down in reverse order of their initialization.
    /// This method is idempotent - calling it multiple times has no effect after the first call.
    /// </remarks>
    public async Task TeardownAsync(CancellationToken ct = default)
    {
        if (_teardownComplete || !_setupComplete)
            return;

        lock (_lock)
        {
            if (_teardownComplete)
                return;

            _teardownComplete = true;
        }

        // Execute teardown in reverse order
        for (var i = _fixtureOrder.Count - 1; i >= 0; i--)
        {
            var fixture = _fixtureOrder[i];
            try
            {
                await fixture.InternalTeardownAsync(ct);
            }
            catch (Exception ex)
            {
                // Continue tearing down other fixtures even if one fails
                // Log the error but don't stop the teardown process
                fixture.Reporter?.WriteLine($"Assembly teardown failed for {fixture.GetType().Name}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Retrieves a registered assembly fixture by type.
    /// </summary>
    /// <typeparam name="T">The type of assembly fixture to retrieve.</typeparam>
    /// <returns>The registered instance of the fixture.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the fixture has not been registered or initialized.
    /// </exception>
    /// <remarks>
    /// This method provides convenient access to assembly fixtures from within tests.
    /// </remarks>
    public static T GetFixture<T>() where T : AssemblyFixture
    {
        var type = typeof(T);

        if (!Instance._fixtures.TryGetValue(type, out var fixture))
        {
            throw new InvalidOperationException(
                $"Assembly fixture '{type.Name}' has not been registered. " +
                $"Ensure the assembly has [assembly: AssemblySetup(typeof({type.Name}))] declared.");
        }

        return (T)fixture;
    }

    /// <summary>
    /// Resets the coordinator state. Used for testing purposes.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _instance = null;
            _isInitialized = false;
        }
    }
}
