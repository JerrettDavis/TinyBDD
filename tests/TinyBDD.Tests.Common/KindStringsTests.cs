using System.Reflection;

namespace TinyBDD.Tests.Common;

/// <summary>
/// Tests for the internal <see cref="KindStrings"/> helper, accessed via reflection
/// because the type is internal but [InternalsVisibleTo] is not configured for
/// this test assembly.
/// </summary>
public class KindStringsTests
{
    private static string CallFor(StepPhase phase, StepWord word)
    {
        var type = typeof(StepPhase).Assembly.GetType("TinyBDD.KindStrings")!;
        var method = type.GetMethod("For", BindingFlags.Public | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object[] { phase, word })!;
    }

    [Theory]
    [InlineData(StepPhase.Given, StepWord.Primary, "Given")]
    [InlineData(StepPhase.When, StepWord.Primary, "When")]
    [InlineData(StepPhase.Then, StepWord.Primary, "Then")]
    [InlineData(StepPhase.Given, StepWord.And, "And")]
    [InlineData(StepPhase.When, StepWord.And, "And")]
    [InlineData(StepPhase.Then, StepWord.And, "And")]
    [InlineData(StepPhase.Given, StepWord.But, "But")]
    [InlineData(StepPhase.When, StepWord.But, "But")]
    [InlineData(StepPhase.Then, StepWord.But, "But")]
    public void For_ReturnsExpectedKeyword(StepPhase phase, StepWord word, string expected)
    {
        Assert.Equal(expected, CallFor(phase, word));
    }

    [Fact]
    public void For_OutOfRangePhase_FallsBackToPhaseToString()
    {
        // Cast an out-of-range value to exercise the default switch arm.
        var outOfRange = (StepPhase)int.MaxValue;
        var result = CallFor(outOfRange, StepWord.Primary);
        Assert.Equal(outOfRange.ToString(), result);
    }
}
