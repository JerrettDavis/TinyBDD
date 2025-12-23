namespace TinyBDD;

/// <summary>
/// Represents a single row of example data for data-driven scenarios.
/// </summary>
/// <typeparam name="TExample">The type of example data.</typeparam>
public sealed class ExampleRow<TExample>
{
    /// <summary>
    /// Gets the zero-based index of this example in the examples collection.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the example data for this row.
    /// </summary>
    public TExample Data { get; }

    /// <summary>
    /// Gets an optional label for this example row, used in reporting.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleRow{TExample}"/> class.
    /// </summary>
    /// <param name="index">The zero-based index of this example.</param>
    /// <param name="data">The example data.</param>
    /// <param name="label">An optional label for reporting.</param>
    internal ExampleRow(int index, TExample data, string? label = null)
    {
        Index = index;
        Data = data;
        Label = label;
    }

    /// <summary>
    /// Returns a string representation of this example row.
    /// </summary>
    public override string ToString() =>
        Label ?? $"Example {Index + 1}: {Data}";
}
