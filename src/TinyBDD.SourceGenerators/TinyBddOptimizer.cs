using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
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
        // Find all methods marked with [GenerateOptimized]
        var methodsToOptimize = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsOptimizableMethod(node),
                transform: static (ctx, _) => GetMethodToOptimize(ctx))
            .Where(static m => m is not null);

        // Generate optimized code
        context.RegisterSourceOutput(methodsToOptimize, static (spc, method) => Execute(spc, method!));
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

        return new MethodInfo(
            methodSyntax,
            methodSymbol,
            context.SemanticModel);
    }

    private static bool ContainsBddChain(MethodDeclarationSyntax method)
    {
        // Look for Given/When/Then/And/But method calls
        var body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? "";
        
        // Check for BDD method calls - be very broad to catch all patterns
        return body.Contains("Given") && (body.Contains("When") || body.Contains("Then"));
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
                new DiagnosticDescriptor(
                    "TBDD001",
                    "TinyBDD optimization failed",
                    "Failed to optimize method '{0}': {1}",
                    "TinyBDD.SourceGenerators",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                method.MethodSyntax.GetLocation(),
                method.MethodName,
                ex.Message);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
