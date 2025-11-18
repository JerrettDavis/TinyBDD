using TinyBDD.Assertions;

namespace TinyBDD.Tests.Common;

public class TinyBddAssertionExceptionTests
{
    [Fact]
    public void InnerException_Ctor_Sets_Properties()
    {
        var inner = new InvalidOperationException("inner boom");
        var ex = new TinyBddAssertionException("outer boom", inner)
        {
            Expected = 42,
            Actual = 41,
            Subject = "answer",
            Because = "we need life meaning",
            WithHint = "check config"
        };

        Assert.Equal("outer boom", ex.Message);
        Assert.Same(inner, ex.InnerException);
        Assert.Equal(42, ex.Expected);
        Assert.Equal(41, ex.Actual);
        Assert.Equal("answer", ex.Subject);
        Assert.Equal("we need life meaning", ex.Because);
        Assert.Equal("check config", ex.WithHint);
    }
}
