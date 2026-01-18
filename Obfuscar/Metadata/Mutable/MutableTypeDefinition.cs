using System;
using System.Collections.Generic;
using System.Reflection;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a type definition in the mutable object model.
    /// This replaces Mono.Cecil.TypeDefinition.
    /// </summary>
    public class MutableTypeDefinition : MutableTypeReference, ITypeDefinition
    {
        /// <summary>
        /// Creates a new type definition.
        /// </summary>
        public MutableTypeDefinition(string @namespace, string name, TypeAttributes attributes, MutableTypeReference baseType)
            : base(@namespace, name, null)
        {
            Attributes = attributes;
            BaseType = baseType;
            Fields = new List<MutableFieldDefinition>();
            Methods = new List<MutableMethodDefinition>();
            Properties = new List<MutablePropertyDefinition>();
            Events = new List<MutableEventDefinition>();
            NestedTypes = new List<MutableTypeDefinition>();
            Interfaces = new List<MutableInterfaceImplementation>();
            GenericParameters = new List<MutableGenericParameter>();
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The type attributes (visibility, sealed, abstract, etc.).
        /// </summary>
        public TypeAttributes Attributes { get; set; }

        /// <summary>
        /// Explicit layout packing size (0 means unspecified).
        /// </summary>
        public int PackingSize { get; set; }

        /// <summary>
        /// Explicit layout class size (0 means unspecified).
        /// </summary>
        public int ClassSize { get; set; }

        public int MetadataToken { get; set; }

        /// <summary>
        /// The base type.
        /// </summary>
        public MutableTypeReference BaseType { get; set; }

        /// <summary>
        /// The fields defined in this type.
        /// </summary>
        public List<MutableFieldDefinition> Fields { get; }

        /// <summary>
        /// The methods defined in this type.
        /// </summary>
        public List<MutableMethodDefinition> Methods { get; }

        /// <summary>
        /// The properties defined in this type.
        /// </summary>
        public List<MutablePropertyDefinition> Properties { get; }

        /// <summary>
        /// The events defined in this type.
        /// </summary>
        public List<MutableEventDefinition> Events { get; }

        /// <summary>
        /// Nested types defined in this type.
        /// </summary>
        public List<MutableTypeDefinition> NestedTypes { get; }

        /// <summary>
        /// Interfaces implemented by this type.
        /// </summary>
        public List<MutableInterfaceImplementation> Interfaces { get; }

        /// <summary>
        /// Generic parameters of this type.
        /// </summary>
        public List<MutableGenericParameter> GenericParameters { get; }

        /// <summary>
        /// Custom attributes applied to this type.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Whether this type has generic parameters.
        /// </summary>
        public bool HasGenericParameters => GenericParameters.Count > 0;

        /// <summary>
        /// Whether this type is public.
        /// </summary>
        public override bool IsPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;

        /// <summary>
        /// Whether this type is nested public.
        /// </summary>
        public bool IsNestedPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;

        /// <summary>
        /// Whether this type is abstract.
        /// </summary>
        public override bool IsAbstract => (Attributes & TypeAttributes.Abstract) != 0;

        /// <summary>
        /// Whether this type is sealed.
        /// </summary>
        public override bool IsSealed => (Attributes & TypeAttributes.Sealed) != 0;

        /// <summary>
        /// Whether this type is an interface.
        /// </summary>
        public bool IsInterface => (Attributes & TypeAttributes.Interface) != 0;

        /// <summary>
        /// Whether this type is a class.
        /// </summary>
        public bool IsClass => !IsInterface && !IsValueType;

        /// <summary>
        /// Whether this type is an enum.
        /// </summary>
        public override bool IsEnum { get; set; }

        /// <summary>
        /// Whether this type is serializable.
        /// </summary>
        public override bool IsSerializable => (Attributes & TypeAttributes.Serializable) != 0;

        /// <summary>
        /// Whether this type is nested.
        /// </summary>
        public bool IsNested => DeclaringType != null;

        public override string BaseTypeFullName => BaseType?.FullName ?? string.Empty;

        public override IEnumerable<string> InterfaceTypeFullNames
        {
            get
            {
                foreach (var iface in Interfaces)
                    yield return iface.InterfaceType?.FullName ?? string.Empty;
            }
        }

        public override IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in CustomAttributes)
                    yield return attr.AttributeTypeName ?? string.Empty;
            }
        }

        IEnumerable<IMethodDefinition> ITypeDefinition.Methods => Methods;

        IEnumerable<IPropertyDefinition> ITypeDefinition.Properties => Properties;

        IEnumerable<IEventDefinition> ITypeDefinition.Events => Events;

        IEnumerable<ITypeDefinition> ITypeDefinition.NestedTypes => NestedTypes;

        IEnumerable<IGenericParameter> ITypeDefinition.GenericParameters => GenericParameters;

        ITypeDefinition ITypeDefinition.DeclaringType => DeclaringType as MutableTypeDefinition;

        IEnumerable<ICustomAttribute> ITypeDefinition.CustomAttributes => CustomAttributes;

        IEnumerable<IField> IType.Fields => Fields;

        /// <summary>
        /// Gets the IL processor for creating new methods in this type.
        /// </summary>
        public MutableILProcessor GetILProcessor(MutableMethodDefinition method)
        {
            if (method.Body == null)
            {
                method.Body = new MutableMethodBody(method);
            }
            return new MutableILProcessor(method.Body);
        }

        /// <inheritdoc/>
        public override MutableTypeDefinition Resolve() => this;
    }

    /// <summary>
    /// Represents an interface implementation.
    /// </summary>
    public class MutableInterfaceImplementation
    {
        /// <summary>
        /// Creates a new interface implementation.
        /// </summary>
        public MutableInterfaceImplementation(MutableTypeReference interfaceType)
        {
            InterfaceType = interfaceType;
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The interface type.
        /// </summary>
        public MutableTypeReference InterfaceType { get; set; }

        /// <summary>
        /// Custom attributes on the interface implementation.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }
    }

    /// <summary>
    /// Represents a generic parameter.
    /// </summary>
    public class MutableGenericParameter : MutableTypeReference, IGenericParameter
    {
        /// <summary>
        /// Creates a new generic parameter.
        /// </summary>
        public MutableGenericParameter(string name, object owner)
            : base(null, name, null)
        {
            Owner = owner;
            Constraints = new List<MutableGenericParameterConstraint>();
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The owner (type or method) of this generic parameter.
        /// </summary>
        public object Owner { get; }

        /// <summary>
        /// The position of this parameter in the owner's generic parameter list.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The generic parameter attributes.
        /// </summary>
        public System.Reflection.GenericParameterAttributes GenericParameterAttributes { get; set; }

        public System.Reflection.GenericParameterAttributes Attributes => GenericParameterAttributes;

        /// <summary>
        /// Constraints on this generic parameter.
        /// </summary>
        public List<MutableGenericParameterConstraint> Constraints { get; }

        /// <summary>
        /// Custom attributes applied to this generic parameter.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        public bool HasCustomAttributes => CustomAttributes.Count > 0;

        public IEnumerable<string> ConstraintTypeNames
        {
            get
            {
                foreach (var constraint in Constraints)
                    yield return constraint.ConstraintType?.FullName ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Represents a constraint on a generic parameter.
    /// </summary>
    public class MutableGenericParameterConstraint
    {
        /// <summary>
        /// Creates a new generic parameter constraint.
        /// </summary>
        public MutableGenericParameterConstraint(MutableTypeReference constraintType)
        {
            ConstraintType = constraintType;
        }

        /// <summary>
        /// The constraint type.
        /// </summary>
        public MutableTypeReference ConstraintType { get; set; }
    }
}
