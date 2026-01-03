using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Compares direct delegate invocation vs TinyBDD step pipeline dispatch.
/// Measures the overhead of TinyBDD's step execution infrastructure.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class StepPipelineBenchmark
{
    private ScenarioContext? _context;
    private Func<int, int> _transform = x => x + 1;
    private Func<int, Task<int>> _asyncTransform = x => Task.FromResult(x + 1);

    [GlobalSetup]
    public void Setup()
    {
        _context = new ScenarioContext(
            featureName: "Benchmark",
            featureDescription: null,
            scenarioName: "StepPipeline",
            traitBridge: new NullTraitBridge(),
            options: new ScenarioOptions()
        );
    }

    /// <summary>
    /// Baseline: Direct synchronous delegate invocation.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int Baseline_DirectInvoke()
    {
        int value = 10;
        value = _transform(value);
        value = _transform(value);
        value = _transform(value);
        return value;
    }

    /// <summary>
    /// Baseline: Direct async delegate invocation.
    /// </summary>
    [Benchmark]
    public async Task<int> Baseline_DirectInvokeAsync()
    {
        int value = 10;
        value = await _asyncTransform(value);
        value = await _asyncTransform(value);
        value = await _asyncTransform(value);
        return value;
    }

    /// <summary>
    /// TinyBDD: Steps dispatched through TinyBDD pipeline.
    /// </summary>
    [Benchmark]
    public async Task<int> TinyBDD_PipelineDispatch()
    {
        await Bdd.Given(_context!, "start", () => 10)
            .When("transform 1", x => x + 1)
            .And("transform 2", x => x + 1)
            .And("transform 3", x => x + 1)
            .Then("result", v => v > 0);
        
        return _context!.CurrentItem as int? ?? 0;
    }
}
