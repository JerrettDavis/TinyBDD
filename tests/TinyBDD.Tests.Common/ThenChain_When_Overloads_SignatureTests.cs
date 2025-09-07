using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

public class ThenChainWhenOverloadsExecute(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public void Calls_all_When_overloads()
    {
        // Build a chain and get ThenChain<int>. Use a valid Then overload.
        var then = Given("seed", () => 1)
            .Then("noop", _ => Task.CompletedTask);

        // -------- Non-TOut overloads (void-like) --------

        // Action<T>
        then.When(_ => { });
        then.When("Action<T>", _ => { });

        // Func<T, Task>
        then.When(_ => Task.CompletedTask);
        then.When("Func<T, Task>", _ => Task.CompletedTask);

        // Func<T, ValueTask>
        then.When(_ => new ValueTask());
        then.When("Func<T, ValueTask>", _ => new ValueTask());

        // Func<T, CancellationToken, Task>
        then.When((_, __) => Task.CompletedTask);
        then.When("Func<T, CT, Task>", (_, __) => Task.CompletedTask);

        // Func<T, CancellationToken, ValueTask>
        then.When((_, __) => new ValueTask());
        then.When("Func<T, CT, ValueTask>", (_, __) => new ValueTask());

        // -------- TOut (method-generic) overloads --------

        // Func<T, TOut>
        _ = then.When<string>(_ => "ok");
        _ = then.When<string>("Func<T, TOut>", _ => "ok");

        // Func<T, Task<TOut>>
        _ = then.When<string>(_ => Task.FromResult("ok"));
        _ = then.When<string>("Func<T, Task<TOut>>", _ => Task.FromResult("ok"));

        // Func<T, ValueTask<TOut>>
        _ = then.When<string>(_ => new ValueTask<string>("ok"));
        _ = then.When<string>("Func<T, ValueTask<TOut>>", _ => new ValueTask<string>("ok"));

        // Func<T, CancellationToken, Task<TOut>>
        _ = then.When<string>((_, __) => Task.FromResult("ok"));
        _ = then.When<string>("Func<T, CT, Task<TOut>>", (_, __) => Task.FromResult("ok"));

        // Func<T, CancellationToken, ValueTask<TOut>>
        _ = then.When<string>((_, __) => new ValueTask<string>("ok"));
        _ = then.When<string>("Func<T, CT, ValueTask<TOut>>", (_, __) => new ValueTask<string>("ok"));
    }
}