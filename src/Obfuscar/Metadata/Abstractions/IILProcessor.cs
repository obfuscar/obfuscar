using System.Collections.Generic;
using System.Reflection.Emit;

namespace Obfuscar.Metadata.Abstractions
{
    /// <summary>
    /// Abstraction for IL instruction processing and generation.
    /// Replaces Cecil's ILProcessor.
    /// </summary>
    public interface IILProcessor
    {
        /// <summary>The method body being processed.</summary>
        IMethodBody Body { get; }

        /// <summary>Create a new instruction.</summary>
        IInstruction Create(OpCode opCode);

        /// <summary>Create an instruction with an operand.</summary>
        IInstruction Create(OpCode opCode, object operand);

        /// <summary>Create an instruction with a string operand.</summary>
        IInstruction Create(OpCode opCode, string operand);

        /// <summary>Create an instruction with an int operand.</summary>
        IInstruction Create(OpCode opCode, int operand);

        /// <summary>Create an instruction with a method reference.</summary>
        IInstruction Create(OpCode opCode, IMethodReference method);

        /// <summary>Create an instruction with a field reference.</summary>
        IInstruction Create(OpCode opCode, IFieldReference field);

        /// <summary>Create an instruction with a type reference.</summary>
        IInstruction Create(OpCode opCode, ITypeReference type);

        /// <summary>Create an instruction with another instruction as target (branch).</summary>
        IInstruction Create(OpCode opCode, IInstruction target);

        /// <summary>Emit an instruction at the end.</summary>
        void Emit(OpCode opCode);

        /// <summary>Emit an instruction with operand at the end.</summary>
        void Emit(OpCode opCode, object operand);

        /// <summary>Insert an instruction before another.</summary>
        void InsertBefore(IInstruction target, IInstruction instruction);

        /// <summary>Insert an instruction after another.</summary>
        void InsertAfter(IInstruction target, IInstruction instruction);

        /// <summary>Append an instruction at the end.</summary>
        void Append(IInstruction instruction);

        /// <summary>Remove an instruction.</summary>
        void Remove(IInstruction instruction);

        /// <summary>Replace an instruction with another.</summary>
        void Replace(IInstruction target, IInstruction instruction);
    }

    /// <summary>Type reference abstraction.</summary>
    public interface ITypeReference
    {
        string FullName { get; }
        string Name { get; }
        string Namespace { get; }
        string Scope { get; }
        bool IsGenericInstance { get; }
        bool IsArray { get; }
        bool IsByReference { get; }
        bool IsPointer { get; }
    }

    /// <summary>Method reference abstraction.</summary>
    public interface IMethodReference
    {
        string Name { get; }
        string DeclaringTypeName { get; }
        string ReturnTypeName { get; }
        IReadOnlyList<string> ParameterTypeNames { get; }
        bool HasThis { get; }
        bool IsGenericInstance { get; }
    }

    /// <summary>Field reference abstraction.</summary>
    public interface IFieldReference
    {
        string Name { get; }
        string DeclaringTypeName { get; }
        string FieldTypeName { get; }
    }

    /// <summary>
    /// Factory for creating IL-related objects.
    /// </summary>
    public interface IILFactory
    {
        /// <summary>Create an IL processor for a method.</summary>
        IILProcessor CreateProcessor(IMethodDefinition method);

        /// <summary>Import a type into the module.</summary>
        ITypeReference ImportType(System.Type type);

        /// <summary>Import a method into the module.</summary>
        IMethodReference ImportMethod(System.Reflection.MethodInfo method);

        /// <summary>Import a field into the module.</summary>
        IFieldReference ImportField(System.Reflection.FieldInfo field);

        /// <summary>Create a new type definition.</summary>
        ITypeDefinition CreateType(string @namespace, string name, System.Reflection.TypeAttributes attributes);

        /// <summary>Create a new method definition.</summary>
        IMethodDefinition CreateMethod(string name, System.Reflection.MethodAttributes attributes, ITypeReference returnType);

        /// <summary>Create a new field definition.</summary>
        IFieldDefinition CreateField(string name, System.Reflection.FieldAttributes attributes, ITypeReference fieldType);
    }
}
