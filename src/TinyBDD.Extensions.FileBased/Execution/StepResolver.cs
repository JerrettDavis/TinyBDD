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
    private static readonly Dictionary<Type, List<DriverMethodInfo>> _cache = new();
    private static readonly object _cacheLock = new();
    
    private readonly List<DriverMethodInfo> _driverMethods;

    public StepResolver(Type driverType)
    {
        if (driverType == null)
            throw new ArgumentNullException(nameof(driverType));

        if (!typeof(IApplicationDriver).IsAssignableFrom(driverType))
            throw new ArgumentException($"Type {driverType.Name} must implement IApplicationDriver", nameof(driverType));

        // Use cached driver methods if available
        lock (_cacheLock)
        {
            if (!_cache.TryGetValue(driverType, out var cachedMethods))
            {
                cachedMethods = DiscoverDriverMethods(driverType);
                _cache[driverType] = cachedMethods;
            }
            _driverMethods = cachedMethods;
        }
    }

    private static List<DriverMethodInfo> DiscoverDriverMethods(Type driverType)
    {
        var driverMethods = new List<DriverMethodInfo>();
        var methods = driverType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes<DriverMethodAttribute>();
            foreach (var attr in attributes)
            {
                driverMethods.Add(new DriverMethodInfo
                {
                    Method = method,
                    Pattern = attr.StepPattern,
                    Regex = BuildRegexFromPattern(attr.StepPattern)
                });
            }
        }

        return driverMethods;
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
        // Strategy: Escape special regex characters, but preserve {paramName} placeholders
        // to convert them to capture groups.
        
        // Step 1: Temporarily replace {paramName} with a placeholder that won't be escaped
        var tempPlaceholder = Guid.NewGuid().ToString();
        var paramNames = new List<string>();
        var tempPattern = Regex.Replace(pattern, @"\{(\w+)\}", match =>
        {
            paramNames.Add(match.Groups[1].Value);
            return $"{tempPlaceholder}{paramNames.Count - 1}{tempPlaceholder}";
        });
        
        // Step 2: Escape the pattern (now special chars are escaped, but placeholders are safe)
        var escapedPattern = Regex.Escape(tempPattern);
        
        // Step 3: Replace escaped spaces with flexible whitespace
        escapedPattern = escapedPattern.Replace(@"\ ", @"\s+");
        
        // Step 4: Convert placeholders back to capture groups
        for (int i = 0; i < paramNames.Count; i++)
        {
            var placeholder = Regex.Escape($"{tempPlaceholder}{i}{tempPlaceholder}");
            escapedPattern = escapedPattern.Replace(placeholder, $@"(?<{paramNames[i]}>\S+)");
        }
        
        // Step 5: Anchor the regex to match the whole step text
        var regexPattern = "^" + escapedPattern + "$";
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

        // Handle basic type conversions with better error messages
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException)
        {
            throw new InvalidOperationException(
                $"Cannot convert parameter value '{value}' to type '{targetType.Name}'. " +
                $"Ensure the value format matches the expected type.", ex);
        }
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
