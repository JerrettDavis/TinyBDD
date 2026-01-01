using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TinyBDD.Extensions.DependencyInjection;
using TinyBDD.Extensions.Hosting;

namespace TinyBDD.Extensions.Tests.Hosting;

public class HostBuilderExtensionsTests
{
    [Fact]
    public void AddTinyBddHosting_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTinyBddHosting();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IScenarioContextFactory>());
        Assert.NotNull(provider.GetService<IWorkflowRunner>());
        Assert.NotNull(provider.GetService<IOptions<TinyBddHostingOptions>>());
    }

    [Fact]
    public void AddTinyBddHosting_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTinyBddHosting(options =>
        {
            options.StopHostOnCompletion = true;
            options.StopHostOnFailure = false;
            options.StartupDelay = TimeSpan.FromSeconds(5);
            options.ShutdownTimeout = TimeSpan.FromMinutes(2);
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var optionsAccessor = provider.GetRequiredService<IOptions<TinyBddHostingOptions>>();
        Assert.True(optionsAccessor.Value.StopHostOnCompletion);
        Assert.False(optionsAccessor.Value.StopHostOnFailure);
        Assert.Equal(TimeSpan.FromSeconds(5), optionsAccessor.Value.StartupDelay);
        Assert.Equal(TimeSpan.FromMinutes(2), optionsAccessor.Value.ShutdownTimeout);
    }

    [Fact]
    public void AddTinyBddHosting_AlsoAddsTinyBddCore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddTinyBddHosting();
        var provider = services.BuildServiceProvider();

        // Assert - Core DI services should be registered
        Assert.NotNull(provider.GetService<IScenarioContextFactory>());
        Assert.NotNull(provider.GetService<ITraitBridge>());
        Assert.NotNull(provider.GetService<IOptions<TinyBddOptions>>());
    }

    [Fact]
    public void AddWorkflowHostedService_RegistersWorkflowType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTinyBddHosting();

        // Act
        services.AddWorkflowHostedService<TestWorkflow>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<TestWorkflow>());
        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s.GetType().Name.Contains("WorkflowHostedService"));
    }

    [Fact]
    public void AddWorkflowHostedService_WithInstance_RegistersProvidedInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTinyBddHosting();
        var workflow = new TestWorkflow();

        // Act
        services.AddWorkflowHostedService(workflow);
        var provider = services.BuildServiceProvider();

        // Assert
        var registered = provider.GetService<TestWorkflow>();
        Assert.Same(workflow, registered);
    }

    [Fact]
    public void UseTinyBdd_OnHostBuilder_RegistersServices()
    {
        // Arrange
        var builder = Host.CreateDefaultBuilder();

        // Act
        builder.UseTinyBdd();
        using var host = builder.Build();

        // Assert
        Assert.NotNull(host.Services.GetService<IWorkflowRunner>());
        Assert.NotNull(host.Services.GetService<IScenarioContextFactory>());
    }

    [Fact]
    public void UseTinyBdd_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var builder = Host.CreateDefaultBuilder();

        // Act
        builder.UseTinyBdd(options =>
        {
            options.StopHostOnCompletion = true;
        });
        using var host = builder.Build();

        // Assert
        var optionsAccessor = host.Services.GetRequiredService<IOptions<TinyBddHostingOptions>>();
        Assert.True(optionsAccessor.Value.StopHostOnCompletion);
    }

    [Fact]
    public void TinyBddHostingOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new TinyBddHostingOptions();

        // Assert
        Assert.False(options.StopHostOnCompletion);
        Assert.True(options.StopHostOnFailure);
        Assert.Equal(TimeSpan.Zero, options.StartupDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ShutdownTimeout);
    }

    private class TestWorkflow : IWorkflowDefinition
    {
        public string FeatureName => "Test Feature";
        public string ScenarioName => "Test Scenario";
        public string? FeatureDescription => null;

        public ValueTask ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
