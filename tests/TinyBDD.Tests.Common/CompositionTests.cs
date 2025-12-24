using TinyBDD.Extensions;

namespace TinyBDD.Tests.Common;

#region Domain Types

public class ShoppingCart
{
    public List<CartItem> Items { get; } = new();
    public decimal Discount { get; set; }
}

public class CartItem
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class User
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

#endregion

#region Extension Methods (must be top-level)

/// <summary>
/// Demonstrates creating domain-specific reusable steps.
/// </summary>
public static class ShoppingCartSteps
{
    public static ScenarioChain<ShoppingCart> AddItem(
        this ScenarioChain<ShoppingCart> chain, string item, decimal price)
        => chain.And($"add {item}", cart =>
        {
            cart.Items.Add(new CartItem { Name = item, Price = price });
            return cart;
        });

    public static ScenarioChain<ShoppingCart> ApplyDiscount(
        this ScenarioChain<ShoppingCart> chain, decimal percent)
        => chain.And($"apply {percent}% discount", cart =>
        {
            cart.Discount = percent;
            return cart;
        });

    public static ThenChain<ShoppingCart> TotalEquals(
        this ThenChain<ShoppingCart> chain, decimal expected)
        => chain.And($"total equals {expected}", cart =>
        {
            var subtotal = cart.Items.Sum(i => i.Price);
            var total = subtotal * (1 - cart.Discount / 100);
            return Math.Abs(total - expected) < 0.01m;
        });
}

/// <summary>
/// Demonstrates the extension method pattern for reusable steps.
/// </summary>
public static class UserSteps
{
    public static ScenarioChain<User> WithName(this ScenarioChain<User> chain, string name)
        => chain.And($"named {name}", u => { u.Name = name; return u; });

    public static ScenarioChain<User> WithAge(this ScenarioChain<User> chain, int age)
        => chain.And($"aged {age}", u => { u.Age = age; return u; });

    public static ThenChain<User> IsAdult(this ThenChain<User> chain)
        => chain.And("is adult", u => u.Age >= 18);

    public static ThenChain<User> HasValidName(this ThenChain<User> chain)
        => chain.And("has valid name", u => !string.IsNullOrEmpty(u.Name));
}

#endregion

/// <summary>
/// Tests for composition helpers in StepExtensions.
/// </summary>
public class CompositionTests
{
    #region Apply Tests

    [Fact]
    public async Task Apply_ScenarioChain_TransformsChain()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Define a reusable step sequence
        static ScenarioChain<int> DoubleIt(ScenarioChain<int> chain)
            => chain.When("double", v => v * 2);

        // Act & Assert
        await Bdd.Given(ctx, "start value", () => 5)
            .Apply(DoubleIt)
            .Then("equals 10", v => v == 10)
            .AssertPassed();
    }

    [Fact]
    public async Task Apply_ScenarioChain_ChainsMultipleApplies()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ScenarioChain<int> AddTen(ScenarioChain<int> chain)
            => chain.And("add 10", v => v + 10);

        static ScenarioChain<int> MultiplyByThree(ScenarioChain<int> chain)
            => chain.And("multiply by 3", v => v * 3);

        // Act & Assert
        await Bdd.Given(ctx, "start", () => 2)
            .When("initial transform", v => v + 1) // 3
            .Apply(AddTen)                          // 13
            .Apply(MultiplyByThree)                 // 39
            .Then("equals 39", v => v == 39)
            .AssertPassed();
    }

    [Fact]
    public async Task Apply_ScenarioChain_WithTypeTransformation()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ScenarioChain<string> ConvertToString(ScenarioChain<int> chain)
            => chain.When("to string", v => v.ToString());

        // Act & Assert
        await Bdd.Given(ctx, "number", () => 42)
            .Apply(ConvertToString)
            .Then("is '42'", s => s == "42")
            .AssertPassed();
    }

    [Fact]
    public async Task Apply_ThenChain_AddsAssertions()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ThenChain<int> ValidatePositive(ThenChain<int> chain)
            => chain.And("is positive", v => v > 0);

        static ThenChain<int> ValidateLessThan100(ThenChain<int> chain)
            => chain.And("is less than 100", v => v < 100);

        // Act & Assert
        await Bdd.Given(ctx, "value", () => 50)
            .Then("exists", _ => true)
            .Apply(ValidatePositive)
            .Apply(ValidateLessThan100)
            .AssertPassed();
    }

    [Fact]
    public async Task Apply_ThenChain_WithMultipleAssertions()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ThenChain<string> ValidateNonEmpty(ThenChain<string> chain)
            => chain
                .And("is not null", s => s != null)
                .And("is not empty", s => s.Length > 0);

        // Act & Assert
        await Bdd.Given(ctx, "text", () => "hello")
            .Then("is string", _ => true)
            .Apply(ValidateNonEmpty)
            .AssertPassed();
    }

    #endregion

    #region ApplyEffect Tests

    [Fact]
    public async Task ApplyEffect_PreservesChainType()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var log = new List<string>();

        ScenarioChain<int> LogValue(ScenarioChain<int> chain)
            => chain.And("log value", v => log.Add($"Value: {v}"));

        // Act
        await Bdd.Given(ctx, "number", () => 42)
            .ApplyEffect(LogValue)
            .Then("value logged", _ => log.Count == 1)
            .AssertPassed();

        // Assert
        Assert.Contains("Value: 42", log);
    }

    [Fact]
    public async Task ApplyEffect_ChainsMultipleEffects()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var events = new List<string>();

        ScenarioChain<string> LogStart(ScenarioChain<string> chain)
            => chain.And("log start", _ => events.Add("start"));

        ScenarioChain<string> LogEnd(ScenarioChain<string> chain)
            => chain.And("log end", _ => events.Add("end"));

        // Act
        await Bdd.Given(ctx, "process", () => "running")
            .ApplyEffect(LogStart)
            .ApplyEffect(LogEnd)
            .Then("events logged", _ => events.Count == 2)
            .AssertPassed();

        // Assert
        Assert.Equal(new[] { "start", "end" }, events);
    }

    #endregion

    #region ApplyThen Tests

    [Fact]
    public async Task ApplyThen_CombinesTwoTransformations()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ScenarioChain<int> ParseNumber(ScenarioChain<string> chain)
            => chain.When("parse", s => int.Parse(s));

        static ScenarioChain<bool> IsEven(ScenarioChain<int> chain)
            => chain.When("check even", n => n % 2 == 0);

        // Act & Assert
        await Bdd.Given(ctx, "text number", () => "42")
            .ApplyThen(ParseNumber, IsEven)
            .Then("is true", b => b)
            .AssertPassed();
    }

    [Fact]
    public async Task ApplyThen_WithComplexTransformations()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ScenarioChain<string[]> SplitWords(ScenarioChain<string> chain)
            => chain.When("split", s => s.Split(' '));

        static ScenarioChain<int> CountWords(ScenarioChain<string[]> chain)
            => chain.When("count", arr => arr.Length);

        // Act & Assert
        await Bdd.Given(ctx, "sentence", () => "hello world from tests")
            .ApplyThen(SplitWords, CountWords)
            .Then("has 4 words", count => count == 4)
            .AssertPassed();
    }

    #endregion

    #region Reusable Step Patterns

    [Fact]
    public async Task ReusableSteps_DomainSpecific_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.Given(ctx, "empty cart", () => new ShoppingCart())
            .AddItem("Widget", 10.00m)
            .AddItem("Gadget", 20.00m)
            .ApplyDiscount(10)
            .Then("has 2 items", cart => cart.Items.Count == 2)
            .TotalEquals(27.00m) // 30 - 10% = 27
            .AssertPassed();
    }

    #endregion

    #region Complex Composition Patterns

    [Fact]
    public async Task Composition_NestedApplies_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ScenarioChain<int> IncrementByN(ScenarioChain<int> chain, int n)
            => chain.And($"add {n}", v => v + n);

        ScenarioChain<int> AddSequence(ScenarioChain<int> chain)
            => chain
                .Apply(c => IncrementByN(c, 1))
                .Apply(c => IncrementByN(c, 2))
                .Apply(c => IncrementByN(c, 3));

        // Act & Assert
        await Bdd.Given(ctx, "zero", () => 0)
            .Apply(AddSequence)
            .Then("equals 6", v => v == 6)
            .AssertPassed();
    }

    [Fact]
    public async Task Composition_ConditionalSteps_WorksCorrectly()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var shouldDouble = true;

        ScenarioChain<int> OptionalDouble(ScenarioChain<int> chain)
            => shouldDouble
                ? chain.And("double", v => v * 2)
                : chain;

        // Act & Assert
        await Bdd.Given(ctx, "value", () => 5)
            .Apply(OptionalDouble)
            .Then("equals 10", v => v == 10)
            .AssertPassed();
    }

    [Fact]
    public async Task Composition_WithClosure_CapturesState()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);
        var multiplier = 7;

        ScenarioChain<int> MultiplyByFactor(ScenarioChain<int> chain)
            => chain.And("multiply", v => v * multiplier);

        // Act & Assert
        await Bdd.Given(ctx, "base", () => 3)
            .Apply(MultiplyByFactor)
            .Then("equals 21", v => v == 21)
            .AssertPassed();
    }

    [Fact]
    public async Task Composition_AssertionChain_CombinesMultipleChecks()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ThenChain<int> InRange(ThenChain<int> chain, int min, int max)
            => chain
                .And($">= {min}", v => v >= min)
                .And($"<= {max}", v => v <= max);

        // Act & Assert
        await Bdd.Given(ctx, "value in range", () => 50)
            .Then("exists", _ => true)
            .Apply(c => InRange(c, 0, 100))
            .AssertPassed();
    }

    #endregion

    #region Extension Method Style Composition

    [Fact]
    public async Task ExtensionMethodPattern_BuildsFluentApi()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        // Act & Assert
        await Bdd.Given(ctx, "new user", () => new User())
            .WithName("Alice")
            .WithAge(25)
            .Then("user created", _ => true)
            .IsAdult()
            .HasValidName()
            .AssertPassed();
    }

    [Fact]
    public async Task ExtensionMethodPattern_CombinesWithApply()
    {
        // Arrange
        var ctx = Bdd.CreateContext(this);

        static ThenChain<User> StandardValidation(ThenChain<User> chain)
            => chain.IsAdult().HasValidName();

        // Act & Assert
        await Bdd.Given(ctx, "user", () => new User())
            .WithName("Bob")
            .WithAge(30)
            .Then("exists", _ => true)
            .Apply(StandardValidation)
            .AssertPassed();
    }

    #endregion
}
