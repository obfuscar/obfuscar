using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class CecilFieldAdapter : IField
    {
        private readonly Mono.Cecil.FieldDefinition fieldDef;

        public CecilFieldAdapter(Mono.Cecil.FieldDefinition field)
        {
            this.fieldDef = field;
        }

        public string Name => fieldDef.Name;
        public string FieldTypeFullName => fieldDef.FieldType?.FullName ?? string.Empty;
        public string DeclaringTypeFullName => fieldDef.DeclaringType?.FullName ?? string.Empty;
        public bool IsStatic => fieldDef.IsStatic;
        public FieldAttributes Attributes => (FieldAttributes) fieldDef.Attributes;
        public bool HasCustomAttributes => fieldDef.HasCustomAttributes;
        public IEnumerable<string> CustomAttributeTypeFullNames =>
            fieldDef.CustomAttributes?.Select(attr => attr.AttributeType.FullName) ?? Enumerable.Empty<string>();

        /// <summary>Get the underlying Cecil FieldDefinition (for migration compatibility).</summary>
        public Mono.Cecil.FieldDefinition Definition => fieldDef;
    }
}
