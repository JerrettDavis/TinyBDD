using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace PatternKit.Core;

/// <summary>
/// Holds execution state for a single workflow run.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="WorkflowContext"/> is the central container for all state and metadata
/// during workflow execution. It tracks:
/// </para>
/// <list type="bullet">
///   <item><description>Workflow identity (name, description, execution ID)</description></item>
///   <item><description>Step results and input/output history</description></item>
///   <item><description>Current value flowing through the pipeline</description></item>
///   <item><description>Custom metadata and extensions</description></item>
/// </list>
/// <para>
/// Extensions can be attached to add framework-specific functionality without
/// modifying the core context type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = new WorkflowContext
/// {
///     WorkflowName = "Order Processing",
///     Description = "Processes incoming customer orders"
/// };
///
/// ctx.SetMetadata("orderId", orderId);
/// ctx.SetExtension(new LoggingExtension(logger));
/// </code>
/// </example>
public class WorkflowContext : IAsyncDisposable
{
    private readonly List<StepResult> _steps = new();
    private readonly List<StepIO> _io = new();
    private readonly ConcurrentDictionary<string, object?> _metadata = new();
    private readonly ConcurrentDictionary<Type, IWorkflowExtension> _extensions = new();
    private readonly List<Func<ValueTask>> _disposeActions = new();

    /// <summary>
    /// Gets the name identifying this workflow.
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// Gets an optional human-readable description of this workflow.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a unique identifier for this execution instance.
    /// </summary>
    public string ExecutionId { get; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Gets the list of recorded step results in execution order.
    /// </summary>
    public IReadOnlyList<StepResult> Steps => _steps;

    /// <summary>
    /// Gets the running log of per-step inputs and outputs.
    /// </summary>
    public IReadOnlyList<StepIO> IO => _io;

    /// <summary>
    /// Gets or sets the current value flowing through the workflow.
    /// </summary>
    /// <remarks>
    /// This is updated by the execution pipeline after each successful step.
    /// </remarks>
    public object? CurrentValue { get; internal set; }

    /// <summary>
    /// Gets the custom metadata dictionary for this execution.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;

    /// <summary>
    /// Gets the workflow options controlling execution behavior.
    /// </summary>
    public WorkflowOptions Options { get; init; } = WorkflowOptions.Default;

    /// <summary>
    /// Adds a recorded step result.
    /// </summary>
    /// <param name="result">The step result to add.</param>
    internal void AddStep(StepResult result) => _steps.Add(result);

    /// <summary>
    /// Records the input/output for a step.
    /// </summary>
    /// <param name="io">The step IO to add.</param>
    internal void AddIO(StepIO io) => _io.Add(io);

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMetadata(string key, object? value) => _metadata[key] = value;

    /// <summary>
    /// Gets a metadata value.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The metadata key.</param>
    /// <returns>The value if found and of correct type; otherwise <see langword="null"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetMetadata<T>(string key) where T : class
        => _metadata.TryGetValue(key, out var value) && value is T typed ? typed : null;

    /// <summary>
    /// Tries to get a metadata value.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The value if found.</param>
    /// <returns><see langword="true"/> if found; otherwise <see langword="false"/>.</returns>
    public bool TryGetMetadata<T>(string key, out T? value) where T : class
    {
        if (_metadata.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Sets an extension on this context.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">The extension instance.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetExtension<T>(T extension) where T : class, IWorkflowExtension
        => _extensions[typeof(T)] = extension;

    /// <summary>
    /// Gets an extension from this context.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>The extension if found; otherwise <see langword="null"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetExtension<T>() where T : class, IWorkflowExtension
        => _extensions.TryGetValue(typeof(T), out var ext) ? ext as T : null;

    /// <summary>
    /// Registers a disposal action to run when this context is disposed.
    /// </summary>
    /// <param name="disposeAction">The async action to run on disposal.</param>
    public void OnDispose(Func<ValueTask> disposeAction) => _disposeActions.Add(disposeAction);

    /// <summary>
    /// Gets a value indicating whether all recorded steps passed.
    /// </summary>
    public bool AllPassed => _steps.All(s => s.Passed);

    /// <summary>
    /// Gets the first step that failed, if any.
    /// </summary>
    public StepResult? FirstFailure => _steps.FirstOrDefault(s => !s.Passed);

    /// <summary>
    /// Disposes this context and runs any registered disposal actions.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var action in _disposeActions)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch
            {
                // Suppress disposal errors to allow all handlers to run
            }
        }

        _disposeActions.Clear();
        _metadata.Clear();
        _extensions.Clear();
        GC.SuppressFinalize(this);
    }
}
