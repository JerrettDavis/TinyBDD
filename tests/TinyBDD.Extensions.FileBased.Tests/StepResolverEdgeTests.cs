using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Execution;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

/// <summary>
/// Tests for <see cref="StepResolver"/> parameter resolution edge cases not covered
/// by <see cref="StepResolverAdditionalTests"/>: namely, parameters that are absent
/// from both the pattern and the step parameter dictionary.
/// </summary>
public class StepResolverEdgeTests
{
    private sealed class DriverWithUnreferencedDefaultParameter : IApplicationDriver
    {
        public string? LastA { get; private set; }
        public string? LastB { get; private set; }

        // Only {a} is captured by the pattern; b has a default value
        // and is neither captured nor provided via step parameters.
        [DriverMethod("step with {a}")]
        public Task StepMethod(string a, string b = "fallback")
        {
            LastA = a;
            LastB = b;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DriverWithUnreferencedRequiredParameter : IApplicationDriver
    {
        // {a} captured; b is required, has no default, and is not CancellationToken.
        [DriverMethod("step with only {a}")]
        public Task StepMethod(string a, string b)
        {
            _ = a;
            _ = b;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void TryResolve_WhenSecondaryParameter_HasDefault_UsesDefault()
    {
        var resolver = new StepResolver(typeof(DriverWithUnreferencedDefaultParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step with hello",
            Parameters = new Dictionary<string, object?>()
        };

        var resolved = resolver.TryResolve(step, out var methodInfo, out var arguments);

        Assert.True(resolved);
        Assert.NotNull(methodInfo);
        Assert.Equal(2, arguments.Length);
        Assert.Equal("hello", arguments[0]);
        Assert.Equal("fallback", arguments[1]); // Default value path
    }

    [Fact]
    public void TryResolve_WhenSecondaryParameter_Required_NoDefault_NotCt_Throws()
    {
        var resolver = new StepResolver(typeof(DriverWithUnreferencedRequiredParameter));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "step with only world",
            Parameters = new Dictionary<string, object?>()
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => resolver.TryResolve(step, out _, out _));

        Assert.Contains("Cannot resolve parameter", ex.Message);
        Assert.Contains("'b'", ex.Message);
    }
}
