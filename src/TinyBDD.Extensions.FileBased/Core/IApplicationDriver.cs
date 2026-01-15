namespace TinyBDD.Extensions.FileBased.Core;

/// <summary>
/// Defines the contract for application drivers that execute file-based scenario steps.
/// </summary>
/// <remarks>
/// Application drivers serve as the execution surface for file-based tests, providing
/// methods that map to steps in scenario files. Implementations can be partial classes
/// with source-generated method implementations, or fully hand-written.
/// </remarks>
/// <example>
/// <code>
/// public partial class MyApplicationDriver : IApplicationDriver
/// {
///     [DriverMethod("register user")]
///     public async Task RegisterUser(string email)
///     {
///         // Implementation
///     }
/// }
/// </code>
/// </example>
public interface IApplicationDriver
{
    /// <summary>
    /// Called before scenario execution begins.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after scenario execution completes (success or failure).
    /// </summary>
    Task CleanupAsync(CancellationToken cancellationToken = default);
}
