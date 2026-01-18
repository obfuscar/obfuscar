using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class CecilPropertyAdapter : IProperty
    {
        private readonly Mono.Cecil.PropertyDefinition property;

        public CecilPropertyAdapter(Mono.Cecil.PropertyDefinition property)
        {
            this.property = property;
        }

        public string Name => property?.Name ?? string.Empty;
        public string PropertyTypeFullName => property?.PropertyType?.FullName ?? string.Empty;
        public string DeclaringTypeFullName => property?.DeclaringType?.FullName ?? string.Empty;

        public MethodAttributes GetterMethodAttributes =>
            property?.GetMethod != null ? (MethodAttributes)property.GetMethod.Attributes : 0;

        public MethodAttributes SetterMethodAttributes =>
            property?.SetMethod != null ? (MethodAttributes)property.SetMethod.Attributes : 0;

        public bool IsRuntimeSpecialName => property?.IsRuntimeSpecialName == true;

        public bool IsPublic => property != null && property.IsPublic();

        public bool HasCustomAttributes => property?.HasCustomAttributes == true;

        public IEnumerable<string> CustomAttributeTypeFullNames =>
            property?.CustomAttributes?.Select(attr => attr.AttributeType.FullName) ?? Enumerable.Empty<string>();
    }
}
