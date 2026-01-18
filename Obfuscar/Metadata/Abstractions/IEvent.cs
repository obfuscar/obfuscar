using System.Collections.Generic;
using System.Reflection;

namespace Obfuscar.Metadata.Abstractions
{
    public interface IEvent
    {
        string Name { get; }
        string EventTypeFullName { get; }
        string DeclaringTypeFullName { get; }
        MethodAttributes AddMethodAttributes { get; }
        MethodAttributes RemoveMethodAttributes { get; }
        bool IsRuntimeSpecialName { get; }
        bool IsPublic { get; }
        bool HasCustomAttributes { get; }
        IEnumerable<string> CustomAttributeTypeFullNames { get; }
    }
}
