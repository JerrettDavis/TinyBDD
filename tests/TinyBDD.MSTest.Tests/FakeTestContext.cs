using System.Collections;

namespace TinyBDD.MSTest.Tests;

internal sealed class FakeTestContext : TestContext
{
    public override IDictionary Properties { get; } = new Dictionary<string, object>();
    public override string? FullyQualifiedTestClassName { get; }
    public override string? TestName { get; }
    public List<string> Lines { get; } = new();

    public FakeTestContext(string? fqcn, string? testName)
    {
        FullyQualifiedTestClassName = fqcn;
        TestName = testName;
    }

    public override void Write(string format, params object[] args) => Lines.Add(string.Format(format, args));
    public override void WriteLine(string format, params object[] args) => Lines.Add(string.Format(format, args));
    public override void WriteLine(string message) => Lines.Add(message);

    // --- Required abstract members we don't use in tests ---
    public override void AddResultFile(string fileName)
    {
        // no-op; we just collect logs in-memory
    }

    public override void Write(string? message)
    {
        if (message is not null)
            Lines.Add(message);
    }

    public override void DisplayMessage(MessageLevel messageLevel, string message)
    {
        // Could store level for inspection if needed
        Lines.Add($"[{messageLevel}] {message}");
    }
}

// Public driver that lets tests call init/cleanup manually
public sealed class MsBaseDriver : TinyBddMsTestBase
{
    public void SetContext(TestContext tc) => TestContext = tc;
    public void CallInit() => TinyBdd_Init();
    public void CallCleanup() => TinyBdd_Cleanup();
    public ScenarioContext? Current => Ambient.Current.Value;
}

// Helper to get the FQCN string MSTest normally provides
internal static class Fqcn
{
    public static string Of<T>() => typeof(T).AssemblyQualifiedName!;
    public static string Of(Type t) => t.AssemblyQualifiedName!;
}