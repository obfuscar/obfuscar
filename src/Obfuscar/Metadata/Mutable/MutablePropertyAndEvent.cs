using System.Collections.Generic;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a property definition in the mutable object model.
    /// This replaces legacy Mono.Cecil.PropertyDefinition.
    /// </summary>
    public class MutablePropertyDefinition : IPropertyDefinition
    {
        /// <summary>
        /// Creates a new property definition.
        /// </summary>
        public MutablePropertyDefinition(string name, System.Reflection.PropertyAttributes attributes, MutableTypeReference propertyType)
        {
            Name = name;
            Attributes = attributes;
            PropertyType = propertyType;
            CustomAttributes = new List<MutableCustomAttribute>();
            Parameters = new List<MutableParameterDefinition>();
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The property attributes.
        /// </summary>
        public System.Reflection.PropertyAttributes Attributes { get; set; }

        public int MetadataToken { get; set; }

        /// <summary>
        /// The type of the property.
        /// </summary>
        public MutableTypeReference PropertyType { get; set; }

        /// <summary>
        /// The type that declares this property.
        /// </summary>
        public MutableTypeDefinition DeclaringType { get; set; }

        /// <summary>
        /// The getter method.
        /// </summary>
        public MutableMethodDefinition GetMethod { get; set; }

        /// <summary>
        /// The setter method.
        /// </summary>
        public MutableMethodDefinition SetMethod { get; set; }

        /// <summary>
        /// Whether this property has a getter.
        /// </summary>
        public bool HasGetter => GetMethod != null;

        /// <summary>
        /// Whether this property has a setter.
        /// </summary>
        public bool HasSetter => SetMethod != null;

        /// <summary>
        /// Custom attributes applied to this property.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Parameters (for indexed properties).
        /// </summary>
        public List<MutableParameterDefinition> Parameters { get; }

        /// <summary>
        /// The constant value (for const properties).
        /// </summary>
        public object Constant { get; set; }

        public string PropertyTypeFullName => PropertyType?.FullName ?? string.Empty;

        public string DeclaringTypeFullName => DeclaringType?.FullName ?? string.Empty;

        public System.Reflection.MethodAttributes GetterMethodAttributes => GetMethod?.Attributes ?? 0;

        public System.Reflection.MethodAttributes SetterMethodAttributes => SetMethod?.Attributes ?? 0;

        public bool IsRuntimeSpecialName => (GetMethod?.IsRuntimeSpecialName ?? false) || (SetMethod?.IsRuntimeSpecialName ?? false);

        public bool IsPublic => (GetMethod?.IsPublic ?? false) || (SetMethod?.IsPublic ?? false);

        public bool HasCustomAttributes => CustomAttributes.Count > 0;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in CustomAttributes)
                    yield return attr.AttributeTypeName ?? string.Empty;
            }
        }

        IMethodDefinition IPropertyDefinition.GetMethod => GetMethod;

        IMethodDefinition IPropertyDefinition.SetMethod => SetMethod;

        IEnumerable<ICustomAttribute> IPropertyDefinition.CustomAttributes => CustomAttributes;

        ITypeDefinition IPropertyDefinition.DeclaringType => DeclaringType;

        /// <summary>
        /// Gets the full name of the property.
        /// </summary>
        public string FullName => $"{PropertyType?.FullName ?? "?"} {DeclaringType?.FullName ?? "?"}::{Name}";

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }

    /// <summary>
    /// Represents an event definition in the mutable object model.
    /// This replaces legacy Mono.Cecil.EventDefinition.
    /// </summary>
    public class MutableEventDefinition : IEventDefinition
    {
        /// <summary>
        /// Creates a new event definition.
        /// </summary>
        public MutableEventDefinition(string name, System.Reflection.EventAttributes attributes, MutableTypeReference eventType)
        {
            Name = name;
            Attributes = attributes;
            EventType = eventType;
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The name of the event.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The event attributes.
        /// </summary>
        public System.Reflection.EventAttributes Attributes { get; set; }

        public int MetadataToken { get; set; }

        /// <summary>
        /// The type of the event (the delegate type).
        /// </summary>
        public MutableTypeReference EventType { get; set; }

        /// <summary>
        /// The type that declares this event.
        /// </summary>
        public MutableTypeDefinition DeclaringType { get; set; }

        /// <summary>
        /// The add accessor method.
        /// </summary>
        public MutableMethodDefinition AddMethod { get; set; }

        /// <summary>
        /// The remove accessor method.
        /// </summary>
        public MutableMethodDefinition RemoveMethod { get; set; }

        /// <summary>
        /// The invoke method (optional).
        /// </summary>
        public MutableMethodDefinition InvokeMethod { get; set; }

        /// <summary>
        /// Custom attributes applied to this event.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Gets the full name of the event.
        /// </summary>
        public string FullName => $"{EventType?.FullName ?? "?"} {DeclaringType?.FullName ?? "?"}::{Name}";

        public string EventTypeFullName => EventType?.FullName ?? string.Empty;

        public string DeclaringTypeFullName => DeclaringType?.FullName ?? string.Empty;

        public System.Reflection.MethodAttributes AddMethodAttributes => AddMethod?.Attributes ?? 0;

        public System.Reflection.MethodAttributes RemoveMethodAttributes => RemoveMethod?.Attributes ?? 0;

        public bool IsRuntimeSpecialName => (AddMethod?.IsRuntimeSpecialName ?? false) || (RemoveMethod?.IsRuntimeSpecialName ?? false);

        public bool IsPublic => (AddMethod?.IsPublic ?? false) || (RemoveMethod?.IsPublic ?? false);

        public bool HasCustomAttributes => CustomAttributes.Count > 0;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in CustomAttributes)
                    yield return attr.AttributeTypeName ?? string.Empty;
            }
        }

        IMethodDefinition IEventDefinition.AddMethod => AddMethod;

        IMethodDefinition IEventDefinition.RemoveMethod => RemoveMethod;

        IMethodDefinition IEventDefinition.InvokeMethod => InvokeMethod;

        IEnumerable<ICustomAttribute> IEventDefinition.CustomAttributes => CustomAttributes;

        ITypeDefinition IEventDefinition.DeclaringType => DeclaringType;

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }
}
