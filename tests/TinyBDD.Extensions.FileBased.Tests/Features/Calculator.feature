Feature: Calculator Operations

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
