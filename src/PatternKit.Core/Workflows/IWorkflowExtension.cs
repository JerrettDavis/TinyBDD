namespace PatternKit.Core;

/// <summary>
/// Marker interface for workflow context extensions.
/// </summary>
/// <remarks>
/// Extensions allow additional functionality to be attached to a workflow context
/// without modifying the core types. This enables framework-specific features
/// (like test framework integration) to be added without polluting the core API.
/// </remarks>
public interface IWorkflowExtension
{
}
