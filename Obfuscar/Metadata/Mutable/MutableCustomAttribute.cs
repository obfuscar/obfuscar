using System.Collections.Generic;
using System.Linq;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a custom attribute in the mutable object model.
    /// This replaces legacy Mono.Cecil.CustomAttribute.
    /// </summary>
    public class MutableCustomAttribute : ICustomAttribute
    {
        /// <summary>
        /// Creates a new custom attribute.
        /// </summary>
        public MutableCustomAttribute(MutableMethodReference constructor)
        {
            Constructor = constructor;
            ConstructorArguments = new List<MutableCustomAttributeArgument>();
            Fields = new List<MutableCustomAttributeNamedArgument>();
            Properties = new List<MutableCustomAttributeNamedArgument>();
        }

        /// <summary>
        /// The constructor method of the attribute.
        /// </summary>
        public MutableMethodReference Constructor { get; set; }

        /// <summary>
        /// The attribute type.
        /// </summary>
        public MutableTypeReference AttributeType => Constructor?.DeclaringType;

        /// <summary>
        /// Positional constructor arguments.
        /// </summary>
        public List<MutableCustomAttributeArgument> ConstructorArguments { get; }

        /// <summary>
        /// Named field arguments.
        /// </summary>
        public List<MutableCustomAttributeNamedArgument> Fields { get; }

        /// <summary>
        /// Named property arguments.
        /// </summary>
        public List<MutableCustomAttributeNamedArgument> Properties { get; }

        public string AttributeTypeName => AttributeType?.FullName ?? string.Empty;

        IMethodDefinition ICustomAttribute.Constructor => Constructor?.Resolve();

        IEnumerable<ICustomAttributeArgument> ICustomAttribute.ConstructorArguments => ConstructorArguments;

        IEnumerable<ICustomAttributeNamedArgument> ICustomAttribute.NamedArguments =>
            Fields.Concat(Properties);
    }

    /// <summary>
    /// Represents a custom attribute constructor argument.
    /// </summary>
    public class MutableCustomAttributeArgument : ICustomAttributeArgument
    {
        /// <summary>
        /// Creates a new custom attribute argument.
        /// </summary>
        public MutableCustomAttributeArgument(MutableTypeReference type, object value)
        {
            Type = type;
            Value = value;
        }

        /// <summary>
        /// The type of the argument.
        /// </summary>
        public MutableTypeReference Type { get; set; }

        /// <summary>
        /// The value of the argument.
        /// </summary>
        public object Value { get; set; }

        public string TypeName => Type?.FullName ?? string.Empty;
    }

    /// <summary>
    /// Represents a named custom attribute argument.
    /// </summary>
    public class MutableCustomAttributeNamedArgument : ICustomAttributeNamedArgument
    {
        /// <summary>
        /// Creates a new named custom attribute argument.
        /// </summary>
        public MutableCustomAttributeNamedArgument(string name, MutableCustomAttributeArgument argument, bool isField = false)
        {
            Name = name;
            Argument = argument;
            IsField = isField;
        }

        /// <summary>
        /// The name of the field or property.
        /// </summary>
        public string Name { get; set; }

        public bool IsField { get; set; }

        /// <summary>
        /// The argument value.
        /// </summary>
        public MutableCustomAttributeArgument Argument { get; set; }

        ICustomAttributeArgument ICustomAttributeNamedArgument.Argument => Argument;
    }
}
