namespace TinyBDD.Tests.Common;

/// <summary>
/// Tests for data-driven scenario features including ExamplesBuilder,
/// ScenarioOutline, and ScenarioCaseAttribute.
/// </summary>
public class DataDrivenTests
{
    #region ExamplesBuilder Tests

    [Fact]
    public async Task ExamplesBuilder_ForEachAsync_RunsAllExamples()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var examples = new[] { 1, 2, 3 };

        // Act
        var result = await Bdd.Scenario(ctx, "Test scenario", examples)
            .ForEachAsync(row =>
                Bdd.Given(ctx, $"value {row.Data}", () => row.Data)
                    .When("double", v => v * 2)
                    .Then("is even", v => v % 2 == 0));

        // Assert
        Assert.Equal(3, result.Results.Count);
        Assert.True(result.AllPassed);
    }

    [Fact]
    public async Task ExamplesBuilder_ForEachAsync_CapturesFailures()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var examples = new[] { 2, 3, 4 }; // 3 will fail (not even)

        // Act
        var result = await Bdd.Scenario(ctx, "Test scenario", examples)
            .ForEachAsync(row =>
                Bdd.Given(ctx, $"value {row.Data}", () => row.Data)
                    .Then("is even", v => v % 2 == 0));

        // Assert
        Assert.False(result.AllPassed);
        Assert.True(result.Results.Any(r => !r.Passed));  // At least one failure
        Assert.True(result.Results.Any(r => r.Passed));   // At least one pass
    }

    [Fact]
    public async Task ExamplesBuilder_AssertAllPassedAsync_ThrowsOnFailure()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var examples = new[] { 2, 3, 4 };

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() =>
            Bdd.Scenario(ctx, "Test scenario", examples)
                .AssertAllPassedAsync(row =>
                    Bdd.Given(ctx, $"value {row.Data}", () => row.Data)
                        .Then("is even", v => v % 2 == 0)));
    }

    [Fact]
    public async Task ExamplesBuilder_ExampleRow_HasCorrectIndex()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var examples = new[] { "a", "b", "c" };
        var capturedIndices = new List<int>();

        // Act
        await Bdd.Scenario(ctx, "Test scenario", examples)
            .ForEachAsync(row =>
            {
                capturedIndices.Add(row.Index);
                return Bdd.Given(ctx, "value", () => row.Data)
                    .Then("exists", v => v.Length > 0);
            });

        // Assert
        Assert.Equal(new[] { 0, 1, 2 }, capturedIndices);
    }

    [Fact]
    public async Task ExamplesBuilder_WithAnonymousTypes_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act
        var result = await Bdd.Scenario(ctx, "Addition",
                new { a = 1, b = 2, expected = 3 },
                new { a = 5, b = 5, expected = 10 },
                new { a = 10, b = -5, expected = 5 })
            .ForEachAsync(row =>
                Bdd.Given(ctx, $"a={row.Data.a}, b={row.Data.b}", () => (row.Data.a, row.Data.b))
                    .When("added", x => x.Item1 + x.Item2)
                    .Then($"equals {row.Data.expected}", sum => sum == row.Data.expected));

        // Assert
        Assert.True(result.AllPassed);
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public async Task ExamplesBuilder_WithTuples_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act
        var result = await Bdd.Scenario(ctx, "Multiplication",
                (2, 3, 6),
                (4, 5, 20),
                (0, 100, 0))
            .ForEachAsync(row =>
                Bdd.Given(ctx, $"{row.Data.Item1} * {row.Data.Item2}", () => row.Data)
                    .When("multiplied", t => t.Item1 * t.Item2)
                    .Then($"equals {row.Data.Item3}", result => result == row.Data.Item3));

        // Assert
        Assert.True(result.AllPassed);
    }

    #endregion

    #region ScenarioOutline Tests

    [Fact]
    public async Task ScenarioOutline_Given_UsesExampleData()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(int input, int expected)>(ctx, "Doubling")
            .Given("input value", ex => ex.input)
            .When("doubled", v => v * 2)
            .Then("matches expected", (v, ex) => v == ex.expected)
            .Examples(
                (input: 1, expected: 2),
                (input: 5, expected: 10),
                (input: 0, expected: 0))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task ScenarioOutline_When_TransformsWithExampleData()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(int multiplier, int expected)>(ctx, "Multiplication")
            .Given("base value", _ => 10)
            .When("multiply by factor", (v, ex) => v * ex.multiplier)
            .Then("matches expected", (v, ex) => v == ex.expected)
            .Examples(
                (multiplier: 1, expected: 10),
                (multiplier: 2, expected: 20),
                (multiplier: 3, expected: 30))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task ScenarioOutline_Then_AssertWithExampleData()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(string input, int expectedLength)>(ctx, "String length")
            .Given("string value", ex => ex.input)
            .When("get length", s => s.Length)
            .Then("length matches", (len, ex) => len == ex.expectedLength)
            .Examples(
                (input: "", expectedLength: 0),
                (input: "hello", expectedLength: 5),
                (input: "test", expectedLength: 4))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task ScenarioOutline_And_ChainsMultipleAssertions()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(int value, int min, int max)>(ctx, "Range check")
            .Given("a number", ex => ex.value)
            .Then("greater than min", (v, ex) => v > ex.min)
            .And("less than max", (v, ex) => v < ex.max)
            .Examples(
                (value: 5, min: 0, max: 10),
                (value: 50, min: 40, max: 60))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task ScenarioOutline_But_NegativeAssertion()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(int value, int forbidden)>(ctx, "Forbidden values")
            .Given("a number", ex => ex.value)
            .Then("is positive", v => v > 0)
            .But("is not forbidden", (v, ex) => v != ex.forbidden)
            .Examples(
                (value: 5, forbidden: 10),
                (value: 15, forbidden: 20))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task ScenarioOutline_RunAsync_ReturnsDetailedResults()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act
        var result = await Bdd.ScenarioOutline<(int value, bool shouldPass)>(ctx, "Mixed results")
            .Given("value", ex => ex.value)
            .Then("passes if expected", (_, ex) => ex.shouldPass)
            .Examples(
                (value: 1, shouldPass: true),
                (value: 2, shouldPass: false),
                (value: 3, shouldPass: true))
            .RunAsync();

        // Assert
        Assert.Equal(3, result.Results.Count);
        Assert.False(result.AllPassed);
        Assert.Equal(2, result.Results.Count(r => r.Passed));
        Assert.Equal(1, result.Results.Count(r => !r.Passed));
    }

    [Fact]
    public async Task ScenarioOutline_WithoutExamples_ThrowsOnRun()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var outline = Bdd.ScenarioOutline<int>(ctx, "No examples")
            .Given("value", ex => ex)
            .Then("exists", v => v > 0);
        // Note: Examples() not called

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => outline.RunAsync());
    }

    [Fact]
    public async Task ScenarioOutline_WithAsyncGiven_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(int delay, int value)>(ctx, "Async test")
            .Given("async value", async ex =>
            {
                await Task.Delay(ex.delay);
                return ex.value;
            })
            .Then("is correct", (v, ex) => v == ex.value)
            .Examples(
                (delay: 1, value: 42),
                (delay: 1, value: 100))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task ScenarioOutline_And_Transform_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.ScenarioOutline<(int a, int b, int expected)>(ctx, "Chained operations")
            .Given("first value", ex => ex.a)
            .And("add second", (v, ex) => v + ex.b)
            .When("square", v => v * v)
            .Then("matches expected", (v, ex) => v == ex.expected)
            .Examples(
                (a: 1, b: 2, expected: 9),    // (1+2)^2 = 9
                (a: 3, b: 1, expected: 16))   // (3+1)^2 = 16
            .AssertAllPassedAsync();
    }

    #endregion

    #region ExamplesResult Tests

    [Fact]
    public async Task ExamplesResult_AllPassed_TrueWhenAllPass()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act
        var result = await Bdd.Scenario(ctx, "All pass", 1, 2, 3)
            .ForEachAsync(row =>
                Bdd.Given(ctx, "value", () => row.Data)
                    .Then("is positive", v => v > 0));

        // Assert
        Assert.True(result.AllPassed);
    }

    [Fact]
    public async Task ExamplesResult_AssertAllPassed_ThrowsWithAllFailures()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act
        var result = await Bdd.Scenario(ctx, "Some fail", 1, 2, 3)
            .ForEachAsync(row =>
                Bdd.Given(ctx, "value", () => row.Data)
                    .Then("equals 2", v => v == 2)); // Only 2 passes

        // Assert
        var ex = Assert.Throws<AggregateException>(() => result.AssertAllPassed());
        Assert.True(ex.InnerExceptions.Count >= 2); // At least 2 failures (1 and 3 don't equal 2)
    }

    [Fact]
    public async Task ExamplesResult_Results_ContainsExampleData()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var examples = new[] { "alpha", "beta", "gamma" };

        // Act
        var result = await Bdd.Scenario(ctx, "Named examples", examples)
            .ForEachAsync(row =>
                Bdd.Given(ctx, "value", () => row.Data)
                    .Then("has content", s => s.Length > 0));

        // Assert
        for (int i = 0; i < examples.Length; i++)
        {
            Assert.Equal(examples[i], result.Results[i].Data);
        }
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task DataDriven_ComplexBusinessLogic_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert - Testing a discount calculator
        await Bdd.ScenarioOutline<(decimal price, int quantity, decimal discount, decimal expected)>(
                ctx, "Discount calculation")
            .Given("order with price and quantity", ex => new { Price = ex.price, Quantity = ex.quantity })
            .When("calculate total with discount", (order, ex) =>
                order.Price * order.Quantity * (1 - ex.discount / 100m))
            .Then("total matches expected", (total, ex) => Math.Abs(total - ex.expected) < 0.01m)
            .Examples(
                (price: 100m, quantity: 2, discount: 10m, expected: 180m),
                (price: 50m, quantity: 4, discount: 20m, expected: 160m),
                (price: 25m, quantity: 10, discount: 0m, expected: 250m))
            .AssertAllPassedAsync();
    }

    [Fact]
    public async Task DataDriven_StringOperations_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.Scenario(ctx, "String transformations",
                new { input = "hello", operation = "upper", expected = "HELLO" },
                new { input = "WORLD", operation = "lower", expected = "world" },
                new { input = "  trim  ", operation = "trim", expected = "trim" })
            .ForEachAsync(row =>
                Bdd.Given(ctx, "input string", () => row.Data.input)
                    .When($"apply {row.Data.operation}", s => row.Data.operation switch
                    {
                        "upper" => s.ToUpper(),
                        "lower" => s.ToLower(),
                        "trim" => s.Trim(),
                        _ => s
                    })
                    .Then("matches expected", s => s == row.Data.expected));
    }

    #endregion
}
