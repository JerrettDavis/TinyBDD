namespace TinyBDD.Extensions.Hosting;

/// <summary>
/// Configuration options for TinyBDD hosted services.
/// </summary>
public class TinyBddHostingOptions
{
    /// <summary>
    /// Gets or sets whether to stop the host when a workflow completes.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool StopHostOnCompletion { get; set; }

    /// <summary>
    /// Gets or sets whether to stop the host when a workflow fails.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool StopHostOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay before starting workflow execution.
    /// Defaults to <see cref="TimeSpan.Zero"/>.
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the graceful shutdown timeout for running workflows.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
