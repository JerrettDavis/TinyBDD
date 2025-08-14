namespace TinyBDD;

public static class Ambient
{
    public static readonly AsyncLocal<ScenarioContext?> Current = new();
}