namespace TinyBDD.Extensions.FileBased;

/// <summary>
/// File-based DSL extension for TinyBDD, enabling YAML/JSON scenario definitions 
/// with convention-based, source-generated application drivers.
/// </summary>
/// <remarks>
/// <para>
/// This extension enables non-developers to author executable tests using file-based 
/// scenario definitions (YAML, JSON, etc.) that can be transpiled into executable 
/// TinyBDD scenarios via convention-based application drivers.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item>YAML-based scenario definitions</item>
/// <item>Convention-based step resolution via <see cref="Core.DriverMethodAttribute"/></item>
/// <item>Type-safe driver methods with compile-time validation</item>
/// <item>Seamless integration with TinyBDD's fluent API</item>
/// <item>Support for multiple test frameworks (xUnit, NUnit, MSTest)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <para>Example YAML scenario:</para>
/// <code>
/// feature: User Registration
/// scenarios:
///   - name: Register valid user
///     steps:
///       - keyword: Given
///         text: the application is running
///       - keyword: When
///         text: I register a user with email {email}
///         parameters:
///           email: test@example.com
///       - keyword: Then
///         text: the user should exist
/// </code>
/// <para>Example driver implementation:</para>
/// <code>
/// public class UserRegistrationDriver : IApplicationDriver
/// {
///     [DriverMethod("the application is running")]
///     public Task ApplicationIsRunning() { /* ... */ }
///     
///     [DriverMethod("I register a user with email {email}")]
///     public Task RegisterUser(string email) { /* ... */ }
///     
///     [DriverMethod("the user should exist")]
///     public Task<bool> UserExists() { /* ... */ }
///     
///     public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
///     public Task CleanupAsync(CancellationToken ct = default) => Task.CompletedTask;
/// }
/// </code>
/// <para>Example test class:</para>
/// <code>
/// public class UserRegistrationTests : FileBasedTestBase{TDriver} where TDriver : UserRegistrationDriver
/// {
///     [Fact]
///     public async Task ExecuteUserRegistrationScenarios()
///     {
///         await ExecuteScenariosAsync(options => 
///             options.AddYamlFiles("Features/UserRegistration.yml"));
///     }
/// }
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
