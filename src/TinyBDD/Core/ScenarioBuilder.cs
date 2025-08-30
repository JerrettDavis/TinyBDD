using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TinyBDD;

// ----------------------------- StepResult (your type) -----------------------------

// ----------------------------- Internals ------------------------------------------

internal enum StepPhase
{
    Given,
    When,
    Then
}

internal enum StepWord
{
    Primary,
    And,
    But
}

internal static class VT
{
    public static ValueTask<T> From<T>(T v) => ValueTask.FromResult(v);

    public static ValueTask<T> From<T>(Task<T> t) =>
        t.IsCompletedSuccessfully ? ValueTask.FromResult(t.Result) : new ValueTask<T>(t);
}

internal static class KindStrings
{
    public static string For(StepPhase phase, StepWord word)
        => word switch { StepWord.And => "And", StepWord.But => "But", _ => phase.ToString() };
}

internal static class AssertUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Ensure(bool ok, string title)
    {
        if (!ok) throw new BddAssertException($"Assertion failed: {title}");
    }
}

// Shared FIFO pipeline; records StepResult; never rethrows (errors are logged)
internal sealed class Pipeline
{
    private readonly ScenarioContext _ctx;
    private object? _state;
    private StepPhase _lastPhase = StepPhase.Given;
    private readonly ConcurrentQueue<Step> _steps = new();

    private readonly struct Step
    {
        public readonly StepPhase Phase;
        public readonly StepWord Word;
        public readonly string Title;
        public readonly Func<object?, CancellationToken, ValueTask<object?>> Exec;

        public Step(StepPhase phase, StepWord word, string title, Func<object?, CancellationToken, ValueTask<object?>> exec)
        {
            Phase = phase;
            Word = word;
            Title = title;
            Exec = exec;
        }
    }

    public Pipeline(ScenarioContext ctx) => _ctx = ctx;

    public void Enqueue(
        StepPhase phase,
        StepWord word,
        string title,
        Func<object?, CancellationToken, ValueTask<object?>> exec)
    {
        _lastPhase = phase;
        _steps.Enqueue(new Step(phase, word, title, exec));
    }

    public void EnqueueInherit(string title, Func<object?, CancellationToken, ValueTask<object?>> exec, StepWord word)
        => Enqueue(_lastPhase, word, title, exec);

    public async ValueTask RunAsync(CancellationToken ct)
    {
        while (_steps.TryDequeue(out var step))
        {
            var sw = Stopwatch.StartNew();
            Exception? err = null;
            try
            {
                _state = await step.Exec(_state, ct);
            }
            catch (Exception ex)
            {
                // Trap & record; DO NOT rethrow
                err = ex;
            }
            finally
            {
                sw.Stop();
                _ctx.AddStep(new StepResult
                {
                    Kind = KindStrings.For(step.Phase, step.Word),
                    Title = string.IsNullOrWhiteSpace(step.Title) ? step.Phase.ToString() : step.Title,
                    Elapsed = sw.Elapsed,
                    Error = err
                });
            }
        }
    }
}

// ----------------------------- ThenChain<T> (awaitable) ---------------------------

public readonly struct ThenChain<T>
{
    private readonly Pipeline _p;
    internal ThenChain(Pipeline p) => _p = p;

    // await chain;
    public ValueTaskAwaiter GetAwaiter() => _p.RunAsync(default).GetAwaiter();

    // ----- And (explicit title) -----

    public ThenChain<T> And(string title, Action<T> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, (s, ct) =>
        {
            assertion((T)s!);
            return VT.From(s);
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, CancellationToken, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    // bool-returning → assert
    public ThenChain<T> And(string title, Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, (s, ct) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return VT.From(s);
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), title);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), title);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(string title, Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return this;
    }

    // ----- And (default title) -----

    public ThenChain<T> And(Action<T> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", (s, ct) =>
        {
            assertion((T)s!);
            return VT.From(s);
        });
        return this;
    }

    public ThenChain<T> And(Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, CancellationToken, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    // bool-returning
    public ThenChain<T> And(Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", (s, ct) =>
        {
            AssertUtil.Ensure(predicate((T)s!), "And");
            return VT.From(s);
        });
        return this;
    }

    public ThenChain<T> And(Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "And");
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "And");
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "And");
            return s;
        });
        return this;
    }

    public ThenChain<T> And(Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.And, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "And");
            return s;
        });
        return this;
    }

    // ----- But (just like And) -----

    public ThenChain<T> But(string title, Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, (s, ct) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return VT.From(s);
        });
        return this;
    }

    public ThenChain<T> But(Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", (s, ct) =>
        {
            AssertUtil.Ensure(predicate((T)s!), "But");
            return VT.From(s);
        });
        return this;
    }


// -------------------- THEN-BUT (async assertion returning Task) --------------------

// explicit title
    public ThenChain<T> But(string title, Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, async (s, ct) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    public ThenChain<T> But(string title, Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    public ThenChain<T> But(string title, Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }

    // default title
    public ThenChain<T> But(Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", async (s, ct) =>
        {
            await assertion();
            return s;
        });
        return this;
    }

    public ThenChain<T> But(Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return this;
    }

    public ThenChain<T> But(Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.But, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return this;
    }
}

// ----------------------------- ScenarioChain<T> (typed flow) ----------------------

public sealed class ScenarioChain<T>
{
    private readonly Pipeline _p;
    internal ScenarioChain(Pipeline p) => _p = p;

    internal static ScenarioChain<T> Seed(ScenarioContext ctx, string title, Func<CancellationToken, ValueTask<T>> fn)
    {
        var p = new Pipeline(ctx);
        p.Enqueue(StepPhase.Given, StepWord.Primary, title, async (_, ct) => (object?)await fn(ct));
        return new ScenarioChain<T>(p);
    }

    // ---------------- WHEN transforms to U ----------------

    // explicit title
    public ScenarioChain<U> When<U>(string title, Func<T, U> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, (s, ct) => VT.From((object?)f((T)s!)));
        return new ScenarioChain<U>(_p);
    }
    
    
    public ScenarioChain<T> When<U>(string title, Func<T, ValueTask<U>> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => await effect((T)s!));
        return this;
    }

    public ScenarioChain<U> When<U>(string title, Func<T, Task<U>> f) // tokenless async
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, _) => await f((T)s!));
        return new ScenarioChain<U>(_p);
    }
    
    
    // public ScenarioChain<U> WhenAsync<U>(string title, Func<Task<T>, Task<U>> f) // tokenless async
    // {
    //     _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, _) => await f((Task<T>)s!));
    //     return new ScenarioChain<U>(_p);
    // }

    // public ScenarioChain<U> When<U>(string title, Func<T, ValueTask<U>> f) // tokenless VT
    // {
    //     _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => (object?)await f((T)s!));
    //     return new ScenarioChain<U>(_p);
    // }

    // public ScenarioChain<U> When<U>(string title, Func<T, CancellationToken, Task<U>> f)
    // {
    //     _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => (object?)await f((T)s!, ct));
    //     return new ScenarioChain<U>(_p);
    // }

    // public ScenarioChain<U> When<U>(string title, Func<T, CancellationToken, ValueTask<U>> f)
    // {
    //     _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => (object?)await f((T)s!, ct));
    //     return new ScenarioChain<U>(_p);
    // }

    // default title
    public ScenarioChain<U> When<U>(Func<T, U> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", (s, ct) => VT.From((object?)f((T)s!)));
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> When<U>(Func<T, Task<U>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) => (object?)await f((T)s!));
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> When<U>(Func<T, ValueTask<U>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) => (object?)await f((T)s!));
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> When<U>(Func<T, CancellationToken, Task<U>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) => (object?)await f((T)s!, ct));
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> When<U>(Func<T, CancellationToken, ValueTask<U>> f)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) => (object?)await f((T)s!, ct));
        return new ScenarioChain<U>(_p);
    }

    // ---------------- WHEN side-effects keep T ----------------

    // explicit title
    public ScenarioChain<T> When(string title, Action<T> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, (s, ct) =>
        {
            effect((T)s!);
            return VT.From(s);
        });
        return this;
    }

    public ScenarioChain<T> When(string title, Func<T, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        });
        return this;
    }
    
    public ScenarioChain<TOut> When<TOut>(string title, Func<T, CancellationToken, Task<TOut>> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => await effect((T)s!, ct));
        return new ScenarioChain<TOut>(_p);;
    }
    
    
    public ScenarioChain<T> When(string title, Func<T, CancellationToken, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }
    
    public ScenarioChain<T> When<U>(string title, Func<T, CancellationToken, ValueTask<U>> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) => await effect((T)s!, ct));
        return this;
    }

    public ScenarioChain<T> When(string title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    // default title
    public ScenarioChain<T> When(Action<T> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", (s, ct) =>
        {
            effect((T)s!);
            return VT.From(s);
        });
        return this;
    }

    public ScenarioChain<T> When(Func<T, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        });
        return this;
    }

    public ScenarioChain<T> When(Func<T, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        });
        return this;
    }

    public ScenarioChain<T> When(Func<T, CancellationToken, Task> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    public ScenarioChain<T> When(Func<T, CancellationToken, ValueTask> effect)
    {
        _p.Enqueue(StepPhase.When, StepWord.Primary, "", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        });
        return this;
    }

    // ---------------- AND / BUT inherit phase (transform to U) ----------------

    // explicit title (transform)
    public ScenarioChain<U> And<U>(string title, Func<T, U> f)
    {
        _p.EnqueueInherit(title, (s, ct) => VT.From((object?)f((T)s!)), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(string title, Func<T, Task<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(string title, Func<T, ValueTask<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(string title, Func<T, CancellationToken, Task<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(string title, Func<T, CancellationToken, ValueTask<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<U>(_p);
    }


    public ScenarioChain<U> But<U>(string title, Func<T, U> f)
    {
        _p.EnqueueInherit(title, (s, ct) => VT.From((object?)f((T)s!)), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(string title, Func<T, Task<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(string title, Func<T, ValueTask<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(string title, Func<T, CancellationToken, Task<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(string title, Func<T, CancellationToken, ValueTask<U>> f)
    {
        _p.EnqueueInherit(title, async (s, ct) => (object?)await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    // default title (transform)
    public ScenarioChain<U> And<U>(Func<T, U> f)
    {
        _p.EnqueueInherit("", (s, ct) => VT.From((object?)f((T)s!)), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(Func<T, Task<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(Func<T, ValueTask<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(Func<T, CancellationToken, Task<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> And<U>(Func<T, CancellationToken, ValueTask<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!, ct), StepWord.And);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(Func<T, U> f)
    {
        _p.EnqueueInherit("", (s, ct) => VT.From((object?)f((T)s!)), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(Func<T, Task<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(Func<T, ValueTask<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(Func<T, CancellationToken, Task<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    public ScenarioChain<U> But<U>(Func<T, CancellationToken, ValueTask<U>> f)
    {
        _p.EnqueueInherit("", async (s, ct) => (object?)await f((T)s!, ct), StepWord.But);
        return new ScenarioChain<U>(_p);
    }

    // side-effects keep T
    public ScenarioChain<T> And(string title, Action<T> effect)
    {
        _p.EnqueueInherit(title, (s, ct) =>
        {
            effect((T)s!);
            return VT.From(s);
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(string title, Func<T, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(string title, Func<T, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(string title, Func<T, CancellationToken, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(string title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> But(string title, Action<T> effect)
    {
        _p.EnqueueInherit(title, (s, ct) =>
        {
            effect((T)s!);
            return VT.From(s);
        }, StepWord.But);
        return this;
    }

    public ScenarioChain<T> But(string title, Func<T, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.But);
        return this;
    }

    public ScenarioChain<T> But(string title, Func<T, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.But);
        return this;
    }

    public ScenarioChain<T> But(string title, Func<T, CancellationToken, Task> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.But);
        return this;
    }

    public ScenarioChain<T> But(string title, Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit(title, async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.But);
        return this;
    }

    public ScenarioChain<T> And(Action<T> effect)
    {
        _p.EnqueueInherit("", (s, ct) =>
        {
            effect((T)s!);
            return VT.From(s);
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(Func<T, Task> effect)
    {
        _p.EnqueueInherit("", async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(Func<T, ValueTask> effect)
    {
        _p.EnqueueInherit("", async (s, ct) =>
        {
            await effect((T)s!);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(Func<T, CancellationToken, Task> effect)
    {
        _p.EnqueueInherit("", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    public ScenarioChain<T> And(Func<T, CancellationToken, ValueTask> effect)
    {
        _p.EnqueueInherit("", async (s, ct) =>
        {
            await effect((T)s!, ct);
            return s;
        }, StepWord.And);
        return this;
    }

    // BUT (you can mirror all the ANDs as needed)


    // ---------------- THEN → ThenChain<T> (await later) ----------------

    // explicit title
    
    public ThenChain<T> Then(string title, Func<bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, (s, ct) =>
        {
            AssertUtil.Ensure(predicate(), title);
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }
    
    public ThenChain<T> Then(string title, Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, (s, ct) =>
        {
            AssertUtil.Ensure(predicate((T)s!), title);
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }
    


    public ThenChain<T> Then(string title, Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            await assertion();
            return s;
        });
        return new ThenChain<T>(_p);
    }
    
    
    public ThenChain<T> Then<TOut>(string title, Func<T, Task<TOut>> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) => await assertion((T)s!));
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(string title, Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    // public ThenChain<T> Then(string title, Func<T, ValueTask> assertion)
    // {
    //     _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
    //     {
    //         await assertion((T)s!);
    //         return s;
    //     });
    //     return new ThenChain<T>(_p);
    // }
    
    
    public ThenChain<T> Then<TOut>(string title, Func<T, CancellationToken, Task<TOut>> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) => await assertion((T)s!, ct));
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(string title, Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    // public ThenChain<T> Then(string title, Func<T, CancellationToken, ValueTask> assertion)
    // {
    //     _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
    //     {
    //         await assertion((T)s!, ct);
    //         return s;
    //     });
    //     return new ThenChain<T>(_p);
    // }

    public ThenChain<T> Then(string title, Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            var result = await predicate((T)s!);
            AssertUtil.Ensure(result, title);
            return result;
        });
        return new ThenChain<T>(_p);
    }

    // public ThenChain<T> Then(string title, Func<T, ValueTask<bool>> predicate)
    // {
    //     _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
    //     {
    //         AssertUtil.Ensure(await predicate((T)s!), title);
    //         return s;
    //     });
    //     return new ThenChain<T>(_p);
    // }

    public ThenChain<T> Then(string title, Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(string title, Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, title, async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), title);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    // default title
    public ThenChain<T> Then(Func<T, bool> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", (s, ct) =>
        {
            AssertUtil.Ensure(predicate((T)s!), "Then");
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion();
            return s;
        });
        return new ThenChain<T>(_p);
    }



    public ThenChain<T> Then(Func<T, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion((T)s!);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, CancellationToken, Task> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, CancellationToken, ValueTask> assertion)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            await assertion((T)s!, ct);
            return s;
        });
        return new ThenChain<T>(_p);
    }
    
    public ThenChain<T> Then(Func<bool> predicate)
    {
        
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", (s, _) =>
        {
            AssertUtil.Ensure(predicate(), "Then");
            return VT.From(s);
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, CancellationToken, Task<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }

    public ThenChain<T> Then(Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        _p.Enqueue(StepPhase.Then, StepWord.Primary, "", async (s, ct) =>
        {
            AssertUtil.Ensure(await predicate((T)s!, ct), "Then");
            return s;
        });
        return new ThenChain<T>(_p);
    }
}

// ----------------------------- Bdd / Flow entry points -----------------------------

public static class Bdd
{
    public static ScenarioContext CreateContext(object featureSource, string? scenarioName = null, ITraitBridge? traits = null)
    {
        var featureAttr = featureSource.GetType().GetCustomAttribute<FeatureAttribute>();
        var featureName = featureAttr?.Name ?? featureSource.GetType().Name;
        var featureDesc = featureAttr?.Description;

        var method = FindCurrentTestMethod();
        var scenarioAttr = method?.GetCustomAttribute<ScenarioAttribute>();

        var resolvedScenarioName =
            !string.IsNullOrWhiteSpace(scenarioName) ? scenarioName :
            !string.IsNullOrWhiteSpace(scenarioAttr?.Name) ? scenarioAttr.Name! :
            method?.Name ?? "Scenario";

        var ctx = new ScenarioContext(featureName, featureDesc, resolvedScenarioName, traits ?? new NullTraitBridge());

        foreach (var tag in featureSource.GetType().GetCustomAttributes<TagAttribute>(inherit: true))
            ctx.AddTag(tag.Name);

        if (method is not null)
        {
            foreach (var tag in method.GetCustomAttributes<TagAttribute>(inherit: true))
                ctx.AddTag(tag.Name);
            if (scenarioAttr?.Tags is { Length: > 0 })
                foreach (var t in scenarioAttr.Tags)
                    ctx.AddTag(t);
        }

        return ctx;
    }

    private static MethodInfo? FindCurrentTestMethod()
    {
        var frames = new StackTrace().GetFrames();
        if (frames is null) return null;

        foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
            if (mi.GetCustomAttribute<ScenarioAttribute>() is not null)
                return mi;

        foreach (var mi in frames.Select(f => f.GetMethod()).OfType<MethodInfo>())
            if (HasAnyTestAttribute(mi))
                return mi;

        return null;
    }

    private static bool HasAnyTestAttribute(MethodInfo m)
    {
        var names = m.GetCustomAttributes(inherit: true).Select(a => a.GetType().FullName ?? "");
        foreach (var n in names)
            if (n is "Xunit.FactAttribute"
                or "Xunit.TheoryAttribute"
                or "NUnit.Framework.TestAttribute"
                or "NUnit.Framework.TestCaseAttribute"
                or "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                return true;
        return false;
    }

    // Entry points → ScenarioChain<T>

    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<T> setup)
        => ScenarioChain<T>.Seed(ctx, title, ct => VT.From(setup()));
    
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, ct => VT.From(setup()));
    
    
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, ct => setup());

    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<CancellationToken, Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, ct => VT.From(setup(ct)));

    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, string title, Func<CancellationToken, ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, title, setup);

    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<T> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", ct => VT.From(setup()));
    
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", ct => setup());
    
    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", ct => VT.From(setup()));

    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<CancellationToken, Task<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", ct => VT.From(setup(ct)));

    public static ScenarioChain<T> Given<T>(ScenarioContext ctx, Func<CancellationToken, ValueTask<T>> setup)
        => ScenarioChain<T>.Seed(ctx, $"Given {typeof(T).Name}", setup);
}

public static class Flow
{
    public static ScenarioChain<T> Given<T>(string title, Func<T> setup)
        => Bdd.Given(Require(), title, setup);
   
    public static ScenarioChain<T> Given<T>(string title, Func<ValueTask<T>> setup)
        => Bdd.Given(Require(), title, setup);
    
    public static ScenarioChain<T> Given<T>(string title, Func<CancellationToken,ValueTask<T>> setup)
        => Bdd.Given(Require(), title, setup);
    
    public static ScenarioChain<T> Given<T>(string title, Func<Task<T>> setup)
        => Bdd.Given(Require(), title, setup);

    public static ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), title, setup);

    public static ScenarioChain<T> Given<T>(Func<T> setup)
        => Bdd.Given(Require(), setup);
    
    
    public static ScenarioChain<T> Given<T>(Func<ValueTask<T>> setup)
        => Bdd.Given(Require(), setup);
    
    public static ScenarioChain<T> Given<T>(Func<Task<T>> setup)
        => Bdd.Given(Require(), setup);

    public static ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), setup);

    public static FromContext From(ScenarioContext ctx) => new(ctx);

    private static ScenarioContext Require()
        => Ambient.Current.Value ?? throw new InvalidOperationException(
            "TinyBDD ambient ScenarioContext not set. Inherit from TinyBdd*Base or set Ambient.Current manually.");
}

public readonly struct FromContext(ScenarioContext ctx)
{
    public ScenarioChain<T> Given<T>(string title, Func<T> setup) => Bdd.Given(ctx, title, setup);
    public ScenarioChain<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, title, setup);
    public ScenarioChain<T> Given<T>(Func<T> setup) => Bdd.Given(ctx, setup);
    public ScenarioChain<T> Given<T>(Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, setup);
}