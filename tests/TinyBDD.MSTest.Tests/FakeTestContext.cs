using System.Collections;

namespace TinyBDD.MSTest.Tests;

internal sealed class FakeTestContext(string fqcn, string testName) 
    : TestContext
{
    #if NET6_0_OR_GREATER
    public override IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
    #else
    public override IDictionary Properties { get; } = new Dictionary<string, object>();
    #endif
    public override string FullyQualifiedTestClassName { get; } = fqcn;
    public override string TestName { get; } = testName;
    public List<string> Lines { get; } = [];

    public override void Write(string format, params object?[] args) => Lines.Add(string.Format(format, args));
    public override void WriteLine(string format, params object?[] args) => Lines.Add(string.Format(format, args));
    public override void WriteLine(string? message) => Lines.Add(message ?? string.Empty);

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

public sealed class MsBaseDriver : TinyBddMsTestBase
{
    public void SetContext(TestContext tc) => TestContext = tc;
    public void CallInit() => TinyBdd_Init();
    public void CallCleanup() => TinyBdd_Cleanup();
    public ScenarioContext? Current => Ambient.Current.Value;
}

internal static class Fqcn
{
    public static string Of<T>() => typeof(T).AssemblyQualifiedName!;
    public static string Of(Type t) => t.AssemblyQualifiedName!;
}