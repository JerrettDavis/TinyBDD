using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Configuration;

/// <summary>
/// Configuration options for file-based DSL scenarios.
/// </summary>
public sealed class FileBasedDslOptions
{
    /// <summary>
    /// File patterns to discover scenario files.
    /// </summary>
    public List<string> FilePatterns { get; } = new();

    /// <summary>
    /// DSL parser to use for parsing scenario files.
    /// </summary>
    public IDslParser Parser { get; set; } = new YamlDslParser();

    /// <summary>
    /// Base directory for resolving relative file paths.
    /// </summary>
    public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Driver type to use for executing scenarios.
    /// </summary>
    public Type? DriverType { get; set; }
}
