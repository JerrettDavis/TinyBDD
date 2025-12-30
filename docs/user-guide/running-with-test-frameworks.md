# Running with Test Frameworks

This guide provides framework-specific instructions for running TinyBDD scenarios with xUnit (v2 and v3), NUnit, and MSTest, including CLI commands, IDE integration, and CI/CD pipeline configuration.

## xUnit v2

xUnit v2 is the current stable version widely used in .NET projects.

### Installation

Add both the core library and xUnit adapter:

```bash
dotnet add package TinyBDD
dotnet add package TinyBDD.Xunit
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
```

### Project Setup

Create a test project with xUnit:

```bash
# Create a new xUnit test project
dotnet new xunit -n MyApp.Tests

# Navigate to the project
cd MyApp.Tests

# Add TinyBDD packages
dotnet add package TinyBDD
dotnet add package TinyBDD.Xunit
```

### Basic Test Class

```csharp
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    public CalculatorTests(ITestOutputHelper output) : base(output)
    {
    }
    
    [Scenario("Adding numbers"), Fact]
    public async Task AddingNumbers()
    {
        await Given("first number", () => 2)
            .And("second number", (first, ct) => Task.FromResult((first, second: 3)))
            .When("added together", numbers => numbers.first + numbers.second)
            .Then("result is correct", sum => sum == 5)
            .AssertPassed();
    }
    
    [Scenario("Subtraction"), Fact]
    public async Task Subtraction()
    {
        await Given("minuend", () => 10)
            .And("subtrahend", (minuend, ct) => Task.FromResult((minuend, subtrahend: 4)))
            .When("subtracting", numbers => numbers.minuend - numbers.subtrahend)
            .Then("result is correct", result => result == 6)
            .AssertPassed();
    }
}
```

### Running Tests

#### Command Line

```bash
# Run all tests
dotnet test

# Run tests in a specific project
dotnet test MyApp.Tests/MyApp.Tests.csproj

# Run with detailed output
dotnet test --verbosity normal

# Run tests matching a filter
dotnet test --filter "FullyQualifiedName~Calculator"

# Run tests by trait (tag)
dotnet test --filter "Category=smoke"
```

#### Visual Studio

1. Open Test Explorer: **Test → Test Explorer**
2. Click **Run All** or right-click specific tests to run
3. View test output in the Test Detail Summary pane

#### Visual Studio Code

1. Install the .NET Test Explorer extension
2. Open the Test Explorer view
3. Click the play button next to tests to run them

#### JetBrains Rider

1. Tests appear automatically in the Unit Tests window
2. Right-click a test or test class and select **Run**
3. View BDD output in the test output panel

### Theory Tests (Parameterized)

Combine xUnit Theory with TinyBDD:

```csharp
[Scenario("Addition with multiple values"), Theory]
[InlineData(1, 2, 3)]
[InlineData(5, 5, 10)]
[InlineData(-1, 1, 0)]
public async Task AdditionWithMultipleValues(int a, int b, int expected)
{
    await Given("numbers", () => (a, b))
        .When("added", nums => nums.a + nums.b)
        .Then("result matches", sum => sum == expected)
        .AssertPassed();
}
```

### Configuration

Create `xunit.runner.json` in your test project for xUnit configuration:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 8,
  "diagnosticMessages": false,
  "methodDisplay": "method",
  "methodDisplayOptions": "all"
}
```

Set the file properties:

```xml
<ItemGroup>
  <None Update="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## xUnit v3

xUnit v3 introduces architectural improvements and enhanced extensibility.

### Installation

```bash
dotnet add package TinyBDD
dotnet add package TinyBDD.Xunit.v3
dotnet add package xunit.v3
```

### Project Setup

```bash
# Create a new test project
dotnet new classlib -n MyApp.Tests

# Navigate to the project
cd MyApp.Tests

# Add required packages
dotnet add package TinyBDD
dotnet add package TinyBDD.Xunit.v3
dotnet add package xunit.v3
```

### Basic Test Class

```csharp
using TinyBDD.Xunit.v3;
using Xunit;

[Feature("Calculator")]
public class CalculatorTests : TinyBddXunitBase
{
    [Scenario("Adding numbers"), Fact]
    public async Task AddingNumbers()
    {
        await Given("first number", () => 2)
            .And("second number", (first, ct) => Task.FromResult((first, second: 3)))
            .When("added together", numbers => numbers.first + numbers.second)
            .Then("result is correct", sum => sum == 5)
            .AssertPassed();
    }
}
```

### Key Differences from v2

- **No ITestOutputHelper**: v3 uses a different logging mechanism
- **Enhanced extensibility**: Better support for custom test frameworks
- **Improved performance**: Optimized test discovery and execution

### Running Tests

Commands are the same as xUnit v2:

```bash
# Run all tests
dotnet test

# Run with filtering
dotnet test --filter "FullyQualifiedName~Calculator"
```

### Attributes

Use `[UseTinyBdd]` attribute when not using the base class:

```csharp
[Feature("Calculator")]
[UseTinyBdd]
public class CalculatorTests
{
    [Scenario("Adding numbers"), Fact]
    public async Task AddingNumbers()
    {
        var ctx = Bdd.CreateContext(this);
        
        await Bdd.Given(ctx, "numbers", () => (2, 3))
            .When("added", nums => nums.Item1 + nums.Item2)
            .Then("result is 5", sum => sum == 5)
            .AssertPassed();
    }
}
```

## NUnit

NUnit is a popular testing framework with rich assertion capabilities.

### Installation

```bash
dotnet add package TinyBDD
dotnet add package TinyBDD.NUnit
dotnet add package NUnit
dotnet add package NUnit3TestAdapter
```

### Project Setup

```bash
# Create a new NUnit test project
dotnet new nunit -n MyApp.Tests

# Navigate to the project
cd MyApp.Tests

# Add TinyBDD packages
dotnet add package TinyBDD
dotnet add package TinyBDD.NUnit
```

### Basic Test Class

```csharp
using NUnit.Framework;
using TinyBDD.NUnit;

[Feature("Calculator")]
public class CalculatorTests : TinyBddNUnitBase
{
    [Scenario("Adding numbers")]
    [Test]
    public async Task AddingNumbers()
    {
        await Given("first number", () => 2)
            .And("second number", (first, ct) => Task.FromResult((first, second: 3)))
            .When("added together", numbers => numbers.first + numbers.second)
            .Then("result is correct", sum => sum == 5)
            .AssertPassed();
    }
    
    [Scenario("Division by zero")]
    [Test]
    public async Task DivisionByZero()
    {
        await Given("dividend", () => 10)
            .And("divisor", (dividend, ct) => Task.FromResult((dividend, divisor: 0)))
            .When("dividing", numbers => 
            {
                if (numbers.divisor == 0)
                    throw new DivideByZeroException();
                return numbers.dividend / numbers.divisor;
            })
            .AssertFailed(); // Expects an exception
    }
}
```

### Running Tests

#### Command Line

```bash
# Run all tests
dotnet test

# Run tests in a specific namespace
dotnet test --filter "FullyQualifiedName~MyApp.Tests"

# Run tests by category (tag)
dotnet test --filter "TestCategory=smoke"

# Run with detailed output
dotnet test --verbosity detailed
```

#### Visual Studio

1. Open Test Explorer: **Test → Test Explorer**
2. Run tests using the toolbar buttons
3. View output in Test Detail Summary

#### NUnit Console Runner

For advanced scenarios, use the NUnit console runner:

```bash
# Install the console runner
dotnet tool install -g NUnit.ConsoleRunner

# Run tests
nunit3-console MyApp.Tests.dll
```

### TestCase (Parameterized Tests)

```csharp
[Scenario("Addition with test cases")]
[TestCase(1, 2, 3)]
[TestCase(5, 5, 10)]
[TestCase(-1, 1, 0)]
public async Task AdditionWithTestCases(int a, int b, int expected)
{
    await Given("numbers", () => (a, b))
        .When("added", nums => nums.a + nums.b)
        .Then("result matches", sum => sum == expected)
        .AssertPassed();
}
```

### Categories and Tags

```csharp
[Feature("User Management")]
[Category("integration")]
public class UserManagementTests : TinyBddNUnitBase
{
    [Scenario("Create user")]
    [Test]
    [Category("smoke")]
    public async Task CreateUser()
    {
        await Given("user data", () => new UserData("test@example.com"))
            .When("creating user", data => _service.CreateAsync(data))
            .Then("user created", result => result.IsSuccess)
            .AssertPassed();
    }
}
```

### Configuration

Create `.runsettings` file for NUnit configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <NUnit>
    <NumberOfTestWorkers>8</NumberOfTestWorkers>
    <DefaultTimeout>60000</DefaultTimeout>
    <StopOnError>false</StopOnError>
  </NUnit>
</RunSettings>
```

Run tests with settings:

```bash
dotnet test --settings test.runsettings
```

## MSTest

MSTest is Microsoft's testing framework, well-integrated with Visual Studio.

### Installation

```bash
dotnet add package TinyBDD
dotnet add package TinyBDD.MSTest
dotnet add package MSTest.TestFramework
dotnet add package MSTest.TestAdapter
```

### Project Setup

```bash
# Create a new MSTest project
dotnet new mstest -n MyApp.Tests

# Navigate to the project
cd MyApp.Tests

# Add TinyBDD packages
dotnet add package TinyBDD
dotnet add package TinyBDD.MSTest
```

### Basic Test Class

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TinyBDD.MSTest;

[TestClass]
[Feature("Calculator")]
public class CalculatorTests : TinyBddMsTestBase
{
    [Scenario("Adding numbers")]
    [TestMethod]
    public async Task AddingNumbers()
    {
        await Given("first number", () => 2)
            .And("second number", (first, ct) => Task.FromResult((first, second: 3)))
            .When("added together", numbers => numbers.first + numbers.second)
            .Then("result is correct", sum => sum == 5)
            .AssertPassed();
    }
    
    [Scenario("Multiplication")]
    [TestMethod]
    public async Task Multiplication()
    {
        await Given("factors", () => (3, 4))
            .When("multiplied", factors => factors.Item1 * factors.Item2)
            .Then("product is correct", product => product == 12)
            .AssertPassed();
    }
}
```

### Running Tests

#### Command Line

```bash
# Run all tests
dotnet test

# Run tests by test name
dotnet test --filter "Name~Adding"

# Run tests by category
dotnet test --filter "TestCategory=smoke"

# Run tests by priority
dotnet test --filter "Priority=1"

# Run with detailed output
dotnet test --verbosity detailed
```

#### Visual Studio

MSTest integrates seamlessly with Visual Studio:

1. Open Test Explorer: **Test → Test Explorer**
2. Tests are automatically discovered
3. Run, debug, or profile tests directly from Test Explorer
4. View detailed BDD output in Test Detail Summary

#### vstest.console

For CI/CD scenarios:

```bash
vstest.console MyApp.Tests.dll /Settings:test.runsettings
```

### Data-Driven Tests

MSTest supports various data sources:

#### DataRow

```csharp
[Scenario("Addition with data rows")]
[DataRow(1, 2, 3)]
[DataRow(5, 5, 10)]
[DataRow(-1, 1, 0)]
[TestMethod]
public async Task AdditionWithDataRows(int a, int b, int expected)
{
    await Given("numbers", () => (a, b))
        .When("added", nums => nums.a + nums.b)
        .Then("result matches", sum => sum == expected)
        .AssertPassed();
}
```

#### DynamicData

```csharp
public static IEnumerable<object[]> TestData
{
    get
    {
        yield return new object[] { 1, 2, 3 };
        yield return new object[] { 5, 5, 10 };
        yield return new object[] { -1, 1, 0 };
    }
}

[Scenario("Addition with dynamic data")]
[DynamicData(nameof(TestData))]
[TestMethod]
public async Task AdditionWithDynamicData(int a, int b, int expected)
{
    await Given("numbers", () => (a, b))
        .When("added", nums => nums.a + nums.b)
        .Then("result matches", sum => sum == expected)
        .AssertPassed();
}
```

### Categories and Priorities

```csharp
[TestClass]
[Feature("Payment Processing")]
[TestCategory("integration")]
public class PaymentTests : TinyBddMsTestBase
{
    [Scenario("Process payment")]
    [TestMethod]
    [TestCategory("smoke")]
    [Priority(1)]
    public async Task ProcessPayment()
    {
        await Given("payment request", () => new PaymentRequest(100m))
            .When("processing", request => _gateway.ProcessAsync(request))
            .Then("payment succeeds", result => result.IsSuccess)
            .AssertPassed();
    }
}
```

### Configuration

Create `.runsettings` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <MSTest>
    <Parallelize>
      <Workers>8</Workers>
      <Scope>MethodLevel</Scope>
    </Parallelize>
    <TestTimeout>60000</TestTimeout>
    <DeploymentEnabled>false</DeploymentEnabled>
  </MSTest>
</RunSettings>
```

Run with settings:

```bash
dotnet test --settings test.runsettings
```

## CI/CD Integration

### GitHub Actions

Create `.github/workflows/test.yml`:

```yaml
name: Tests

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal --logger "trx;LogFileName=test-results.trx"
    
    - name: Publish Test Results
      uses: EnricoMi/publish-unit-test-result-action@v2
      if: always()
      with:
        files: '**/test-results.trx'
```

### Azure DevOps

Create `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  displayName: 'Use .NET SDK'
  inputs:
    version: '9.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build solution'
  inputs:
    command: 'build'
    arguments: '--configuration $(buildConfiguration) --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    arguments: '--configuration $(buildConfiguration) --no-build --logger trx'
    publishTestResults: true
```

### GitLab CI

Create `.gitlab-ci.yml`:

```yaml
image: mcr.microsoft.com/dotnet/sdk:9.0

stages:
  - test

test:
  stage: test
  script:
    - dotnet restore
    - dotnet build --configuration Release --no-restore
    - dotnet test --configuration Release --no-build --logger "junit;LogFilePath=test-results.xml"
  artifacts:
    when: always
    reports:
      junit: test-results.xml
```

### Jenkins

Create `Jenkinsfile`:

```groovy
pipeline {
    agent any
    
    stages {
        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }
        
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test') {
            steps {
                sh 'dotnet test --configuration Release --no-build --logger "trx;LogFileName=test-results.trx"'
            }
        }
    }
    
    post {
        always {
            mstest testResultsFile: '**/test-results.trx'
        }
    }
}
```

## Advanced Running Options

### Filtering Tests

#### By Fully Qualified Name

```bash
# Run specific test class
dotnet test --filter "FullyQualifiedName~CalculatorTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~CalculatorTests.AddingNumbers"
```

#### By Traits/Categories/Tags

```bash
# xUnit
dotnet test --filter "Category=smoke"

# NUnit
dotnet test --filter "TestCategory=smoke"

# MSTest
dotnet test --filter "TestCategory=smoke"
```

#### Complex Filters

```bash
# AND condition
dotnet test --filter "(Category=smoke)&(Category=fast)"

# OR condition
dotnet test --filter "(Category=smoke)|(Category=regression)"

# NOT condition
dotnet test --filter "Category!=slow"
```

### Parallel Execution

#### xUnit

```json
{
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 8
}
```

#### NUnit

```xml
<NUnit>
  <NumberOfTestWorkers>8</NumberOfTestWorkers>
</NUnit>
```

#### MSTest

```xml
<MSTest>
  <Parallelize>
    <Workers>8</Workers>
    <Scope>MethodLevel</Scope>
  </Parallelize>
</MSTest>
```

### Test Output and Logging

```bash
# Detailed verbosity
dotnet test --verbosity detailed

# Diagnostic output
dotnet test --verbosity diagnostic

# Custom logger
dotnet test --logger "console;verbosity=detailed"

# Multiple loggers
dotnet test --logger "trx;LogFileName=results.trx" --logger "console;verbosity=normal"
```

### Code Coverage

```bash
# Using coverlet
dotnet test --collect:"XPlat Code Coverage"

# With specific format
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

## Best Practices

1. **Use descriptive scenario names**: Make test names clear in test runners
2. **Tag appropriately**: Use tags/categories for effective filtering
3. **Configure parallelization**: Balance speed vs. resource constraints
4. **Integrate with CI/CD**: Automate test execution on every change
5. **Monitor test duration**: Track and optimize slow tests
6. **Collect coverage metrics**: Measure and improve test coverage
7. **Use runsettings files**: Standardize test configuration across environments
8. **Archive test results**: Keep historical test results for trend analysis

## Next Steps

- Learn about [Reporting](reporting.md) for custom output formats
- Explore [Troubleshooting & FAQ](troubleshooting-faq.md) for common issues
- See [Samples Index](samples-index.md) for runnable examples

