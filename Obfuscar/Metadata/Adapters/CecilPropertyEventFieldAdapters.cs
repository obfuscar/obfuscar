using System;
using System.Collections.Generic;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;

// Use aliases to avoid ambiguity
using SysMethodAttributes = System.Reflection.MethodAttributes;
using SysFieldAttributes = System.Reflection.FieldAttributes;

namespace Obfuscar.Metadata.Adapters
{
    /// <summary>
    /// Cecil-backed IPropertyDefinition implementation.
    /// </summary>
    public class CecilPropertyDefinitionAdapter : IPropertyDefinition
    {
        private readonly PropertyDefinition property;
        private readonly SrmAssemblyReader srmReader;

        public CecilPropertyDefinitionAdapter(PropertyDefinition property, SrmAssemblyReader srmReader = null)
        {
            this.property = property ?? throw new ArgumentNullException(nameof(property));
            this.srmReader = srmReader;
        }

        // IProperty properties
        public string Name => property.Name;
        public string PropertyTypeFullName => property.PropertyType?.FullName;
        public string DeclaringTypeFullName => property.DeclaringType?.FullName;

        public SysMethodAttributes GetterMethodAttributes
        {
            get
            {
                if (property.GetMethod == null) return 0;
                return (SysMethodAttributes)property.GetMethod.Attributes;
            }
        }

        public SysMethodAttributes SetterMethodAttributes
        {
            get
            {
                if (property.SetMethod == null) return 0;
                return (SysMethodAttributes)property.SetMethod.Attributes;
            }
        }

        public bool IsRuntimeSpecialName => property.IsRuntimeSpecialName;

        public bool IsPublic
        {
            get
            {
                return (property.GetMethod?.IsPublic ?? false) || (property.SetMethod?.IsPublic ?? false);
            }
        }

        public bool HasCustomAttributes => property.HasCustomAttributes;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in property.CustomAttributes)
                {
                    yield return attr.AttributeType.FullName;
                }
            }
        }

        // IPropertyDefinition properties
        public int MetadataToken => property.MetadataToken.ToInt32();

        public IMethodDefinition GetMethod
        {
            get
            {
                if (property.GetMethod == null) return null;
                return new CecilMethodDefinitionAdapter(property.GetMethod, srmReader);
            }
        }

        public IMethodDefinition SetMethod
        {
            get
            {
                if (property.SetMethod == null) return null;
                return new CecilMethodDefinitionAdapter(property.SetMethod, srmReader);
            }
        }

        public IEnumerable<Abstractions.ICustomAttribute> CustomAttributes
        {
            get
            {
                foreach (var attr in property.CustomAttributes)
                {
                    yield return new CecilCustomAttributeAdapter(attr, srmReader);
                }
            }
        }

        public ITypeDefinition DeclaringType
        {
            get
            {
                if (property.DeclaringType == null) return null;
                return new CecilTypeDefinitionAdapter(property.DeclaringType, srmReader);
            }
        }

        /// <summary>Get the underlying Cecil PropertyDefinition (for migration compatibility).</summary>
        public PropertyDefinition Definition => property;
    }

    /// <summary>
    /// Cecil-backed IEventDefinition implementation.
    /// </summary>
    public class CecilEventDefinitionAdapter : IEventDefinition
    {
        private readonly EventDefinition evt;
        private readonly SrmAssemblyReader srmReader;

        public CecilEventDefinitionAdapter(EventDefinition evt, SrmAssemblyReader srmReader = null)
        {
            this.evt = evt ?? throw new ArgumentNullException(nameof(evt));
            this.srmReader = srmReader;
        }

        // IEvent properties
        public string Name => evt.Name;
        public string EventTypeFullName => evt.EventType?.FullName;
        public string DeclaringTypeFullName => evt.DeclaringType?.FullName;

        public SysMethodAttributes AddMethodAttributes
        {
            get
            {
                if (evt.AddMethod == null) return 0;
                return (SysMethodAttributes)evt.AddMethod.Attributes;
            }
        }

        public SysMethodAttributes RemoveMethodAttributes
        {
            get
            {
                if (evt.RemoveMethod == null) return 0;
                return (SysMethodAttributes)evt.RemoveMethod.Attributes;
            }
        }

        public bool IsRuntimeSpecialName => evt.IsRuntimeSpecialName;

        public bool IsPublic
        {
            get
            {
                return (evt.AddMethod?.IsPublic ?? false) || (evt.RemoveMethod?.IsPublic ?? false);
            }
        }

        public bool HasCustomAttributes => evt.HasCustomAttributes;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in evt.CustomAttributes)
                {
                    yield return attr.AttributeType.FullName;
                }
            }
        }

        // IEventDefinition properties
        public int MetadataToken => evt.MetadataToken.ToInt32();

        public IMethodDefinition AddMethod
        {
            get
            {
                if (evt.AddMethod == null) return null;
                return new CecilMethodDefinitionAdapter(evt.AddMethod, srmReader);
            }
        }

        public IMethodDefinition RemoveMethod
        {
            get
            {
                if (evt.RemoveMethod == null) return null;
                return new CecilMethodDefinitionAdapter(evt.RemoveMethod, srmReader);
            }
        }

        public IMethodDefinition InvokeMethod
        {
            get
            {
                if (evt.InvokeMethod == null) return null;
                return new CecilMethodDefinitionAdapter(evt.InvokeMethod, srmReader);
            }
        }

        public IEnumerable<Abstractions.ICustomAttribute> CustomAttributes
        {
            get
            {
                foreach (var attr in evt.CustomAttributes)
                {
                    yield return new CecilCustomAttributeAdapter(attr, srmReader);
                }
            }
        }

        public ITypeDefinition DeclaringType
        {
            get
            {
                if (evt.DeclaringType == null) return null;
                return new CecilTypeDefinitionAdapter(evt.DeclaringType, srmReader);
            }
        }

        /// <summary>Get the underlying Cecil EventDefinition (for migration compatibility).</summary>
        public EventDefinition Definition => evt;
    }

    /// <summary>
    /// Cecil-backed IFieldDefinition implementation.
    /// </summary>
    public class CecilFieldDefinitionAdapter : IFieldDefinition
    {
        private readonly FieldDefinition fieldDef;

        public CecilFieldDefinitionAdapter(FieldDefinition field)
        {
            this.fieldDef = field ?? throw new ArgumentNullException(nameof(field));
        }

        // IField properties
        public string Name => fieldDef.Name;
        public string FieldTypeFullName => fieldDef.FieldType?.FullName;
        public string DeclaringTypeFullName => fieldDef.DeclaringType?.FullName;
        public bool IsStatic => fieldDef.IsStatic;
        public SysFieldAttributes Attributes => (SysFieldAttributes)fieldDef.Attributes;
        public bool HasCustomAttributes => fieldDef.HasCustomAttributes;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in fieldDef.CustomAttributes)
                {
                    yield return attr.AttributeType.FullName;
                }
            }
        }

        // IFieldDefinition properties
        public int MetadataToken => fieldDef.MetadataToken.ToInt32();
        public object ConstantValue => fieldDef.HasConstant ? fieldDef.Constant : null;
        public bool HasConstant => fieldDef.HasConstant;

        public IEnumerable<Abstractions.ICustomAttribute> CustomAttributes
        {
            get
            {
                foreach (var attr in fieldDef.CustomAttributes)
                {
                    yield return new CecilCustomAttributeAdapter(attr, null);
                }
            }
        }

        /// <summary>Get the underlying Cecil FieldDefinition (for migration compatibility).</summary>
        public FieldDefinition Definition => fieldDef;
    }
}
