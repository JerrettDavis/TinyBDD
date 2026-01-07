using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyBDD.SourceGenerators;

internal class MethodInfo
{
    public MethodDeclarationSyntax MethodSyntax { get; }
    public IMethodSymbol MethodSymbol { get; }
    public SemanticModel SemanticModel { get; }
    public string MethodName => MethodSymbol.Name;
    public string ContainingType => MethodSymbol.ContainingType.ToDisplayString();

    public MethodInfo(
        MethodDeclarationSyntax methodSyntax,
        IMethodSymbol methodSymbol,
        SemanticModel semanticModel)
    {
        MethodSyntax = methodSyntax;
        MethodSymbol = methodSymbol;
        SemanticModel = semanticModel;
    }
}

internal class StepInfo
{
    public string Kind { get; set; } = ""; // Given, When, Then, And, But
    public string Title { get; set; } = "";
    public string LambdaBody { get; set; } = ""; // The lambda expression as string
    public ITypeSymbol? InputType { get; set; } // Type of lambda parameter
    public ITypeSymbol? OutputType { get; set; } // Return type
    public bool IsAsync { get; set; }
    public bool IsTransform { get; set; } // true if returns value, false if just effect
    public string ParameterName { get; set; } = ""; // Lambda parameter name (e.g., "x", "str")
}

/// <summary>
/// Generates optimized procedural code from BDD chains.
/// </summary>
internal class OptimizedCodeGenerator
{
    private readonly MethodDeclarationSyntax _methodSyntax;
    private readonly IMethodSymbol _methodSymbol;
    private readonly SemanticModel _semanticModel;

    public OptimizedCodeGenerator(MethodInfo method)
    {
        _methodSyntax = method.MethodSyntax;
        _methodSymbol = method.MethodSymbol;
        _semanticModel = method.SemanticModel;
    }

    public string? Generate()
    {
        // Find the BDD chain in the method body
        var bddChain = FindBddChain();
        if (bddChain == null)
            return null;

        // Parse the chain into steps
        var steps = ParseSteps(bddChain);
        if (steps.Count == 0)
            return null;

        // Generate the optimized code
        return GenerateOptimizedMethod(steps);
    }

    private InvocationExpressionSyntax? FindBddChain()
    {
        // Look for await expressions first (since BDD chains are typically awaited)
        var awaitExprs = _methodSyntax.DescendantNodes()
            .OfType<AwaitExpressionSyntax>()
            .ToList();

        foreach (var awaitExpr in awaitExprs)
        {
            // Get the expression being awaited - this should be the TOP of the chain (Then)
            var expr = awaitExpr.Expression;
            
            // Check if this is a BDD chain by looking for Given anywhere in the chain
            if (ContainsBddStartingMethod(expr))
            {
                // Return the top-level invocation (Then, or last method in chain)
                if (expr is InvocationExpressionSyntax topInvocation)
                {
                    return topInvocation;
                }
            }
        }

        return null;
    }

    private bool ContainsBddStartingMethod(ExpressionSyntax expr)
    {
        // Recursively check if this expression or any of its sub-expressions contain a Given call
        if (expr is InvocationExpressionSyntax invocation)
        {
            if (IsBddStartingMethod(invocation))
                return true;
                
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return ContainsBddStartingMethod(memberAccess.Expression);
            }
        }
        return false;
    }

    private bool IsBddStartingMethod(InvocationExpressionSyntax invocation)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(invocation);
        var method = symbolInfo.Symbol as IMethodSymbol;

        if (method == null)
            return false;

        // Check if it's a Given method from TinyBDD namespace
        return method.Name == "Given" &&
               method.ContainingNamespace?.ToDisplayString().StartsWith("TinyBDD") == true;
    }

    private List<StepInfo> ParseSteps(InvocationExpressionSyntax startInvocation)
    {
        var steps = new List<StepInfo>();
        
        // Collect all invocations in the chain
        var invocations = new List<InvocationExpressionSyntax>();
        CollectChainInvocations(startInvocation, invocations);
        
        // Process each invocation
        ITypeSymbol? previousOutputType = null;
        
        foreach (var invocation in invocations)
        {
            var stepInfo = ParseSingleStep(invocation, previousOutputType);
            if (stepInfo != null)
            {
                steps.Add(stepInfo);
                previousOutputType = stepInfo.OutputType;
            }
        }

        return steps;
    }

    private void CollectChainInvocations(ExpressionSyntax expr, List<InvocationExpressionSyntax> invocations)
    {
        if (expr is InvocationExpressionSyntax invocation)
        {
            // First recurse to get parent invocations
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                CollectChainInvocations(memberAccess.Expression, invocations);
            }
            
            // Then add this invocation
            invocations.Add(invocation);
        }
    }

    private StepInfo? ParseSingleStep(InvocationExpressionSyntax invocation, ITypeSymbol? inputType)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(invocation);
        var method = symbolInfo.Symbol as IMethodSymbol;

        if (method == null)
            return null;

        var stepKind = method.Name; // Given, When, Then, And, But
        var arguments = invocation.ArgumentList.Arguments;

        // Skip if not a BDD step method
        if (!IsBddStepMethod(stepKind))
            return null;

        string title = "";
        ExpressionSyntax? lambdaExpr = null;

        // Parse arguments based on overload patterns
        // Common patterns:
        // - Given(title, () => value)
        // - Given(title, x => transform(x))
        // - When(x => transform(x))
        // - Then(x => assertion)
        
        if (arguments.Count >= 2)
        {
            // First arg is title (if it's a string literal or constant)
            var firstArg = arguments[0].Expression;
            if (firstArg is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                title = literal.Token.ValueText;
                lambdaExpr = arguments[1].Expression;
            }
            else
            {
                // First arg might be context parameter (Bdd.Given(ctx, ...))
                if (arguments.Count >= 3)
                {
                    var secondArg = arguments[1].Expression;
                    if (secondArg is LiteralExpressionSyntax titleLiteral && titleLiteral.Token.IsKind(SyntaxKind.StringLiteralToken))
                    {
                        title = titleLiteral.Token.ValueText;
                        lambdaExpr = arguments[2].Expression;
                    }
                }
            }
        }
        else if (arguments.Count == 1)
        {
            // Single argument - could be title-only or lambda-only
            var arg = arguments[0].Expression;
            if (arg is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                title = literal.Token.ValueText;
            }
            else
            {
                lambdaExpr = arg;
            }
        }

        if (lambdaExpr == null)
            return null;

        // Extract lambda information
        var lambdaInfo = ExtractLambdaInfo(lambdaExpr, inputType, method);
        
        return new StepInfo
        {
            Kind = stepKind,
            Title = title,
            LambdaBody = lambdaExpr.ToString(),
            InputType = lambdaInfo.InputType,
            OutputType = lambdaInfo.OutputType,
            IsAsync = lambdaInfo.IsAsync,
            IsTransform = lambdaInfo.IsTransform,
            ParameterName = lambdaInfo.ParameterName
        };
    }

    private bool IsBddStepMethod(string methodName)
    {
        return methodName == "Given" || methodName == "When" || methodName == "Then" ||
               methodName == "And" || methodName == "But";
    }

    private (ITypeSymbol? InputType, ITypeSymbol? OutputType, bool IsAsync, bool IsTransform, string ParameterName) 
        ExtractLambdaInfo(ExpressionSyntax lambdaExpr, ITypeSymbol? inputType, IMethodSymbol method)
    {
        string paramName = "";
        bool isAsync = false;

        // Extract parameter name from lambda
        if (lambdaExpr is SimpleLambdaExpressionSyntax simpleLambda)
        {
            paramName = simpleLambda.Parameter.Identifier.Text;
            isAsync = simpleLambda.AsyncKeyword.Kind() != SyntaxKind.None;
        }
        else if (lambdaExpr is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            paramName = parenLambda.ParameterList.Parameters.FirstOrDefault()?.Identifier.Text ?? "";
            isAsync = parenLambda.AsyncKeyword.Kind() != SyntaxKind.None;
        }

        // Get type information from semantic model
        var typeInfo = _semanticModel.GetTypeInfo(lambdaExpr);
        var convertedType = typeInfo.ConvertedType as INamedTypeSymbol;

        ITypeSymbol? outputType = null;
        ITypeSymbol? actualInputType = inputType;
        bool isTransform = true;

        if (convertedType?.DelegateInvokeMethod != null)
        {
            var invokeMethod = convertedType.DelegateInvokeMethod;
            
            // Get input type from first parameter if we don't have it
            if (actualInputType == null && invokeMethod.Parameters.Length > 0)
            {
                actualInputType = invokeMethod.Parameters[0].Type;
            }

            // Get output type
            var returnType = invokeMethod.ReturnType;
            
            // Unwrap Task<T> or ValueTask<T>
            if (returnType is INamedTypeSymbol namedReturn)
            {
                if (namedReturn.Name == "Task" || namedReturn.Name == "ValueTask")
                {
                    if (namedReturn.TypeArguments.Length > 0)
                    {
                        outputType = namedReturn.TypeArguments[0];
                    }
                    else
                    {
                        // Task or ValueTask without <T> means void/effect
                        isTransform = false;
                    }
                }
                else
                {
                    outputType = returnType;
                }
            }
            else
            {
                outputType = returnType;
            }

            // Check if it's a void return (effect, not transform)
            if (outputType?.SpecialType == SpecialType.System_Void)
            {
                isTransform = false;
                outputType = actualInputType; // Effect passes through the input type
            }
        }

        // For Given (no input), infer output from method's generic parameter
        if (actualInputType == null && method.ReturnType is INamedTypeSymbol returnTypeSymbol)
        {
            // ScenarioChain<T> -> extract T
            if (returnTypeSymbol.TypeArguments.Length > 0)
            {
                outputType = returnTypeSymbol.TypeArguments[0];
            }
        }

        return (actualInputType, outputType, isAsync, isTransform, paramName);
    }

    private string GenerateOptimizedMethod(List<StepInfo> steps)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        // Namespace
        var namespaceName = _methodSymbol.ContainingNamespace.ToDisplayString();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // Class
        var className = _methodSymbol.ContainingType.Name;
        sb.AppendLine($"    partial class {className}");
        sb.AppendLine("    {");

        // Method signature
        var isAsync = _methodSymbol.IsAsync;
        var returnType = _methodSymbol.ReturnType.ToDisplayString();
        var methodName = _methodSymbol.Name;
        var parameters = string.Join(", ", _methodSymbol.Parameters.Select(p =>
            $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine($"        {(isAsync ? "async " : "")}{returnType} {methodName}_Optimized({parameters})");
        sb.AppendLine("        {");

        // Method body
        GenerateMethodBody(sb, steps);

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateMethodBody(StringBuilder sb, List<StepInfo> steps)
    {
        if (steps.Count == 0)
        {
            // Fallback if we couldn't parse the chain
            sb.AppendLine("            // TODO: Could not parse BDD chain, falling back");
            if (_methodSymbol.IsAsync && _methodSymbol.ReturnType.Name == "Task")
            {
                sb.AppendLine($"            await {_methodSymbol.Name}();");
            }
            else
            {
                sb.AppendLine($"            return {_methodSymbol.Name}();");
            }
            return;
        }

        // Generate optimized implementation
        sb.AppendLine("            // Optimized implementation - no boxing!");
        sb.AppendLine("            var __ctx = TinyBDD.Ambient.Current.Value;");
        sb.AppendLine("            var __sw = System.Diagnostics.Stopwatch.StartNew();");
        sb.AppendLine();
        sb.AppendLine("            try");
        sb.AppendLine("            {");

        // Generate each step
        for (int i = 0; i < steps.Count; i++)
        {
            GenerateStep(sb, steps[i], i);
            if (i < steps.Count - 1)
                sb.AppendLine();
        }

        sb.AppendLine("            }");
        sb.AppendLine("            catch (System.Exception __ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                // TODO: Handle errors according to ScenarioOptions");
        sb.AppendLine("                throw;");
        sb.AppendLine("            }");
    }

    private void GenerateStep(StringBuilder sb, StepInfo step, int stepIndex)
    {
        var stepVar = $"__step{stepIndex}";
        var prevStepVar = stepIndex > 0 ? $"__step{stepIndex - 1}" : null;

        sb.AppendLine($"                // {step.Kind}: {step.Title}");
        sb.AppendLine("                __sw.Restart();");

        // Generate step execution
        if (step.IsTransform)
        {
            // Transform step (returns a value)
            var outputTypeName = step.OutputType?.ToDisplayString() ?? "var";
            var lambdaBody = RewriteLambdaBody(step.LambdaBody, step.ParameterName, prevStepVar);

            if (step.IsAsync)
            {
                sb.AppendLine($"                {outputTypeName} {stepVar} = await ({lambdaBody});");
            }
            else
            {
                sb.AppendLine($"                {outputTypeName} {stepVar} = {lambdaBody};");
            }
        }
        else
        {
            // Effect step (no return value, just side effect)
            var lambdaBody = RewriteLambdaBody(step.LambdaBody, step.ParameterName, prevStepVar);
            
            if (step.IsAsync)
            {
                sb.AppendLine($"                await ({lambdaBody});");
            }
            else
            {
                sb.AppendLine($"                {lambdaBody};");
            }

            // Pass through previous value if exists
            if (prevStepVar != null && stepIndex < 999) // Not the last step
            {
                sb.AppendLine($"                var {stepVar} = {prevStepVar};");
            }
        }

        sb.AppendLine("                __sw.Stop();");

        // Record step result
        sb.AppendLine("                __ctx.AddStep(new TinyBDD.StepResult");
        sb.AppendLine("                {");
        sb.AppendLine($"                    Kind = \"{step.Kind}\",");
        sb.AppendLine($"                    Title = \"{EscapeString(step.Title)}\",");
        sb.AppendLine("                    Elapsed = __sw.Elapsed");
        sb.AppendLine("                });");

        // For Then steps, handle assertions
        if (step.Kind == "Then")
        {
            GenerateThenAssertion(sb, step, prevStepVar);
        }
    }

    private void GenerateThenAssertion(StringBuilder sb, StepInfo step, string? prevStepVar)
    {
        // Then steps are assertions - if the lambda returns false, throw
        // The lambda body will be something like "x => x == 8"
        // We need to check if it returned false and throw if so
        
        // For now, we'll handle this by checking the last step variable
        // which should contain the boolean result
        var lastStepIndex = int.Parse(prevStepVar?.Replace("__step", "") ?? "0") + 1;
        sb.AppendLine($"                if (!__step{lastStepIndex})");
        sb.AppendLine("                {");
        sb.AppendLine($"                    throw new TinyBDD.BddStepException(\"Assertion failed: {EscapeString(step.Title)}\", __ctx, null);");
        sb.AppendLine("                }");
    }

    private string RewriteLambdaBody(string lambdaBody, string paramName, string? prevStepVar)
    {
        // Rewrite lambda to use previous step variable
        // E.g., "x => x + 3" becomes just the body part with x replaced
        
        if (string.IsNullOrWhiteSpace(lambdaBody))
        {
            return "/* empty lambda */";
        }

        var arrowIndex = lambdaBody.IndexOf("=>");
        if (arrowIndex < 0)
        {
            // No arrow found, might be a method reference or malformed lambda
            return lambdaBody;
        }

        // Extract body after arrow
        var bodyStart = arrowIndex + 2; // Skip "=>"
        if (bodyStart >= lambdaBody.Length)
        {
            // Arrow is at the end, nothing after it
            return "/* malformed lambda */";
        }

        var body = lambdaBody.Substring(bodyStart).Trim();
        
        if (string.IsNullOrEmpty(paramName) || prevStepVar == null)
        {
            // No parameter replacement needed (Given step with no input)
            return body;
        }

        // Replace parameter name with previous step variable
        // This is naive but works for simple cases
        // TODO: Use Roslyn's semantic rewriting for complex cases
        return ReplaceIdentifier(body, paramName, prevStepVar);
    }

    private string ReplaceIdentifier(string code, string oldName, string newName)
    {
        // Simple word boundary replacement using regex
        // This replaces the identifier only when it's a complete word (not part of another identifier)
        if (string.IsNullOrEmpty(oldName))
            return code;

        // Use word boundary regex to replace only complete identifiers
        // \b ensures we don't replace partial matches (e.g., "x" in "max")
        var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(oldName)}\b";
        return System.Text.RegularExpressions.Regex.Replace(code, pattern, newName);
    }

    private string EscapeString(string str)
    {
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
