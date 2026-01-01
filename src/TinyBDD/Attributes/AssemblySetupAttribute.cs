namespace TinyBDD;

/// <summary>
/// Marks a type as an assembly-wide fixture that should be initialized before any tests run.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute at the assembly level to register one or more <see cref="AssemblyFixture"/>
/// types that will be instantiated and initialized before any tests execute, and torn down after
/// all tests complete.
/// </para>
/// <para>
/// Multiple fixtures can be registered by applying this attribute multiple times.
/// Fixtures are executed in the order they are registered.
/// </para>
/// <para>
/// This is a framework-agnostic mechanism that works with xUnit, NUnit, and MSTest.
/// </para>
/// </remarks>
/// <example>
/// <para>Example: Register a database fixture</para>
/// <code>
/// using TinyBDD;
///
/// [assembly: AssemblySetup(typeof(DatabaseFixture))]
///
/// public class DatabaseFixture : AssemblyFixture
/// {
///     private TestDatabase? _db;
///
///     public TestDatabase Database => _db!;
///
///     protected override async Task SetupAsync(CancellationToken ct)
///     {
///         _db = new TestDatabase();
///         await _db.StartAsync(ct);
///     }
///
///     protected override async Task TeardownAsync(CancellationToken ct)
///     {
///         if (_db is not null)
///             await _db.DisposeAsync();
///     }
/// }
/// </code>
/// </example>
/// <example>
/// <para>Example: Register multiple fixtures</para>
/// <code>
/// [assembly: AssemblySetup(typeof(DatabaseFixture))]
/// [assembly: AssemblySetup(typeof(WebServerFixture))]
/// [assembly: AssemblySetup(typeof(MessageQueueFixture))]
/// </code>
/// </example>
/// <seealso cref="AssemblyFixture"/>
/// <seealso cref="AssemblyFixtureCoordinator"/>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class AssemblySetupAttribute : Attribute
{
    /// <summary>
    /// Gets the type of the assembly fixture to initialize.
    /// </summary>
    public Type FixtureType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblySetupAttribute"/> class.
    /// </summary>
    /// <param name="fixtureType">
    /// The type of the assembly fixture. Must derive from <see cref="AssemblyFixture"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fixtureType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="fixtureType"/> does not derive from <see cref="AssemblyFixture"/>.
    /// </exception>
    public AssemblySetupAttribute(Type fixtureType)
    {
        if (fixtureType is null)
            throw new ArgumentNullException(nameof(fixtureType));

        if (!typeof(AssemblyFixture).IsAssignableFrom(fixtureType))
            throw new ArgumentException(
                $"Type {fixtureType.FullName} must derive from {nameof(AssemblyFixture)}",
                nameof(fixtureType));

        if (fixtureType.IsAbstract)
            throw new ArgumentException(
                $"Type {fixtureType.FullName} cannot be abstract",
                nameof(fixtureType));

        FixtureType = fixtureType;
    }
}
