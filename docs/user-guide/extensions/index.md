# TinyBDD Extensions

TinyBDD provides extension packages that integrate the BDD workflow engine with enterprise .NET infrastructure, enabling TinyBDD to serve as a general-purpose application orchestrator beyond testing.

## Available Extensions

### [Dependency Injection](dependency-injection.md)
**TinyBDD.Extensions.DependencyInjection** integrates TinyBDD with `Microsoft.Extensions.DependencyInjection`, providing:
- `IScenarioContextFactory` for DI-aware context creation
- `AddTinyBdd()` service collection extension
- Configurable `TinyBddOptions` for default scenario behavior
- Custom trait bridge registration

### [Hosting](hosting.md)
**TinyBDD.Extensions.Hosting** integrates TinyBDD with `Microsoft.Extensions.Hosting`, enabling:
- BDD workflows as hosted services via `IWorkflowDefinition`
- `IWorkflowRunner` for programmatic workflow execution
- `WorkflowHostedService<T>` for background workflow execution
- `UseTinyBdd()` host builder extensions

### [Reporting](reporting.md)
**TinyBDD.Extensions.Reporting** provides structured JSON reporting through the observer pattern:
- `JsonReportObserver` for capturing scenario execution data
- JSON reports suitable for CI/CD artifacts and trend analysis
- Integration with TinyBDD's observer extensibility layer
- Configurable JSON serialization options

### [File-Based DSL](file-based.md)
**TinyBDD.Extensions.FileBased** enables writing BDD scenarios in external files:
- Gherkin .feature files for business-readable specifications
- YAML format for programmatic test generation
- Convention-based driver methods with `[DriverMethod]` attributes
- Scenario Outline support with Examples tables
- Multi-framework support (xUnit, NUnit, MSTest)

## Why Use TinyBDD as an Orchestrator?

TinyBDD's Given/When/Then pattern isn't just for testing. The same structured approach works for:

| Use Case | Description |
|----------|-------------|
| **Application Bootstrapping** | Define startup sequences as clear, traceable steps |
| **Data Pipelines** | Express ETL workflows with explicit preconditions and validations |
| **Saga Orchestration** | Coordinate distributed transactions with observable step outcomes |
| **Health Checks** | Structure diagnostic sequences with Given/When/Then semantics |
| **Batch Processing** | Define batch job workflows with clear phase separation |
| **Integration Workflows** | Orchestrate multi-service interactions with step-by-step traceability |

## Core Benefits

### Traceability
Every step is recorded with timing, input/output, and error information. This provides production-grade observability for any workflow.

### Testability
The same workflow definitions can run in both production and test environments. Write your workflow once, test it thoroughly, deploy with confidence.

### Composability
Workflows can be composed, extended, and customized through the fluent API. Build complex orchestrations from simple, reusable steps.

### Error Handling
Built-in support for continuation on error, step timeouts, and graceful failure handling. Configure behavior per-scenario with `ScenarioOptions`.

## Getting Started

Install the packages:
```bash
dotnet add package TinyBDD.Extensions.DependencyInjection
dotnet add package TinyBDD.Extensions.Hosting
dotnet add package TinyBDD.Extensions.Reporting
dotnet add package TinyBDD.Extensions.FileBased
```

Register services:
```csharp
services.AddTinyBdd(options =>
{
    options.DefaultScenarioOptions = new ScenarioOptions
    {
        ContinueOnError = false,
        StepTimeout = TimeSpan.FromSeconds(30)
    };
});
```

Define and run workflows:
```csharp
public class StartupWorkflow : IWorkflowDefinition
{
    public string FeatureName => "Application Startup";
    public string ScenarioName => "Initialize services";
    public string? FeatureDescription => null;

    public async ValueTask ExecuteAsync(ScenarioContext context, CancellationToken ct)
    {
        await Bdd.Given(context, "configuration loaded", () => LoadConfiguration())
            .When("database connected", cfg => ConnectDatabase(cfg))
            .And("cache warmed", db => WarmCache(db))
            .Then("ready to serve", state => state.IsReady);
    }
}
```

## Next Steps

- [File-Based DSL Guide](file-based.md) - Gherkin and YAML scenario files
- [Dependency Injection Guide](dependency-injection.md) - Full DI integration reference
- [Hosting Guide](hosting.md) - Hosted service patterns and configuration
- [Reporting Guide](reporting.md) - JSON reporting and observer pattern
- [Orchestrator Patterns](../orchestrator-patterns.md) - Advanced workflow patterns
- [Enterprise Samples](../samples-enterprise.md) - Production-ready examples

Return to: [User Guide](../index.md)
