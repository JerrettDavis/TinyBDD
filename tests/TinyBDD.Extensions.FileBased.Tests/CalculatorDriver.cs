using TinyBDD.Extensions.FileBased.Core;

namespace TinyBDD.Extensions.FileBased.Tests;

/// <summary>
/// Application driver for calculator tests.
/// </summary>
public class CalculatorDriver : IApplicationDriver
{
    private int _result;

    [DriverMethod("a calculator")]
    public Task Initialize()
    {
        _result = 0;
        return Task.CompletedTask;
    }

    [DriverMethod("I add {a} and {b}")]
    public Task Add(int a, int b)
    {
        _result = a + b;
        return Task.CompletedTask;
    }

    [DriverMethod("I multiply {a} and {b}")]
    public Task Multiply(int a, int b)
    {
        _result = a * b;
        return Task.CompletedTask;
    }

    [DriverMethod("the result should be {expected}")]
    public Task<bool> VerifyResult(int expected)
    {
        return Task.FromResult(_result == expected);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _result = 0;
        return Task.CompletedTask;
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
