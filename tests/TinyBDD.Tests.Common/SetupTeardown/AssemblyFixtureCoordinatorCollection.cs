namespace TinyBDD.Tests.Common.SetupTeardown;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AssemblyFixtureCoordinatorCollection
{
    public const string Name = nameof(AssemblyFixtureCoordinatorCollection);
}
