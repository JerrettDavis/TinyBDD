using TinyBDD.Assertions;

namespace TinyBDD.Tests.Common;

public class TinyBddAssertionExceptionTests
{
    [Fact]
    public void Ctor_Message_Inner_Sets_Properties()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new TinyBddAssertionException("msg", inner);

        Assert.Equal("msg", ex.Message);
        Assert.Same(inner, ex.InnerException);

        // Exercise structured fields
        ex.Expected = 1;
        ex.Actual = 2;
        ex.Subject = "sub";
        ex.Because = "why";
        ex.WithHint = "hint";

        Assert.Equal(1, ex.Expected);
        Assert.Equal(2, ex.Actual);
        Assert.Equal("sub", ex.Subject);
        Assert.Equal("why", ex.Because);
        Assert.Equal("hint", ex.WithHint);
    }
}

