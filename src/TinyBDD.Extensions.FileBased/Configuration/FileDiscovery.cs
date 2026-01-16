using Microsoft.Extensions.FileSystemGlobbing;

namespace TinyBDD.Extensions.FileBased.Configuration;

/// <summary>
/// Discovers scenario files based on glob patterns.
/// </summary>
internal static class FileDiscovery
{
    /// <summary>
    /// Finds all files matching the specified patterns.
    /// </summary>
    /// <param name="patterns">File patterns to match.</param>
    /// <param name="baseDirectory">Base directory for relative paths.</param>
    /// <returns>List of matched file paths.</returns>
    public static List<string> DiscoverFiles(IEnumerable<string> patterns, string baseDirectory)
    {
        var matcher = new Matcher();
        
        foreach (var pattern in patterns)
        {
            matcher.AddInclude(pattern);
        }

        var result = matcher.Execute(
            new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(
                new DirectoryInfo(baseDirectory)));

        return result.Files
            .Select(f => Path.GetFullPath(Path.Combine(baseDirectory, f.Path)))
            .ToList();
    }
}
