# Core Concepts

This section provides in-depth coverage of PatternKit's fundamental concepts and architecture. Understanding these concepts will help you use PatternKit effectively in any scenario.

## Architecture Overview

PatternKit is built around four core concepts:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Workflow                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    WorkflowContext                          ││
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   ││
│  │  │ Metadata │  │  Steps   │  │    IO    │  │Extensions│   ││
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘   ││
│  └─────────────────────────────────────────────────────────────┘│
│                              │                                   │
│                    ExecutionPipeline                            │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                  Fluent Chain API                           ││
│  │   Given ─→ When ─→ And ─→ But ─→ Then ─→ And ─→ Finally   ││
│  │      │         │         │        │         │               ││
│  │      └─────────┴─────────┴────────┴─────────┘               ││
│  │                    Behaviors                                 ││
│  │              (Timing, Retry, Circuit)                       ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

## Documentation

| Topic | Description |
|-------|-------------|
| [Workflows](workflows.md) | Understanding workflow execution, chains, and contexts |
| [Steps and Phases](steps.md) | Step types, phases, and execution flow |
| [Behaviors](behaviors.md) | Cross-cutting concerns with the behavior system |
| [Step Handlers](handlers.md) | Mediator pattern for step implementation |
| [Dependency Injection](dependency-injection.md) | Integration with Microsoft.Extensions.DependencyInjection |
| [Hosting](hosting.md) | Running workflows as hosted services |

## Quick Reference

### The Fluent API

PatternKit uses a fluent builder pattern:

```csharp
await Workflow
    .Given(context, title, initializer)    // Start with preconditions
    .And(title, transform)                  // Additional setup
    .When(title, action)                    // Perform actions
    .And(title, action)                     // Additional actions
    .But(title, action)                     // Contrasting actions
    .Then(title, assertion)                 // Verify outcomes
    .And(title, assertion)                  // Additional assertions
    .Finally(title, cleanup);               // Always-run cleanup
```

### Workflow Lifecycle

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│    Setup     │ ──▶ │    Action    │ ──▶ │    Assert    │
│   (Given)    │     │    (When)    │     │    (Then)    │
└──────────────┘     └──────────────┘     └──────────────┘
       │                    │                    │
       │    And / But       │    And / But       │    And / But
       ▼                    ▼                    ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Additional  │     │  Additional  │     │  Additional  │
│    Setup     │     │   Actions    │     │  Assertions  │
└──────────────┘     └──────────────┘     └──────────────┘
                                                 │
                                                 ▼
                                          ┌──────────────┐
                                          │   Cleanup    │
                                          │  (Finally)   │
                                          └──────────────┘
```

### Key Types

| Type | Purpose |
|------|---------|
| `WorkflowContext` | Central state container for workflow execution |
| `WorkflowChain<T>` | Fluent builder for composing steps |
| `ResultChain<T>` | Terminal chain that can be awaited |
| `StepResult` | Captures individual step execution results |
| `WorkflowOptions` | Configures workflow execution behavior |
| `IBehavior<T>` | Interface for cross-cutting behaviors |
| `IStepHandler<TReq, TRes>` | Interface for mediated step handlers |

### Value Flow

Values flow through the workflow pipeline:

```csharp
await Workflow
    .Given(ctx, "start", () => 1)           // T = int (value: 1)
    .When("double", x => x * 2)             // T = int (value: 2)
    .When("to string", x => x.ToString())   // T = string (value: "2")
    .Then("not empty", s => s.Length > 0);  // T = string (unchanged)
```

Each step receives the output of the previous step and produces a new value.

### Error Handling

PatternKit distinguishes between:

1. **Assertion Failures** - Expected failures from `Then` predicates returning `false`
2. **Step Exceptions** - Unexpected exceptions during step execution
3. **Workflow Exceptions** - Infrastructure-level errors

```csharp
try
{
    await workflow.AssertPassed();
}
catch (WorkflowAssertionException ex)
{
    // A Then predicate returned false
    Console.WriteLine($"Assertion failed: {ex.Message}");
}
catch (WorkflowStepException ex)
{
    // A step threw an exception
    Console.WriteLine($"Step failed: {ex.Message}");
    Console.WriteLine($"At step: {ex.Context.FirstFailure?.Title}");
}
```

## Design Principles

PatternKit follows these design principles:

### 1. Zero Dependencies for Core

`PatternKit.Core` has no external dependencies, ensuring:
- Minimal footprint
- No version conflicts
- Maximum compatibility

### 2. AoT Compatibility

All code is designed for ahead-of-time compilation:
- No `System.Reflection.Emit`
- No dynamic type generation
- Trimmer-safe implementations
- Source generator friendly

### 3. Performance First

Every API is designed for performance:
- `ValueTask` for allocation-free async
- State-passing to avoid closures
- Struct-based types where appropriate
- Aggressive inlining hints

### 4. Extensibility

Multiple extension points:
- `IWorkflowExtension` for context attachments
- `IBehavior<T>` for execution middleware
- `IStepHandler<TReq, TRes>` for step mediation
- Hook callbacks for step lifecycle

### 5. Composition over Inheritance

All extensibility uses composition:
- Behaviors wrap step execution
- Extensions attach to contexts
- Handlers implement interfaces
- No base classes to extend
