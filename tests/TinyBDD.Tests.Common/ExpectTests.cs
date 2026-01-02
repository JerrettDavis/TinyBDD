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

    // ====== Collection Assertions Tests ======

    [Fact]
    public async Task ToHaveCount_Succeeds_For_Correct_Count()
    {
        var list = new List<int> { 1, 2, 3 };
        await Expect.That(list).ToHaveCount(3);
    }

    [Fact]
    public async Task ToHaveCount_Fails_For_Incorrect_Count()
    {
        var list = new List<int> { 1, 2, 3 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list, "numbers").ToHaveCount(2));
        Assert.Contains("expected numbers to have count 2", ex.Message);
        Assert.Contains("but was 3", ex.Message);
    }

    [Fact]
    public async Task ToBeEmpty_Succeeds_For_Empty_Collection()
    {
        var list = new List<int>();
        await Expect.That(list).ToBeEmpty();
    }

    [Fact]
    public async Task ToBeEmpty_Fails_For_Non_Empty_Collection()
    {
        var list = new List<int> { 1, 2 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list, "items").ToBeEmpty());
        Assert.Contains("expected items to be empty", ex.Message);
        Assert.Contains("but it had 2 item(s)", ex.Message);
    }

    [Fact]
    public async Task ToHaveAtLeast_Succeeds_For_Sufficient_Count()
    {
        var list = new List<int> { 1, 2, 3 };
        await Expect.That(list).ToHaveAtLeast(2);
        await Expect.That(list).ToHaveAtLeast(3);
    }

    [Fact]
    public async Task ToHaveAtLeast_Fails_For_Insufficient_Count()
    {
        var list = new List<int> { 1, 2 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list).ToHaveAtLeast(3));
        Assert.Contains("expected collection to have at least 3 item(s)", ex.Message);
        Assert.Contains("but was 2", ex.Message);
    }

    [Fact]
    public async Task ToHaveNoMoreThan_Succeeds_For_Valid_Count()
    {
        var list = new List<int> { 1, 2, 3 };
        await Expect.That(list).ToHaveNoMoreThan(3);
        await Expect.That(list).ToHaveNoMoreThan(5);
    }

    [Fact]
    public async Task ToHaveNoMoreThan_Fails_For_Exceeding_Count()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list).ToHaveNoMoreThan(3));
        Assert.Contains("expected collection to have no more than 3 item(s)", ex.Message);
        Assert.Contains("but was 4", ex.Message);
    }

    [Fact]
    public async Task ToContain_Succeeds_When_Item_Present()
    {
        var list = new List<string> { "apple", "banana", "cherry" };
        await Expect.That(list).ToContain("banana");
    }

    [Fact]
    public async Task ToContain_Fails_When_Item_Absent()
    {
        var list = new List<string> { "apple", "banana" };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list, "fruits").ToContain("cherry"));
        Assert.Contains("expected fruits to contain \"cherry\"", ex.Message);
    }

    [Fact]
    public async Task ToContainMatch_Succeeds_When_Predicate_Matches()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        await Expect.That(list).ToContainMatch<int>(x => x > 3);
    }

    [Fact]
    public async Task ToContainMatch_Fails_When_No_Match()
    {
        var list = new List<int> { 1, 2, 3 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list).ToContainMatch<int>(x => x > 10, "greater than 10"));
        Assert.Contains("expected collection to contain an item greater than 10", ex.Message);
    }

    [Fact]
    public async Task ToHaveCountMatching_Succeeds_For_Correct_Count()
    {
        var list = new List<int> { 1, 2, 3, 4, 5, 6 };
        await Expect.That(list).ToHaveCountMatching<int>(3, x => x % 2 == 0, "even numbers");
    }

    [Fact]
    public async Task ToHaveCountMatching_Fails_For_Incorrect_Count()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list).ToHaveCountMatching<int>(3, x => x % 2 == 0));
        Assert.Contains("expected collection to have 3 item(s) matching predicate", ex.Message);
        Assert.Contains("but was 2", ex.Message);
    }

    [Fact]
    public async Task ToHaveFewerThanCountMatching_Succeeds_When_Count_Is_Less()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        await Expect.That(list).ToHaveFewerThanCountMatching<int>(3, x => x % 2 == 0);
    }

    [Fact]
    public async Task ToHaveFewerThanCountMatching_Fails_When_Count_Equals_Or_Exceeds()
    {
        var list = new List<int> { 1, 2, 3, 4, 6 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list).ToHaveFewerThanCountMatching<int>(3, x => x % 2 == 0));
        Assert.Contains("expected collection to have fewer than 3 item(s) matching predicate", ex.Message);
        Assert.Contains("but was 3", ex.Message);
    }

    [Fact]
    public async Task ToHaveMoreThanCountMatching_Succeeds_When_Count_Exceeds()
    {
        var list = new List<int> { 1, 2, 3, 4, 6 };
        await Expect.That(list).ToHaveMoreThanCountMatching<int>(2, x => x % 2 == 0);
    }

    [Fact]
    public async Task ToHaveMoreThanCountMatching_Fails_When_Count_Is_Less_Or_Equal()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(list).ToHaveMoreThanCountMatching<int>(2, x => x % 2 == 0));
        Assert.Contains("expected collection to have more than 2 item(s) matching predicate", ex.Message);
        Assert.Contains("but was 2", ex.Message);
    }

    // ====== Instance State Assertions Tests ======

    [Fact]
    public async Task ToBeOfType_Succeeds_For_Exact_Type()
    {
        var obj = "test";
        await Expect.That(obj).ToBeOfType<string>();
    }

    [Fact]
    public async Task ToBeOfType_Fails_For_Different_Type()
    {
        var obj = "test";
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(obj).ToBeOfType<int>());
        Assert.Contains("expected value to be of type Int32", ex.Message);
        Assert.Contains("but was String", ex.Message);
    }

    [Fact]
    public async Task ToBeAssignableTo_Succeeds_For_Compatible_Type()
    {
        var obj = "test";
        await Expect.That(obj).ToBeAssignableTo<object>();
        await Expect.That(obj).ToBeAssignableTo<IEnumerable<char>>();
    }

    [Fact]
    public async Task ToBeAssignableTo_Fails_For_Incompatible_Type()
    {
        var obj = "test";
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That(obj).ToBeAssignableTo<int>());
        Assert.Contains("expected value to be assignable to Int32", ex.Message);
        Assert.Contains("but was String", ex.Message);
    }

    // ====== Exception Assertions Tests ======

    [Fact]
    public async Task ToThrow_Succeeds_When_Exception_Thrown()
    {
        await Expect.That<object>(null!).ToThrow(() => throw new Exception("test"));
    }

    [Fact]
    public async Task ToThrow_Fails_When_No_Exception()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!, "operation").ToThrow(() => { }));
        Assert.Contains("expected operation to throw an exception", ex.Message);
        Assert.Contains("but it did not", ex.Message);
    }

    [Fact]
    public async Task ToThrowExactly_Succeeds_For_Exact_Exception_Type()
    {
        await Expect.That<object>(null!).ToThrowExactly<InvalidOperationException>(
            () => throw new InvalidOperationException("test"));
    }

    [Fact]
    public async Task ToThrowExactly_Fails_When_No_Exception()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactly<InvalidOperationException>(() => { }));
        Assert.Contains("expected action to throw InvalidOperationException", ex.Message);
        Assert.Contains("but it did not", ex.Message);
    }

    [Fact]
    public async Task ToThrowExactly_Fails_For_Different_Exception_Type()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactly<ArgumentException>(
                () => throw new InvalidOperationException("test")));
        Assert.Contains("expected action to throw ArgumentException", ex.Message);
        Assert.Contains("but threw InvalidOperationException", ex.Message);
    }

    [Fact]
    public async Task ToThrowWithMessage_Succeeds_For_Matching_Message()
    {
        await Expect.That<object>(null!).ToThrowWithMessage(
            () => throw new Exception("expected message"), "expected message");
    }

    [Fact]
    public async Task ToThrowWithMessage_Fails_For_Different_Message()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowWithMessage(
                () => throw new Exception("actual"), "expected"));
        Assert.Contains("expected action to throw exception with message \"expected\"", ex.Message);
        Assert.Contains("but message was \"actual\"", ex.Message);
    }

    [Fact]
    public async Task ToThrowExactlyWithMessage_Succeeds_For_Exact_Type_And_Message()
    {
        await Expect.That<object>(null!).ToThrowExactlyWithMessage<ArgumentException>(
            () => throw new ArgumentException("test message"), "test message");
    }

    [Fact]
    public async Task ToThrowExactlyWithMessage_Fails_For_Wrong_Exception_Type()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactlyWithMessage<ArgumentException>(
                () => throw new InvalidOperationException("test"), "test"));
        Assert.Contains("expected action to throw ArgumentException", ex.Message);
        Assert.Contains("but threw InvalidOperationException", ex.Message);
    }

    [Fact]
    public async Task ToThrowExactlyWithMessage_Fails_For_Wrong_Message()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactlyWithMessage<ArgumentException>(
                () => throw new ArgumentException("actual"), "expected"));
        Assert.Contains("expected action to throw ArgumentException with message \"expected\"", ex.Message);
        Assert.Contains("but message was \"actual\"", ex.Message);
    }

    [Fact]
    public async Task ToNotThrow_Succeeds_When_No_Exception()
    {
        await Expect.That<object>(null!).ToNotThrow(() => { });
    }

    [Fact]
    public async Task ToNotThrow_Fails_When_Exception_Thrown()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!, "safe operation").ToNotThrow(
                () => throw new InvalidOperationException("boom")));
        Assert.Contains("expected safe operation to not throw", ex.Message);
        Assert.Contains("but threw InvalidOperationException", ex.Message);
    }
}
