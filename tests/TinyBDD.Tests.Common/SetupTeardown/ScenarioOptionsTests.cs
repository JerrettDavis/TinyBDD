namespace TinyBDD.Tests.Common.SetupTeardown;

public class ScenarioOptionsExtensionTests
{
    [Fact]
    public void ScenarioOptions_ShowBackgroundSection_DefaultIsFalse()
    {
        // Arrange & Act
        var options = new ScenarioOptions();

        // Assert
        Assert.False(options.ShowBackgroundSection);
    }

    [Fact]
    public void ScenarioOptions_ShowBackgroundSection_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new ScenarioOptions { ShowBackgroundSection = true };

        // Assert
        Assert.True(options.ShowBackgroundSection);
    }

    [Fact]
    public void ScenarioOptions_ShowFeatureSetup_DefaultIsFalse()
    {
        // Arrange & Act
        var options = new ScenarioOptions();

        // Assert
        Assert.False(options.ShowFeatureSetup);
    }

    [Fact]
    public void ScenarioOptions_ShowFeatureSetup_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new ScenarioOptions { ShowFeatureSetup = true };

        // Assert
        Assert.True(options.ShowFeatureSetup);
    }

    [Fact]
    public void ScenarioOptions_ShowFeatureTeardown_DefaultIsFalse()
    {
        // Arrange & Act
        var options = new ScenarioOptions();

        // Assert
        Assert.False(options.ShowFeatureTeardown);
    }

    [Fact]
    public void ScenarioOptions_ShowFeatureTeardown_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new ScenarioOptions { ShowFeatureTeardown = true };

        // Assert
        Assert.True(options.ShowFeatureTeardown);
    }

    [Fact]
    public void ScenarioOptions_AllShowFlags_CanBeSetTogether()
    {
        // Arrange & Act
        var options = new ScenarioOptions
        {
            ShowBackgroundSection = true,
            ShowFeatureSetup = true,
            ShowFeatureTeardown = true
        };

        // Assert
        Assert.True(options.ShowBackgroundSection);
        Assert.True(options.ShowFeatureSetup);
        Assert.True(options.ShowFeatureTeardown);
    }
}
