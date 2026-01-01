namespace TinyBDD.Extensions.Hosting;

/// <summary>
/// Defines a TinyBDD workflow that can be executed as a hosted service.
/// </summary>
/// <remarks>
/// Implement this interface to define reusable BDD workflows that can be
/// registered with the host and executed as background services.
/// </remarks>
/// <example>
/// <code>
/// public class OrderProcessingWorkflow : IWorkflowDefinition
/// {
///     public string FeatureName => "Order Processing";
///     public string ScenarioName => "Process pending orders";
///
///     public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
///     {
///         await Bdd.Given(context, "pending orders", () => GetPendingOrders())
///             .When("processed", orders => ProcessOrders(orders))
///             .Then("all completed", results => results.All(r => r.Success));
///     }
/// }
/// </code>
/// </example>
public interface IWorkflowDefinition
{
    /// <summary>
    /// Gets the feature name for this workflow.
    /// </summary>
    string FeatureName { get; }

    /// <summary>
    /// Gets the scenario name for this workflow.
    /// </summary>
    string ScenarioName { get; }

    /// <summary>
    /// Gets an optional description for the feature.
    /// </summary>
    string? FeatureDescription => null;

    /// <summary>
    /// Executes the workflow using the provided scenario context.
    /// </summary>
    /// <param name="context">The scenario context for this execution.</param>
    /// <param name="cancellationToken">Token to signal cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken);
}
