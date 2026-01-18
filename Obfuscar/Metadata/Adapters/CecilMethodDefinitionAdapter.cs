using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Obfuscar.Metadata.Abstractions;

// Use aliases to avoid ambiguity
using SysMethodAttributes = System.Reflection.MethodAttributes;
using SysParameterAttributes = System.Reflection.ParameterAttributes;
using CecilExceptionHandlerType = Mono.Cecil.Cil.ExceptionHandlerType;

namespace Obfuscar.Metadata.Adapters
{
    /// <summary>
    /// Cecil-backed IMethodDefinition implementation.
    /// </summary>
    public class CecilMethodDefinitionAdapter : IMethodDefinition
    {
        private readonly MethodDefinition method;
        private readonly SrmAssemblyReader srmReader;

        public CecilMethodDefinitionAdapter(MethodDefinition method, SrmAssemblyReader srmReader = null)
        {
            this.method = method ?? throw new ArgumentNullException(nameof(method));
            this.srmReader = srmReader;
        }

        // IMethod properties
        public string Name => method.Name;
        public string ReturnTypeFullName => method.ReturnType?.FullName;
        public string DeclaringTypeFullName => method.DeclaringType?.FullName;
        public SysMethodAttributes Attributes => (SysMethodAttributes)method.Attributes;

        public MethodSemantics SemanticsAttributes
        {
            get
            {
                var sem = method.SemanticsAttributes;
                var result = MethodSemantics.None;
                if ((sem & Mono.Cecil.MethodSemanticsAttributes.Getter) != 0)
                    result |= MethodSemantics.Getter;
                if ((sem & Mono.Cecil.MethodSemanticsAttributes.Setter) != 0)
                    result |= MethodSemantics.Setter;
                if ((sem & Mono.Cecil.MethodSemanticsAttributes.AddOn) != 0)
                    result |= MethodSemantics.AddOn;
                if ((sem & Mono.Cecil.MethodSemanticsAttributes.RemoveOn) != 0)
                    result |= MethodSemantics.RemoveOn;
                if ((sem & Mono.Cecil.MethodSemanticsAttributes.Fire) != 0)
                    result |= MethodSemantics.Fire;
                if ((sem & Mono.Cecil.MethodSemanticsAttributes.Other) != 0)
                    result |= MethodSemantics.Other;
                return result;
            }
        }

        public bool IsRuntime => method.IsRuntime;
        public bool IsSpecialName => method.IsSpecialName;
        public bool IsPublic => method.IsPublic;
        public bool IsFamily => method.IsFamily;
        public bool IsFamilyOrAssembly => method.IsFamilyOrAssembly;

        public IReadOnlyList<string> ParameterTypeFullNames
        {
            get
            {
                var list = new List<string>();
                foreach (var param in method.Parameters)
                {
                    list.Add(param.ParameterType.FullName);
                }
                return list;
            }
        }

        // IMethodDefinition properties
        public int MetadataToken => method.MetadataToken.ToInt32();
        public bool HasBody => method.HasBody;

        public Abstractions.IMethodBody Body
        {
            get
            {
                if (!method.HasBody) return null;
                return new CecilMethodBodyAdapter(method.Body);
            }
        }

        public IEnumerable<IParameter> Parameters
        {
            get
            {
                foreach (var param in method.Parameters)
                {
                    yield return new CecilParameterAdapter(param, srmReader);
                }
            }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get
            {
                foreach (var gp in method.GenericParameters)
                {
                    yield return new CecilGenericParameterAdapter(gp);
                }
            }
        }

        public bool HasGenericParameters => method.HasGenericParameters;

        public IEnumerable<Abstractions.ICustomAttribute> CustomAttributes
        {
            get
            {
                foreach (var attr in method.CustomAttributes)
                {
                    yield return new CecilCustomAttributeAdapter(attr, srmReader);
                }
            }
        }

        public bool HasCustomAttributes => method.HasCustomAttributes;
        public bool IsVirtual => method.IsVirtual;
        public bool IsAbstract => method.IsAbstract;
        public bool IsFinal => method.IsFinal;
        public bool IsStatic => method.IsStatic;
        public bool IsConstructor => method.IsConstructor;
        public bool IsPrivate => method.IsPrivate;
        public bool IsHideBySig => method.IsHideBySig;
        public bool IsNewSlot => method.IsNewSlot;
        public bool IsCompilerControlled => method.IsCompilerControlled;

        public ITypeDefinition DeclaringType
        {
            get
            {
                if (method.DeclaringType == null) return null;
                return new CecilTypeDefinitionAdapter(method.DeclaringType, srmReader);
            }
        }

        /// <summary>Get the underlying Cecil MethodDefinition (for migration compatibility).</summary>
        public MethodDefinition Definition => method;
    }

    internal class CecilParameterAdapter : IParameter
    {
        private readonly ParameterDefinition param;
        private readonly SrmAssemblyReader srmReader;

        public CecilParameterAdapter(ParameterDefinition param, SrmAssemblyReader srmReader = null)
        {
            this.param = param;
            this.srmReader = srmReader;
        }

        public string Name => param.Name;
        public int Index => param.Index;
        public string ParameterTypeName => param.ParameterType.FullName;
        public SysParameterAttributes Attributes => (SysParameterAttributes)param.Attributes;
        public bool IsOut => param.IsOut;
        public bool IsIn => param.IsIn;
        public bool HasDefault => param.HasDefault;
        public object DefaultValue => param.HasDefault ? param.Constant : null;

        public IEnumerable<Abstractions.ICustomAttribute> CustomAttributes
        {
            get
            {
                foreach (var attr in param.CustomAttributes)
                {
                    yield return new CecilCustomAttributeAdapter(attr, srmReader);
                }
            }
        }
    }

    internal class CecilMethodBodyAdapter : Abstractions.IMethodBody
    {
        private readonly Mono.Cecil.Cil.MethodBody body;

        public CecilMethodBodyAdapter(Mono.Cecil.Cil.MethodBody body)
        {
            this.body = body;
        }

        public IEnumerable<Abstractions.IInstruction> Instructions
        {
            get
            {
                foreach (var instr in body.Instructions)
                {
                    yield return new CecilInstructionAdapter(instr);
                }
            }
        }

        public IEnumerable<Abstractions.IExceptionHandler> ExceptionHandlers
        {
            get
            {
                foreach (var handler in body.ExceptionHandlers)
                {
                    yield return new CecilExceptionHandlerAdapter(handler);
                }
            }
        }

        public IEnumerable<Abstractions.IVariableDefinition> Variables
        {
            get
            {
                foreach (var v in body.Variables)
                {
                    yield return new CecilVariableDefinitionAdapter(v);
                }
            }
        }

        public int MaxStackSize => body.MaxStackSize;
        public bool InitLocals => body.InitLocals;
    }

    internal class CecilInstructionAdapter : Abstractions.IInstruction
    {
        private readonly Instruction instr;

        public CecilInstructionAdapter(Instruction instr)
        {
            this.instr = instr;
        }

        public int Offset => instr.Offset;

        public System.Reflection.Emit.OpCode OpCode
        {
            get
            {
                // Map Cecil opcode to System.Reflection.Emit.OpCodes
                var cecilCode = instr.OpCode.Code;
                var fieldName = cecilCode.ToString();
                var opField = typeof(System.Reflection.Emit.OpCodes).GetField(fieldName);
                if (opField != null)
                    return (System.Reflection.Emit.OpCode)opField.GetValue(null);
                return System.Reflection.Emit.OpCodes.Nop;
            }
        }

        public object Operand => instr.Operand;

        /// <summary>Get the underlying Cecil Instruction (for migration compatibility).</summary>
        public Instruction CecilInstruction => instr;
    }

    internal class CecilExceptionHandlerAdapter : Abstractions.IExceptionHandler
    {
        private readonly ExceptionHandler handler;

        public CecilExceptionHandlerAdapter(ExceptionHandler handler)
        {
            this.handler = handler;
        }

        public Abstractions.ExceptionHandlerType HandlerType
        {
            get
            {
                switch (handler.HandlerType)
                {
                    case CecilExceptionHandlerType.Catch:
                        return Abstractions.ExceptionHandlerType.Catch;
                    case CecilExceptionHandlerType.Filter:
                        return Abstractions.ExceptionHandlerType.Filter;
                    case CecilExceptionHandlerType.Finally:
                        return Abstractions.ExceptionHandlerType.Finally;
                    case CecilExceptionHandlerType.Fault:
                        return Abstractions.ExceptionHandlerType.Fault;
                    default:
                        return Abstractions.ExceptionHandlerType.Catch;
                }
            }
        }

        public int TryStart => handler.TryStart?.Offset ?? 0;
        public int TryEnd => handler.TryEnd?.Offset ?? 0;
        public int HandlerStart => handler.HandlerStart?.Offset ?? 0;
        public int HandlerEnd => handler.HandlerEnd?.Offset ?? 0;
        public string CatchTypeName => handler.CatchType?.FullName;
        public int FilterStart => handler.FilterStart?.Offset ?? 0;
    }

    internal class CecilVariableDefinitionAdapter : Abstractions.IVariableDefinition
    {
        private readonly VariableDefinition v;

        public CecilVariableDefinitionAdapter(VariableDefinition v)
        {
            this.v = v;
        }

        public int Index => v.Index;
        public string VariableTypeName => v.VariableType.FullName;
        public bool IsPinned => v.IsPinned;
    }
}
