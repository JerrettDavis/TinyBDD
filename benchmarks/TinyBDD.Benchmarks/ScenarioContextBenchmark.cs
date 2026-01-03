using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Compares dictionary access (baseline) vs TinyBDD ScenarioContext data flow.
/// Measures overhead of TinyBDD's state management.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class ScenarioContextBenchmark
{
    private Dictionary<string, object> _dict = new();
    private ScenarioContext? _context;

    [Params(10, 100)]
    public int Operations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _dict = new Dictionary<string, object>();
        _context = new ScenarioContext(
            featureName: "Benchmark",
            featureDescription: null,
            scenarioName: "ContextAccess",
            traitBridge: new NullTraitBridge(),
            options: new ScenarioOptions()
        );
    }

    /// <summary>
    /// Baseline: Dictionary reads and writes.
    /// </summary>
    [Benchmark(Baseline = true)]
    public object Baseline_DictionaryAccess()
    {
        object? result = null;
        for (int i = 0; i < Operations; i++)
        {
            _dict[$"key_{i}"] = i;
            result = _dict[$"key_{i}"];
        }
        return result!;
    }

    /// <summary>
    /// TinyBDD: Chain transformations passing state through pipeline.
    /// </summary>
    [Benchmark]
    public async Task<int> TinyBDD_ContextFlow()
    {
        var chain = Bdd.Given(_context!, "initial value", () => 0);
        
        for (int i = 0; i < Operations; i++)
        {
            var localI = i;
            chain = chain.And($"transform {localI}", prev => prev + localI);
        }
        
        await chain.Then("final value", v => v >= 0);
        return _context!.CurrentItem as int? ?? 0;
    }
}
