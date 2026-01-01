namespace TinyBDD.Extensions.DependencyInjection;

/// <summary>
/// Configuration options for TinyBDD when used with dependency injection.
/// </summary>
public class TinyBddOptions
{
    /// <summary>
    /// Gets or sets the default <see cref="ScenarioOptions"/> applied to all scenarios
    /// created through the DI container.
    /// </summary>
    public ScenarioOptions DefaultScenarioOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to automatically register the <see cref="IScenarioContextFactory"/>
    /// as a scoped service. Defaults to <c>true</c>.
    /// </summary>
    public bool RegisterContextFactory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable step timing reporting through the DI pipeline.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableStepTiming { get; set; }
}
