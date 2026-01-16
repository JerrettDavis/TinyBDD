using TinyBDD.Extensions.FileBased.Core;
using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Execution;

/// <summary>
/// Executes file-based scenarios using an application driver.
/// </summary>
public sealed class ScenarioExecutor
{
    private readonly IApplicationDriver _driver;
    private readonly StepResolver _stepResolver;

    public ScenarioExecutor(IApplicationDriver driver, Type driverType)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _stepResolver = new StepResolver(driverType);
    }

    /// <summary>
    /// Executes a scenario definition as a TinyBDD scenario.
    /// </summary>
    /// <param name="feature">The feature containing the scenario.</param>
    /// <param name="scenario">The scenario to execute.</param>
    /// <param name="context">The scenario context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteAsync(
        FeatureDefinition feature,
        ScenarioDefinition scenario,
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        if (feature == null) throw new ArgumentNullException(nameof(feature));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (context == null) throw new ArgumentNullException(nameof(context));

        // Add tags from feature and scenario
        foreach (var tag in feature.Tags.Concat(scenario.Tags))
        {
            context.AddTag(tag);
        }

        await _driver.InitializeAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            object? state = null;
            StepPhase currentPhase = StepPhase.Given;
            string? lastKeyword = null;

            foreach (var step in scenario.Steps)
            {
                // Determine phase from keyword
                currentPhase = DeterminePhase(step.Keyword, lastKeyword, currentPhase);
                lastKeyword = step.Keyword;

                // Resolve step to driver method
                if (!_stepResolver.TryResolve(step, out var methodInfo, out var arguments))
                {
                    throw new InvalidOperationException(
                        $"No driver method found for step: {step.Keyword} {step.Text}");
                }

                // Execute the step
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                Exception? stepError = null;

                try
                {
                    var result = methodInfo!.Method.Invoke(_driver, arguments);
                    
                    // Handle async methods
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                        
                        // Get result value for Task<T>
                        var resultType = task.GetType();
                        if (resultType.IsGenericType)
                        {
                            var resultProperty = resultType.GetProperty("Result");
                            state = resultProperty?.GetValue(task);
                        }
                    }
                    else
                    {
                        state = result;
                    }

                    // For Then steps, if the result is a boolean, treat false as an assertion failure
                    if (currentPhase == StepPhase.Then && state is bool boolResult && !boolResult)
                    {
                        throw new InvalidOperationException($"Assertion failed: {step.Text}");
                    }
                }
                catch (Exception ex)
                {
                    var actualException = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                    stepError = actualException;
                }
                finally
                {
                    stopwatch.Stop();
                    
                    var stepResult = new StepResult
                    {
                        Kind = GetStepKind(currentPhase, MapKeywordToStepWord(step.Keyword)),
                        Title = step.Text,
                        Elapsed = stopwatch.Elapsed,
                        Error = stepError
                    };

                    context.AddStep(stepResult);

                    if (stepError != null)
                    {
                        throw new BddStepException($"Step failed: {step.Keyword} {step.Text}", context, stepError);
                    }
                }
            }
        }
        finally
        {
            await _driver.CleanupAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static StepPhase DeterminePhase(string keyword, string? lastKeyword, StepPhase currentPhase)
    {
        var normalizedKeyword = keyword.Trim().ToUpperInvariant();

        return normalizedKeyword switch
        {
            "GIVEN" => StepPhase.Given,
            "WHEN" => StepPhase.When,
            "THEN" => StepPhase.Then,
            "AND" or "BUT" => currentPhase, // Inherit from previous
            _ => currentPhase
        };
    }

    private static StepWord MapKeywordToStepWord(string keyword)
    {
        var normalizedKeyword = keyword.Trim().ToUpperInvariant();

        return normalizedKeyword switch
        {
            "GIVEN" => StepWord.Primary,
            "WHEN" => StepWord.Primary,
            "THEN" => StepWord.Primary,
            "AND" => StepWord.And,
            "BUT" => StepWord.But,
            _ => StepWord.Primary
        };
    }

    private static string GetStepKind(StepPhase phase, StepWord word)
    {
        if (word == StepWord.And) return "And";
        if (word == StepWord.But) return "But";

        return phase switch
        {
            StepPhase.Given => "Given",
            StepPhase.When => "When",
            StepPhase.Then => "Then",
            _ => phase.ToString()
        };
    }
}
