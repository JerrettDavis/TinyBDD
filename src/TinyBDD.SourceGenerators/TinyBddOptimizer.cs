using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace TinyBDD.SourceGenerators;

/// <summary>
/// Incremental source generator that optimizes TinyBDD scenarios marked with [GenerateOptimized].
/// Transforms fluent BDD chains into direct procedural code to eliminate boxing overhead.
/// </summary>
[Generator]
public class TinyBddOptimizer : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all methods that could be optimized (broad predicate)
        var candidateMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsOptimizableMethod(node),
                transform: static (ctx, _) => GetMethodToOptimize(ctx))
            .Where(static m => m is not null);

        // Group methods by containing type and determine eligibility
        var groupedByType = candidateMethods
            .Collect()
            .Select(static (methods, _) => GroupMethodsByContainingType(methods!));

        // Register source output for eligible methods
        context.RegisterSourceOutput(groupedByType, static (spc, groups) => 
        {
            foreach (var group in groups)
            {
                ProcessTypeGroup(spc, group);
            }
        });
    }

    private static bool IsOptimizableMethod(SyntaxNode node)
    {
        // Any public async method that returns Task/ValueTask is potentially optimizable
        if (node is not MethodDeclarationSyntax method)
            return false;

        // Must be public
        if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            return false;

        // Must be async or return Task-like type
        var isAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
        var returnsTask = method.ReturnType?.ToString().Contains("Task") == true;
        
        return isAsync || returnsTask;
    }

    private static MethodInfo? GetMethodToOptimize(GeneratorSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);

        if (methodSymbol is null)
            return null;

        // Skip if method has [DisableOptimization] attribute
        var hasDisableAttr = methodSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "DisableOptimizationAttribute" &&
            attr.AttributeClass.ContainingNamespace.ToDisplayString() == "TinyBDD");

        if (hasDisableAttr)
            return null;

        // Skip if method has [GenerateOptimized(Enabled = false)]
        var generateOptimizedAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(attr =>
                attr.AttributeClass?.Name == "GenerateOptimizedAttribute" &&
                attr.AttributeClass.ContainingNamespace.ToDisplayString() == "TinyBDD");

        if (generateOptimizedAttr is not null)
        {
            var enabledArg = generateOptimizedAttr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Enabled");

            if (enabledArg.Value.Value is bool enabled && !enabled)
                return null;
        }

        // Check if method contains BDD chain - if not, skip it
        if (!ContainsBddChain(methodSyntax))
            return null;

        // Check type eligibility
        var containingType = methodSymbol.ContainingType;
        var (isEligible, ineligibilityReason) = CheckTypeEligibility(containingType);

        return new MethodInfo(
            methodSyntax,
            methodSymbol,
            context.SemanticModel,
            isEligible,
            ineligibilityReason);
    }

    private static bool ContainsBddChain(MethodDeclarationSyntax method)
    {
        // Look for Given/When/Then/And/But method calls
        var body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? "";
        
        // Check for BDD method calls - be very broad to catch all patterns
        return body.Contains("Given") && (body.Contains("When") || body.Contains("Then"));
    }

    /// <summary>
    /// Checks if a type is eligible for optimization generation.
    /// Returns (isEligible, diagnosticDescriptor if not eligible).
    /// </summary>
    private static (bool IsEligible, DiagnosticDescriptor? Reason) CheckTypeEligibility(INamedTypeSymbol type)
    {
        // Check if nested
        if (type.ContainingType != null)
        {
            return (false, Diagnostics.NestedTypeNotSupported);
        }

        // Check if generic
        if (type.TypeParameters.Length > 0)
        {
            return (false, Diagnostics.GenericTypeNotSupported);
        }

        // Check if partial
        if (!IsPartial(type))
        {
            return (false, Diagnostics.TypeNotPartial);
        }

        return (true, null);
    }

    /// <summary>
    /// Checks if a type is declared as partial in all of its declarations.
    /// Returns false if the type has no declarations or if any declaration is not partial.
    /// </summary>
    private static bool IsPartial(INamedTypeSymbol type)
    {
        // Type must have at least one declaration
        if (type.DeclaringSyntaxReferences.Length == 0)
        {
            return false;
        }

        // All declarations must be partial
        foreach (var declRef in type.DeclaringSyntaxReferences)
        {
            if (declRef.GetSyntax() is TypeDeclarationSyntax typeDecl)
            {
                if (!typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Groups methods by their containing type for spam control.
    /// </summary>
    private static ImmutableArray<TypeGroup> GroupMethodsByContainingType(ImmutableArray<MethodInfo> methods)
    {
        var groups = methods
            .GroupBy(m => m.MethodSymbol.ContainingType, SymbolEqualityComparer.Default)
            .Select(g => new TypeGroup(
                (INamedTypeSymbol)g.Key!,  // Key is guaranteed to be non-null after GroupBy
                g.ToImmutableArray()))
            .ToImmutableArray();

        return groups;
    }

    /// <summary>
    /// Processes a group of methods in the same containing type.
    /// Emits one diagnostic per type (spam control) and generates code for eligible methods.
    /// </summary>
    private static void ProcessTypeGroup(SourceProductionContext context, TypeGroup group)
    {
        var eligibleMethods = group.Methods.Where(m => m.IsEligible).ToList();
        var ineligibleMethods = group.Methods.Where(m => !m.IsEligible).ToList();

        // If there are ineligible methods, emit one diagnostic per type (spam control)
        if (ineligibleMethods.Any())
        {
            // Group ineligible methods by reason and emit one diagnostic per reason per type
            var byReason = ineligibleMethods
                .GroupBy(m => m.IneligibilityReason, (key, methods) => (Reason: key, Methods: methods.ToList()))
                .ToList();

            foreach (var (reason, methods) in byReason)
            {
                if (reason != null)
                {
                    // Emit one diagnostic for the first method in the group
                    var firstMethod = methods.First();
                    var containingTypeName = firstMethod.MethodSymbol.ContainingType.ToDisplayString();
                    var firstMethodName = firstMethod.MethodName;

                    // Try to get the type declaration location (preferred), otherwise use method location
                    Location location;
                    var typeDecl = firstMethod.MethodSymbol.ContainingType.DeclaringSyntaxReferences.FirstOrDefault();
                    if (typeDecl != null && typeDecl.GetSyntax() is TypeDeclarationSyntax typeDeclaration)
                    {
                        location = typeDeclaration.Identifier.GetLocation();
                    }
                    else
                    {
                        location = firstMethod.MethodSyntax.Identifier.GetLocation();
                    }

                    var diagnostic = Diagnostic.Create(
                        reason,
                        location,
                        containingTypeName,
                        firstMethodName);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Generate optimized code for all eligible methods
        foreach (var method in eligibleMethods)
        {
            Execute(context, method);
        }
    }

    private static void Execute(SourceProductionContext context, MethodInfo method)
    {
        try
        {
            var generator = new OptimizedCodeGenerator(method);
            var generatedCode = generator.Generate();

            if (generatedCode != null)
            {
                var fileName = $"{method.ContainingType.Replace(".", "_")}_{method.MethodName}_Optimized.g.cs";
                context.AddSource(fileName, SourceText.From(generatedCode, Encoding.UTF8));
            }
        }
        catch (System.Exception ex)
        {
            // Report diagnostic if generation fails
            var diagnostic = Diagnostic.Create(
                Diagnostics.OptimizationFailed,
                method.MethodSyntax.GetLocation(),
                method.MethodName,
                ex.Message);

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Represents a group of methods in the same containing type.
    /// </summary>
    private sealed class TypeGroup
    {
        public INamedTypeSymbol ContainingType { get; }
        public ImmutableArray<MethodInfo> Methods { get; }

        public TypeGroup(INamedTypeSymbol containingType, ImmutableArray<MethodInfo> methods)
        {
            ContainingType = containingType;
            Methods = methods;
        }
    }
}
