using System.Collections.Generic;
using System.Reflection;

namespace Obfuscar.Metadata.Abstractions
{
    public interface IProperty
    {
        string Name { get; }
        string PropertyTypeFullName { get; }
        string DeclaringTypeFullName { get; }
        MethodAttributes GetterMethodAttributes { get; }
        MethodAttributes SetterMethodAttributes { get; }
        bool IsRuntimeSpecialName { get; }
        bool IsPublic { get; }
        bool HasCustomAttributes { get; }
        IEnumerable<string> CustomAttributeTypeFullNames { get; }
    }
}
