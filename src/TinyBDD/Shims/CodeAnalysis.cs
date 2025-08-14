#if NETSTANDARD1_0_OR_GREATER || NET6_0
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class SetsRequiredMembersAttribute : Attribute
{
}

#endif