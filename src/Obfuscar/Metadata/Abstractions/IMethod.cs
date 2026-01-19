using System.Collections.Generic;
using System.Reflection;

namespace Obfuscar.Metadata.Abstractions
{
    public interface IMethod
    {
        string Name { get; }
        string ReturnTypeFullName { get; }
        string DeclaringTypeFullName { get; }
        MethodAttributes Attributes { get; }
        MethodSemantics SemanticsAttributes { get; }
        bool IsRuntime { get; }
        bool IsSpecialName { get; }
        bool IsPublic { get; }
        bool IsFamily { get; }
        bool IsFamilyOrAssembly { get; }
        IReadOnlyList<string> ParameterTypeFullNames { get; }
    }
}
