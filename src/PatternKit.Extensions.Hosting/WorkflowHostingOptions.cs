namespace PatternKit.Extensions.Hosting;

/// <summary>
/// Configuration options for workflow hosting.
/// </summary>
public sealed class WorkflowHostingOptions
{
    /// <summary>
    /// Gets or sets whether to start workflows automatically when the host starts.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool StartAutomatically { get; set; } = true;

    /// <summary>
    /// Gets or sets the graceful shutdown timeout.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to run workflows in parallel.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool EnableParallelExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// Default is <see langword="null"/> (use Environment.ProcessorCount).
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// Gets or sets whether to continue on workflow failure.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool ContinueOnFailure { get; set; } = true;
}
