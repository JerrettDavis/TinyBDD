# TinyBDD.Extensions.FileBased

**File-based DSL extension for TinyBDD** - enabling YAML/JSON scenario definitions with convention-based, source-generated application drivers.

[![NuGet](https://img.shields.io/nuget/v/TinyBDD.Extensions.FileBased.svg)](https://www.nuget.org/packages/TinyBDD.Extensions.FileBased/)

---

## Features

- **File-based scenario definitions** using YAML (JSON support coming soon)
- **Convention-based step matching** via `[DriverMethod]` attributes
- **Type-safe driver methods** with compile-time validation
- **Seamless TinyBDD integration** - leverages existing fluent API and reporting
- **Multi-framework support** - works with xUnit, NUnit, MSTest
- **No runtime reflection glue** - fast, deterministic execution

---

## Installation

```powershell
dotnet add package TinyBDD.Extensions.FileBased
```

Requires TinyBDD and a test framework adapter:
```powershell
dotnet add package TinyBDD.Xunit  # or TinyBDD.NUnit, TinyBDD.MSTest
```

---

## Quick Start

### 1. Define Scenarios in YAML

Create a YAML file (e.g., `Features/Calculator.yml`):

```yaml
feature: Calculator Operations
description: Basic arithmetic operations
tags:
  - calculator
  - smoke

scenarios:
  - name: Add two numbers
    tags:
      - addition
    steps:
      - keyword: Given
        text: a calculator
      - keyword: When
        text: I add 5 and 3
        parameters:
          a: 5
          b: 3
      - keyword: Then
        text: the result should be 8
        parameters:
          expected: 8
```

### 2. Implement an Application Driver

Create a driver class that implements `IApplicationDriver`:

```csharp
using TinyBDD.Extensions.FileBased.Core;

public class CalculatorDriver : IApplicationDriver
{
    private int _result;

    [DriverMethod("a calculator")]
    public Task Initialize()
    {
        _result = 0;
        return Task.CompletedTask;
    }

    [DriverMethod("I add {a} and {b}")]
    public Task Add(int a, int b)
    {
        _result = a + b;
        return Task.CompletedTask;
    }

    [DriverMethod("the result should be {expected}")]
    public Task<bool> VerifyResult(int expected)
    {
        return Task.FromResult(_result == expected);
    }

    // Lifecycle methods
    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task CleanupAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

### 3. Create a Test Class

```csharp
using TinyBDD.Extensions.FileBased;

public class CalculatorTests : FileBasedTestBase<CalculatorDriver>
{
    [Fact]
    public async Task ExecuteCalculatorScenarios()
    {
        await ExecuteScenariosAsync(options =>
        {
            options.AddYamlFiles("Features/Calculator.yml")
                   .WithBaseDirectory(Directory.GetCurrentDirectory());
        });
    }
}
```

### 4. Run Tests

```powershell
dotnet test
```

Output:
```
Feature: Calculator Operations
Scenario: Add two numbers
  Given a calculator [OK] 0 ms
  When I add 5 and 3 [OK] 0 ms
  Then the result should be 8 [OK] 0 ms
```

---

## How It Works

### Step Resolution

Steps in YAML files are matched to driver methods using pattern matching:

1. `[DriverMethod("step with {param}")]` defines a pattern
2. Parameters `{param}` become named capture groups
3. Step text is matched against patterns (case-insensitive)
4. Parameters are extracted and type-converted for method invocation

**Example:**

```csharp
[DriverMethod("I add {a} and {b}")]
public Task Add(int a, int b) { ... }
```

Matches:
- `"I add 5 and 3"` → `Add(5, 3)`
- `"i add 10 and 20"` → `Add(10, 20)` (case-insensitive)

### Parameter Extraction

Parameters can come from:
1. **Pattern placeholders**: Extracted from step text
2. **YAML parameters**: Explicitly defined in the `parameters` section
3. **Method defaults**: When a parameter has a default value

Priority: YAML parameters > Pattern placeholders > Method defaults

**Limitations:**
- Pattern placeholder parameters must not contain whitespace (use YAML `parameters` section for complex values)
- Example: `{email}` matches `"user@example.com"` but not `"John Doe"`

### Boolean Assertions

For `Then` steps, if the driver method returns `Task<bool>`:
- `true` = assertion passes
- `false` = assertion fails (throws exception)

```csharp
[DriverMethod("the user should exist")]
public Task<bool> UserExists() => Task.FromResult(_userExists);
```

---

## Configuration

### Fluent API

```csharp
await ExecuteScenariosAsync(options =>
{
    // Add YAML files
    options.AddYamlFiles("Features/**/*.yml")
    
           // Set base directory for relative paths
           .WithBaseDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".."))
           
           // Use custom driver (inferred from FileBasedTestBase<T>)
           .UseApplicationDriver<MyCustomDriver>();
});
```

### File Discovery

The extension uses [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing/) for file pattern matching:

- `Features/*.yml` - all YAML files in Features directory
- `**/*.yml` - all YAML files recursively
- `Features/Calculator.yml` - specific file

---

## YAML Schema

### Feature

```yaml
feature: Feature Name              # Required
description: Optional description  # Optional
tags:                              # Optional
  - tag1
  - tag2
scenarios:                         # Required
  - # Scenario definitions
```

### Scenario

```yaml
- name: Scenario Name              # Required
  description: Optional description # Optional
  tags:                            # Optional
    - scenario-tag
  steps:                           # Required
    - # Step definitions
```

### Step

```yaml
- keyword: Given|When|Then|And|But  # Required
  text: Step description            # Required
  parameters:                       # Optional
    paramName: value
```

**Keywords:**
- `Given` - Setup/preconditions
- `When` - Actions
- `Then` - Assertions
- `And` - Continues previous keyword
- `But` - Continues previous keyword (with contrast)

---

## Advanced Usage

### Custom Driver Instantiation

Override `CreateDriver()` for dependency injection:

```csharp
public class MyTests : FileBasedTestBase<MyDriver>
{
    protected override MyDriver CreateDriver()
    {
        return new MyDriver(_serviceProvider.GetRequiredService<IMyService>());
    }
}
```

### Multiple Scenarios per File

YAML files can contain multiple scenarios - all will be executed sequentially:

```yaml
feature: User Management
scenarios:
  - name: Register user
    steps: [ ... ]
  
  - name: Login user
    steps: [ ... ]
  
  - name: Delete user
    steps: [ ... ]
```

### Shared Steps

Driver methods are reusable across scenarios:

```csharp
[DriverMethod("the application is running")]
public Task ApplicationIsRunning() { ... }  // Used by multiple scenarios
```

---

## Roadmap

- [ ] JSON DSL support
- [ ] Gherkin `.feature` file support
- [ ] Source generator for driver scaffolding
- [ ] Driver method analyzer (compile-time diagnostics)
- [ ] Step parameter validation
- [ ] Table/data-driven step parameters

---

## Philosophy

This extension aligns with TinyBDD's philosophy:

1. **Code is the spec** - Test code remains the source of truth
2. **No heavy DSLs** - YAML is optional and supplements code-first approach
3. **Framework-agnostic** - Works with existing test frameworks
4. **Performance-first** - Convention over reflection, compile-time safety

File-based scenarios are ideal for:
- **Standardized applications** with well-defined boundaries
- **Business-readable test definitions** authored by non-developers
- **Large test suites** where boilerplate reduction matters

Use code-first TinyBDD when you need:
- Maximum flexibility
- Complex test logic
- Step observers/hooks
- One-off test scenarios

---

## License

[MIT License](../../LICENSE)
