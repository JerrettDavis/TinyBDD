---
uid: samples.file-based.calculator
title: Sample - Calculator Testing with File-Based DSL
description: Demonstrates basic file-based DSL usage with Gherkin .feature files, driver methods, and test execution
ms.topic: sample
---

# Sample: Calculator Testing with File-Based DSL

> **Scenario**: Test a simple calculator application using Gherkin .feature files
> **Demonstrates**: Gherkin syntax, driver methods, parameter extraction, boolean assertions, Scenario Outlines
> **Complexity**: Beginner
> **Source Code**: [GitHub](https://github.com/JerrettDavis/TinyBDD/tree/main/tests/TinyBDD.Extensions.FileBased.Tests)

## Overview

This sample shows how to test a calculator using TinyBDD's File-Based DSL extension with Gherkin .feature files. It demonstrates the fundamental patterns for writing business-readable test specifications and implementing driver methods.

The sample covers:
- Creating Gherkin .feature files
- Implementing application drivers with `[DriverMethod]` attributes
- Parameter extraction from step text
- Boolean assertions for Then steps
- Scenario Outlines with Examples tables
- Test class configuration and execution

## Prerequisites

- .NET 8.0 or later
- TinyBDD.Extensions.FileBased 3.0.0+
- TinyBDD.Xunit 3.0.0+ (or your preferred test framework)
- xUnit test runner

## Project Structure

```text
CalculatorTesting/
├── Features/
│   ├── Calculator.feature
│   └── ScenarioOutline.feature
├── Drivers/
│   └── CalculatorDriver.cs
├── Tests/
│   └── CalculatorTests.cs
└── Calculator.cs
```

## Implementation

### Step 1: Create Application Under Test

**Why**: Need something to test. Keep it simple for this demonstration.

```csharp
// Calculator.cs
public class Calculator
{
    private int _result;

    public void Clear()
    {
        _result = 0;
    }

    public void Add(int a, int b)
    {
        _result = a + b;
    }

    public void Multiply(int a, int b)
    {
        _result = a * b;
    }

    public int GetResult()
    {
        return _result;
    }
}
```

### Step 2: Write Gherkin Feature Files

**Why**: Gherkin provides business-readable test specifications that non-developers can understand and potentially author.

Create `Features/Calculator.feature`:

```gherkin
Feature: Calculator Operations
  Testing basic arithmetic operations
  to ensure calculator works correctly

@calculator @smoke
Scenario: Add two numbers
  Given a calculator
  When I add 5 and 3
  Then the result should be 8

@calculator @multiplication
Scenario: Multiply two numbers
  Given a calculator
  When I multiply 4 and 7
  Then the result should be 28
```

Create `Features/ScenarioOutline.feature`:

```gherkin
Feature: Scenario Outline Examples
  Testing parameterized scenarios with examples

@outline @smoke
Scenario Outline: Multiply two numbers
  Given a calculator
  When I multiply <a> and <b>
  Then the result should be <expected>

Examples:
  | a | b | expected |
  | 2 | 3 | 6        |
  | 4 | 5 | 20       |
  | 0 | 9 | 0        |
  | 10 | 10 | 100     |
```

### Step 3: Implement Driver

**Why**: Drivers bridge the gap between business-readable specifications and test implementation.

```csharp
// Drivers/CalculatorDriver.cs
using TinyBDD.Extensions.FileBased.Core;

/// <summary>
/// Application driver for calculator tests.
/// Implements IApplicationDriver to work with file-based DSL.
/// </summary>
public class CalculatorDriver : IApplicationDriver
{
    private readonly Calculator _calculator;

    public CalculatorDriver()
    {
        _calculator = new Calculator();
    }

    /// <summary>
    /// Matches: "a calculator"
    /// Initializes calculator to clean state.
    /// </summary>
    [DriverMethod("a calculator")]
    public Task Initialize()
    {
        _calculator.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Matches: "I add 5 and 3" -> Add(5, 3)
    /// Pattern placeholders {a} and {b} are extracted and converted to int.
    /// </summary>
    [DriverMethod("I add {a} and {b}")]
    public Task Add(int a, int b)
    {
        _calculator.Add(a, b);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Matches: "I multiply 4 and 7" -> Multiply(4, 7)
    /// Demonstrates same pattern as Add with different operation.
    /// </summary>
    [DriverMethod("I multiply {a} and {b}")]
    public Task Multiply(int a, int b)
    {
        _calculator.Multiply(a, b);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Matches: "the result should be 8" -> VerifyResult(8)
    /// Returns Task&lt;bool&gt; for assertion:
    /// - true = test passes
    /// - false = test fails with step description as error message
    /// </summary>
    [DriverMethod("the result should be {expected}")]
    public Task<bool> VerifyResult(int expected)
    {
        var actual = _calculator.GetResult();
        return Task.FromResult(actual == expected);
    }

    /// <summary>
    /// Called once before all scenarios execute.
    /// Use for expensive setup (database connections, service initialization).
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // No expensive setup needed for calculator
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called once after all scenarios complete.
    /// Use for cleanup (close connections, dispose resources).
    /// </summary>
    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        // No cleanup needed for calculator
        return Task.CompletedTask;
    }
}
```

### Step 4: Create Test Class

**Why**: Test class configures file discovery and executes scenarios through the test framework.

```csharp
// Tests/CalculatorTests.cs
using TinyBDD.Extensions.FileBased;
using TinyBDD.Xunit;
using Xunit;

[Feature("File-Based DSL - Calculator")]
public class CalculatorTests : FileBasedTestBase<CalculatorDriver>
{
    [Scenario("Execute calculator scenarios")]
    [Fact]
    public async Task ExecuteCalculatorScenarios()
    {
        await ExecuteScenariosAsync(options =>
        {
            options.AddFeatureFiles("Features/Calculator.feature")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }

    [Scenario("Execute scenario outlines")]
    [Fact]
    public async Task ExecuteScenarioOutlines()
    {
        await ExecuteScenariosAsync(options =>
        {
            options.AddFeatureFiles("Features/ScenarioOutline.feature")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }

    [Scenario("Execute all calculator features")]
    [Fact]
    public async Task ExecuteAllFeatures()
    {
        // Can use glob patterns to execute multiple feature files
        await ExecuteScenariosAsync(options =>
        {
            options.AddFeatureFiles("Features/**/*.feature")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }
}
```

## Running the Sample

### Execute Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~ExecuteCalculatorScenarios"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Expected Output

```
Feature: Calculator Operations
Scenario: Add two numbers
  Given a calculator [OK] 0 ms
  When I add 5 and 3 [OK] 0 ms
  Then the result should be 8 [OK] 1 ms

Scenario: Multiply two numbers
  Given a calculator [OK] 0 ms
  When I multiply 4 and 7 [OK] 0 ms
  Then the result should be 28 [OK] 0 ms

Feature: Scenario Outline Examples
Scenario Outline: Multiply two numbers (Example 1: a=2, b=3, expected=6)
  Given a calculator [OK] 0 ms
  When I multiply 2 and 3 [OK] 0 ms
  Then the result should be 6 [OK] 0 ms

Scenario Outline: Multiply two numbers (Example 2: a=4, b=5, expected=20)
  Given a calculator [OK] 0 ms
  When I multiply 4 and 5 [OK] 0 ms
  Then the result should be 20 [OK] 1 ms

...
```

## Best Practices Demonstrated

### 1. Clear Step Descriptions

Steps describe behavior, not implementation details:

```gherkin
# Good: Business-readable
Given a calculator
When I add 5 and 3
Then the result should be 8

# Avoid: Implementation details
Given Calculator object instantiated
When Add method called with parameters 5, 3
Then _result field equals 8
```

### 2. Thin Driver Methods

Drivers delegate to application code rather than reimplementing logic:

```csharp
// Good: Thin wrapper
[DriverMethod("I add {a} and {b}")]
public Task Add(int a, int b)
{
    _calculator.Add(a, b);  // Delegate to application
    return Task.CompletedTask;
}

// Avoid: Reimplementation
[DriverMethod("I add {a} and {b}")]
public Task Add(int a, int b)
{
    _result = a + b;  // Don't reimplement calculator here
    return Task.CompletedTask;
}
```

### 3. Boolean Assertions for Then Steps

Returning `Task<bool>` provides clean assertion syntax:

```csharp
// Good: Boolean return
[DriverMethod("the result should be {expected}")]
public Task<bool> VerifyResult(int expected)
{
    return Task.FromResult(_calculator.GetResult() == expected);
}

// Also works: Assertion library
[DriverMethod("the result should be {expected}")]
public Task VerifyResult(int expected)
{
    _calculator.GetResult().Should().Be(expected);
    return Task.CompletedTask;
}
```

### 4. Parameterized Tests with Scenario Outline

Use Scenario Outline for testing multiple input combinations:

```gherkin
Scenario Outline: Operation
  When I multiply <a> and <b>
  Then the result should be <expected>

Examples:
  | a | b | expected |
  | 2 | 3 | 6        |
  | 4 | 5 | 20       |
```

This generates separate test cases, providing better failure isolation than a single test with loops.

## Configuration

### Dependency Injection

For drivers requiring dependencies:

```csharp
public class CalculatorTests : FileBasedTestBase<CalculatorDriver>
{
    private readonly IServiceProvider _serviceProvider;

    public CalculatorTests()
    {
        // Configure services
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, ConsoleLogger>();
        _serviceProvider = services.BuildServiceProvider();
    }

    protected override CalculatorDriver CreateDriver()
    {
        return new CalculatorDriver(
            _serviceProvider.GetRequiredService<ILogger>());
    }
}
```

### Multiple Feature Files

Use glob patterns for file discovery:

```csharp
// Single directory
options.AddFeatureFiles("Features/*.feature")

// Recursive
options.AddFeatureFiles("Features/**/*.feature")

// Multiple patterns
options.AddFeatureFiles("Features/Smoke/**/*.feature")
       .AddFeatureFiles("Features/Regression/**/*.feature")
```

## Deployment Considerations

### Checklist

- [ ] Feature files included in build output (Copy to Output Directory)
- [ ] File paths configured for CI environment
- [ ] Parallel test execution configured appropriately
- [ ] Test results captured in CI-friendly format

### File Inclusion

Ensure feature files are copied to output:

```xml
<ItemGroup>
  <None Update="Features\**\*.feature">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### CI Configuration

```yaml
# GitHub Actions example
- name: Run file-based tests
  run: |
    dotnet test --logger "trx;LogFileName=test-results.trx"

- name: Publish test results
  uses: dorny/test-reporter@v1
  with:
    name: File-Based DSL Tests
    path: '**/*.trx'
    reporter: dotnet-trx
```

## Additional Resources

- [File-Based DSL Guide](../../user-guide/extensions/file-based.md) - Complete reference
- [Gherkin Syntax](../../user-guide/extensions/file-based-gherkin.md) - Gherkin details
- [API Reference: IApplicationDriver](xref:TinyBDD.Extensions.FileBased.Core.IApplicationDriver) - Driver interface
- [Source Code](https://github.com/JerrettDavis/TinyBDD/tree/main/tests/TinyBDD.Extensions.FileBased.Tests) - Full implementation

Return to: [Samples Index](../index.md) | [User Guide](../../user-guide/index.md)
