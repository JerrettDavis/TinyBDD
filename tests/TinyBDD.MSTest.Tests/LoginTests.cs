namespace TinyBDD.MSTest.Tests;

using static Flow;

[Feature("Login")]
[TestClass]
public class LoginTests : TinyBddMsTestBase
{
    [Scenario("An absolutely real login scenario", "Tag1", "Tag2")]
    [TestMethod]
    public async Task GivenACompletelyRealLogin_WhenWeDoSomething_ThenHave2()
    {
        await Given("completely real login", () => 1)
            .When("we do something", x => x + 1)
            .Then("we have 2", v => v == 2);

        Scenario.AssertPassed();
    }


    [Scenario("An absolutely real async login scenario", "Tag1", "Tag2")]
    [TestMethod]
    public async Task GivenACompletelyRealLogin_WhenWeDoSomethingAsync_ThenHave2()
    {
        await Given("completely real async login", () => 1)
            .When("we do something", x => Task.FromResult(x + 1))
            .Then("we have 2", v => v == 2);

        Scenario.AssertPassed();
    }


    [Scenario("An absolutely real async login scenario with async assert", "Tag1", "Tag2")]
    [TestMethod]
    public async Task GivenACompletelyRealLogin_WhenWeDoSomethingAsync_ThenHave2Async()
    {
        await Given("completely real login with async assert", () => 1)
            .When("we do something", x => Task.FromResult(x + 1))
            .Then("we have 2", v => Task.FromResult(v == 2));

        Scenario.AssertPassed();
    }

    [Scenario("An absolutely async real async login scenario with async assert", "Tag1", "Tag2")]
    [TestMethod]
    public async Task GivenACompletelyAsyncRealLogin_WhenWeDoSomethingAsync_ThenHave2Async()
    {
        await Given("completely real login with async assert and no await", () => Task.FromResult(1))
            .When("we do something", async (x, _) => await x + 1)
            .Then("we have 2", v => v == 2);

        Scenario.AssertPassed();
    }


    [Scenario("Given async; When sync transform; Then sync bool")]
    [TestMethod]
    public async Task GivenAsync_WhenSync_ThenSyncBool()
    {
        await Given("async given", () => Task.FromResult(1))
            .When("add one", async x => await x + 1)
            .Then("equals 2", v => v == 2);

        Scenario.AssertPassed();
    }

    [Scenario("Given sync; When async transform; Then async bool")]
    [TestMethod]
    public async Task GivenSync_WhenAsync_ThenAsyncBool()
    {
        await Given("sync given", () => 2)
            .When("halve", x => Task.FromResult(x / 2))
            .Then("equals 1 (async)", v => Task.FromResult(v == 1));

        Scenario.AssertPassed();
    }

    [Scenario("Given sync; When side-effect (Action); Then predicate on state")]
    [TestMethod]
    public async Task WhenSideEffect_Action()
    {
        await Given("list", () => new List<int>())
            .When("add 42", list =>
            {
                list.Add(42);
                return list;
            })
            .Then("has one item", list => list.Count == 1)
            .And("contains 42", list => list.Contains(42));

        Scenario.AssertPassed();
    }

    [Scenario("Given sync; When side-effect (Action<T,CancellationToken>); Then predicate")]
    [TestMethod]
    public async Task WhenSideEffect_Action_WithToken()
    {
        await Given("list", () => new List<int>())
            .When("add 7 with token", (list, _) => list.Add(7))
            .Then("has one item", list => list.Count == 1);

        Scenario.AssertPassed();
    }

    [Scenario("Then with custom fail message")]
    [TestMethod]
    public async Task ThenPredicate_WithCustomMessage()
    {
        // This will pass; flip predicate to 'v == 3' to see the message in action.
        await Given("start", () => 1)
            .When("add one", x => x + 1)
            .Then("should be 2", v => v == 2);

        Scenario.AssertPassed();
    }

    [Scenario("And/But chaining (sync + async predicates)")]
    [TestMethod]
    public async Task And_But_Chaining()
    {
        await Given("start", () => 5)
            .When("double", x => Task.FromResult(x * 2)) // 10
            .Then(">= 10", v => v >= 10)                 // sync
            .And("<= 20 (async)", v => Task.FromResult(v <= 20))
            .But("!= 11", v => v != 11);

        Scenario.AssertPassed();
    }

    [Scenario("Cancel-aware overloads")]
    [TestMethod]
    public async Task CancelAwareOverloads()
    {
        await Given("token value", _ => Task.FromResult(3)) // Given(Func<CancellationToken, Task<T>>)
            .When("add with token", (x, _) => Task.FromResult(x + 2))
            .Then("equals 5 (async, token)", (v, _) => Task.FromResult(v == 5));

        Scenario.AssertPassed();
    }

    [Scenario("Mixed: Given async; When async; Then sync + And sync")]
    [TestMethod]
    public async Task Mixed_AllGood()
    {
        await Given("async start", () => Task.FromResult(10))
            .When("minus 3 async", async x => await x - 3) // 7
            .Then(">= 7", v => v >= 7)
            .And("== 7", v => v == 7);

        Scenario.AssertPassed();
    }

    [Scenario("Failure path surfaces BddAssertException message")]
    [TestMethod]
    public async Task Failure_Shows_Custom_Message()
    {
        try
        {
            await Given("start", () => 1)
                .When("add one", x => x + 1) // 2
                .Then("should be 3", v => v == 3);
        }
        catch (BddStepException ex) // wrapped
        {
            Assert.Contains("should be 3", ex.InnerException?.Message ?? "");
        }
    }

    [Scenario("Transform via Given.Then(alias); Then predicate")]
    [TestMethod]
    public async Task GivenThenTransform_Alias()
    {
        await Given("start", () => 3)
            .Then("triple (transform)", (x, _) => Task.FromResult(x * 3)) // alias to When<TOut>
            .And("== 9", v => v == 9);

        Scenario.AssertPassed();
    }

    [Scenario("Make the previous 'invalid await' variant correct")]
    [TestMethod]
    public async Task GivenAsync_WhenSync_ThenSyncBool_Fixed()
    {
        await Given("async start", () => Task.FromResult(1))
            .When("add one", async x => await x + 1)
            .Then("== 2", v => v == 2);

        Scenario.AssertPassed();
    }
}