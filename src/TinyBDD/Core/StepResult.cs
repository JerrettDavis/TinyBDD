namespace TinyBDD;

public sealed class StepResult
{
    public required string Kind { get; init; }     // Given, When, Then, And, But
    public required string Title { get; init; }
    public TimeSpan Elapsed { get; init; }
    public Exception? Error { get; init; }
}
