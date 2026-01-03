using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Compares exception handling baseline vs TinyBDD step failure handling.
/// Measures overhead of TinyBDD's error capture and reporting.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class ExceptionHandlingBenchmark
{
    private ScenarioContext? _context;

    [GlobalSetup]
    public void Setup()
    {
        _context = new ScenarioContext(
            featureName: "Benchmark",
            featureDescription: null,
            scenarioName: "ExceptionHandling",
            traitBridge: new NullTraitBridge(),
            options: new ScenarioOptions()
        );
    }

    /// <summary>
    /// Baseline: Try-catch with direct assertion throw.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool Baseline_TryCatchException()
    {
        try
        {
            var value = 42;
            if (value != 100) // Will fail
                throw new InvalidOperationException("Value mismatch");
            return true;
        }
        catch (InvalidOperationException)
        {
            // Swallow expected exception
            return false;
        }
    }

    /// <summary>
    /// TinyBDD: Step failure with exception capture in pipeline.
    /// </summary>
    [Benchmark]
    public async Task<bool> TinyBDD_StepFailure()
    {
        try
        {
            await Bdd.Given(_context!, "value", () => 42)
                .Then("should be 100", v => v == 100);
            return true;
        }
        catch
        {
            // Swallow expected assertion failure
            return false;
        }
    }

    /// <summary>
    /// Baseline: Success path with no exception.
    /// </summary>
    [Benchmark]
    public bool Baseline_SuccessPath()
    {
        var value = 42;
        var result = value == 42;
        if (!result)
            throw new InvalidOperationException("Assertion failed");
        return true;
    }

    /// <summary>
    /// TinyBDD: Success path with no exception.
    /// </summary>
    [Benchmark]
    public async Task<bool> TinyBDD_SuccessPath()
    {
        await Bdd.Given(_context!, "value", () => 42)
            .Then("should be 42", v => v == 42);
        return true;
    }
}
