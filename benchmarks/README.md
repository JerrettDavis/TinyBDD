# TinyBDD Benchmarks

This project contains comprehensive performance benchmarks for TinyBDD to measure and track runtime overhead.

## Overview

The benchmarks compare TinyBDD's DSL implementation against baseline implementations to quantify the overhead introduced by:

- Step execution pipeline
- Scenario context management
- Step recording and reporting
- Exception handling and capture

## Running Benchmarks Locally

### Prerequisites

- .NET 9.0 SDK or later
- Release configuration (benchmarks must run in Release mode)

### Run All Benchmarks

```bash
cd benchmarks/TinyBDD.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmarks

```bash
# Run only single assertion benchmarks
dotnet run -c Release --filter *SingleAssertion*

# Run only multiple assertions benchmarks
dotnet run -c Release --filter *MultipleAssertions*

# Run only scenario context benchmarks
dotnet run -c Release --filter *ScenarioContext*

# Run only step pipeline benchmarks
dotnet run -c Release --filter *StepPipeline*

# Run only exception handling benchmarks
dotnet run -c Release --filter *ExceptionHandling*
```

### Quick Smoke Test

For fast validation (useful in CI):

```bash
dotnet run -c Release --filter *SingleAssertion* --job short
```

## Benchmark Scenarios

### 1. SingleAssertionBenchmark

**Purpose**: Measures the most basic overhead - a single assertion/check.

- **Baseline**: Direct boolean check with throw on failure
- **TinyBDD**: Single Given/Then step chain

**What it measures**: Minimum overhead of TinyBDD's fluent API for the simplest case.

### 2. MultipleAssertionsBenchmark

**Purpose**: Measures overhead as the number of assertions/steps scales.

- **Baseline**: Loop with direct assertions
- **TinyBDD**: Chain with N And/Then steps
- **Params**: N = 10, 100, 1000

**What it measures**: How TinyBDD overhead scales with step count.

### 3. ScenarioContextBenchmark

**Purpose**: Measures state management overhead.

- **Baseline**: Dictionary read/write operations
- **TinyBDD**: Value flow through step chain transformations
- **Params**: Operations = 10, 100

**What it measures**: Overhead of TinyBDD's context and state passing mechanism.

### 4. StepPipelineBenchmark

**Purpose**: Measures step dispatch overhead.

- **Baseline (sync)**: Direct delegate invocation
- **Baseline (async)**: Direct async delegate invocation
- **TinyBDD**: Steps dispatched through pipeline

**What it measures**: Cost of TinyBDD's step execution infrastructure vs raw delegates.

### 5. ExceptionHandlingBenchmark

**Purpose**: Measures exception/failure handling overhead.

- **Baseline**: Try-catch with direct exception throw
- **TinyBDD**: Step failure with exception capture
- **Includes**: Both success and failure paths

**What it measures**: Overhead of TinyBDD's error capture and step failure recording.

## Interpreting Results

### Key Metrics

- **Mean**: Average execution time - primary metric for comparison
- **Median**: Middle value - useful for identifying skewed distributions
- **StdDev**: Standard deviation - indicates consistency
- **Allocated**: Memory allocated per operation
- **Gen0/Gen1/Gen2**: Garbage collection counts

### Baseline Ratio

Each TinyBDD benchmark includes a baseline comparison. Look for the "Ratio" column:

- **1.00**: Same performance as baseline (no overhead)
- **2.00**: 2x slower than baseline (100% overhead)
- **10.00**: 10x slower than baseline (900% overhead)

### What's Acceptable?

- **<5x overhead**: Generally acceptable for test code
- **5-10x overhead**: Borderline - monitor for regressions
- **>10x overhead**: Investigate - may indicate issues

Remember: These are microbenchmarks. Real-world test scenarios involve actual work (database calls, API requests, etc.) where TinyBDD overhead becomes negligible.

## CI Integration

Benchmarks are automatically run in CI:

- **PR builds**: Run a fast smoke test subset (`--filter *SingleAssertion* --job short`)
- **Main/nightly**: Run full benchmark suite (optional)

Results are uploaded as CI artifacts in markdown format.

## Output Artifacts

BenchmarkDotNet generates several output files in `BenchmarkDotNet.Artifacts/`:

- `results/*.md`: Markdown summary tables
- `results/*.html`: HTML reports
- `results/*.csv`: Raw data for analysis
- `results/*.json`: Structured data for tooling

## Adding New Benchmarks

1. Create a new class in this project
2. Add `[SimpleJob(RuntimeMoniker.Net90)]` and `[MemoryDiagnoser]` attributes
3. Implement a baseline benchmark marked with `[Benchmark(Baseline = true)]`
4. Implement TinyBDD equivalent benchmark(s) marked with `[Benchmark]`
5. Use `[GlobalSetup]` for one-time initialization
6. Use `[Params]` to test different input sizes

Example:

```csharp
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class MyBenchmark
{
    [Params(10, 100)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data
    }

    [Benchmark(Baseline = true)]
    public void Baseline()
    {
        // Baseline implementation
    }

    [Benchmark]
    public async Task TinyBDD_Version()
    {
        // TinyBDD equivalent
    }
}
```

## Non-Goals (v1)

- **Not about optimization**: This is measurement infrastructure, not optimization work
- **No external I/O**: Benchmarks avoid filesystem, network, or database operations
- **Not comprehensive**: Initial set covers core scenarios; expand as needed

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [BenchmarkDotNet Best Practices](https://benchmarkdotnet.org/articles/guides/good-practices.html)
