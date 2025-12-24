namespace TinyBDD.Extensions;

/// <summary>
/// Extension methods for composing reusable step sequences.
/// </summary>
public static class StepExtensions
{
    /// <summary>
    /// Applies a reusable step sequence to the scenario chain.
    /// </summary>
    /// <typeparam name="T">The input type of the chain.</typeparam>
    /// <typeparam name="TOut">The output type after applying the steps.</typeparam>
    /// <param name="chain">The current scenario chain.</param>
    /// <param name="steps">A function that applies additional steps to the chain.</param>
    /// <returns>The chain after applying the steps.</returns>
    /// <example>
    /// <code>
    /// public static ScenarioChain&lt;User&gt; WithLoggedInUser(this ScenarioChain&lt;object&gt; chain)
    ///     => chain.When("user logs in", _ => new User { IsLoggedIn = true });
    ///
    /// await Given("system ready", () => new object())
    ///     .Apply(c => c.WithLoggedInUser())
    ///     .Then("user is logged in", u => u.IsLoggedIn)
    ///     .AssertPassed();
    /// </code>
    /// </example>
    public static ScenarioChain<TOut> Apply<T, TOut>(
        this ScenarioChain<T> chain,
        Func<ScenarioChain<T>, ScenarioChain<TOut>> steps)
        => steps(chain);

    /// <summary>
    /// Applies a reusable assertion sequence to the then chain.
    /// </summary>
    /// <typeparam name="T">The type carried by the chain.</typeparam>
    /// <param name="chain">The current then chain.</param>
    /// <param name="assertions">A function that applies additional assertions.</param>
    /// <returns>The chain after applying the assertions.</returns>
    /// <example>
    /// <code>
    /// public static ThenChain&lt;User&gt; AssertValidUser(this ThenChain&lt;User&gt; chain)
    ///     => chain
    ///         .And("has id", u => u.Id > 0)
    ///         .And("has name", u => !string.IsNullOrEmpty(u.Name));
    ///
    /// await Given("a user", () => new User { Id = 1, Name = "John" })
    ///     .Then("exists", u => u != null)
    ///     .Apply(c => c.AssertValidUser())
    ///     .AssertPassed();
    /// </code>
    /// </example>
    public static ThenChain<T> Apply<T>(
        this ThenChain<T> chain,
        Func<ThenChain<T>, ThenChain<T>> assertions)
        => assertions(chain);

    /// <summary>
    /// Applies a side-effect step that preserves the chain type.
    /// </summary>
    /// <typeparam name="T">The type carried by the chain.</typeparam>
    /// <param name="chain">The current scenario chain.</param>
    /// <param name="steps">A function that applies side-effect steps.</param>
    /// <returns>The same chain after applying the steps.</returns>
    public static ScenarioChain<T> ApplyEffect<T>(
        this ScenarioChain<T> chain,
        Func<ScenarioChain<T>, ScenarioChain<T>> steps)
        => steps(chain);

    /// <summary>
    /// Chains multiple scenario steps together, allowing composition of reusable step definitions.
    /// </summary>
    /// <typeparam name="T">The input type.</typeparam>
    /// <typeparam name="TIntermediate">The intermediate type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="chain">The current chain.</param>
    /// <param name="first">First step sequence to apply.</param>
    /// <param name="second">Second step sequence to apply.</param>
    /// <returns>The chain after applying both step sequences.</returns>
    public static ScenarioChain<TOut> ApplyThen<T, TIntermediate, TOut>(
        this ScenarioChain<T> chain,
        Func<ScenarioChain<T>, ScenarioChain<TIntermediate>> first,
        Func<ScenarioChain<TIntermediate>, ScenarioChain<TOut>> second)
        => second(first(chain));
}
