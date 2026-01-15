using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Parsers;

/// <summary>
/// Defines the contract for parsing file-based scenario definitions.
/// </summary>
public interface IDslParser
{
    /// <summary>
    /// Parses a file and returns feature definitions.
    /// </summary>
    /// <param name="filePath">Path to the file to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed feature definition.</returns>
    Task<FeatureDefinition> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}
