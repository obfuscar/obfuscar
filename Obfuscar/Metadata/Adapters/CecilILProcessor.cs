using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Obfuscar.Metadata.Abstractions;

// Use alias to avoid ambiguity
using CecilOpCodes = Mono.Cecil.Cil.OpCodes;
using SysOpCode = System.Reflection.Emit.OpCode;

namespace Obfuscar.Metadata.Adapters
{
    /// <summary>
    /// Cecil-backed IILProcessor implementation for IL manipulation.
    /// </summary>
    public class CecilILProcessor : IILProcessor
    {
        private readonly ILProcessor processor;
        private readonly ModuleDefinition module;

        public CecilILProcessor(MethodDefinition method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            
            this.processor = method.Body.GetILProcessor();
            this.module = method.Module;
        }

        public Abstractions.IMethodBody Body => 
            new CecilMethodBodyAdapter(processor.Body);

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode)
        {
            var cecilOpCode = MapOpCode(opCode);
            var instr = processor.Create(cecilOpCode);
            return new CecilInstructionAdapter(instr);
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, object operand)
        {
            var cecilOpCode = MapOpCode(opCode);
            // Handle different operand types
            if (operand is CecilInstructionAdapter instrAdapter)
            {
                return new CecilInstructionAdapter(processor.Create(cecilOpCode, instrAdapter.CecilInstruction));
            }
            throw new NotSupportedException($"Operand type {operand?.GetType().Name} not supported yet");
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, string operand)
        {
            var cecilOpCode = MapOpCode(opCode);
            return new CecilInstructionAdapter(processor.Create(cecilOpCode, operand));
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, int operand)
        {
            var cecilOpCode = MapOpCode(opCode);
            return new CecilInstructionAdapter(processor.Create(cecilOpCode, operand));
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, Abstractions.IMethodReference method)
        {
            throw new NotImplementedException("Method reference operand not yet implemented");
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, Abstractions.IFieldReference field)
        {
            throw new NotImplementedException("Field reference operand not yet implemented");
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, Abstractions.ITypeReference type)
        {
            throw new NotImplementedException("Type reference operand not yet implemented");
        }

        public Abstractions.IInstruction Create(System.Reflection.Emit.OpCode opCode, Abstractions.IInstruction target)
        {
            var cecilOpCode = MapOpCode(opCode);
            if (target is CecilInstructionAdapter adapter)
            {
                return new CecilInstructionAdapter(processor.Create(cecilOpCode, adapter.CecilInstruction));
            }
            throw new ArgumentException("Target must be a CecilInstructionAdapter", nameof(target));
        }

        public void Emit(System.Reflection.Emit.OpCode opCode)
        {
            var cecilOpCode = MapOpCode(opCode);
            var instr = processor.Create(cecilOpCode);
            processor.Append(instr);
        }

        public void Emit(System.Reflection.Emit.OpCode opCode, object operand)
        {
            var instr = Create(opCode, operand);
            Append(instr);
        }

        public void InsertBefore(Abstractions.IInstruction target, Abstractions.IInstruction instruction)
        {
            if (target is CecilInstructionAdapter targetAdapter && instruction is CecilInstructionAdapter instrAdapter)
            {
                processor.InsertBefore(targetAdapter.CecilInstruction, instrAdapter.CecilInstruction);
            }
            else
            {
                throw new ArgumentException("Both target and instruction must be CecilInstructionAdapter");
            }
        }

        public void InsertAfter(Abstractions.IInstruction target, Abstractions.IInstruction instruction)
        {
            if (target is CecilInstructionAdapter targetAdapter && instruction is CecilInstructionAdapter instrAdapter)
            {
                processor.InsertAfter(targetAdapter.CecilInstruction, instrAdapter.CecilInstruction);
            }
            else
            {
                throw new ArgumentException("Both target and instruction must be CecilInstructionAdapter");
            }
        }

        public void Append(Abstractions.IInstruction instruction)
        {
            if (instruction is CecilInstructionAdapter adapter)
            {
                processor.Append(adapter.CecilInstruction);
            }
            else
            {
                throw new ArgumentException("Instruction must be a CecilInstructionAdapter", nameof(instruction));
            }
        }

        public void Remove(Abstractions.IInstruction instruction)
        {
            if (instruction is CecilInstructionAdapter adapter)
            {
                processor.Remove(adapter.CecilInstruction);
            }
            else
            {
                throw new ArgumentException("Instruction must be a CecilInstructionAdapter", nameof(instruction));
            }
        }

        public void Replace(Abstractions.IInstruction target, Abstractions.IInstruction instruction)
        {
            if (target is CecilInstructionAdapter targetAdapter && instruction is CecilInstructionAdapter instrAdapter)
            {
                processor.Replace(targetAdapter.CecilInstruction, instrAdapter.CecilInstruction);
            }
            else
            {
                throw new ArgumentException("Both target and instruction must be CecilInstructionAdapter");
            }
        }

        /// <summary>
        /// Get the underlying Cecil ILProcessor for migration compatibility.
        /// </summary>
        public ILProcessor CecilProcessor => processor;

        // Map System.Reflection.Emit.OpCode to Mono.Cecil.Cil.OpCode
        private static Mono.Cecil.Cil.OpCode MapOpCode(System.Reflection.Emit.OpCode opCode)
        {
            // Use reflection to find the matching Cecil opcode by name
            var field = typeof(OpCodes).GetField(opCode.Name, 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);
            if (field != null)
            {
                return (Mono.Cecil.Cil.OpCode)field.GetValue(null);
            }
            throw new NotSupportedException($"OpCode {opCode.Name} not found in Cecil");
        }
    }

    /// <summary>
    /// Cecil-backed IILFactory implementation.
    /// </summary>
    public class CecilILFactory : IILFactory
    {
        private readonly ModuleDefinition module;

        public CecilILFactory(ModuleDefinition module)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public IILProcessor CreateProcessor(Abstractions.IMethodDefinition method)
        {
            if (method is CecilMethodDefinitionAdapter adapter)
            {
                return new CecilILProcessor(adapter.Definition);
            }
            throw new ArgumentException("Method must be a CecilMethodDefinitionAdapter", nameof(method));
        }

        public Abstractions.ITypeReference ImportType(Type type)
        {
            var typeRef = module.ImportReference(type);
            return new CecilTypeReferenceAdapter(typeRef);
        }

        public Abstractions.IMethodReference ImportMethod(System.Reflection.MethodInfo method)
        {
            var methodRef = module.ImportReference(method);
            return new CecilMethodReferenceAdapter(methodRef);
        }

        public Abstractions.IFieldReference ImportField(System.Reflection.FieldInfo field)
        {
            var fieldRef = module.ImportReference(field);
            return new CecilFieldReferenceAdapter(fieldRef);
        }

        public Abstractions.ITypeDefinition CreateType(string @namespace, string name, System.Reflection.TypeAttributes attributes)
        {
            var typeDef = new TypeDefinition(@namespace, name, (Mono.Cecil.TypeAttributes)attributes);
            return new CecilTypeDefinitionAdapter(typeDef);
        }

        public Abstractions.IMethodDefinition CreateMethod(string name, System.Reflection.MethodAttributes attributes, Abstractions.ITypeReference returnType)
        {
            TypeReference retType = module.TypeSystem.Void;
            if (returnType is CecilTypeReferenceAdapter adapter)
            {
                retType = adapter.TypeRef;
            }
            var methodDef = new MethodDefinition(name, (Mono.Cecil.MethodAttributes)attributes, retType);
            return new CecilMethodDefinitionAdapter(methodDef);
        }

        public Abstractions.IFieldDefinition CreateField(string name, System.Reflection.FieldAttributes attributes, Abstractions.ITypeReference fieldType)
        {
            TypeReference fldType = module.TypeSystem.Object;
            if (fieldType is CecilTypeReferenceAdapter adapter)
            {
                fldType = adapter.TypeRef;
            }
            var fieldDef = new FieldDefinition(name, (Mono.Cecil.FieldAttributes)attributes, fldType);
            return new CecilFieldDefinitionAdapter(fieldDef);
        }

        /// <summary>
        /// Get the underlying Cecil ModuleDefinition for migration compatibility.
        /// </summary>
        public ModuleDefinition Module => module;
    }

    internal class CecilTypeReferenceAdapter : Abstractions.ITypeReference
    {
        private readonly TypeReference typeRef;

        public CecilTypeReferenceAdapter(TypeReference typeRef)
        {
            this.typeRef = typeRef;
        }

        public string FullName => typeRef.FullName;
        public string Name => typeRef.Name;
        public string Namespace => typeRef.Namespace;
        public string Scope => typeRef.Scope?.Name ?? string.Empty;
        public bool IsGenericInstance => typeRef.IsGenericInstance;
        public bool IsArray => typeRef.IsArray;
        public bool IsByReference => typeRef.IsByReference;
        public bool IsPointer => typeRef.IsPointer;

        internal TypeReference TypeRef => typeRef;
    }

    internal class CecilMethodReferenceAdapter : Abstractions.IMethodReference
    {
        private readonly MethodReference methodRef;

        public CecilMethodReferenceAdapter(MethodReference methodRef)
        {
            this.methodRef = methodRef;
        }

        public string Name => methodRef.Name;
        public string DeclaringTypeName => methodRef.DeclaringType?.FullName;
        public string ReturnTypeName => methodRef.ReturnType?.FullName;

        public IReadOnlyList<string> ParameterTypeNames
        {
            get
            {
                var list = new List<string>();
                foreach (var param in methodRef.Parameters)
                {
                    list.Add(param.ParameterType.FullName);
                }
                return list;
            }
        }

        public bool HasThis => methodRef.HasThis;
        public bool IsGenericInstance => methodRef.IsGenericInstance;

        internal MethodReference MethodRef => methodRef;
    }

    internal class CecilFieldReferenceAdapter : Abstractions.IFieldReference
    {
        private readonly FieldReference fieldRef;

        public CecilFieldReferenceAdapter(FieldReference fieldRef)
        {
            this.fieldRef = fieldRef;
        }

        public string Name => fieldRef.Name;
        public string DeclaringTypeName => fieldRef.DeclaringType?.FullName;
        public string FieldTypeName => fieldRef.FieldType?.FullName;

        internal FieldReference FieldRef => fieldRef;
    }
}
