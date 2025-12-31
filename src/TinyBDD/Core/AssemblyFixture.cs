namespace TinyBDD;

/// <summary>
/// Base class for assembly-wide setup and teardown that executes once per test assembly.
/// </summary>
/// <remarks>
/// <para>
/// Derive from this class to create fixtures that need to run expensive setup operations
/// once for the entire test assembly (e.g., starting a test database, initializing a DI container,
/// spawning external services, or loading test data).
/// </para>
/// <para>
/// The lifecycle is:
/// <list type="number">
/// <item><description><see cref="SetupAsync"/> is called once before any tests run in the assembly.</description></item>
/// <item><description>All test features and scenarios execute.</description></item>
/// <item><description><see cref="TeardownAsync"/> is called once after all tests complete.</description></item>
/// </list>
/// </para>
/// <para>
/// Assembly fixtures are framework-agnostic and work with xUnit, NUnit, and MSTest.
/// They are discovered via the <see cref="AssemblySetupAttribute"/> attribute.
/// </para>
/// </remarks>
/// <example>
/// <para>Example: Database fixture for an assembly</para>
/// <code>
/// using TinyBDD;
///
/// [assembly: AssemblySetup(typeof(DatabaseFixture))]
///
/// public class DatabaseFixture : AssemblyFixture
/// {
///     private TestDatabase? _db;
///
///     public TestDatabase Database => _db ?? throw new InvalidOperationException("Database not initialized");
///
///     protected override async Task SetupAsync(CancellationToken ct)
///     {
///         _db = new TestDatabase();
///         await _db.StartAsync(ct);
///         await _db.SeedTestDataAsync(ct);
///     }
///
///     protected override async Task TeardownAsync(CancellationToken ct)
///     {
///         if (_db is not null)
///             await _db.DisposeAsync();
///     }
/// }
///
/// // In your tests:
/// public class MyTests : TinyBddXunitBase
/// {
///     [Fact]
///     public async Task CanQueryDatabase()
///     {
///         var db = AssemblyFixture.Get&lt;DatabaseFixture&gt;().Database;
///         // Use the database...
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AssemblySetupAttribute"/>
/// <seealso cref="AssemblyFixtureCoordinator"/>
public abstract class AssemblyFixture
{
    /// <summary>
    /// Gets or sets the optional reporter used to emit Gherkin-style assembly setup/teardown logs.
    /// </summary>
    /// <remarks>
    /// If set, the coordinator will write "Assembly Setup" and "Assembly Teardown" sections
    /// to the reporter during lifecycle events.
    /// </remarks>
    public IBddReporter? Reporter { get; set; }

    /// <summary>
    /// Override to perform assembly-wide setup operations.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous setup operation.</returns>
    /// <remarks>
    /// This method is called once before any tests run in the assembly.
    /// Use this to initialize expensive resources that can be shared across all tests.
    /// </remarks>
    protected virtual Task SetupAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    /// <summary>
    /// Override to perform assembly-wide teardown operations.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous teardown operation.</returns>
    /// <remarks>
    /// This method is called once after all tests complete in the assembly.
    /// Use this to clean up expensive resources initialized in <see cref="SetupAsync"/>.
    /// </remarks>
    protected virtual Task TeardownAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    /// <summary>
    /// Internal method called by the coordinator to execute setup.
    /// </summary>
    internal async Task InternalSetupAsync(CancellationToken ct = default)
    {
        if (Reporter is not null)
            Reporter.WriteLine("Assembly Setup:");

        try
        {
            await SetupAsync(ct);

            if (Reporter is not null)
                Reporter.WriteLine($"  {GetType().Name} [OK]");
        }
        catch (Exception ex)
        {
            if (Reporter is not null)
            {
                Reporter.WriteLine($"  {GetType().Name} [FAIL]");
                Reporter.WriteLine($"    Error: {ex.GetType().Name}: {ex.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// Internal method called by the coordinator to execute teardown.
    /// </summary>
    internal async Task InternalTeardownAsync(CancellationToken ct = default)
    {
        if (Reporter is not null)
            Reporter.WriteLine("Assembly Teardown:");

        try
        {
            await TeardownAsync(ct);

            if (Reporter is not null)
                Reporter.WriteLine($"  {GetType().Name} [OK]");
        }
        catch (Exception ex)
        {
            if (Reporter is not null)
            {
                Reporter.WriteLine($"  {GetType().Name} [FAIL]");
                Reporter.WriteLine($"    Error: {ex.GetType().Name}: {ex.Message}");
            }
            throw;
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
    /// The fixture must be registered via <see cref="AssemblySetupAttribute"/> and
    /// initialized by the coordinator before it can be retrieved.
    /// </remarks>
    public static T Get<T>() where T : AssemblyFixture
        => AssemblyFixtureCoordinator.GetFixture<T>();
}
