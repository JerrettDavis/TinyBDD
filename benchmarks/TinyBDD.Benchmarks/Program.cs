using BenchmarkDotNet.Running;

namespace TinyBDD.Benchmarks;

/// <summary>
/// Entry point for TinyBDD benchmarks.
/// Run with: dotnet run -c Release
/// For specific benchmark: dotnet run -c Release --filter *SingleAssertion*
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
