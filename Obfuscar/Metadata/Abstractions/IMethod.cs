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
        IReadOnlyList<string> ParameterTypeFullNames { get; }
    }
}
