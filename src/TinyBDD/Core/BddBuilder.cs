namespace TinyBDD;

/// <summary>
/// Represents the state after a <c>Given</c> step and before a <c>When</c> step.
/// Instances are produced by <see cref="Bdd.Given{T}(ScenarioContext, string, Func{T})"/> and consumed by
/// fluent extension methods in <see cref="BddFluentTaskExtensions"/>.
/// </summary>
/// <typeparam name="TGiven">The type produced by the <c>Given</c> step.</typeparam>
public sealed class GivenBuilder<TGiven>
{
    internal readonly ScenarioContext Ctx;
    internal readonly string Title;
    internal readonly Func<CancellationToken, Task<TGiven>> Fn;

    internal GivenBuilder(
        ScenarioContext ctx, 
        string title, 
        Func<CancellationToken, Task<TGiven>> fn)
    {
        Ctx = ctx;
        Title = title;
        Fn = fn;
    }
}

/// <summary>
/// Represents the state after a non-transforming <c>When</c> step (side-effect) and before assertions.
/// </summary>
/// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
public sealed class WhenBuilder<TGiven>
{
    internal readonly ScenarioContext Ctx;
    internal readonly TGiven Given;
    internal readonly string Title;
    internal readonly Func<TGiven, CancellationToken, Task> Fn;

    internal WhenBuilder(
        ScenarioContext ctx, 
        TGiven given, 
        string title, 
        Func<TGiven, CancellationToken, Task> fn)
    {
        Ctx = ctx;
        Given = given;
        Title = title;
        Fn = fn;
    }
}

/// <summary>
/// Represents the state after a transforming <c>When</c> step and before assertions.
/// </summary>
/// <typeparam name="TGiven">The type produced by the preceding <c>Given</c> step.</typeparam>
/// <typeparam name="TOut">The type produced by the <c>When</c> step.</typeparam>
public sealed class WhenBuilder<TGiven, TOut>
{
    internal readonly ScenarioContext Ctx;
    internal readonly TGiven Given;
    internal readonly string Title;
    internal readonly Func<TGiven, CancellationToken, Task<TOut>> Fn;

    internal WhenBuilder(
        ScenarioContext ctx, 
        TGiven given, 
        string title, 
        Func<TGiven, CancellationToken, Task<TOut>> fn)
    {
        Ctx = ctx;
        Given = given;
        Title = title;
        Fn = fn;
    }
}

/// <summary>
/// Represents a point in the chain where untyped assertions (<c>Then</c>/<c>And</c>/<c>But</c>) can follow.
/// </summary>
public sealed class ThenBuilder
{
    internal ScenarioContext Ctx { get; }
    internal ThenBuilder(ScenarioContext ctx) => Ctx = ctx;
}

/// <summary>
/// Represents a point in the chain where typed assertions operate on a produced value.
/// </summary>
/// <typeparam name="T">The value type carried forward to assertions.</typeparam>
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