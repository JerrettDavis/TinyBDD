---
uid: extensions.file-based-gherkin
title: Gherkin .feature Files
description: Complete reference for writing Gherkin .feature files with TinyBDD.Extensions.FileBased
ms.topic: reference
---

# Gherkin .feature Files Reference

Complete guide to using standard Gherkin syntax with TinyBDD's File-Based DSL extension.

## Overview

Gherkin is a business-readable domain-specific language for describing software behavior. TinyBDD.Extensions.FileBased provides full support for standard Gherkin .feature files.

## Basic Structure

```gherkin
Feature: Feature Name
  Feature description (optional)

Scenario: Scenario Name
  Given precondition
  When action
  Then expectation
```

## Feature

Defines a software feature being tested.

```gherkin
Feature: User Authentication
  Users need to authenticate to access the application
```

**Syntax**:
- `Feature:` keyword followed by feature name
- Optional multi-line description (indented)
- Must appear at top of file (before scenarios)

## Scenario

Defines a single test case.

```gherkin
@smoke @auth
Scenario: Login with valid credentials
  Given a registered user
  When they provide correct username and password
  Then they should be authenticated
```

**Syntax**:
- Optional tags (`@tag`) on line before scenario
- `Scenario:` keyword followed by scenario name
- Steps indented below scenario

## Scenario Outline

Parameterized scenario executed once for each example row.

```gherkin
Scenario Outline: Calculate total
  Given a cart with <items> items
  When I calculate the total
  Then the total should be <expected>

Examples:
  | items | expected |
  | 1     | 10.00    |
  | 5     | 50.00    |
  | 10    | 100.00   |
```

**Behavior**:
- Placeholders `<name>` in steps are replaced with values from Examples table
- One scenario executed per table row
- Scenario name includes example values for clarity

## Step Keywords

### Given

Establishes preconditions and initial state.

```gherkin
Given a calculator
Given the user is logged in
Given the database contains test data
```

### When

Describes the action being tested.

```gherkin
When I add 5 and 3
When the user clicks the login button
When the background job runs
```

### Then

States the expected outcome.

```gherkin
Then the result should be 8
Then the user should see the dashboard
Then the database should contain 10 records
```

### And / But

Continues the previous step type.

```gherkin
Given a user account
And a valid session token
But the account is not activated

When I submit the form
And I wait for processing

Then I should see a confirmation
And I should receive an email
But I should not be charged yet
```

## Tags

Organize and filter scenarios with tags.

```gherkin
@smoke @calculator @arithmetic
Scenario: Add two numbers
  Given a calculator
  When I add 5 and 3
  Then the result should be 8
```

**Usage**:
- Multiple tags per scenario
- Tags start with `@`
- Placed on line before `Scenario:` or `Scenario Outline:`
- Feature-level tags apply to all scenarios

```gherkin
@calculator
Feature: Calculator Operations

@smoke
Scenario: Add numbers
  ...

@regression
Scenario: Multiply numbers
  ...
```

## Comments

Comments start with `#` and are ignored by the parser.

```gherkin
Feature: Calculator
  # This is a comment

  Scenario: Add numbers
    Given a calculator  # Initialize calculator
    When I add 5 and 3
    # TODO: Add more test cases
    Then the result should be 8
```

## Parameter Extraction

### From Placeholders

Parameters can be extracted from step text using `{placeholder}` syntax in driver methods:

```gherkin
When I add 5 and 3
```

```csharp
[DriverMethod("I add {a} and {b}")]
public Task Add(int a, int b)  // a=5, b=3
```

### From Quotes

Quoted strings in steps are treated as single parameters:

```gherkin
When I login with username "john.doe@example.com" and password "secret123"
```

```csharp
[DriverMethod("I login with username {username} and password {password}")]
public Task Login(string username, string password)
```

**Note**: Automatic quote extraction is not yet implemented. Use explicit placeholders in patterns.

## Examples Table

### Basic Table

```gherkin
Examples:
  | column1 | column2 | column3 |
  | value1  | value2  | value3  |
  | value4  | value5  | value6  |
```

### Multiple Examples

Scenario Outlines can have multiple Examples sections:

```gherkin
Scenario Outline: Process data
  Given input <data>
  When I process it
  Then output should be <result>

Examples: Valid data
  | data  | result |
  | hello | HELLO  |
  | world | WORLD  |

Examples: Special characters
  | data  | result |
  | @test | @TEST  |
  | #hash | #HASH  |
```

Each Examples section generates separate scenarios.

## Complete Example

```gherkin
@calculator @regression
Feature: Calculator Operations
  Test basic arithmetic operations
  to ensure calculator works correctly

Background:
  Given a calculator is initialized

@smoke @addition
Scenario: Add positive numbers
  Given a calculator
  When I add 5 and 3
  Then the result should be 8
  And the result should be positive

@multiplication
Scenario Outline: Multiply numbers
  When I multiply <a> and <b>
  Then the result should be <expected>

Examples: Positive numbers
  | a | b | expected |
  | 2 | 3 | 6        |
  | 4 | 5 | 20       |

Examples: With zero
  | a | b | expected |
  | 0 | 5 | 0        |
  | 5 | 0 | 0        |

@error-handling
Scenario: Division by zero
  When I divide 10 by 0
  Then an error should occur
  And the error message should contain "division by zero"
```

## Driver Method Mapping

### Simple Mapping

```gherkin
Given a calculator
```

```csharp
[DriverMethod("a calculator")]
public Task Initialize()
```

### With Parameters

```gherkin
When I add 5 and 3
```

```csharp
[DriverMethod("I add {a} and {b}")]
public Task Add(int a, int b)
```

### With Return Values

```gherkin
Then the result should be 8
```

```csharp
[DriverMethod("the result should be {expected}")]
public Task<bool> VerifyResult(int expected)
{
    return Task.FromResult(_result == expected);
}
```

## Best Practices

### Use Business Language

```gherkin
# Good: Business terms
Given a customer with premium subscription
When they purchase a product
Then they should receive a 20% discount

# Avoid: Technical terms
Given database record id=123 with subscription_type='premium'
When POST /api/orders with {product_id: 456}
Then response.discount_percent == 20
```

### Keep Steps Focused

```gherkin
# Good: Each step does one thing
Given a user account
And a valid session token
When I access the dashboard
Then I should see my profile

# Avoid: Multiple actions in one step
Given a user account with valid token accessing dashboard
Then profile should display
```

### Write Declarative, Not Imperative

```gherkin
# Good: Describes what, not how
Given the user is logged in
When they view their order history
Then they should see recent orders

# Avoid: Describes implementation
Given I POST to /auth/login with credentials
When I GET /api/orders?user_id=123
Then response status is 200 and body contains order array
```

## Limitations

Current implementation limitations:

### Data Tables in Steps

Not yet supported:

```gherkin
# NOT SUPPORTED YET
Given the following users:
  | username | email            |
  | alice    | alice@example.com |
  | bob      | bob@example.com   |
```

**Workaround**: Use Scenario Outline with Examples table or pass structured data through YAML format.

### Doc Strings

Not yet supported:

```gherkin
# NOT SUPPORTED YET
Given the following JSON:
  """
  {
    "name": "test",
    "value": 123
  }
  """
```

**Workaround**: Pass complex strings through YAML parameters section.

### Background

Background sections are parsed but not yet executed before each scenario.

**Workaround**: Include setup steps in each scenario or use driver `InitializeAsync()` method.

## Related Topics

- [File-Based DSL Overview](file-based.md) - Main guide
- [YAML Format Reference](file-based-yaml.md) - Alternative format
- [Samples: Calculator Testing](../../samples/file-based/calculator.md) - Basic example

Return to: [File-Based DSL](file-based.md) | [Extensions](index.md)
