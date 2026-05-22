namespace TinyBDD.Tests.Common;

public class GenerateOptimizedAttributeTests
{
    [Fact]
    public void GenerateOptimizedAttribute_Default_EnabledIsTrue()
    {
        var attr = new GenerateOptimizedAttribute();
        Assert.True(attr.Enabled);
    }

    [Fact]
    public void GenerateOptimizedAttribute_Enabled_CanBeSetToFalse()
    {
        var attr = new GenerateOptimizedAttribute { Enabled = false };
        Assert.False(attr.Enabled);
    }

    [Fact]
    public void GenerateOptimizedAttribute_Enabled_CanBeRoundTripped()
    {
        var attr = new GenerateOptimizedAttribute { Enabled = true };
        Assert.True(attr.Enabled);
        attr.Enabled = false;
        Assert.False(attr.Enabled);
        attr.Enabled = true;
        Assert.True(attr.Enabled);
    }

    [Fact]
    public void GenerateOptimizedAttribute_HasAttributeUsage_ForMethodsOnly()
    {
        var usage = typeof(GenerateOptimizedAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        Assert.Equal(AttributeTargets.Method, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }

    [GenerateOptimized]
    private void MethodWithDefaultAttribute() { }

    [GenerateOptimized(Enabled = false)]
    private void MethodWithDisabledOptimization() { }

    [Fact]
    public void GenerateOptimizedAttribute_CanBeAppliedToMethods()
    {
        var defaultAttr = typeof(GenerateOptimizedAttributeTests)
            .GetMethod(nameof(MethodWithDefaultAttribute),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetCustomAttributes(typeof(GenerateOptimizedAttribute), false)
            .Cast<GenerateOptimizedAttribute>()
            .Single();
        Assert.True(defaultAttr.Enabled);

        var disabledAttr = typeof(GenerateOptimizedAttributeTests)
            .GetMethod(nameof(MethodWithDisabledOptimization),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetCustomAttributes(typeof(GenerateOptimizedAttribute), false)
            .Cast<GenerateOptimizedAttribute>()
            .Single();
        Assert.False(disabledAttr.Enabled);
    }
}
