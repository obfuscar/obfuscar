using System.Collections.Generic;

namespace Obfuscar.Metadata.Abstractions
{
    public interface IType
    {
        string Scope { get; }
        string FullName { get; }
        string Name { get; }
        string Namespace { get; }
        string BaseTypeFullName { get; }
        IEnumerable<string> InterfaceTypeFullNames { get; }
        bool IsPublic { get; }
        bool IsSerializable { get; }
        bool IsSealed { get; }
        bool IsAbstract { get; }
        bool IsEnum { get; }
        IEnumerable<string> CustomAttributeTypeFullNames { get; }
        IEnumerable<IField> Fields { get; }
    }
}
