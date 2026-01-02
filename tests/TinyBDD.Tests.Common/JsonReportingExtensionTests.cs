using System.IO;
using System.Text.Json;
using TinyBDD.Extensions.Reporting;

namespace TinyBDD.Tests.Common;

public class JsonReportingExtensionTests
{
    [Feature("Extensions")]
    private sealed class Host { }

    [Scenario("JSON report is generated")]
    [Fact]
    public async Task JsonReport_IsGenerated()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            var ctx = Bdd.CreateContext(new Host(), options: options);
            await Bdd.Given(ctx, "start", () => 1)
                .When("add", x => x + 1)
                .Then("is 2", x => x == 2);

            Assert.True(File.Exists(tempFile), "JSON report file should exist");

            var json = File.ReadAllText(tempFile);
            Assert.NotEmpty(json);

            var report = JsonSerializer.Deserialize<JsonReport>(json);
            Assert.NotNull(report);
            Assert.Single(report.Scenarios);

            var scenario = report.Scenarios[0];
            Assert.Equal("Extensions", scenario.FeatureName);
            Assert.True(scenario.Passed);
            Assert.False(scenario.Failed);
            Assert.Equal(3, scenario.Steps.Count);

            Assert.Collection(scenario.Steps,
                s => Assert.Equal("Given", s.Kind),
                s => Assert.Equal("When", s.Kind),
                s => Assert.Equal("Then", s.Kind));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Scenario("JSON report captures step IO")]
    [Fact]
    public async Task JsonReport_CapturesStepIO()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            var ctx = Bdd.CreateContext(new Host(), options: options);
            await Bdd.Given(ctx, "start", () => 5)
                .When("double", x => x * 2)
                .Then("is 10", x => x == 10);

            var json = File.ReadAllText(tempFile);
            var report = JsonSerializer.Deserialize<JsonReport>(json);
            Assert.NotNull(report);

            var whenStep = report.Scenarios[0].Steps.FirstOrDefault(s => s.Kind == "When");
            Assert.NotNull(whenStep);
            Assert.Equal(5, ((JsonElement)whenStep.Input!).GetInt32());
            Assert.Equal(10, ((JsonElement)whenStep.Output!).GetInt32());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Scenario("JSON report captures failures")]
    [Fact]
    public async Task JsonReport_CapturesFailures()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            var ctx = Bdd.CreateContext(new Host(), options: options with { ContinueOnError = true });
            
            await Bdd.Given(ctx, "start", () => 1)
                .When("fail", FailingAction)
                .Then("never reached", x => x == 1);

            var json = File.ReadAllText(tempFile);
            var report = JsonSerializer.Deserialize<JsonReport>(json);
            Assert.NotNull(report);

            var scenario = report.Scenarios[0];
            Assert.False(scenario.Passed);
            Assert.True(scenario.Failed);

            var failedStep = scenario.Steps.FirstOrDefault(s => s.Kind == "When");
            Assert.NotNull(failedStep);
            Assert.False(failedStep.Passed);
            Assert.NotNull(failedStep.Error);
            Assert.Contains("Test failure", failedStep.Error);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static void FailingAction(int x)
    {
        throw new InvalidOperationException("Test failure");
    }

    [Scenario("Multiple scenarios appended to report")]
    [Fact]
    public async Task MultipleScenarios_AppendedToReport()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            // First scenario
            var ctx1 = Bdd.CreateContext(new Host(), scenarioName: "Scenario1", options: options);
            await Bdd.Given(ctx1, "start", () => 1)
                .Then("is 1", x => x == 1);

            // Second scenario
            var ctx2 = Bdd.CreateContext(new Host(), scenarioName: "Scenario2", options: options);
            await Bdd.Given(ctx2, "start", () => 2)
                .Then("is 2", x => x == 2);

            var json = File.ReadAllText(tempFile);
            var report = JsonSerializer.Deserialize<JsonReport>(json);
            Assert.NotNull(report);
            Assert.Equal(2, report.Scenarios.Length);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Scenario("JSON report with custom serialization options")]
    [Fact]
    public async Task JsonReport_WithCustomSerializationOptions()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var customOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            var options = Bdd.Configure(b => b.AddJsonReport(tempFile, customOptions));

            var ctx = Bdd.CreateContext(new Host(), options: options);
            await Bdd.Given(ctx, "start", () => 1)
                .Then("is 1", x => x == 1);

            var json = File.ReadAllText(tempFile);
            Assert.NotEmpty(json);
            // Non-indented JSON should not have many newlines
            Assert.True(json.Split('\n').Length < 10, "JSON should not be indented");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Scenario("JSON report captures tags")]
    [Fact]
    public async Task JsonReport_CapturesTags()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            var ctx = Bdd.CreateContext(new Host(), options: options);
            ctx.AddTag("smoke");
            ctx.AddTag("critical");
            
            await Bdd.Given(ctx, "start", () => 1)
                .Then("is 1", x => x == 1);

            var json = File.ReadAllText(tempFile);
            var report = JsonSerializer.Deserialize<JsonReport>(json);
            Assert.NotNull(report);

            var scenario = report.Scenarios[0];
            Assert.Contains("smoke", scenario.Tags);
            Assert.Contains("critical", scenario.Tags);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Scenario("JSON report captures duration")]
    [Fact]
    public async Task JsonReport_CapturesDuration()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            var ctx = Bdd.CreateContext(new Host(), options: options);
            await Bdd.Given(ctx, "start", () => 1)
                .When("delay", DelayAndAdd)
                .Then("is 2", x => x == 2);

            var json = File.ReadAllText(tempFile);
            var report = JsonSerializer.Deserialize<JsonReport>(json);
            Assert.NotNull(report);

            var scenario = report.Scenarios[0];
            Assert.True(scenario.Duration > TimeSpan.Zero, "Duration should be positive");
            Assert.True(scenario.EndTime > scenario.StartTime, "End time should be after start time");
            
            // Check step durations
            foreach (var step in scenario.Steps)
            {
                Assert.True(step.Duration >= TimeSpan.Zero, "Step duration should not be negative");
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static async Task<int> DelayAndAdd(int x)
    {
        await Task.Delay(10);
        return x + 1;
    }

    [Scenario("JSON report handles directory creation")]
    [Fact]
    public async Task JsonReport_HandlesDirectoryCreation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempFile = Path.Combine(tempDir, "report.json");
        
        try
        {
            Assert.False(Directory.Exists(tempDir), "Directory should not exist initially");

            var options = Bdd.Configure(b => b.AddJsonReport(tempFile));

            var ctx = Bdd.CreateContext(new Host(), options: options);
            await Bdd.Given(ctx, "start", () => 1)
                .Then("is 1", x => x == 1);

            Assert.True(File.Exists(tempFile), "Report file should be created");
            Assert.True(Directory.Exists(tempDir), "Directory should be created");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Scenario("JSON report observer null path throws")]
    [Fact]
    public void JsonReportObserver_NullPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonReportObserver(null!));
    }
}
