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
}

