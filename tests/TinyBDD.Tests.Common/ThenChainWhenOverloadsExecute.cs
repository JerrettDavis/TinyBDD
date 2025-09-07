using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TinyBDD.Tests.Common;

public class ThenChainWhenOverloadsExecute(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Drive the previous step so the When delegate actually runs
    private static void Drive(ScenarioChain<int> chain) =>
        chain.Then("sink", _ => Task.CompletedTask);

    private static void Drive<TOut>(ScenarioChain<TOut> chain) =>
        chain.Then("sink", _ => Task.CompletedTask);

    [Fact]
    public void Covers_WhenTransform_and_all_ToCT_variants_including_CT_and_fast_slow_paths()
    {
        var then = Given("seed", () => 1)
           .Then("noop", _ => Task.CompletedTask); // ScenarioChain<int>

        // ---------- Non-TOut (void-like) ----------
        // Action<T>
        Drive(then.When((int _) => { }));
        Drive(then.When("Action<T>", (int _) => { }));

        // Func<T, Task>  — fast & slow
        Task TaskFast(int _) => Task.CompletedTask;
        Task TaskSlow(int _) => Task.Delay(1);
        Drive(then.When((Func<int, Task>)TaskFast));
        Drive(then.When("Func<T, Task>", (Func<int, Task>)TaskFast));
        Drive(then.When((Func<int, Task>)TaskSlow));
        Drive(then.When("Func<T, Task>", (Func<int, Task>)TaskSlow));

        // Func<T, ValueTask> — fast & slow
        ValueTask VTFast(int _) => new();                          // completed VT
        ValueTask VTSlow(int _) => new(Task.Delay(1));             // wraps Task (awaits)
        Drive(then.When((Func<int, ValueTask>)VTFast));
        Drive(then.When("Func<T, ValueTask>", (Func<int, ValueTask>)VTFast));
        Drive(then.When((Func<int, ValueTask>)VTSlow));
        Drive(then.When("Func<T, ValueTask>", (Func<int, ValueTask>)VTSlow));

        // Func<T, CancellationToken, Task> — fast & slow
        Task TaskFastCt(int _, CancellationToken __) => Task.CompletedTask;
        Task TaskSlowCt(int _, CancellationToken ct) => Task.Delay(1, ct);
        Drive(then.When((Func<int, CancellationToken, Task>)TaskFastCt));
        Drive(then.When("Func<T, CT, Task>", (Func<int, CancellationToken, Task>)TaskFastCt));
        Drive(then.When((Func<int, CancellationToken, Task>)TaskSlowCt));
        Drive(then.When("Func<T, CT, Task>", (Func<int, CancellationToken, Task>)TaskSlowCt));

        // Func<T, CancellationToken, ValueTask> — fast & slow
        ValueTask VTFastCt(int _, CancellationToken __) => new();
        ValueTask VTSlowCt(int _, CancellationToken ct) => new(Task.Delay(1, ct));
        Drive(then.When((Func<int, CancellationToken, ValueTask>)VTFastCt));
        Drive(then.When("Func<T, CT, ValueTask>", (Func<int, CancellationToken, ValueTask>)VTFastCt));
        Drive(then.When((Func<int, CancellationToken, ValueTask>)VTSlowCt));
        Drive(then.When("Func<T, CT, ValueTask>", (Func<int, CancellationToken, ValueTask>)VTSlowCt));

        // ---------- TOut (transform) ----------
        // Func<T, TOut> — (no async split here)
        string Ret(int _) => "ok";
        Drive(then.When<string>((Func<int, string>)Ret));
        Drive(then.When<string>("Func<T, TOut>", (Func<int, string>)Ret));

        // Func<T, Task<TOut>> — fast & slow (hits ToCT(Func<T,Task<TOut>>))
        Task<string> TFast(int _) => Task.FromResult("ok");        // fast path
        Task<string> TSlow(int _) => Task.Delay(1).ContinueWith(_ => "ok");
        Drive(then.When<string>((Func<int, Task<string>>)TFast));
        Drive(then.When<string>("Func<T, Task<TOut>>", (Func<int, Task<string>>)TFast));
        Drive(then.When<string>((Func<int, Task<string>>)TSlow));
        Drive(then.When<string>("Func<T, Task<TOut>>", (Func<int, Task<string>>)TSlow));

        // Func<T, ValueTask<TOut>> — fast & slow (hits ToCT(Func<T,ValueTask<TOut>>))
        ValueTask<string> VTFastOut(int _) => new("ok");           // completed VT<T>
        ValueTask<string> VTSlowOut(int _) => new(Task.FromResult("ok"));
        Drive(then.When<string>((Func<int, ValueTask<string>>)VTFastOut));
        Drive(then.When<string>("Func<T, ValueTask<TOut>>", (Func<int, ValueTask<string>>)VTFastOut));
        Drive(then.When<string>((Func<int, ValueTask<string>>)VTSlowOut));
        Drive(then.When<string>("Func<T, ValueTask<TOut>>", (Func<int, ValueTask<string>>)VTSlowOut));

        // Func<T, CT, Task<TOut>> — fast & slow (hits ToCT(Func<T,CT,Task<TOut>>))
        Task<string> TFastCt(int _, CancellationToken __) => Task.FromResult("ok");
        Task<string> TSlowCt(int _, CancellationToken ct) => Task.Delay(1, ct).ContinueWith(_ => "ok", ct);
        Drive(then.When<string>((Func<int, CancellationToken, Task<string>>)TFastCt));
        Drive(then.When<string>("Func<T, CT, Task<TOut>>", (Func<int, CancellationToken, Task<string>>)TFastCt));
        Drive(then.When<string>((Func<int, CancellationToken, Task<string>>)TSlowCt));
        Drive(then.When<string>("Func<T, CT, Task<TOut>>", (Func<int, CancellationToken, Task<string>>)TSlowCt));

        // Func<T, CT, ValueTask<TOut>> — fast & slow (hits WhenTransform directly)
        ValueTask<string> VTFastCtOut(int _, CancellationToken __) => new("ok");
        ValueTask<string> VTSlowCtOut(int _, CancellationToken ct) => new(Task.Run(() => "ok", ct));
        Drive(then.When<string>((Func<int, CancellationToken, ValueTask<string>>)VTFastCtOut));
        Drive(then.When<string>("Func<T, CT, ValueTask<TOut>>", (Func<int, CancellationToken, ValueTask<string>>)VTFastCtOut));
        Drive(then.When<string>((Func<int, CancellationToken, ValueTask<string>>)VTSlowCtOut));
        Drive(then.When<string>("Func<T, CT, ValueTask<TOut>>", (Func<int, CancellationToken, ValueTask<string>>)VTSlowCtOut));
    }
    
    
    private ThenChain<int> Start() =>
        Given("seed", () => 1)
            .Then("noop", _ => Task.CompletedTask);

    private static ThenChain<TOut> Sink<TOut>(ScenarioChain<TOut> chain) =>
        chain.Then("sink", _ => Task.CompletedTask);

    [Fact]
    public async Task Covers_all_public_When_overloads()
    {
        using var cts = new CancellationTokenSource(); // real token that flows

        // -------------------- effect (no TOut) --------------------
        // Action<T>
        await Start().When((Action<int>)(_ => { })).Then(_ => true).AssertPassed(cts.Token);
        await Start().When("Action<T>", (Action<int>)(_ => { })).Then(_ => true).AssertPassed(cts.Token);

        // Func<T, Task>
        await Start().When((Func<int, Task>)(_ => Task.CompletedTask)).Then(_ => true).AssertPassed(cts.Token);
        await Start().When("Func<T,Task>", (Func<int, Task>)(_ => Task.CompletedTask)).Then(_ => true).AssertPassed(cts.Token);

        // Func<T, ValueTask>
        await Start().When((Func<int, ValueTask>)(_ => new ValueTask())).Then(_ => true).AssertPassed(cts.Token);
        await Start().When("Func<T,ValueTask>", (Func<int, ValueTask>)(_ => new ValueTask())).Then(_ => true).AssertPassed(cts.Token);

        // Func<T, CancellationToken, Task>
        await Start().When((Func<int, CancellationToken, Task>)((_, ct) => Task.CompletedTask)).Then(_ => true).AssertPassed(cts.Token);
        await Start().When("Func<T,CT,Task>", (Func<int, CancellationToken, Task>)((_, ct) => Task.CompletedTask)).Then(_ => true)
            .AssertPassed(cts.Token);

        // Func<T, CancellationToken, ValueTask>
        await Start().When((Func<int, CancellationToken, ValueTask>)((_, ct) => new ValueTask())).Then(_ => true).AssertPassed(cts.Token);
        await Start().When("Func<T,CT,ValueTask>", (Func<int, CancellationToken, ValueTask>)((_, ct) => new ValueTask())).Then(_ => true)
            .AssertPassed(cts.Token);

        // -------------------- transform (TOut) --------------------
        // Func<T, TOut>
        await Sink(Start().When<string>((Func<int, string>)(_ => "ok"))).AssertPassed(cts.Token);
        await Sink(Start().When<string>("Func<T,TOut>", (Func<int, string>)(_ => "ok"))).AssertPassed(cts.Token);

        // Func<T, Task<TOut>>
        await Sink(Start().When<string>((Func<int, Task<string>>)(_ => Task.FromResult("ok")))).AssertPassed(cts.Token);
        await Sink(Start().When<string>("Func<T,Task<TOut>>", (Func<int, Task<string>>)(_ => Task.FromResult("ok")))).AssertPassed(cts.Token);

        // Func<T, ValueTask<TOut>>
        await Sink(Start().When<string>((Func<int, ValueTask<string>>)(_ => new ValueTask<string>("ok")))).AssertPassed(cts.Token);
        await Sink(Start().When<string>("Func<T,ValueTask<TOut>>", (Func<int, ValueTask<string>>)(_ => new ValueTask<string>("ok"))))
            .AssertPassed(cts.Token);

        // Func<T, CancellationToken, Task<TOut>>
        await Sink(Start().When<string>((Func<int, CancellationToken, Task<string>>)((_, ct) => Task.FromResult("ok")))).AssertPassed(cts.Token);
        await Sink(Start().When<string>("Func<T,CT,Task<TOut>>", (Func<int, CancellationToken, Task<string>>)((_, ct) => Task.FromResult("ok"))))
            .AssertPassed(cts.Token);

        // Func<T, CancellationToken, ValueTask<TOut>>
        await Sink(Start().When<string>((Func<int, CancellationToken, ValueTask<string>>)((_, ct) => new ValueTask<string>("ok"))))
            .AssertPassed(cts.Token);
        await Sink(Start().When<string>("Func<T,CT,ValueTask<TOut>>",
            (Func<int, CancellationToken, ValueTask<string>>)((_, ct) => new ValueTask<string>("ok")))).AssertPassed(cts.Token);
    }
}
