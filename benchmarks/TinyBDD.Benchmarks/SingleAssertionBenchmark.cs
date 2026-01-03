using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Compares a single assertion baseline vs TinyBDD single step.
/// Measures the overhead of TinyBDD's DSL for the simplest case.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class SingleAssertionBenchmark
{
    private ScenarioContext? _context;
    private int _value;

    [GlobalSetup]
    public void Setup()
    {
        _value = 42;
        _context = new ScenarioContext(
            featureName: "Benchmark",
            featureDescription: null,
            scenarioName: "SingleAssertion",
            traitBridge: new NullTraitBridge(),
            options: new ScenarioOptions()
        );
    }

    /// <summary>
    /// Baseline: Direct assertion with no TinyBDD overhead.
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool Baseline_SingleAssertion()
    {
        var result = _value == 42;
        if (!result)
            throw new InvalidOperationException("Assertion failed");
        return result;
    }

    /// <summary>
    /// TinyBDD: Single Given/Then step performing the same check.
    /// </summary>
    [Benchmark]
    public async Task<bool> TinyBDD_SingleAssertion()
    {
        await Bdd.Given(_context!, "value is 42", () => _value)
            .Then("should be 42", v => v == 42);
        return true;
    }
}

/// <summary>
/// Null trait bridge for benchmarking - does nothing.
/// </summary>
internal sealed class NullTraitBridge : ITraitBridge
{
    public void AddTag(string tag) { }
}
