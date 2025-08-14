namespace TinyBDD;

public static class Flow
{
    // Ambient start: use Ambient.Current or throw if missing
    public static GivenBuilder<T> Given<T>(string title, Func<T> setup)
        => Bdd.Given(Require(), title, setup);

    public static GivenBuilder<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), title, setup);

    public static GivenBuilder<T> Given<T>(Func<T> setup)
        => Bdd.Given(Require(), setup);

    public static GivenBuilder<T> Given<T>(Func<CancellationToken, Task<T>> setup)
        => Bdd.Given(Require(), setup);

    // “From(context)” entry if you want to be explicit in a method parameter style
    public static FromContext From(ScenarioContext ctx) => new(ctx);

    public readonly struct FromContext(ScenarioContext ctx)
    {
        public GivenBuilder<T> Given<T>(string title, Func<T> setup) => Bdd.Given(ctx, title, setup);
        public GivenBuilder<T> Given<T>(string title, Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, title, setup);
        public GivenBuilder<T> Given<T>(Func<T> setup) => Bdd.Given(ctx, setup);
        public GivenBuilder<T> Given<T>(Func<CancellationToken, Task<T>> setup) => Bdd.Given(ctx, setup);
    }

    private static ScenarioContext Require()
        => Ambient.Current.Value ?? throw new InvalidOperationException(
            "TinyBDD ambient ScenarioContext not set. Inherit from TinyBdd*Base or set Ambient.Current manually.");
}