using System.Runtime.CompilerServices;

namespace PatternKit.Core;

/// <summary>
/// Entry points for creating workflows using fluent Given/When/Then chains.
/// </summary>
/// <remarks>
/// <para>
/// Use this static API to start a workflow with a <see cref="WorkflowContext"/>.
/// Begin with one of the <c>Given</c> overloads to establish initial state,
/// then chain through <c>When</c> for actions and <c>Then</c> for assertions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = new WorkflowContext { WorkflowName = "Calculator" };
///
/// await Workflow.Given(ctx, "numbers", () => new[] { 1, 2, 3 })
///               .When("sum", arr => arr.Sum())
///               .Then("> 0", total => total > 0);
/// </code>
/// </example>
public static class Workflow
{
    #region Given - Factory overloads

    /// <summary>
    /// Starts a <c>Given</c> step with a synchronous factory.
    /// </summary>
    /// <typeparam name="T">The type produced by the factory.</typeparam>
    /// <param name="ctx">Workflow context.</param>
    /// <param name="title">Human-friendly step title.</param>
    /// <param name="setup">Synchronous factory function.</param>
    /// <returns>A <see cref="WorkflowChain{T}"/> for further chaining.</returns>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, Func<T> setup)
        => Seed(ctx, title, Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with an async Task factory.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, Func<Task<T>> setup)
        => Seed(ctx, title, Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with an async ValueTask factory.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, Func<ValueTask<T>> setup)
        => Seed(ctx, title, Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with a token-aware Task factory.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, Func<CancellationToken, Task<T>> setup)
        => Seed(ctx, title, Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with a token-aware ValueTask factory.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, Func<CancellationToken, ValueTask<T>> setup)
        => Seed(ctx, title, setup);

    #endregion

    #region Given - Value overloads

    /// <summary>
    /// Starts a <c>Given</c> step with a pre-existing value.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, T value)
        => Seed(ctx, title, _ => new ValueTask<T>(value));

    /// <summary>
    /// Starts a <c>Given</c> step with an action and seed value.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, string title, Action setup, T seed)
        => Seed(ctx, title, Wrap(setup, seed));

    #endregion

    #region Given - State-passing overloads

    /// <summary>
    /// Starts a <c>Given</c> step with state, avoiding closure allocation.
    /// </summary>
    public static WorkflowChain<T> Given<TState, T>(WorkflowContext ctx, string title, TState state, Func<TState, T> setup)
        => Seed(ctx, title, Wrap(state, setup));

    /// <summary>
    /// Starts a <c>Given</c> step with state using async Task.
    /// </summary>
    public static WorkflowChain<T> Given<TState, T>(WorkflowContext ctx, string title, TState state, Func<TState, Task<T>> setup)
        => Seed(ctx, title, Wrap(state, setup));

    /// <summary>
    /// Starts a <c>Given</c> step with state using async ValueTask.
    /// </summary>
    public static WorkflowChain<T> Given<TState, T>(WorkflowContext ctx, string title, TState state, Func<TState, ValueTask<T>> setup)
        => Seed(ctx, title, Wrap(state, setup));

    /// <summary>
    /// Starts a <c>Given</c> step with state using token-aware Task.
    /// </summary>
    public static WorkflowChain<T> Given<TState, T>(WorkflowContext ctx, string title, TState state, Func<TState, CancellationToken, Task<T>> setup)
        => Seed(ctx, title, Wrap(state, setup));

    /// <summary>
    /// Starts a <c>Given</c> step with state using token-aware ValueTask.
    /// </summary>
    public static WorkflowChain<T> Given<TState, T>(WorkflowContext ctx, string title, TState state, Func<TState, CancellationToken, ValueTask<T>> setup)
        => Seed(ctx, title, Wrap(state, setup));

    #endregion

    #region Given - Auto-title overloads

    /// <summary>
    /// Starts a <c>Given</c> step with auto-generated title.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, Func<T> setup)
        => Seed(ctx, AutoTitle<T>(), Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with auto-generated title using async Task.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, Func<Task<T>> setup)
        => Seed(ctx, AutoTitle<T>(), Wrap(setup));

    /// <summary>
    /// Starts a <c>Given</c> step with auto-generated title using async ValueTask.
    /// </summary>
    public static WorkflowChain<T> Given<T>(WorkflowContext ctx, Func<ValueTask<T>> setup)
        => Seed(ctx, AutoTitle<T>(), Wrap(setup));

    #endregion

    #region Internal helpers

    private static WorkflowChain<T> Seed<T>(
        WorkflowContext ctx,
        string title,
        Func<CancellationToken, ValueTask<T>> setup)
        => WorkflowChain<T>.Seed(ctx, title, setup);

    private static string AutoTitle<T>() => $"Given {typeof(T).Name}";

    // Wrap overloads - normalize various factory shapes
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<T> f)
        => _ => new ValueTask<T>(f());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<Task<T>> f)
        => _ => new ValueTask<T>(f());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<ValueTask<T>> f)
        => _ => f();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Func<CancellationToken, Task<T>> f)
        => ct => new ValueTask<T>(f(ct));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<T>(Action setup, T seed)
        => _ =>
        {
            setup();
            return new ValueTask<T>(seed);
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<TState, T>(TState state, Func<TState, T> setup)
        => _ => new ValueTask<T>(setup(state));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<TState, T>(TState state, Func<TState, Task<T>> setup)
        => _ => new ValueTask<T>(setup(state));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<TState, T>(TState state, Func<TState, ValueTask<T>> setup)
        => _ => setup(state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<TState, T>(TState state, Func<TState, CancellationToken, Task<T>> setup)
        => ct => new ValueTask<T>(setup(state, ct));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<CancellationToken, ValueTask<T>> Wrap<TState, T>(TState state, Func<TState, CancellationToken, ValueTask<T>> setup)
        => ct => setup(state, ct);

    #endregion
}
