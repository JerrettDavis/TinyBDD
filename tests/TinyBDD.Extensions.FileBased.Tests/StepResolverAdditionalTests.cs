using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Execution;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

/// <summary>
/// Additional tests for StepResolver to cover missing branches and default conditions.
/// </summary>
public class StepResolverAdditionalTests
{
    private class DriverWithDefaultParameter : IApplicationDriver
    {
        [DriverMethod("step with optional {name}")]
        public Task StepWithDefault(string name = "default") => Task.CompletedTask;

        [DriverMethod("step needing {missing}")]
        public Task StepNeedingParameter(string missing) => Task.CompletedTask;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class DriverWithCancellationToken : IApplicationDriver
    {
        [DriverMethod("cancellable step")]
        public Task CancellableStep(CancellationToken ct) => Task.CompletedTask;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void TryResolve_WhenParameterCapturedButHasDefault_UsesCapturedValue()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step with optional value",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(resolved);
        Assert.NotNull(methodInfo);
        Assert.Single(arguments);
        Assert.Equal("value", arguments[0]); // Captured value from pattern should be used
    }

    [Fact]
    public void TryResolve_WhenNoMatchFound_ReturnsFalse()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "completely unmatched step",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.False(resolved); // Should not resolve when pattern doesn't match
        Assert.Null(methodInfo);
    }

    [Fact]
    public void TryResolve_WhenParameterIsCancellationToken_ProvidesCancellationTokenNone()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithCancellationToken));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "cancellable step",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(resolved);
        Assert.NotNull(methodInfo);
        Assert.Single(arguments);
        Assert.Equal(CancellationToken.None, arguments[0]);
    }

    [Fact]
    public void TryResolve_WhenParameterInYamlDictionary_UsesYamlValue()
    {
        // Arrange - Testing priority: YAML parameters > regex captures
        var resolver = new StepResolver(typeof(DriverWithDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step with optional test",
            Parameters = new Dictionary<string, object?> { ["name"] = "yaml-value" }
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(resolved);
        Assert.Single(arguments);
        Assert.Equal("yaml-value", arguments[0]); // YAML value has priority over captured value
    }

    [Fact]
    public void TryResolve_WhenRequiredParameterMissing_ReturnsFalse()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "totally different step text",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.False(resolved); // Should not resolve when no pattern matches
        Assert.Null(methodInfo);
    }

    [Fact]
    public void ConvertValue_WhenValueIsNull_ReturnsNull()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step with optional null",
            Parameters = new Dictionary<string, object?> { ["name"] = null }
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(resolved);
        Assert.NotNull(methodInfo);
        Assert.Single(arguments);
        Assert.Null(arguments[0]); // Null should be preserved
    }

    [Fact]
    public void ConvertValue_WhenTargetTypeIsCancellationToken_ReturnsCancellationTokenNone()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithCancellationToken));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "cancellable step",
            Parameters = new Dictionary<string, object?> { ["ct"] = "something" }
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(resolved);
        Assert.Equal(CancellationToken.None, arguments[0]);
    }

    [Fact]
    public void ConvertValue_WhenTypeIsAssignable_ReturnsValueDirectly()
    {
        // Arrange - testing when value type is already assignable
        var resolver = new StepResolver(typeof(DriverWithDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step with optional test",
            Parameters = new Dictionary<string, object?> { ["name"] = "test" }
        };

        // Act
        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(resolved);
        Assert.Equal("test", arguments[0]);
    }

    [Fact]
    public void ConvertValue_WhenConversionFails_ThrowsDescriptiveException()
    {
        // Arrange
        var resolver = new StepResolver(typeof(DriverWithInvalidConversion));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step needing number invalid",
            Parameters = new Dictionary<string, object?> { ["num"] = "not-a-number" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.TryResolve(step, out _, out _));
        
        Assert.Contains("Cannot convert parameter value", exception.Message);
        Assert.Contains("not-a-number", exception.Message);
        Assert.Contains("Int32", exception.Message);
    }

    private class DriverWithInvalidConversion : IApplicationDriver
    {
        [DriverMethod("step needing number {num}")]
        public Task StepNeedingNumber(int num) => Task.CompletedTask;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
