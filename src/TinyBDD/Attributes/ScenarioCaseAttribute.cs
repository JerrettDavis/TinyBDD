namespace TinyBDD;

/// <summary>
/// Specifies a set of values for a data-driven scenario.
/// Multiple instances of this attribute can be applied to a single test method
/// to execute the scenario with different data sets.
/// </summary>
/// <remarks>
/// <para>
/// This attribute works similarly to xUnit's <c>[InlineData]</c> or NUnit's <c>[TestCase]</c>,
/// but is specifically designed for TinyBDD scenarios. Each attribute instance defines
/// one row of test data.
/// </para>
/// <para>
/// When used with the TinyBDD framework adapters, the test will be executed once
/// for each <see cref="ScenarioCaseAttribute"/> applied to the method.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Scenario("Adding numbers")]
/// [ScenarioCase(1, 2, 3)]
/// [ScenarioCase(5, 5, 10)]
/// [ScenarioCase(-1, 1, 0)]
/// public async Task AdditionScenario(int a, int b, int expected)
/// {
///     await Given($"{a} and {b}", () => (a, b))
///         .When("added", x => x.a + x.b)
///         .Then($"equals {expected}", sum => sum == expected)
///         .AssertPassed();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ScenarioCaseAttribute : Attribute
{
    /// <summary>
    /// Gets the values for this scenario case.
    /// </summary>
    public object?[] Values { get; }

    /// <summary>
    /// Gets or sets an optional display name for this case.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioCaseAttribute"/> class
    /// with the specified test values.
    /// </summary>
    /// <param name="values">The values to pass to the test method.</param>
    public ScenarioCaseAttribute(params object?[] values)
    {
        Values = values ?? [];
    }

    /// <summary>
    /// Returns a string representation of the scenario case values.
    /// </summary>
    public override string ToString() =>
        DisplayName ?? $"({string.Join(", ", Values.Select(v => v?.ToString() ?? "null"))})";
}
