using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Parsers;

namespace TinyBDD.Extensions.FileBased.Configuration;

/// <summary>
/// Fluent builder for configuring file-based DSL scenarios.
/// </summary>
public sealed class FileBasedDslOptionsBuilder
{
    private readonly FileBasedDslOptions _options = new();

    /// <summary>
    /// Adds Gherkin .feature files matching the specified pattern.
    /// This is the recommended first-class approach for file-based scenarios.
    /// </summary>
    /// <param name="pattern">File pattern (e.g., "Features/**/*.feature").</param>
    /// <returns>This builder for chaining.</returns>
    public FileBasedDslOptionsBuilder AddFeatureFiles(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        _options.FilePatterns.Add(pattern);
        _options.Parser = new GherkinDslParser();
        return this;
    }

    /// <summary>
    /// Adds YAML files matching the specified pattern.
    /// </summary>
    /// <param name="pattern">File pattern (e.g., "Features/**/*.yml").</param>
    /// <returns>This builder for chaining.</returns>
    public FileBasedDslOptionsBuilder AddYamlFiles(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        _options.FilePatterns.Add(pattern);
        _options.Parser = new YamlDslParser();
        return this;
    }

    /// <summary>
    /// Sets the base directory for resolving relative file paths.
    /// </summary>
    /// <param name="baseDirectory">The base directory path.</param>
    /// <returns>This builder for chaining.</returns>
    public FileBasedDslOptionsBuilder WithBaseDirectory(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be null or empty", nameof(baseDirectory));

        _options.BaseDirectory = baseDirectory;
        return this;
    }

    /// <summary>
    /// Sets the application driver type to use for executing scenarios.
    /// </summary>
    /// <typeparam name="TDriver">The driver type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public FileBasedDslOptionsBuilder UseApplicationDriver<TDriver>() where TDriver : IApplicationDriver
    {
        _options.DriverType = typeof(TDriver);
        return this;
    }

    /// <summary>
    /// Sets a custom DSL parser.
    /// </summary>
    /// <param name="parser">The parser to use.</param>
    /// <returns>This builder for chaining.</returns>
    public FileBasedDslOptionsBuilder WithParser(IDslParser parser)
    {
        _options.Parser = parser ?? throw new ArgumentNullException(nameof(parser));
        return this;
    }

    /// <summary>
    /// Builds the configuration options.
    /// </summary>
    /// <returns>The configured options.</returns>
    public FileBasedDslOptions Build()
    {
        if (_options.DriverType == null)
            throw new InvalidOperationException("Driver type must be specified using UseApplicationDriver<T>()");

        if (_options.FilePatterns.Count == 0)
            throw new InvalidOperationException("At least one file pattern must be specified");

        return _options;
    }
}
