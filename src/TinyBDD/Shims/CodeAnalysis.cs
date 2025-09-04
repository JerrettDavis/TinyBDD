#if NETSTANDARD1_0_OR_GREATER || NET6_0
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class SetsRequiredMembersAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
internal
#endif
    sealed class RequiresUnreferencedCodeAttribute(string message) : Attribute
{
    public string Message { get; } = message;
}

#endif