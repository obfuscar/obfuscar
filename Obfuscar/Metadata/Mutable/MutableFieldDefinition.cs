using System.Collections.Generic;
using System.Reflection;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a field reference in the mutable object model.
    /// This replaces Mono.Cecil.FieldReference.
    /// </summary>
    public class MutableFieldReference : IField
    {
        /// <summary>
        /// Creates a new field reference.
        /// </summary>
        public MutableFieldReference(string name, MutableTypeReference fieldType, MutableTypeReference declaringType)
        {
            Name = name;
            FieldType = fieldType;
            DeclaringType = declaringType;
        }

        /// <summary>
        /// The name of the field.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the field.
        /// </summary>
        public MutableTypeReference FieldType { get; set; }

        /// <summary>
        /// The type that declares this field.
        /// </summary>
        public MutableTypeReference DeclaringType { get; set; }

        /// <summary>
        /// Resolves this field reference to a field definition.
        /// </summary>
        public virtual MutableFieldDefinition Resolve()
        {
            return this as MutableFieldDefinition;
        }

        /// <summary>
        /// Gets the full name of the field.
        /// </summary>
        public string FullName => $"{FieldType?.FullName ?? "?"} {DeclaringType?.FullName ?? "?"}::{Name}";

        public string FieldTypeFullName => FieldType?.FullName ?? string.Empty;

        public string DeclaringTypeFullName => DeclaringType?.FullName ?? string.Empty;

        public virtual bool IsStatic => false;

        public virtual FieldAttributes Attributes { get; set; }

        public virtual bool HasCustomAttributes => false;

        public virtual IEnumerable<string> CustomAttributeTypeFullNames => System.Array.Empty<string>();

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }

    /// <summary>
    /// Represents a field definition in the mutable object model.
    /// This replaces Mono.Cecil.FieldDefinition.
    /// </summary>
    public class MutableFieldDefinition : MutableFieldReference, IFieldDefinition
    {
        /// <summary>
        /// Creates a new field definition.
        /// </summary>
        public MutableFieldDefinition(string name, FieldAttributes attributes, MutableTypeReference fieldType)
            : base(name, fieldType, null)
        {
            Attributes = attributes;
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The field attributes.
        /// </summary>
        public override FieldAttributes Attributes { get; set; }

        public int MetadataToken { get; set; }

        /// <summary>
        /// Custom attributes applied to this field.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Initial value for the field (for fields with RVA).
        /// </summary>
        public byte[] InitialValue { get; set; }

        /// <summary>
        /// The constant value (for const fields).
        /// </summary>
        public object Constant { get; set; }

        public object ConstantValue => Constant;

        public bool HasConstant => Constant != null;

        /// <summary>
        /// Whether this field is public.
        /// </summary>
        public bool IsPublic => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

        /// <summary>
        /// Whether this field is private.
        /// </summary>
        public bool IsPrivate => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

        /// <summary>
        /// Whether this field is family (protected).
        /// </summary>
        public bool IsFamily => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

        /// <summary>
        /// Whether this field is assembly (internal).
        /// </summary>
        public bool IsAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

        /// <summary>
        /// Whether this field is static.
        /// </summary>
        public override bool IsStatic => (Attributes & FieldAttributes.Static) != 0;

        /// <summary>
        /// Whether this field is read-only (initonly).
        /// </summary>
        public bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) != 0;

        public override bool HasCustomAttributes => CustomAttributes.Count > 0;

        public override IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in CustomAttributes)
                    yield return attr.AttributeTypeName ?? string.Empty;
            }
        }

        IEnumerable<ICustomAttribute> IFieldDefinition.CustomAttributes => CustomAttributes;

        /// <summary>
        /// Whether this field is a literal (const).
        /// </summary>
        public bool IsLiteral => (Attributes & FieldAttributes.Literal) != 0;

        /// <summary>
        /// Whether this field has RVA (initial data).
        /// </summary>
        public bool HasRVA => (Attributes & FieldAttributes.HasFieldRVA) != 0;

        /// <summary>
        /// Whether this field is special name.
        /// </summary>
        public bool IsSpecialName => (Attributes & FieldAttributes.SpecialName) != 0;

        /// <inheritdoc/>
        public override MutableFieldDefinition Resolve() => this;
    }
}
