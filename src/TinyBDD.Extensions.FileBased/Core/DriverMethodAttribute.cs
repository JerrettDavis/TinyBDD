namespace TinyBDD.Extensions.FileBased.Core;

/// <summary>
/// Marks a method in an <see cref="IApplicationDriver"/> implementation as executable from file-based scenarios.
/// </summary>
/// <remarks>
/// The step pattern is used to match against steps defined in scenario files. Patterns are
/// case-insensitive and support parameter placeholders.
/// </remarks>
/// <example>
/// <code>
/// [DriverMethod("register user with email {email}")]
/// public async Task RegisterUser(string email)
/// {
///     // Implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class DriverMethodAttribute : Attribute
{
    /// <summary>
    /// The step pattern to match. Use {paramName} for parameters.
    /// </summary>
    public string StepPattern { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DriverMethodAttribute"/>.
    /// </summary>
    /// <param name="stepPattern">The step pattern to match.</param>
    public DriverMethodAttribute(string stepPattern)
    {
        StepPattern = stepPattern ?? throw new ArgumentNullException(nameof(stepPattern));
    }
}
