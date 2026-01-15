using TinyBDD.Extensions.FileBased.Configuration;
using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Execution;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased;

/// <summary>
/// Base class for file-based BDD tests.
/// </summary>
/// <typeparam name="TDriver">The application driver type.</typeparam>
/// <remarks>
/// <para>
/// Each scenario execution creates a new driver instance, ensuring scenario isolation.
/// Driver state is not shared between scenarios within the same test run.
/// </para>
/// <para>
/// Driver method discovery is cached statically per driver type for performance.
/// The first scenario execution will discover and cache driver methods, and subsequent
/// executions (even across different test runs in the same process) will reuse the cache.
/// </para>
/// </remarks>
public abstract class FileBasedTestBase<TDriver> where TDriver : IApplicationDriver, new()
{
    /// <summary>
    /// Executes all scenarios from file-based definitions.
    /// </summary>
    /// <param name="configureOptions">Configuration action.</param>
    /// <param name="traitBridge">Optional trait bridge for test framework integration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ExecuteScenariosAsync(
        Action<FileBasedDslOptionsBuilder> configureOptions,
        ITraitBridge? traitBridge = null,
        CancellationToken cancellationToken = default)
    {
        var builder = new FileBasedDslOptionsBuilder();
        builder.UseApplicationDriver<TDriver>();
        configureOptions(builder);
        
        var options = builder.Build();
        
        // Discover files
        var files = FileDiscovery.DiscoverFiles(options.FilePatterns, options.BaseDirectory);
        
        if (files.Count == 0)
        {
            throw new InvalidOperationException(
                $"No scenario files found matching patterns: {string.Join(", ", options.FilePatterns)}");
        }

        // Parse and execute each file
        foreach (var file in files)
        {
            var feature = await options.Parser.ParseAsync(file, cancellationToken).ConfigureAwait(false);
            
            foreach (var scenario in feature.Scenarios)
            {
                await ExecuteScenarioAsync(feature, scenario, traitBridge, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Executes a single scenario.
    /// </summary>
    /// <param name="feature">The feature containing the scenario.</param>
    /// <param name="scenario">The scenario to execute.</param>
    /// <param name="traitBridge">Optional trait bridge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ExecuteScenarioAsync(
        FeatureDefinition feature,
        ScenarioDefinition scenario,
        ITraitBridge? traitBridge = null,
        CancellationToken cancellationToken = default)
    {
        // Create a scenario context
        var context = new ScenarioContext(
            feature.Name,
            feature.Description,
            scenario.Name,
            traitBridge ?? new NullTraitBridge(),
            new ScenarioOptions());

        // Create driver instance
        var driver = CreateDriver();
        var executor = new ScenarioExecutor(driver, typeof(TDriver));

        // Execute the scenario
        await executor.ExecuteAsync(feature, scenario, context, cancellationToken).ConfigureAwait(false);

        // Assert that scenario passed
        context.AssertPassed();
    }

    /// <summary>
    /// Creates a new instance of the driver. Override to provide custom instantiation logic.
    /// </summary>
    protected virtual TDriver CreateDriver()
    {
        return new TDriver();
    }
}
