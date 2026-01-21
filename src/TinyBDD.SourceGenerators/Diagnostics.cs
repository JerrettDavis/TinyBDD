using Microsoft.CodeAnalysis;

namespace TinyBDD.SourceGenerators;

/// <summary>
/// Centralized diagnostic descriptors for TinyBDD source generator warnings and errors.
/// </summary>
internal static class Diagnostics
{
    private const string Category = "TinyBDD.SourceGenerators";

    /// <summary>
    /// TBDD001: TinyBDD optimization failed (existing diagnostic).
    /// Emitted when an exception occurs during code generation.
    /// </summary>
    public static readonly DiagnosticDescriptor OptimizationFailed = new DiagnosticDescriptor(
        id: "TBDD001",
        title: "TinyBDD optimization failed",
        messageFormat: "Failed to optimize method '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An exception occurred while generating optimized code for a TinyBDD scenario method.");

    /// <summary>
    /// TBDD010: Optimization skipped: containing type is not partial.
    /// Emitted when a method would be optimized, but the containing type is not declared partial.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeNotPartial = new DiagnosticDescriptor(
        id: "TBDD010",
        title: "Optimization skipped: containing type is not partial",
        messageFormat: "TinyBDD optimization was skipped for '{0}.{1}' because containing type '{0}' is not declared partial. Mark '{0}' as partial to enable source-generated optimizations (or opt out via [DisableOptimization]).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The containing type must be declared as 'partial' to enable source-generated optimization. Add the 'partial' keyword to the type declaration, or use [DisableOptimization] to suppress this warning.",
        helpLinkUri: "https://github.com/JerrettDavis/TinyBDD#source-generation");

    /// <summary>
    /// TBDD011: Optimization skipped: nested types not supported.
    /// Emitted when a method is in a nested type, which the generator does not currently support.
    /// </summary>
    public static readonly DiagnosticDescriptor NestedTypeNotSupported = new DiagnosticDescriptor(
        id: "TBDD011",
        title: "Optimization skipped: nested types not supported",
        messageFormat: "TinyBDD optimization was skipped for '{0}.{1}' because nested types are not currently supported by the optimizer. Move the scenario to a top-level partial type or disable optimization for this method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nested types are not currently supported by the TinyBDD optimizer. Move the test class to the top level or use [DisableOptimization] to suppress this warning.",
        helpLinkUri: "https://github.com/JerrettDavis/TinyBDD#source-generation");

    /// <summary>
    /// TBDD012: Optimization skipped: generic types not supported.
    /// Emitted when a method is in a generic type, which the generator does not currently support.
    /// </summary>
    public static readonly DiagnosticDescriptor GenericTypeNotSupported = new DiagnosticDescriptor(
        id: "TBDD012",
        title: "Optimization skipped: generic types not supported",
        messageFormat: "TinyBDD optimization was skipped for '{0}.{1}' because generic containing types are not currently supported by the optimizer. Use a non-generic partial type or disable optimization for this method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Generic types are not currently supported by the TinyBDD optimizer. Move the test to a non-generic type or use [DisableOptimization] to suppress this warning.",
        helpLinkUri: "https://github.com/JerrettDavis/TinyBDD#source-generation");
}
