using TinyBDD.Assertions;

namespace TinyBDD.Tests.Common;

public class ExpectTests
{
    [Fact]
    public void True_Returns_Input()
    {
        Assert.True(Expect.True(true));
        Assert.False(Expect.True(false));
    }

    [Fact]
    public void Equal_Compares_GenericValues()
    {
        Assert.True(Expect.Equal(5, 5));
        Assert.False(Expect.Equal("a", "b"));
    }

    [Fact]
    public void NotNull_Returns_True_When_Object_Is_Not_Null()
    {
        Assert.True(Expect.NotNull(new object()));
        Assert.False(Expect.NotNull(null));
    }

    // --- FluentAssertion (Expect.For deferred-throw) tests ---

    [Fact]
    public async Task For_ToBe_And_ToEqual_Behavior_Throws_On_Failure()
    {
        // success returns the assertion back (chainable) and does not throw until awaited
        var a = Expect.For(5, "count").ToBe(5);
        await Assert.IsType<FluentAssertion<int>>(a);
        await a; // should not throw

        // failure throws when awaited
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For(5).ToBe(6));

        var b = Expect.For("a").ToEqual("a");
        await Assert.IsType<FluentAssertion<string>>(b);
        await b; // should not throw

        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For("a").ToEqual("b"));
    }

    [Fact]
    public async Task For_BooleanAndNullChecks_Throw_On_Failure()
    {
        // ToBeTrue/ToBeFalse
        await Expect.For(true).ToBeTrue();
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For(false).ToBeTrue());

        await Expect.For(false).ToBeFalse();
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For(true).ToBeFalse());

        // Null checks
        await Expect.For<object?>(null).ToBeNull();
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For<object?>(new object()).ToBeNull());

        await Expect.For<object?>(new object()).ToNotBeNull();
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For<object?>(null).ToNotBeNull());
    }

    [Fact]
    public async Task For_ToSatisfy_And_BecauseWith_Include_Info_In_Message()
    {
        await Expect.For(10).ToSatisfy(x => x > 5);
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.For(1).ToSatisfy(x => x > 5));

        // Because/With should appear in failure message
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For(2, "two").Because("just checking").With("hint").ToBe(3)
        );

        Assert.Contains("expected two to be", ex.Message);
        Assert.Contains("because just checking", ex.Message);
        Assert.Contains("(hint)", ex.Message);
    }

    // --- FluentAssertion tests (throwing assertions) ---

    [Fact]
    public async Task That_ToBe_Succeeds_And_Fails_With_Message()
    {
        // success returns the assertion back (chainable)
        var a = Expect.That(3).ToBe(3);
        await Assert.IsType<FluentAssertion<int>>(a);
        await a;

        // failure throws with clear message
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That(3, "value").ToBe(4));
        Assert.Contains("expected value to be 4", ex.Message);
        Assert.Contains("but was 3", ex.Message);
    }

    [Fact]
    public async Task That_Because_And_With_Appear_In_Message()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That("x", "subject").Because("it matters").With("extra").ToBe("y")
        );

        Assert.Contains("expected subject to be \"y\"", ex.Message);
        Assert.Contains("but was \"x\"", ex.Message);
        Assert.Contains("because it matters", ex.Message);
        Assert.Contains("(extra)", ex.Message);
    }

    [Fact]
    public async Task That_ToEqual_And_Boolean_Null_Assertions()
    {
        // ToEqual failure
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That("a").ToEqual("b"));
        Assert.Contains("expected value to equal \"b\"", ex.Message);

        // ToBeTrue/ToBeFalse
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That("notbool").ToBeTrue());
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That("notbool").ToBeFalse());

        // Null assertions
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That("s").ToBeNull());
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That<object?>(null).ToNotBeNull());
    }

    [Fact]
    public async Task That_ToSatisfy_Uses_Description_When_Provided()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () => await Expect.That(5, "n").ToSatisfy(x => x < 0, "be negative"));
        Assert.Contains("expected n to be negative", ex.Message);
    }

    // --- ShouldExtensions tests ---

    [Fact]
    public void ShouldContain_String_And_Enumerable()
    {
        Assert.True("hello".ShouldContain("ell"));
        Assert.False("hello".ShouldContain("ELL")); // ordinal case-sensitive

        var list = new List<int> { 1, 2, 3 };
        Assert.True(list.ShouldContain(2));
        Assert.False(list.ShouldContain(9));

        Assert.True(list.ShouldContain(x => x % 2 == 0));
        Assert.False(list.ShouldContain(x => x > 10));
    }

    [Fact]
    public void ShouldHaveCount_And_ElementAtOrDefault()
    {
        var list = new List<string> { "a", "b", "c" };
        Assert.True(list.ShouldHaveCount(3));
        Assert.False(list.ShouldHaveCount(2));

        Assert.Equal("b", list.ElementAtOrDefault((Index)1));
        // call our null-safe extension explicitly to avoid ambiguity with System.Linq
        Assert.Null(ShouldExtensions.ElementAtOrDefault((IEnumerable<string>?)null, 5));
    }

    [Fact]
    public void ElementAtOrDefault_Extension_Covers_All_Paths()
    {
        // null source
        Assert.Null(ShouldExtensions.ElementAtOrDefault((IEnumerable<string>?)null, 0));

        // negative index
        var strList = new List<string> { "a", "b", "c" };
        Assert.Null(ShouldExtensions.ElementAtOrDefault(strList, -1));

        // empty source
        var empty = new List<string>();
        Assert.Null(ShouldExtensions.ElementAtOrDefault(empty, 0));

        // in-range: first and last
        Assert.Equal("a", ShouldExtensions.ElementAtOrDefault(strList, 0));
        Assert.Equal("c", ShouldExtensions.ElementAtOrDefault(strList, 2));

        // out-of-range (>= count)
        Assert.Null(ShouldExtensions.ElementAtOrDefault(strList, 3));
        Assert.Null(ShouldExtensions.ElementAtOrDefault(strList, 100));

        // value types
        var ints = new List<int> { 10, 20, 30 };
        Assert.Equal(10, ShouldExtensions.ElementAtOrDefault(ints, 0));
        Assert.Equal(30, ShouldExtensions.ElementAtOrDefault(ints, 2));
        Assert.Equal(default, ShouldExtensions.ElementAtOrDefault(ints, -5));
        Assert.Equal(default, ShouldExtensions.ElementAtOrDefault(ints, 5));
    }

    [Fact]
    public async Task FluentAssertion_FormatsNullExpectedValue()
    {
        // Call ToBe(null) so that Fmt(expected) is called with null
        var assertion = Expect.That("actual").ToBe(null!);

        // Evaluate the assertion which should throw and call Fmt(null)
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(() => assertion.EvaluateAsync().AsTask());

        // Verify the exception message contains "null" for the expected value
        Assert.Contains("null", ex.Message);
    }
}
