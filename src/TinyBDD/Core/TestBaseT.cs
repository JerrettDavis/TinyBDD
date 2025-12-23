namespace TinyBDD;

/// <summary>
/// A strongly-typed base class for TinyBDD tests with typed background state.
/// </summary>
/// <typeparam name="TBackground">The type of background state produced by background steps.</typeparam>
/// <remarks>
/// <para>
/// This base class extends <see cref="TestBase"/> to provide strongly-typed access
/// to background state. Override <see cref="ConfigureTypedBackground"/> instead of
/// <see cref="TestBase.ConfigureBackground"/> to define background steps that produce
/// a specific type.
/// </para>
/// <para>
/// Use <see cref="Background"/> to access the typed background state after
/// <see cref="TestBase.ExecuteBackgroundAsync"/> has been called.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record TestContext(DbConnection Db, User CurrentUser);
///
/// [Feature("User Management")]
/// public class UserTests : TestBase&lt;TestContext&gt;
/// {
///     protected override ScenarioChain&lt;TestContext&gt; ConfigureTypedBackground()
///     {
///         return Given("database connection", () => new DbConnection())
///             .And("admin user", db => new TestContext(db, db.GetAdmin()));
///     }
///
///     [Scenario("Admin can view users")]
///     [Test]
///     public async Task AdminCanViewUsers()
///     {
///         await GivenBackground()
///             .When("listing users", ctx => ctx.Db.GetUsers())
///             .Then("users returned", users => users.Any())
///             .AssertPassed();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="TestBase"/>
/// <seealso cref="TestBase.ExecuteBackgroundAsync"/>
public abstract class TestBase<TBackground> : TestBase where TBackground : class
{
    /// <summary>
    /// Gets the typed background state after <see cref="TestBase.ExecuteBackgroundAsync"/> completes.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if background steps have not been executed.
    /// </exception>
    protected TBackground Background
    {
        get
        {
            if (!BackgroundExecuted)
                throw new InvalidOperationException(
                    "Background steps have not been executed. Call ExecuteBackgroundAsync() first.");

            return (TBackground)BackgroundState!;
        }
    }

    /// <summary>
    /// Override to configure typed background steps that run before each scenario.
    /// </summary>
    /// <returns>A <see cref="ScenarioChain{TBackground}"/> representing the background steps.</returns>
    /// <remarks>
    /// <para>
    /// This method is called by the sealed <see cref="ConfigureBackground"/> implementation.
    /// The chain should produce the background state but should not call <c>AssertPassed()</c>.
    /// </para>
    /// </remarks>
    protected abstract ScenarioChain<TBackground> ConfigureTypedBackground();

    /// <summary>
    /// Sealed implementation that delegates to <see cref="ConfigureTypedBackground"/>.
    /// </summary>
    protected sealed override ScenarioChain<object>? ConfigureBackground()
    {
        var typedChain = ConfigureTypedBackground();
        // Convert to object chain by adding a passthrough When
        return typedChain.When("", state => (object)state);
    }

    /// <summary>
    /// Starts a Given step with the typed background state.
    /// </summary>
    /// <returns>A <see cref="ScenarioChain{TBackground}"/> starting with the background state.</returns>
    protected ScenarioChain<TBackground> GivenBackground()
    {
        if (!BackgroundExecuted)
            throw new InvalidOperationException(
                "Background steps have not been executed. Call ExecuteBackgroundAsync() first.");

        return Given("background", () => Background);
    }

    /// <summary>
    /// Starts a Given step with the typed background state and a custom title.
    /// </summary>
    /// <param name="title">A custom title for the Given step.</param>
    /// <returns>A <see cref="ScenarioChain{TBackground}"/> starting with the background state.</returns>
    protected ScenarioChain<TBackground> GivenBackground(string title)
    {
        if (!BackgroundExecuted)
            throw new InvalidOperationException(
                "Background steps have not been executed. Call ExecuteBackgroundAsync() first.");

        return Given(title, () => Background);
    }
}
