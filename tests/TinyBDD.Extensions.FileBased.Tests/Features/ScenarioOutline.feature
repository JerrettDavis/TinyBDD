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
