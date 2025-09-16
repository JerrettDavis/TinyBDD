using TinyBDD.Assertions;

namespace TinyBDD.Tests.Common;

public class FluentPredicateTests
{
    [Fact]
    public void ToBe_Returns_True_When_Equals_And_False_When_Not()
    {
        var p = new FluentPredicate<int>(5, "count");
        Assert.True(p.ToBe(5));
        Assert.False(p.ToBe(6));
    }

    [Fact]
    public void ToEqual_Works_For_Generic_Expected_Types()
    {
        var p = new FluentPredicate<object>("a", "value");
        Assert.True(p.ToEqual("a"));
        Assert.False(p.ToEqual("b"));
    }

    [Fact]
    public void BooleanChecks_Return_Correct_Results()
    {
        Assert.True(new FluentPredicate<bool>(true, "b").ToBeTrue());
        Assert.False(new FluentPredicate<bool>(false, "b").ToBeTrue());

        Assert.True(new FluentPredicate<bool>(false, "b").ToBeFalse());
        Assert.False(new FluentPredicate<bool>(true, "b").ToBeFalse());
    }

    [Fact]
    public void NullChecks_Return_Correct_Results()
    {
        Assert.True(new FluentPredicate<object?>(null, "s").ToBeNull());
        Assert.False(new FluentPredicate<object?>(new object(), "s").ToBeNull());

        Assert.True(new FluentPredicate<object?>(new object(), "s").ToNotBeNull());
        Assert.False(new FluentPredicate<object?>(null, "s").ToNotBeNull());
    }

    [Fact]
    public void ToSatisfy_Evaluates_Predicate()
    {
        Assert.True(new FluentPredicate<int>(10, "n").ToSatisfy(x => x > 5));
        Assert.False(new FluentPredicate<int>(1, "n").ToSatisfy(x => x > 5));
    }

    [Fact]
    public void Because_And_With_Do_Not_Affect_Result_But_Preserve_Subject()
    {
        var p = new FluentPredicate<int>(2, "two").Because("reason").With("hint");
        Assert.Equal("two", p.ToString());
        Assert.True(p.ToBe(2));
    }

    [Fact]
    public void ToString_Returns_Value_When_Subject_Is_Null()
    {
        var p = new FluentPredicate<int>(3, null);
        Assert.Equal("value", p.ToString());
    }
}

