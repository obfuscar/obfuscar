using System;
using System.Collections.Generic;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;

// Use aliases to avoid ambiguity with Cecil types
using SysTypeAttributes = System.Reflection.TypeAttributes;
using SysGenericParameterAttributes = System.Reflection.GenericParameterAttributes;

namespace Obfuscar.Metadata.Adapters
{
    /// <summary>
    /// Cecil-backed ITypeDefinition implementation.
    /// </summary>
    public class CecilTypeDefinitionAdapter : ITypeDefinition
    {
        private readonly TypeDefinition type;
        private readonly SrmAssemblyReader srmReader;

        public CecilTypeDefinitionAdapter(TypeDefinition type, SrmAssemblyReader srmReader = null)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.srmReader = srmReader;
        }

        /// <summary>
        /// Gets the underlying Cecil TypeDefinition for reference equality and migration purposes.
        /// </summary>
        internal TypeDefinition UnderlyingType => type;

        // Equality based on underlying Cecil type for dictionary key usage
        public override bool Equals(object obj)
        {
            if (obj is CecilTypeDefinitionAdapter other)
                return ReferenceEquals(type, other.type);
            return false;
        }

        public override int GetHashCode() => type?.GetHashCode() ?? 0;

        // IType properties
        public string Scope => type.Module?.Assembly?.Name?.Name ?? string.Empty;
        public string FullName => type.FullName;
        public string Name => type.Name;
        public string Namespace => type.Namespace;
        public string BaseTypeFullName => type.BaseType?.FullName;

        public IEnumerable<string> InterfaceTypeFullNames
        {
            get
            {
                foreach (var iface in type.Interfaces)
                {
                    yield return iface.InterfaceType.FullName;
                }
            }
        }

        public bool IsPublic => type.IsPublic || type.IsNestedPublic;
        public bool IsSerializable => type.IsSerializable;
        public bool IsSealed => type.IsSealed;
        public bool IsAbstract => type.IsAbstract;
        public bool IsEnum => type.IsEnum;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in type.CustomAttributes)
                {
                    yield return attr.AttributeType.FullName;
                }
            }
        }

        public IEnumerable<IField> Fields
        {
            get
            {
                foreach (var fld in type.Fields)
                {
                    yield return new CecilFieldAdapter(fld);
                }
            }
        }

        // ITypeDefinition properties
        public int MetadataToken => type.MetadataToken.ToInt32();

        public SysTypeAttributes Attributes => (SysTypeAttributes)type.Attributes;

        public IEnumerable<IMethodDefinition> Methods
        {
            get
            {
                foreach (var method in type.Methods)
                {
                    yield return new CecilMethodDefinitionAdapter(method, srmReader);
                }
            }
        }

        public IEnumerable<IPropertyDefinition> Properties
        {
            get
            {
                foreach (var prop in type.Properties)
                {
                    yield return new CecilPropertyDefinitionAdapter(prop, srmReader);
                }
            }
        }

        public IEnumerable<IEventDefinition> Events
        {
            get
            {
                foreach (var evt in type.Events)
                {
                    yield return new CecilEventDefinitionAdapter(evt, srmReader);
                }
            }
        }

        public IEnumerable<ITypeDefinition> NestedTypes
        {
            get
            {
                foreach (var nested in type.NestedTypes)
                {
                    yield return new CecilTypeDefinitionAdapter(nested, srmReader);
                }
            }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get
            {
                foreach (var gp in type.GenericParameters)
                {
                    yield return new CecilGenericParameterAdapter(gp);
                }
            }
        }

        public bool HasGenericParameters => type.HasGenericParameters;
        public bool IsNested => type.IsNested;
        public bool IsValueType => type.IsValueType;
        public bool IsInterface => type.IsInterface;
        public bool IsClass => type.IsClass;

        public ITypeDefinition DeclaringType
        {
            get
            {
                if (type.DeclaringType == null) return null;
                return new CecilTypeDefinitionAdapter(type.DeclaringType, srmReader);
            }
        }

        public IEnumerable<Abstractions.ICustomAttribute> CustomAttributes
        {
            get
            {
                foreach (var attr in type.CustomAttributes)
                {
                    yield return new CecilCustomAttributeAdapter(attr, srmReader);
                }
            }
        }

        /// <summary>Get the underlying Cecil TypeDefinition (for migration compatibility).</summary>
        public TypeDefinition Definition => type;
    }

    internal class CecilGenericParameterAdapter : IGenericParameter
    {
        private readonly GenericParameter gp;

        public CecilGenericParameterAdapter(GenericParameter gp)
        {
            this.gp = gp;
        }

        public string Name => gp.Name;
        public int Position => gp.Position;
        public SysGenericParameterAttributes Attributes => (SysGenericParameterAttributes)gp.Attributes;

        public IEnumerable<string> ConstraintTypeNames
        {
            get
            {
                foreach (var constraint in gp.Constraints)
                {
                    yield return constraint.ConstraintType.FullName;
                }
            }
        }
    }

    internal class CecilCustomAttributeAdapter : Abstractions.ICustomAttribute
    {
        private readonly CustomAttribute attr;
        private readonly SrmAssemblyReader srmReader;

        public CecilCustomAttributeAdapter(CustomAttribute attr, SrmAssemblyReader srmReader = null)
        {
            this.attr = attr;
            this.srmReader = srmReader;
        }

        public string AttributeTypeName => attr.AttributeType.FullName;

        public IMethodDefinition Constructor
        {
            get
            {
                if (attr.Constructor is MethodDefinition md)
                    return new CecilMethodDefinitionAdapter(md, srmReader);
                return null;
            }
        }

        public IEnumerable<Abstractions.ICustomAttributeArgument> ConstructorArguments
        {
            get
            {
                foreach (var arg in attr.ConstructorArguments)
                {
                    yield return new CecilCustomAttributeArgumentAdapter(arg);
                }
            }
        }

        public IEnumerable<Abstractions.ICustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                foreach (var prop in attr.Properties)
                {
                    yield return new CecilCustomAttributeNamedArgumentAdapter(prop, false);
                }
                foreach (var fld in attr.Fields)
                {
                    yield return new CecilCustomAttributeNamedArgumentAdapter(fld, true);
                }
            }
        }
    }

    internal class CecilCustomAttributeArgumentAdapter : Abstractions.ICustomAttributeArgument
    {
        private readonly Mono.Cecil.CustomAttributeArgument arg;

        public CecilCustomAttributeArgumentAdapter(Mono.Cecil.CustomAttributeArgument arg)
        {
            this.arg = arg;
        }

        public string TypeName => arg.Type.FullName;
        public object Value => arg.Value;
    }

    internal class CecilCustomAttributeNamedArgumentAdapter : Abstractions.ICustomAttributeNamedArgument
    {
        private readonly string name;
        private readonly Mono.Cecil.CustomAttributeArgument arg;
        private readonly bool isField;

        public CecilCustomAttributeNamedArgumentAdapter(Mono.Cecil.CustomAttributeNamedArgument namedArg, bool isField)
        {
            this.name = namedArg.Name;
            this.arg = namedArg.Argument;
            this.isField = isField;
        }

        public string Name => name;
        public bool IsField => isField;
        public Abstractions.ICustomAttributeArgument Argument => new CecilCustomAttributeArgumentAdapter(arg);
    }
}
