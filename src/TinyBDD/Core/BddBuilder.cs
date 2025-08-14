namespace TinyBDD;

public sealed class GivenBuilder<TGiven>
{
    internal readonly ScenarioContext _ctx;
    internal readonly string _title;
    internal readonly Func<CancellationToken, Task<TGiven>> _fn;

    internal GivenBuilder(ScenarioContext ctx, string title, Func<CancellationToken, Task<TGiven>> fn)
    {
        _ctx = ctx;
        _title = title;
        _fn = fn;
    }
}

public sealed class WhenBuilder<TGiven>
{
    internal readonly ScenarioContext _ctx;
    internal readonly TGiven _given;
    internal readonly string _title;
    internal readonly Func<TGiven, CancellationToken, Task> _fn;

    internal WhenBuilder(ScenarioContext ctx, TGiven given, string title, Func<TGiven, CancellationToken, Task> fn)
    {
        _ctx = ctx;
        _given = given;
        _title = title;
        _fn = fn;
    }
}

public sealed class WhenBuilder<TGiven, TOut>
{
    internal readonly ScenarioContext _ctx;
    internal readonly TGiven _given;
    internal readonly string _title;
    internal readonly Func<TGiven, CancellationToken, Task<TOut>> _fn;

    internal WhenBuilder(ScenarioContext ctx, TGiven given, string title, Func<TGiven, CancellationToken, Task<TOut>> fn)
    {
        _ctx = ctx;
        _given = given;
        _title = title;
        _fn = fn;
    }
}

public sealed class ThenBuilder
{
    internal ScenarioContext Ctx { get; }
    internal ThenBuilder(ScenarioContext ctx) => Ctx = ctx;
}

public sealed class ThenBuilder<T>
{
    internal ScenarioContext Ctx { get; }
    internal T Value { get; }

    internal ThenBuilder(ScenarioContext ctx, T value)
    {
        Ctx = ctx;
        Value = value;
    }
}