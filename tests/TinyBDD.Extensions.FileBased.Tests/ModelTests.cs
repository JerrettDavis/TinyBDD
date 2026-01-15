using TinyBDD.Extensions.FileBased.Models;

namespace TinyBDD.Extensions.FileBased.Tests;

public class ModelTests
{
    [Fact]
    public void FeatureDefinition_DefaultConstructor_InitializesProperties()
    {
        // Arrange & Act
        var feature = new FeatureDefinition();

        // Assert
        Assert.NotNull(feature.Name);
        Assert.Empty(feature.Name);
        Assert.Null(feature.Description);
        Assert.NotNull(feature.Scenarios);
        Assert.Empty(feature.Scenarios);
        Assert.NotNull(feature.Tags);
        Assert.Empty(feature.Tags);
    }

    [Fact]
    public void FeatureDefinition_SetProperties_StoresValues()
    {
        // Arrange
        var feature = new FeatureDefinition();
        var scenario = new ScenarioDefinition { Name = "Test Scenario" };

        // Act
        feature.Name = "Test Feature";
        feature.Description = "Feature Description";
        feature.Scenarios.Add(scenario);
        feature.Tags.Add("@smoke");

        // Assert
        Assert.Equal("Test Feature", feature.Name);
        Assert.Equal("Feature Description", feature.Description);
        Assert.Single(feature.Scenarios);
        Assert.Equal("Test Scenario", feature.Scenarios[0].Name);
        Assert.Single(feature.Tags);
        Assert.Equal("@smoke", feature.Tags[0]);
    }

    [Fact]
    public void ScenarioDefinition_DefaultConstructor_InitializesProperties()
    {
        // Arrange & Act
        var scenario = new ScenarioDefinition();

        // Assert
        Assert.NotNull(scenario.Name);
        Assert.Empty(scenario.Name);
        Assert.Null(scenario.Description);
        Assert.NotNull(scenario.Steps);
        Assert.Empty(scenario.Steps);
        Assert.NotNull(scenario.Tags);
        Assert.Empty(scenario.Tags);
    }

    [Fact]
    public void ScenarioDefinition_SetProperties_StoresValues()
    {
        // Arrange
        var scenario = new ScenarioDefinition();
        var step = new StepDefinition { Keyword = "Given", Text = "a precondition" };

        // Act
        scenario.Name = "Test Scenario";
        scenario.Description = "Scenario Description";
        scenario.Steps.Add(step);
        scenario.Tags.Add("@regression");

        // Assert
        Assert.Equal("Test Scenario", scenario.Name);
        Assert.Equal("Scenario Description", scenario.Description);
        Assert.Single(scenario.Steps);
        Assert.Equal("Given", scenario.Steps[0].Keyword);
        Assert.Single(scenario.Tags);
        Assert.Equal("@regression", scenario.Tags[0]);
    }

    [Fact]
    public void StepDefinition_DefaultConstructor_InitializesProperties()
    {
        // Arrange & Act
        var step = new StepDefinition();

        // Assert
        Assert.NotNull(step.Keyword);
        Assert.Empty(step.Keyword);
        Assert.NotNull(step.Text);
        Assert.Empty(step.Text);
        Assert.NotNull(step.Parameters);
        Assert.Empty(step.Parameters);
    }

    [Fact]
    public void StepDefinition_SetProperties_StoresValues()
    {
        // Arrange
        var step = new StepDefinition();

        // Act
        step.Keyword = "When";
        step.Text = "I perform an action";
        step.Parameters["param1"] = "value1";
        step.Parameters["param2"] = 42;

        // Assert
        Assert.Equal("When", step.Keyword);
        Assert.Equal("I perform an action", step.Text);
        Assert.Equal(2, step.Parameters.Count);
        Assert.Equal("value1", step.Parameters["param1"]);
        Assert.Equal(42, step.Parameters["param2"]);
    }

    [Fact]
    public void StepDefinition_Parameters_SupportsNullValues()
    {
        // Arrange
        var step = new StepDefinition();

        // Act
        step.Parameters["nullParam"] = null;

        // Assert
        Assert.Single(step.Parameters);
        Assert.Null(step.Parameters["nullParam"]);
    }
}
