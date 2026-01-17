using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    // SRM adapter: currently wraps a Mono.Cecil.FieldDefinition materialized by SrmAssemblyReader
    public class SrmFieldAdapter : IField
    {
        private readonly Mono.Cecil.FieldDefinition field;

        public SrmFieldAdapter(Mono.Cecil.FieldDefinition field)
        {
            this.field = field;
        }

        public string Name => field.Name;
        public string FieldTypeFullName => field.FieldType?.FullName ?? string.Empty;
        public string DeclaringTypeFullName => field.DeclaringType?.FullName ?? string.Empty;
        public bool IsStatic => field.IsStatic;
        public FieldAttributes Attributes => (FieldAttributes) field.Attributes;
        public bool HasCustomAttributes => field.HasCustomAttributes;
        public IEnumerable<string> CustomAttributeTypeFullNames =>
            field.CustomAttributes?.Select(attr => attr.AttributeType.FullName) ?? Enumerable.Empty<string>();
    }
}
