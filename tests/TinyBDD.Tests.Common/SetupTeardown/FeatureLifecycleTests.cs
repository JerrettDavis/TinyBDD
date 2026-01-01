namespace TinyBDD.Tests.Common.SetupTeardown;

public class FeatureLifecycleTests
{
    [Fact]
    public async Task ConfigureFeatureSetup_ExecutesOnce()
    {
        // Arrange
        var feature = new TestFeature();
        var ctx = Bdd.CreateContext(feature);
        Ambient.Current.Value = ctx;

        // Act
        await feature.PublicExecuteFeatureSetupAsync();
        await feature.PublicExecuteFeatureSetupAsync(); // Should only execute once

        // Assert
        Assert.Equal(1, feature.FeatureSetupCount);
        Assert.True(feature.PublicFeatureSetupExecuted);
    }

    [Fact]
    public async Task ConfigureFeatureTeardown_Executes()
    {
        // Arrange
        var feature = new TestFeature();
        var ctx = Bdd.CreateContext(feature);
        Ambient.Current.Value = ctx;
        await feature.PublicExecuteFeatureSetupAsync();

        // Act
        await feature.PublicExecuteFeatureTeardownAsync();

        // Assert
        Assert.Equal(1, feature.FeatureTeardownCount);
    }

    [Fact]
    public async Task FeatureSetup_WithState_StoresState()
    {
        // Arrange
        var feature = new FeatureWithState();
        var ctx = Bdd.CreateContext(feature);
        Ambient.Current.Value = ctx;

        // Act
        await feature.PublicExecuteFeatureSetupAsync();

        // Assert
        Assert.NotNull(feature.PublicFeatureState);
        Assert.Equal("TestData", ((FeatureData)feature.PublicFeatureState!).Value);
    }

    [Fact]
    public async Task FeatureTeardown_WithState_AccessesState()
    {
        // Arrange
        var feature = new FeatureWithState();
        var ctx = Bdd.CreateContext(feature);
        Ambient.Current.Value = ctx;
        await feature.PublicExecuteFeatureSetupAsync();

        // Act
        await feature.PublicExecuteFeatureTeardownAsync();

        // Assert
        Assert.True(feature.TeardownAccessedState);
    }

    [Fact]
    public async Task GivenFeature_WithTitle_CreatesChain()
    {
        // Arrange
        var feature = new FeatureWithState();
        var ctx = Bdd.CreateContext(feature);
        Ambient.Current.Value = ctx;
        await feature.PublicExecuteFeatureSetupAsync();

        // Act
        var chain = feature.PublicGivenFeature<FeatureData>("feature state");

        // Assert
        Assert.NotNull(chain);
    }

    [Fact]
    public async Task GivenFeature_WithoutTitle_CreatesChain()
    {
        // Arrange
        var feature = new FeatureWithState();
        var ctx = Bdd.CreateContext(feature);
        Ambient.Current.Value = ctx;
        await feature.PublicExecuteFeatureSetupAsync();

        // Act
        var chain = feature.PublicGivenFeature<FeatureData>();

        // Assert
        Assert.NotNull(chain);
    }
}

// Test feature classes
public class TestFeature : TestBase
{
    public int FeatureSetupCount { get; private set; }
    public int FeatureTeardownCount { get; private set; }

    protected override IBddReporter Reporter { get; } = new StringBddReporter();

    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given("feature setup", () =>
        {
            FeatureSetupCount++;
            return new object();
        });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("feature teardown", () =>
        {
            FeatureTeardownCount++;
            return new object();
        });
    }

    // Expose protected members for testing
    public async Task PublicExecuteFeatureSetupAsync() => await ExecuteFeatureSetupAsync();
    public async Task PublicExecuteFeatureTeardownAsync() => await ExecuteFeatureTeardownAsync();
    public bool PublicFeatureSetupExecuted => FeatureSetupExecuted;
}

public class FeatureData
{
    public string Value { get; set; } = "";
}

public class FeatureWithState : TestBase
{
    public bool TeardownAccessedState { get; private set; }

    protected override IBddReporter Reporter { get; } = new StringBddReporter();

    protected override ScenarioChain<object>? ConfigureFeatureSetup()
    {
        return Given<object>("feature setup with state", () =>
        {
            return new FeatureData { Value = "TestData" };
        });
    }

    protected override ScenarioChain<object>? ConfigureFeatureTeardown()
    {
        return Given("feature teardown", () =>
        {
            if (FeatureState is FeatureData data && data.Value == "TestData")
            {
                TeardownAccessedState = true;
            }
            return new object();
        });
    }

    // Expose protected members for testing
    public async Task PublicExecuteFeatureSetupAsync() => await ExecuteFeatureSetupAsync();
    public async Task PublicExecuteFeatureTeardownAsync() => await ExecuteFeatureTeardownAsync();
    public object? PublicFeatureState => FeatureState;
    public ScenarioChain<T> PublicGivenFeature<T>() where T : class => GivenFeature<T>();
    public ScenarioChain<T> PublicGivenFeature<T>(string title) where T : class => GivenFeature<T>(title);
}
