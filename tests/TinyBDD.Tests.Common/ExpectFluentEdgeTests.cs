using TinyBDD.Assertions;

namespace TinyBDD.Tests.Common;

/// <summary>
/// Additional fluent-assertion edge tests focusing on rarely-hit branches:
/// the failure paths when an action does not throw (for ToThrowWithMessage /
/// ToThrowExactlyWithMessage) and the inner-assertion re-throw paths.
/// </summary>
public class ExpectFluentEdgeTests
{
    // ----- ToThrowWithMessage edge cases -----

    [Fact]
    public async Task ToThrowWithMessage_WhenActionDoesNotThrow_Fails()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!, "op").ToThrowWithMessage(() => { }, "boom"));
        Assert.Contains("but it did not", ex.Message);
    }

    [Fact]
    public async Task ToThrowWithMessage_WhenInnerAssertionFails_ReThrowsAssertionException()
    {
        // Inner action throws a TinyBddAssertionException (e.g., a failing inner assertion).
        // The outer ToThrowWithMessage should *not* swallow it via the generic catch;
        // it must re-throw via the `catch (TinyBddAssertionException) { throw; }` branch.
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowWithMessage(
                () => throw new TinyBddAssertionException("inner assertion failed"),
                "anything"));
        Assert.Contains("inner assertion failed", ex.Message);
    }

    // ----- ToThrowExactlyWithMessage edge cases -----

    [Fact]
    public async Task ToThrowExactlyWithMessage_WhenActionDoesNotThrow_Fails()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactlyWithMessage<InvalidOperationException>(
                () => { }, "boom"));
        Assert.Contains("but it did not", ex.Message);
    }

    [Fact]
    public async Task ToThrowExactlyWithMessage_WhenInnerAssertionFails_ReThrowsAssertionException()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactlyWithMessage<InvalidOperationException>(
                () => throw new TinyBddAssertionException("inner exact assertion failed"),
                "anything"));
        Assert.Contains("inner exact assertion failed", ex.Message);
    }

    // ----- ToThrow inner-assertion re-throw -----

    [Fact]
    public async Task ToThrow_WhenInnerAssertionFails_ReThrowsAssertionException()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrow(
                () => throw new TinyBddAssertionException("inner failure")));
        // Should bubble inner message rather than swallow it.
        Assert.Contains("inner failure", ex.Message);
    }

    // ----- ToThrowExactly inner-assertion re-throw -----

    [Fact]
    public async Task ToThrowExactly_WhenInnerAssertionFails_ReThrowsAssertionException()
    {
        var ex = await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.That<object>(null!).ToThrowExactly<InvalidOperationException>(
                () => throw new TinyBddAssertionException("nested")));
        Assert.Contains("nested", ex.Message);
    }

    // ----- Collection assertions: non-enumerable actual values -----
    // These exercise the failure branches that report "expected ... to be enumerable".

    [Fact]
    public async Task ToHaveCount_OnNonEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For(42).ToHaveCount(3));
    }

    [Fact]
    public async Task ToBeEmpty_OnNonEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For(42).ToBeEmpty());
    }

    [Fact]
    public async Task ToHaveAtLeast_OnNonEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For(42).ToHaveAtLeast(1));
    }

    [Fact]
    public async Task ToHaveNoMoreThan_OnNonEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For(42).ToHaveNoMoreThan(5));
    }

    [Fact]
    public async Task ToContain_OnNonEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For(42).ToContain(1));
    }

    [Fact]
    public async Task ToContainMatch_OnWrongTypeEnumerable_Fails()
    {
        // ToContainMatch<TItem> is typed - passing the wrong enumerable item type fails.
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For((object)42).ToContainMatch<string>(s => s == "x"));
    }

    [Fact]
    public async Task ToHaveCountMatching_OnWrongTypeEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For((object)42).ToHaveCountMatching<string>(1, s => s == "x"));
    }

    [Fact]
    public async Task ToHaveFewerThanCountMatching_OnWrongTypeEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For((object)42).ToHaveFewerThanCountMatching<string>(1, s => s == "x"));
    }

    [Fact]
    public async Task ToHaveMoreThanCountMatching_OnWrongTypeEnumerable_Fails()
    {
        await Assert.ThrowsAsync<TinyBddAssertionException>(async () =>
            await Expect.For((object)42).ToHaveMoreThanCountMatching<string>(1, s => s == "x"));
    }
}
