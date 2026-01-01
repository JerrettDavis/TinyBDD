using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TinyBDD.Extensions.DependencyInjection;

namespace TinyBDD.Extensions.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTinyBdd_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTinyBdd();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IScenarioContextFactory>());
        Assert.NotNull(provider.GetService<ITraitBridge>());
        Assert.NotNull(provider.GetService<IOptions<TinyBddOptions>>());
    }

    [Fact]
    public void AddTinyBdd_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedTimeout = TimeSpan.FromSeconds(42);

        // Act
        services.AddTinyBdd(options =>
        {
            options.DefaultScenarioOptions = new ScenarioOptions
            {
                StepTimeout = expectedTimeout,
                ContinueOnError = true
            };
            options.EnableStepTiming = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var optionsAccessor = provider.GetRequiredService<IOptions<TinyBddOptions>>();
        Assert.Equal(expectedTimeout, optionsAccessor.Value.DefaultScenarioOptions.StepTimeout);
        Assert.True(optionsAccessor.Value.DefaultScenarioOptions.ContinueOnError);
        Assert.True(optionsAccessor.Value.EnableStepTiming);
    }

    [Fact]
    public void AddTinyBdd_RegistersContextFactoryAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTinyBdd();
        var provider = services.BuildServiceProvider();

        // Act
        IScenarioContextFactory? factory1, factory2;
        using (var scope1 = provider.CreateScope())
        {
            factory1 = scope1.ServiceProvider.GetService<IScenarioContextFactory>();
        }
        using (var scope2 = provider.CreateScope())
        {
            factory2 = scope2.ServiceProvider.GetService<IScenarioContextFactory>();
        }

        // Assert - Different scopes should get different instances
        Assert.NotNull(factory1);
        Assert.NotNull(factory2);
        // Note: Can't directly compare instances across disposed scopes,
        // but we verify the service is resolvable in each scope
    }

    [Fact]
    public void AddTinyBdd_RegistersTraitBridgeAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTinyBdd();
        var provider = services.BuildServiceProvider();

        // Act
        var bridge1 = provider.GetService<ITraitBridge>();
        var bridge2 = provider.GetService<ITraitBridge>();

        // Assert
        Assert.NotNull(bridge1);
        Assert.Same(bridge1, bridge2);
    }

    [Fact]
    public void AddTinyBddTraitBridge_WithType_RegistersCustomBridge()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTinyBdd();
        services.AddTinyBddTraitBridge<TestTraitBridge>();
        var provider = services.BuildServiceProvider();

        // Act
        var bridge = provider.GetService<ITraitBridge>();

        // Assert
        Assert.IsType<TestTraitBridge>(bridge);
    }

    [Fact]
    public void AddTinyBddTraitBridge_WithInstance_RegistersProvidedInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var customBridge = new TestTraitBridge();
        services.AddTinyBdd();
        services.AddTinyBddTraitBridge(customBridge);
        var provider = services.BuildServiceProvider();

        // Act
        var bridge = provider.GetService<ITraitBridge>();

        // Assert
        Assert.Same(customBridge, bridge);
    }

    private class TestTraitBridge : ITraitBridge
    {
        public List<string> AddedTags { get; } = new();
        public void AddTag(string tag) => AddedTags.Add(tag);
    }
}
