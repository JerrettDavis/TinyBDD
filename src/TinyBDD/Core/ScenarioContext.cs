using TinyBDD.Extensions;

namespace TinyBDD;

/// <summary>
/// Holds feature, scenario and step information during a single BDD run.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ScenarioContext"/> is created via <see cref="Bdd.CreateContext(object,string,ITraitBridge,ScenarioOptions)"/>
/// or set as ambient using <see cref="Ambient.Current"/> when using the <see cref="Flow"/> API.
/// </para>
/// <para>
/// Test adapters use <see cref="TraitBridge"/> to register tags/categories with the underlying
/// framework while steps are recorded in <see cref="Steps"/> as the scenario executes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ctx = Bdd.CreateContext(this);
/// await Bdd.Given(ctx, "seed", () => 1)
///          .When("+1", x => x + 1)
///          .Then("== 2", v => v == 2);
///
/// ctx.AssertPassed();
/// foreach (var step in ctx.Steps)
///     Console.WriteLine($"{step.Kind} {step.Title}: {(step.Error is null ? "OK" : step.Error.Message)}");
/// </code>
/// </example>
/// <seealso cref="Bdd"/>
/// <seealso cref="Flow"/>
/// <seealso cref="FeatureAttribute"/>
/// <seealso cref="ScenarioAttribute"/>
/// <seealso cref="TagAttribute"/>
public sealed class ScenarioContext
{
    /// <summary>The logical feature under test, e.g. a capability in your system.</summary>
    public string FeatureName { get; }

    /// <summary>Optional human-readable feature description.</summary>
    public string? FeatureDescription { get; }

    /// <summary>The specific scenario name, typically the test method name or <see cref="ScenarioAttribute.Name"/>.</summary>
    public string ScenarioName { get; }

    /// <summary>All tags attached via <see cref="TagAttribute"/> on class/method or <see cref="ScenarioAttribute.Tags"/>.</summary>
    public IReadOnlyCollection<string> Tags => _tags.ToList();

    /// <summary>All recorded steps in execution order.</summary>
    public IReadOnlyList<StepResult> Steps => _steps;

    /// <summary>Running log of per-step inputs and outputs.</summary>
    public IReadOnlyList<StepIO> IO => _io;

    /// <summary>The current carried item (latest successful state).</summary>
    public object? CurrentItem { get; internal set; }

    private readonly List<StepResult> _steps = new(capacity: 8); // Pre-allocate for typical scenario
    private readonly HashSet<string> _tags = new(StringComparer.Ordinal); // Use ordinal comparison for performance
    private readonly List<StepIO> _io = new(capacity: 8); // Pre-allocate for typical scenario

    /// <summary>
    /// Bridge for integrating tags/categories with a host test framework.
    /// </summary>
    public ITraitBridge TraitBridge { get; }

    /// <summary>
    /// Scenario options. See <see cref="ScenarioOptions"/>.
    /// </summary>   
    public ScenarioOptions Options { get; }

    /// <summary>
    /// Creates a new scenario context.
    /// </summary>
    /// <param name="featureName">Feature name.</param>
    /// <param name="featureDescription">Optional feature description.</param>
    /// <param name="scenarioName">Scenario name.</param>
    /// <param name="traitBridge">Bridge for traits/categories.</param>
    /// <param name="options">Scenario options.</param>
    public ScenarioContext(
        string featureName,
        string? featureDescription,
        string scenarioName,
        ITraitBridge traitBridge,
        ScenarioOptions options)
    {
        FeatureName = featureName;
        FeatureDescription = featureDescription;
        ScenarioName = scenarioName;
        TraitBridge = traitBridge;
        Options = options;
    }

    /// <summary>
    /// Adds a tag to the scenario and forwards it to <see cref="TraitBridge"/>.
    /// </summary>
    /// <param name="tag">The tag name to add.</param>
    public void AddTag(string tag)
    {
        _tags.Add(tag);
        TraitBridge.AddTag(tag);
    }

    /// <summary>   
    /// Adds one or more tags to the scenario context in bulk.
    /// </summary>
    /// <param name="tags">Array of string values representing the tags to be added. Each tag must be a non-empty string.</param>
    public void AddTags(params string[] tags) => Array.ForEach(tags, AddTag);

    /// <summary>
    /// Adds one or more tags to the scenario context in bulk.
    /// </summary>
    /// <param name="tags">An enumerable collection of string values representing the tags to be added. Each tag must be a non-empty string.</param>   
    public void AddTags(IEnumerable<string> tags) => AddTags(tags.ToArray());

    /// <summary>
    /// Adds a recorded step to <see cref="Steps"/>. Intended for internal use by the framework.
    /// </summary>
    internal void AddStep(StepResult s) => _steps.Add(s);

    /// <summary>
    /// Records input/output for a step. Intended for internal use by the framework.
    /// </summary>
    internal void AddIO(StepIO io) => _io.Add(io);
}

public class ScenarioContextPrototype
{
    public string? FeatureName { get; set; }
    public string? FeatureDescription { get; set; }
    public string? ScenarioName { get; set; }
    public List<string> Tags { get; } = [];
    public ITraitBridge? TraitBridge { get; set; }
    public ScenarioOptions? Options { get; set; }

    private void ValidateFeatureName()
        => Throw.ValidationExceptionIf(
            string.IsNullOrWhiteSpace(FeatureName),
            "Feature name must be specified.");

    private void ValidateScenarioName()
        => Throw.ValidationExceptionIf(
            string.IsNullOrWhiteSpace(ScenarioName),
            "Scenario name must be specified.");

    private void ValidateTraitBridge()
        => Throw.ValidationExceptionIf(
            TraitBridge is null,
            "Trait bridge must be specified.");

    private void ValidateOptions()
        => Throw.ValidationExceptionIf(
            Options is null,
            "Scenario options must be specified.");

    public void Validate()
    {
        ValidateFeatureName();
        ValidateScenarioName();
        ValidateTraitBridge();
        ValidateOptions();
    }
}