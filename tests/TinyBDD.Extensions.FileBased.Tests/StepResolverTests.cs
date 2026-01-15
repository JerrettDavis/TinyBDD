using System.Reflection;
using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Execution;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

public class StepResolverTests
{
    private class TestDriver : IApplicationDriver
    {
        [DriverMethod("simple step")]
        public Task SimpleStep() => Task.CompletedTask;

        [DriverMethod("step with {param}")]
        public Task StepWithParameter(string param) => Task.CompletedTask;

        [DriverMethod("step with {a} and {b}")]
        public Task StepWithMultipleParameters(int a, int b) => Task.CompletedTask;
        
        [DriverMethod("I add 5+3")]
        public Task AddWithPlusSign() => Task.CompletedTask;
        
        [DriverMethod("I check [value]")]
        public Task CheckWithBrackets() => Task.CompletedTask;
        
        [DriverMethod("I call method()")]
        public Task CallMethodWithParentheses() => Task.CompletedTask;
        
        [DriverMethod("I multiply {a}*{b}")]
        public Task MultiplyWithAsterisk(int a, int b) => Task.CompletedTask;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void Constructor_WithValidDriverType_DiscoversDriverMethods()
    {
        // Arrange & Act
        var resolver = new StepResolver(typeof(TestDriver));

        // Assert - Should not throw
        Assert.NotNull(resolver);
    }

    [Fact]
    public void Constructor_WithNullDriverType_ThrowsException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StepResolver(null!));
    }

    [Fact]
    public void Constructor_WithNonDriverType_ThrowsException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new StepResolver(typeof(string)));
    }

    [Fact]
    public void TryResolve_SimpleStep_ReturnsTrue()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "simple step",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("SimpleStep", methodInfo.Method.Name);
        Assert.Empty(arguments);
    }

    [Fact]
    public void TryResolve_StepWithParameter_ExtractsParameter()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "When",
            Text = "step with test",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("StepWithParameter", methodInfo.Method.Name);
        Assert.Single(arguments);
        Assert.Equal("test", arguments[0]);
    }

    [Fact]
    public void TryResolve_StepWithMultipleParameters_ExtractsAllParameters()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "Then",
            Text = "step with 5 and 10",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("StepWithMultipleParameters", methodInfo.Method.Name);
        Assert.Equal(2, arguments.Length);
        Assert.Equal(5, arguments[0]);
        Assert.Equal(10, arguments[1]);
    }

    [Fact]
    public void TryResolve_UnmatchedStep_ReturnsFalse()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "Given",
            Text = "unmatched step",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.False(result);
        Assert.Null(methodInfo);
    }

    [Fact]
    public void TryResolve_StepWithPlusSign_MatchesCorrectly()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "When",
            Text = "I add 5+3",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("AddWithPlusSign", methodInfo.Method.Name);
    }

    [Fact]
    public void TryResolve_StepWithBrackets_MatchesCorrectly()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "Then",
            Text = "I check [value]",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("CheckWithBrackets", methodInfo.Method.Name);
    }

    [Fact]
    public void TryResolve_StepWithParentheses_MatchesCorrectly()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "When",
            Text = "I call method()",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("CallMethodWithParentheses", methodInfo.Method.Name);
    }

    [Fact]
    public void TryResolve_StepWithAsteriskAndParameters_MatchesCorrectly()
    {
        // Arrange
        var resolver = new StepResolver(typeof(TestDriver));
        var step = new StepDefinition
        {
            Keyword = "When",
            Text = "I multiply 4*7",
            Parameters = new Dictionary<string, object?>()
        };

        // Act
        var result = resolver.TryResolve(step, out var methodInfo, out var arguments);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodInfo);
        Assert.Equal("MultiplyWithAsterisk", methodInfo.Method.Name);
        Assert.Equal(2, arguments.Length);
        Assert.Equal(4, arguments[0]);
        Assert.Equal(7, arguments[1]);
    }
}
