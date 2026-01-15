using System.Reflection;
using System.Text.RegularExpressions;
using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Execution;

/// <summary>
/// Resolves step definitions to driver methods using pattern matching.
/// </summary>
public sealed class StepResolver
{
    private readonly List<DriverMethodInfo> _driverMethods = new();

    public StepResolver(Type driverType)
    {
        if (driverType == null)
            throw new ArgumentNullException(nameof(driverType));

        if (!typeof(IApplicationDriver).IsAssignableFrom(driverType))
            throw new ArgumentException($"Type {driverType.Name} must implement IApplicationDriver", nameof(driverType));

        DiscoverDriverMethods(driverType);
    }

    private void DiscoverDriverMethods(Type driverType)
    {
        var methods = driverType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes<DriverMethodAttribute>();
            foreach (var attr in attributes)
            {
                _driverMethods.Add(new DriverMethodInfo
                {
                    Method = method,
                    Pattern = attr.StepPattern,
                    Regex = BuildRegexFromPattern(attr.StepPattern)
                });
            }
        }
    }

    /// <summary>
    /// Attempts to resolve a step to a driver method.
    /// </summary>
    /// <param name="step">The step to resolve.</param>
    /// <param name="methodInfo">The resolved method info.</param>
    /// <param name="arguments">The extracted arguments.</param>
    /// <returns>True if resolved, false otherwise.</returns>
    public bool TryResolve(StepDefinition step, out DriverMethodInfo? methodInfo, out object?[] arguments)
    {
        methodInfo = null;
        arguments = Array.Empty<object?>();

        foreach (var driverMethod in _driverMethods)
        {
            var match = driverMethod.Regex.Match(step.Text);
            if (match.Success)
            {
                methodInfo = driverMethod;
                arguments = ExtractArguments(match, driverMethod.Method, step.Parameters);
                return true;
            }
        }

        return false;
    }

    private static Regex BuildRegexFromPattern(string pattern)
    {
        // Convert {paramName} to named capture groups
        // Use [^\s]+ to match non-whitespace characters or .+? for greedy matching
        var regexPattern = Regex.Replace(pattern, @"\{(\w+)\}", @"(?<$1>\S+)");
        // Escape special regex characters except our capture groups
        regexPattern = "^" + regexPattern + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static object?[] ExtractArguments(Match match, MethodInfo method, Dictionary<string, object?> stepParameters)
    {
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            
            // First try to get from step parameters
            if (stepParameters.TryGetValue(param.Name!, out var value))
            {
                arguments[i] = ConvertValue(value, param.ParameterType);
                continue;
            }

            // Then try to get from regex capture groups
            var group = match.Groups[param.Name!];
            if (group.Success)
            {
                arguments[i] = ConvertValue(group.Value, param.ParameterType);
                continue;
            }

            // Use default value if available
            if (param.HasDefaultValue)
            {
                arguments[i] = param.DefaultValue;
            }
            else if (param.ParameterType == typeof(CancellationToken))
            {
                arguments[i] = CancellationToken.None;
            }
            else
            {
                throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' for method '{method.Name}'");
            }
        }

        return arguments;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType == typeof(CancellationToken))
            return CancellationToken.None;

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        // Handle basic type conversions
        return Convert.ChangeType(value, targetType);
    }
}

/// <summary>
/// Information about a driver method.
/// </summary>
public sealed class DriverMethodInfo
{
    public MethodInfo Method { get; init; } = null!;
    public string Pattern { get; init; } = string.Empty;
    public Regex Regex { get; init; } = null!;
}
