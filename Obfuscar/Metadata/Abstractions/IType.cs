using System.Collections.Generic;

namespace Obfuscar.Metadata.Abstractions
{
    public interface IType
    {
        string FullName { get; }
        string Name { get; }
        string Namespace { get; }
        IEnumerable<IField> Fields { get; }
    }
}
