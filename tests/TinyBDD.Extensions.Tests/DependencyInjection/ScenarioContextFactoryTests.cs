using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TinyBDD.Extensions.DependencyInjection;

namespace TinyBDD.Extensions.Tests.DependencyInjection;

public class ScenarioContextFactoryTests
{
    [Fact]
    public void Create_WithExplicitNames_CreatesContextWithCorrectMetadata()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var context = factory.Create("Test Feature", "Test Scenario", "A test description");

        // Assert
        Assert.Equal("Test Feature", context.FeatureName);
        Assert.Equal("Test Scenario", context.ScenarioName);
        Assert.Equal("A test description", context.FeatureDescription);
    }

    [Fact]
    public void Create_WithNullDescription_CreatesContextWithNullDescription()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var context = factory.Create("Feature", "Scenario");

        // Assert
        Assert.Equal("Feature", context.FeatureName);
        Assert.Equal("Scenario", context.ScenarioName);
        Assert.Null(context.FeatureDescription);
    }

    [Fact]
    public void Create_UsesConfiguredDefaultOptions()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(15);
        var factory = CreateFactory(options =>
        {
            options.DefaultScenarioOptions = new ScenarioOptions
            {
                StepTimeout = timeout,
                ContinueOnError = true
            };
        });

        // Act
        var context = factory.Create("Feature", "Scenario");

        // Assert
        Assert.Equal(timeout, context.Options.StepTimeout);
        Assert.True(context.Options.ContinueOnError);
    }

    [Fact]
    public void Create_CreatesIndependentOptionsPerContext()
    {
        // Arrange
        var factory = CreateFactory(options =>
        {
            options.DefaultScenarioOptions = new ScenarioOptions
            {
                ContinueOnError = true
            };
        });

        // Act
        var context1 = factory.Create("Feature1", "Scenario1");
        var context2 = factory.Create("Feature2", "Scenario2");

        // Assert - Contexts should have independent options objects
        Assert.NotSame(context1.Options, context2.Options);
        Assert.True(context1.Options.ContinueOnError);
        Assert.True(context2.Options.ContinueOnError);
    }

    [Fact]
    public void CreateFromAttributes_WithFeatureSource_ReadsAttributes()
    {
        // Arrange
        var factory = CreateFactory();
        var featureSource = new TestFeatureClass();

        // Act
        var context = factory.CreateFromAttributes(featureSource);

        // Assert
        Assert.Equal("Test Feature Name", context.FeatureName);
    }

    [Fact]
    public void CreateFromAttributes_WithScenarioNameOverride_UsesOverride()
    {
        // Arrange
        var factory = CreateFactory();
        var featureSource = new TestFeatureClass();

        // Act
        var context = factory.CreateFromAttributes(featureSource, "Custom Scenario");

        // Assert
        Assert.Equal("Custom Scenario", context.ScenarioName);
    }

    [Fact]
    public void Create_UsesProvidedTraitBridge()
    {
        // Arrange
        var traitBridge = new TestTraitBridge();
        var factory = CreateFactory(traitBridge: traitBridge);

        // Act
        var context = factory.Create("Feature", "Scenario");
        context.AddTag("test-tag");

        // Assert
        Assert.Contains("test-tag", traitBridge.AddedTags);
    }

    [Fact]
    public async Task Create_ContextCanExecuteWorkflow()
    {
        // Arrange
        var factory = CreateFactory();
        var context = factory.Create("Workflow Test", "Execute steps");

        // Act
        await Bdd.Given(context, "initial value", () => 10)
            .When("doubled", x => x * 2)
            .Then("equals 20", x => x == 20);

        // Assert
        Assert.Equal(3, context.Steps.Count);
        Assert.All(context.Steps, step => Assert.Null(step.Error));
    }

    private static IScenarioContextFactory CreateFactory(
        Action<TinyBddOptions>? configure = null,
        ITraitBridge? traitBridge = null)
    {
        var services = new ServiceCollection();
        services.AddTinyBdd(configure ?? (_ => { }));

        if (traitBridge != null)
        {
            services.AddSingleton(traitBridge);
        }

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IScenarioContextFactory>();
    }

    [Feature("Test Feature Name")]
    private class TestFeatureClass { }

    private class TestTraitBridge : ITraitBridge
    {
        public List<string> AddedTags { get; } = new();
        public void AddTag(string tag) => AddedTags.Add(tag);
    }
}
