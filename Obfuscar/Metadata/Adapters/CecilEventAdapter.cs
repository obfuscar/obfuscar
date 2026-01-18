using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class CecilEventAdapter : IEvent
    {
        private readonly Mono.Cecil.EventDefinition evt;

        public CecilEventAdapter(Mono.Cecil.EventDefinition evt)
        {
            this.evt = evt;
        }

        public string Name => evt?.Name ?? string.Empty;
        public string EventTypeFullName => evt?.EventType?.FullName ?? string.Empty;
        public string DeclaringTypeFullName => evt?.DeclaringType?.FullName ?? string.Empty;

        public MethodAttributes AddMethodAttributes =>
            evt?.AddMethod != null ? (MethodAttributes)evt.AddMethod.Attributes : 0;

        public MethodAttributes RemoveMethodAttributes =>
            evt?.RemoveMethod != null ? (MethodAttributes)evt.RemoveMethod.Attributes : 0;

        public bool IsRuntimeSpecialName => evt?.IsRuntimeSpecialName == true;

        public bool IsPublic => evt != null && evt.IsPublic();

        public bool HasCustomAttributes => evt?.HasCustomAttributes == true;

        public IEnumerable<string> CustomAttributeTypeFullNames =>
            evt?.CustomAttributes?.Select(attr => attr.AttributeType.FullName) ?? Enumerable.Empty<string>();
    }
}
