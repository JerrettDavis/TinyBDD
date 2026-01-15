# TinyBDD.Extensions.FileBased

**File-based DSL extension for TinyBDD** - enabling Gherkin .feature files and YAML scenario definitions with convention-based application drivers.

[![NuGet](https://img.shields.io/nuget/v/TinyBDD.Extensions.FileBased.svg)](https://www.nuget.org/packages/TinyBDD.Extensions.FileBased/)

---

## Features

- **Gherkin .feature files** - First-class support for standard Gherkin syntax (recommended)
- **YAML scenarios** - Alternative format for tooling and automation
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

## Quick Start (Gherkin)

### 1. Define Scenarios in Gherkin

Create a .feature file (e.g., `Features/Calculator.feature`):

```gherkin
Feature: Calculator Operations

@calculator @smoke
Scenario: Add two numbers
  Given a calculator
  When I add 5 and 3
  Then the result should be 8
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
            options.AddFeatureFiles("Features/**/*.feature")
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

## Alternative: YAML Format

For tooling and automation scenarios, YAML is also supported:

```yaml
feature: Calculator Operations
scenarios:
  - name: Add two numbers
    steps:
      - keyword: When
        text: I add 5 and 3
        parameters: { a: 5, b: 3 }
```

Use the same driver implementation and test class structure, but call `AddYamlFiles()` instead:

```csharp
options.AddYamlFiles("Features/**/*.yml")
```

---

## How It Works

### Gherkin Syntax

Standard Gherkin keywords are supported:
- `Feature:` - Feature name and description
- `Scenario:` - Individual test scenarios
- `Given/When/Then/And/But` - Test steps
- `@tags` - Scenario tagging

Example:
```gherkin
Feature: User Management
  User authentication and registration

@auth @smoke
Scenario: Login with valid credentials
  Given the application is running
  When I login with username "admin" and password "secret"
  Then I should be logged in
```

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
    // Add Gherkin .feature files (recommended)
    options.AddFeatureFiles("Features/**/*.feature")
    
           // OR: Add YAML files for tooling scenarios
           //.AddYamlFiles("Features/**/*.yml")
    
           // Set base directory for relative paths
           .WithBaseDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".."))
           
           // Use custom driver (inferred from FileBasedTestBase<T>)
           .UseApplicationDriver<MyCustomDriver>();
});
```

### File Discovery

The extension uses [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing/) for file pattern matching:

- `Features/*.feature` - all .feature files in Features directory
- `**/*.feature` - all .feature files recursively
- `Features/Calculator.feature` - specific file

---

## Format Reference

### Gherkin .feature Files (Recommended)

Standard Gherkin syntax with full support for:
- Feature definitions with descriptions
- Scenarios with tags
- Given/When/Then/And/But steps
- Quoted string parameters (automatically extracted)
- Multiple tags per scenario (`@tag1 @tag2`)

See examples in the [Quick Start](#quick-start-gherkin) section above.

### YAML Schema (Alternative)

For tooling scenarios, YAML provides programmatic scenario definition:

#### Feature

```yaml
feature: Feature Name              # Required
description: Optional description  # Optional
scenarios:                         # Required
  - # Scenario definitions
```

#### Scenario

```yaml
- name: Scenario Name              # Required
  description: Optional description # Optional
  tags:                            # Optional
    - scenario-tag
  steps:                           # Required
    - # Step definitions
```

#### Step

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
- [ ] Source generator for driver scaffolding
- [ ] Driver method analyzer (compile-time diagnostics)
- [ ] Step parameter validation
- [x] Table/data-driven step parameters (Scenario Outline support)

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
