using PatternKit.Core;

namespace PatternKit.Extensions.DependencyInjection;

/// <summary>
/// Configuration options for PatternKit in dependency injection scenarios.
/// </summary>
public sealed class PatternKitOptions
{
    /// <summary>
    /// Gets or sets the default workflow options used when creating contexts.
    /// </summary>
    public WorkflowOptions DefaultWorkflowOptions { get; set; } = WorkflowOptions.Default;

    /// <summary>
    /// Gets or sets whether to automatically register discovered behaviors.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool AutoRegisterBehaviors { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable metrics collection.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool EnableMetrics { get; set; }

    /// <summary>
    /// Gets or sets the default timeout for workflows.
    /// Default is <see langword="null"/> (no timeout).
    /// </summary>
    public TimeSpan? DefaultTimeout { get; set; }

    /// <summary>
    /// Gets the list of behavior types to register, in order of execution.
    /// </summary>
    public List<Type> BehaviorTypes { get; } = new();

    /// <summary>
    /// Gets or sets whether to use scoped workflow contexts.
    /// When true, each scope gets its own context. When false, contexts are transient.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool UseScopedContexts { get; set; } = true;
}
