using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Compares multiple assertions baseline vs TinyBDD multiple steps.
/// Measures TinyBDD overhead as the number of assertions/steps increases.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class MultipleAssertionsBenchmark
{
    private ScenarioContext? _context;
    private int[] _values = Array.Empty<int>();

    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _values = Enumerable.Range(0, N).ToArray();
        _context = new ScenarioContext(
            featureName: "Benchmark",
            featureDescription: null,
            scenarioName: "MultipleAssertions",
            traitBridge: new NullTraitBridge(),
            options: new ScenarioOptions()
        );
    }

    /// <summary>
    /// Baseline: Direct loop with assertions, no TinyBDD.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool Baseline_MultipleAssertions()
    {
        foreach (var value in _values)
        {
            var result = value >= 0;
            if (!result)
                throw new InvalidOperationException($"Assertion failed for {value}");
        }
        return true;
    }

    /// <summary>
    /// TinyBDD: Multiple Then steps in a chain.
    /// </summary>
    [Benchmark]
    public async Task<bool> TinyBDD_MultipleAssertions()
    {
        var chain = Bdd.Given(_context!, "array of values", () => _values)
            .When("validate all", arr =>
            {
                foreach (var value in arr)
                {
                    if (value < 0)
                        throw new InvalidOperationException($"Invalid value: {value}");
                }
                return arr;
            });
        
        await chain.Then("all valid", arr => arr.All(v => v >= 0));
        return true;
    }
}
