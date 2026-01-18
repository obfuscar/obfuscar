using System;
using System.Collections.Generic;
using System.Reflection;

namespace Obfuscar.Metadata.Abstractions
{
    /// <summary>
    /// Extended type definition interface for full type manipulation.
    /// Extends IType with methods, properties, events, and nested types.
    /// </summary>
    public interface ITypeDefinition : IType
    {
        /// <summary>Metadata token/handle identifier.</summary>
        int MetadataToken { get; }

        /// <summary>Type attributes.</summary>
        TypeAttributes Attributes { get; }

        /// <summary>Methods defined in this type.</summary>
        IEnumerable<IMethodDefinition> Methods { get; }

        /// <summary>Properties defined in this type.</summary>
        IEnumerable<IPropertyDefinition> Properties { get; }

        /// <summary>Events defined in this type.</summary>
        IEnumerable<IEventDefinition> Events { get; }

        /// <summary>Nested types.</summary>
        IEnumerable<ITypeDefinition> NestedTypes { get; }

        /// <summary>Generic parameters.</summary>
        IEnumerable<IGenericParameter> GenericParameters { get; }

        /// <summary>Whether the type has generic parameters.</summary>
        bool HasGenericParameters { get; }

        /// <summary>Whether this is a nested type.</summary>
        bool IsNested { get; }

        /// <summary>Whether this is a value type.</summary>
        bool IsValueType { get; }

        /// <summary>Whether this is an interface.</summary>
        bool IsInterface { get; }

        /// <summary>Whether this is a class.</summary>
        bool IsClass { get; }

        /// <summary>Declaring type (if nested).</summary>
        ITypeDefinition DeclaringType { get; }

        /// <summary>Custom attributes on this type.</summary>
        IEnumerable<ICustomAttribute> CustomAttributes { get; }
    }

    /// <summary>Extended field definition.</summary>
    public interface IFieldDefinition : IField
    {
        int MetadataToken { get; }
        object ConstantValue { get; }
        bool HasConstant { get; }
        IEnumerable<ICustomAttribute> CustomAttributes { get; }
    }

    /// <summary>Extended method definition.</summary>
    public interface IMethodDefinition : IMethod
    {
        int MetadataToken { get; }
        bool HasBody { get; }
        IMethodBody Body { get; }
        IEnumerable<IParameter> Parameters { get; }
        IEnumerable<IGenericParameter> GenericParameters { get; }
        bool HasGenericParameters { get; }
        IEnumerable<ICustomAttribute> CustomAttributes { get; }
        bool HasCustomAttributes { get; }
        bool IsVirtual { get; }
        bool IsAbstract { get; }
        bool IsFinal { get; }
        bool IsStatic { get; }
        bool IsConstructor { get; }
        bool IsPrivate { get; }
        bool IsHideBySig { get; }
        bool IsNewSlot { get; }
        bool IsCompilerControlled { get; }
        ITypeDefinition DeclaringType { get; }
    }

    /// <summary>Extended property definition.</summary>
    public interface IPropertyDefinition : IProperty
    {
        int MetadataToken { get; }
        IMethodDefinition GetMethod { get; }
        IMethodDefinition SetMethod { get; }
        IEnumerable<ICustomAttribute> CustomAttributes { get; }
        ITypeDefinition DeclaringType { get; }
    }

    /// <summary>Extended event definition.</summary>
    public interface IEventDefinition : IEvent
    {
        int MetadataToken { get; }
        IMethodDefinition AddMethod { get; }
        IMethodDefinition RemoveMethod { get; }
        IMethodDefinition InvokeMethod { get; }
        IEnumerable<ICustomAttribute> CustomAttributes { get; }
        ITypeDefinition DeclaringType { get; }
    }

    /// <summary>Method body abstraction.</summary>
    public interface IMethodBody
    {
        IEnumerable<IInstruction> Instructions { get; }
        IEnumerable<IExceptionHandler> ExceptionHandlers { get; }
        IEnumerable<IVariableDefinition> Variables { get; }
        int MaxStackSize { get; }
        bool InitLocals { get; }
    }

    /// <summary>IL instruction abstraction.</summary>
    public interface IInstruction
    {
        int Offset { get; }
        System.Reflection.Emit.OpCode OpCode { get; }
        object Operand { get; }
    }

    /// <summary>Exception handler abstraction.</summary>
    public interface IExceptionHandler
    {
        ExceptionHandlerType HandlerType { get; }
        int TryStart { get; }
        int TryEnd { get; }
        int HandlerStart { get; }
        int HandlerEnd { get; }
        string CatchTypeName { get; }
        int FilterStart { get; }
    }

    /// <summary>Exception handler type.</summary>
    public enum ExceptionHandlerType
    {
        Catch,
        Filter,
        Finally,
        Fault
    }

    /// <summary>Local variable definition.</summary>
    public interface IVariableDefinition
    {
        int Index { get; }
        string VariableTypeName { get; }
        bool IsPinned { get; }
    }

    /// <summary>Parameter abstraction.</summary>
    public interface IParameter
    {
        string Name { get; }
        int Index { get; }
        string ParameterTypeName { get; }
        ParameterAttributes Attributes { get; }
        bool IsOut { get; }
        bool IsIn { get; }
        bool HasDefault { get; }
        object DefaultValue { get; }
        IEnumerable<ICustomAttribute> CustomAttributes { get; }
    }

    /// <summary>Generic parameter abstraction.</summary>
    public interface IGenericParameter
    {
        string Name { get; }
        int Position { get; }
        GenericParameterAttributes Attributes { get; }
        IEnumerable<string> ConstraintTypeNames { get; }
    }

    /// <summary>Custom attribute abstraction.</summary>
    public interface ICustomAttribute
    {
        string AttributeTypeName { get; }
        IMethodDefinition Constructor { get; }
        IEnumerable<ICustomAttributeArgument> ConstructorArguments { get; }
        IEnumerable<ICustomAttributeNamedArgument> NamedArguments { get; }
    }

    /// <summary>Custom attribute argument.</summary>
    public interface ICustomAttributeArgument
    {
        string TypeName { get; }
        object Value { get; }
    }

    /// <summary>Custom attribute named argument.</summary>
    public interface ICustomAttributeNamedArgument
    {
        string Name { get; }
        bool IsField { get; }
        ICustomAttributeArgument Argument { get; }
    }
}
