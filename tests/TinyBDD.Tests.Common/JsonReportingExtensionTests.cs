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
}
