using TinyBDD.Extensions.FileBased.Core;

namespace TinyBDD.Extensions.FileBased.Tests;

public class FileBasedTestBaseTests
{
    [Fact]
    public async Task ExecuteScenariosAsync_WithNoMatchingFiles_ThrowsInvalidOperationException()
    {
        // Arrange
        var testClass = new TestFileBasedTest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            testClass.PublicExecuteScenariosAsync(opt =>
            {
                opt.AddYamlFiles("NonExistentDirectory/*.yml");
            }));

        Assert.Contains("No scenario files found", exception.Message);
    }

    [Fact]
    public async Task ExecuteScenarioAsync_WithValidScenario_ExecutesSuccessfully()
    {
        // Arrange
        var testClass = new TestFileBasedTest();
        var feature = TestHelper.CreateTestFeature();
        var scenario = feature.Scenarios[0];

        // Act
        await testClass.PublicExecuteScenarioAsync(feature, scenario);

        // Assert
        Assert.True(testClass.DriverWasUsed);
    }

    [Fact]
    public void CreateDriver_CanBeOverridden()
    {
        // Arrange & Act
        var testClass = new CustomDriverTestClass();
        var driver = testClass.PublicCreateDriver();

        // Assert
        Assert.NotNull(driver);
        Assert.True(driver.WasCustomCreated);
    }

    [Fact]
    public void CreateDriver_DefaultImplementation_CreatesNewInstance()
    {
        // Arrange
        var testClass = new TestFileBasedTest();

        // Act
        var driver1 = testClass.PublicCreateDriver();
        var driver2 = testClass.PublicCreateDriver();

        // Assert
        Assert.NotNull(driver1);
        Assert.NotNull(driver2);
        Assert.NotSame(driver1, driver2);
    }

    private class TestFileBasedTest : FileBasedTestBase<TestTrackingDriver>
    {
        public bool DriverWasUsed { get; private set; }

        public async Task PublicExecuteScenariosAsync(
            Action<Configuration.FileBasedDslOptionsBuilder> configureOptions,
            CancellationToken cancellationToken = default)
        {
            await ExecuteScenariosAsync(configureOptions, null, cancellationToken);
            DriverWasUsed = true;
        }

        public async Task PublicExecuteScenarioAsync(
            Models.FeatureDefinition feature,
            Models.ScenarioDefinition scenario,
            ITraitBridge? traitBridge = null,
            CancellationToken cancellationToken = default)
        {
            await ExecuteScenarioAsync(feature, scenario, traitBridge, cancellationToken);
            DriverWasUsed = true;
        }

        public TestTrackingDriver PublicCreateDriver()
        {
            return CreateDriver();
        }
    }

    private class CustomDriverTestClass : FileBasedTestBase<TestTrackingDriver>
    {
        protected override TestTrackingDriver CreateDriver()
        {
            return new TestTrackingDriver { WasCustomCreated = true };
        }

        public TestTrackingDriver PublicCreateDriver()
        {
            return CreateDriver();
        }
    }

    private class TestTrackingDriver : IApplicationDriver
    {
        public bool WasCustomCreated { get; set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CleanupAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        [DriverMethod("a calculator")]
        public Task GivenCalculator()
        {
            return Task.CompletedTask;
        }

        [DriverMethod("I add {a} and {b}")]
        public Task Add(int a, int b)
        {
            return Task.CompletedTask;
        }

        [DriverMethod("the result should be {expected}")]
        public Task<bool> VerifyResult(int expected)
        {
            return Task.FromResult(true);
        }
    }
}
