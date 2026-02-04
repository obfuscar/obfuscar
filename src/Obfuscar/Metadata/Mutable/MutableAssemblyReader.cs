using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Reads assemblies into the mutable object model using System.Reflection.Metadata.
    /// This replaces the need for legacy Mono.Cecil.AssemblyDefinition.ReadAssembly().
    /// </summary>
    public class MutableAssemblyReader : IDisposable
    {
        private PEReader _peReader;
        private MetadataReader _metadataReader;
        private MutableModuleDefinition _module;
        private MutableAssemblyDefinition _assembly;
        private string _fileName;
        
        // Handle caches
        private Dictionary<TypeDefinitionHandle, MutableTypeDefinition> _typeDefCache;
        private Dictionary<MethodDefinitionHandle, MutableMethodDefinition> _methodDefCache;
        private Dictionary<FieldDefinitionHandle, MutableFieldDefinition> _fieldDefCache;
        private Dictionary<PropertyDefinitionHandle, MutablePropertyDefinition> _propertyDefCache;
        private Dictionary<EventDefinitionHandle, MutableEventDefinition> _eventDefCache;
        private Dictionary<ParameterHandle, MutableParameterDefinition> _parameterDefCache;
        private Dictionary<GenericParameterHandle, MutableGenericParameter> _genericParameterCache;
        private Dictionary<TypeReferenceHandle, MutableTypeReference> _typeRefCache;
        private Dictionary<MemberReferenceHandle, object> _memberRefCache;
        private Dictionary<MethodSpecificationHandle, MutableMethodReference> _methodSpecCache;
        private Dictionary<AssemblyReferenceHandle, MutableAssemblyNameReference> _asmRefCache;

        /// <summary>
        /// Reads an assembly from the specified file path.
        /// </summary>
        public MutableAssemblyDefinition Read(string fileName)
        {
            return Read(fileName, null);
        }

        /// <summary>
        /// Reads an assembly from the specified file path with reader parameters.
        /// </summary>
        public MutableAssemblyDefinition Read(string fileName, MutableReaderParameters parameters)
        {
            _fileName = fileName;
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            return Read(fileStream, parameters ?? new MutableReaderParameters());
        }

        /// <summary>
        /// Reads an assembly from a stream.
        /// </summary>
        public MutableAssemblyDefinition Read(Stream stream, MutableReaderParameters parameters)
        {
            _peReader = new PEReader(stream);
            _metadataReader = _peReader.GetMetadataReader();
            
            InitializeCaches();
            
            // Create assembly
            _assembly = ReadAssemblyDefinition();
            
            // Create main module
            _module = ReadModuleDefinition();
            _module.Assembly = _assembly;
            _module.FileName = _fileName;
            _assembly.Modules.Add(_module);
            _module.InitializeTypeSystem();
            
            // Read assembly references first
            ReadAssemblyReferences();
            
            // Read types
            ReadTypes();

            // Populate custom attributes once all definitions exist
            ReadCustomAttributes();
            
            return _assembly;
        }

        private void InitializeCaches()
        {
            _typeDefCache = new Dictionary<TypeDefinitionHandle, MutableTypeDefinition>();
            _methodDefCache = new Dictionary<MethodDefinitionHandle, MutableMethodDefinition>();
            _fieldDefCache = new Dictionary<FieldDefinitionHandle, MutableFieldDefinition>();
            _propertyDefCache = new Dictionary<PropertyDefinitionHandle, MutablePropertyDefinition>();
            _eventDefCache = new Dictionary<EventDefinitionHandle, MutableEventDefinition>();
            _parameterDefCache = new Dictionary<ParameterHandle, MutableParameterDefinition>();
            _genericParameterCache = new Dictionary<GenericParameterHandle, MutableGenericParameter>();
            _typeRefCache = new Dictionary<TypeReferenceHandle, MutableTypeReference>();
            _memberRefCache = new Dictionary<MemberReferenceHandle, object>();
            _methodSpecCache = new Dictionary<MethodSpecificationHandle, MutableMethodReference>();
            _asmRefCache = new Dictionary<AssemblyReferenceHandle, MutableAssemblyNameReference>();
        }

        private MutableAssemblyDefinition ReadAssemblyDefinition()
        {
            if (!_metadataReader.IsAssembly)
            {
                // Module without assembly manifest
                return new MutableAssemblyDefinition(new MutableAssemblyNameDefinition("module", new Version(0, 0, 0, 0)));
            }

            var asmDef = _metadataReader.GetAssemblyDefinition();
            var name = new MutableAssemblyNameDefinition(
                _metadataReader.GetString(asmDef.Name),
                asmDef.Version)
            {
                Culture = _metadataReader.GetString(asmDef.Culture),
                PublicKey = _metadataReader.GetBlobBytes(asmDef.PublicKey),
                HashAlgorithm = (System.Configuration.Assemblies.AssemblyHashAlgorithm)asmDef.HashAlgorithm,
                Attributes = (AssemblyNameFlags)asmDef.Flags
            };

            return new MutableAssemblyDefinition(name);
        }

        private MutableModuleDefinition ReadModuleDefinition()
        {
            var moduleDef = _metadataReader.GetModuleDefinition();
            var moduleName = _metadataReader.GetString(moduleDef.Name);
            
            var kind = MutableModuleKind.Dll;
            if (_peReader.PEHeaders.IsExe)
            {
                kind = _peReader.PEHeaders.PEHeader.Subsystem == Subsystem.WindowsCui 
                    ? MutableModuleKind.Console 
                    : MutableModuleKind.Windows;
            }

            var module = new MutableModuleDefinition(moduleName, kind)
            {
                Mvid = _metadataReader.GetGuid(moduleDef.Mvid),
                RuntimeVersion = _metadataReader.MetadataVersion
            };

            var corFlags = _peReader.PEHeaders.CorHeader?.Flags ?? CorFlags.ILOnly;
            var attributes = MutableModuleAttributes.ILOnly;
            if ((corFlags & CorFlags.Requires32Bit) != 0)
                attributes |= MutableModuleAttributes.Required32Bit;
            if ((corFlags & CorFlags.Prefers32Bit) != 0)
                attributes |= MutableModuleAttributes.Preferred32Bit;
            if ((corFlags & CorFlags.StrongNameSigned) != 0)
                attributes |= MutableModuleAttributes.StrongNameSigned;
            module.Attributes = attributes;

            return module;
        }

        private void ReadAssemblyReferences()
        {
            foreach (var handle in _metadataReader.AssemblyReferences)
            {
                var asmRef = _metadataReader.GetAssemblyReference(handle);
                var reference = new MutableAssemblyNameReference(
                    _metadataReader.GetString(asmRef.Name),
                    asmRef.Version)
                {
                    Culture = _metadataReader.GetString(asmRef.Culture),
                    PublicKeyToken = _metadataReader.GetBlobBytes(asmRef.PublicKeyOrToken),
                    Attributes = (AssemblyNameFlags)asmRef.Flags
                };
                
                _asmRefCache[handle] = reference;
                _module.AssemblyReferences.Add(reference);
            }
        }

        private void ReadTypes()
        {
            // First pass: create all type definitions
            foreach (var handle in _metadataReader.TypeDefinitions)
            {
                var typeDef = _metadataReader.GetTypeDefinition(handle);
                var ns = _metadataReader.GetString(typeDef.Namespace);
                var name = _metadataReader.GetString(typeDef.Name);
                
                var baseType = ReadTypeReference(typeDef.BaseType);
                var type = new MutableTypeDefinition(ns, name, typeDef.Attributes, baseType)
                {
                    MetadataToken = MetadataTokens.GetToken(handle)
                };
                type.Module = _module;
                type.IsValueType = IsValueType(typeDef);
                type.IsEnum = IsEnum(typeDef);

                _typeDefCache[handle] = type;

                // Add to module or declaring type
                if (typeDef.IsNested)
                {
                    // Will be added to parent in second pass; defer registering until declaring type is known
                }
                else
                {
                    _module.RegisterType(type);
                }
            }

            // Second pass: set up nesting relationships
            foreach (var handle in _metadataReader.TypeDefinitions)
            {
                var typeDef = _metadataReader.GetTypeDefinition(handle);
                if (typeDef.IsNested && _typeDefCache.TryGetValue(handle, out var nestedType))
                {
                    var declaringHandle = typeDef.GetDeclaringType();
                    if (_typeDefCache.TryGetValue(declaringHandle, out var declaringType))
                    {
                        nestedType.DeclaringType = declaringType;
                        declaringType.NestedTypes.Add(nestedType);

                        // Ensure nested types are also registered for fast lookup
                        _module.RegisterType(nestedType);
                    }
                }
            }

            // Third pass: read members
            foreach (var handle in _metadataReader.TypeDefinitions)
            {
                if (!_typeDefCache.TryGetValue(handle, out var type))
                    continue;

                var typeDef = _metadataReader.GetTypeDefinition(handle);
                
                // Read fields
                foreach (var fieldHandle in typeDef.GetFields())
                {
                    var field = ReadFieldDefinition(fieldHandle, type);
                    // Register field into type (adds to list and fast map)
                    type.RegisterField(field);
                }
                
                // Read methods
                foreach (var methodHandle in typeDef.GetMethods())
                {
                    var method = ReadMethodDefinition(methodHandle, type);
                    type.Methods.Add(method);
                }

                // Read explicit method implementations
                foreach (var implHandle in typeDef.GetMethodImplementations())
                {
                    var impl = _metadataReader.GetMethodImplementation(implHandle);
                    var body = ResolveMethodHandle(impl.MethodBody);
                    var declaration = ResolveMethodHandle(impl.MethodDeclaration);
                    if (body != null && declaration != null)
                    {
                        type.MethodImplementations.Add(new MutableMethodImplementation(body, declaration));
                    }
                }

                // Read interfaces
                foreach (var ifaceImpl in typeDef.GetInterfaceImplementations())
                {
                    var impl = _metadataReader.GetInterfaceImplementation(ifaceImpl);
                    var ifaceType = ReadTypeReference(impl.Interface);
                    if (ifaceType != null)
                    {
                        type.Interfaces.Add(new MutableInterfaceImplementation(ifaceType));
                    }
                }

                // Read generic parameters
                foreach (var gpHandle in typeDef.GetGenericParameters())
                {
                    var gp = ReadGenericParameter(gpHandle, type);
                    type.GenericParameters.Add(gp);
                }
            }

            // Fourth pass: read properties and events (need method definitions first)
            foreach (var handle in _metadataReader.TypeDefinitions)
            {
                if (!_typeDefCache.TryGetValue(handle, out var type))
                    continue;

                var typeDef = _metadataReader.GetTypeDefinition(handle);
                
                // Read properties
                foreach (var propHandle in typeDef.GetProperties())
                {
                    var prop = ReadPropertyDefinition(propHandle, type);
                    type.Properties.Add(prop);
                }
                
                // Read events
                foreach (var evtHandle in typeDef.GetEvents())
                {
                    var evt = ReadEventDefinition(evtHandle, type);
                    type.Events.Add(evt);
                }
            }
        }

        private MutableFieldDefinition ReadFieldDefinition(FieldDefinitionHandle handle, MutableTypeDefinition declaringType)
        {
            var fieldDef = _metadataReader.GetFieldDefinition(handle);
            var name = _metadataReader.GetString(fieldDef.Name);
            
            // Decode field type from signature
            var signature = fieldDef.DecodeSignature(new TypeProvider(_module, _metadataReader, _asmRefCache), null);
            
            var field = new MutableFieldDefinition(name, fieldDef.Attributes, signature)
            {
                DeclaringType = declaringType,
                MetadataToken = MetadataTokens.GetToken(handle)
            };
            
            // Read initial value if present
            if ((fieldDef.Attributes & FieldAttributes.HasFieldRVA) != 0)
            {
                var rva = fieldDef.GetRelativeVirtualAddress();
                if (rva != 0)
                {
                    // Read initial value from RVA
                    // This requires more complex handling of field size
                }
            }

            // Read constant value
            if ((fieldDef.Attributes & FieldAttributes.HasDefault) != 0)
            {
                var constant = fieldDef.GetDefaultValue();
                if (!constant.IsNil)
                {
                    field.Constant = ReadConstant(_metadataReader.GetConstant(constant));
                }
            }

            _fieldDefCache[handle] = field;
            return field;
        }

        private MutableMethodDefinition ReadMethodDefinition(MethodDefinitionHandle handle, MutableTypeDefinition declaringType)
        {
            var methodDef = _metadataReader.GetMethodDefinition(handle);
            var name = _metadataReader.GetString(methodDef.Name);
            
            var method = new MutableMethodDefinition(name, methodDef.Attributes, _module.TypeSystem?.Void)
            {
                DeclaringType = declaringType,
                ImplAttributes = methodDef.ImplAttributes,
                MetadataToken = MetadataTokens.GetToken(handle)
            };

            // Decode method signature with generic context (type + method).
            var signature = methodDef.DecodeSignature(
                new TypeProvider(_module, _metadataReader, _asmRefCache),
                new GenericContext(declaringType, method));

            method.ReturnType = signature.ReturnType;
            method.HasThis = signature.Header.IsInstance;

            // Populate parameters from signature first (parameter rows may be absent).
            for (int i = 0; i < signature.ParameterTypes.Length; i++)
            {
                method.Parameters.Add(new MutableParameterDefinition($"param{i}", ParameterAttributes.None, signature.ParameterTypes[i])
                {
                    Index = i
                });
            }

            // Read parameter rows (names/attributes/defaults) and apply to signature parameters.
            foreach (var paramHandle in methodDef.GetParameters())
            {
                var paramDef = _metadataReader.GetParameter(paramHandle);
                if (paramDef.SequenceNumber == 0)
                {
                    // Return parameter metadata is not represented in the mutable model.
                    continue;
                }

                var paramIndex = paramDef.SequenceNumber - 1;
                if (paramIndex < 0 || paramIndex >= method.Parameters.Count)
                {
                    continue;
                }

                var param = method.Parameters[paramIndex];
                param.Name = _metadataReader.GetString(paramDef.Name);
                param.Attributes = paramDef.Attributes;
                param.Index = paramIndex;

                if ((paramDef.Attributes & ParameterAttributes.HasDefault) != 0)
                {
                    var constant = paramDef.GetDefaultValue();
                    if (!constant.IsNil)
                    {
                        param.DefaultValue = ReadConstant(_metadataReader.GetConstant(constant));
                    }
                }

                _parameterDefCache[paramHandle] = param;
            }

            // Read generic parameters
            foreach (var gpHandle in methodDef.GetGenericParameters())
            {
                var gp = ReadGenericParameter(gpHandle, method);
                method.GenericParameters.Add(gp);
            }

            // Read method body if present
            var rva = methodDef.RelativeVirtualAddress;
            if (rva != 0 && !method.IsAbstract && !method.IsPInvokeImpl)
            {
                try
                {
                    ReadMethodBody(method, rva);
                }
                catch
                {
                    // Skip methods with unreadable bodies
                }
            }

            _methodDefCache[handle] = method;
            return method;
        }

        private void ReadMethodBody(MutableMethodDefinition method, int rva)
        {
            var body = _peReader.GetMethodBody(rva);
            method.Body = new MutableMethodBody(method)
            {
                MaxStackSize = body.MaxStack,
                InitLocals = body.LocalVariablesInitialized
            };

            // Read local variables
            if (!body.LocalSignature.IsNil)
            {
                var localSig = _metadataReader.GetStandaloneSignature(body.LocalSignature);
                var locals = localSig.DecodeLocalSignature(
                    new TypeProvider(_module, _metadataReader, _asmRefCache),
                    new GenericContext(method.DeclaringType, method));
                
                for (int i = 0; i < locals.Length; i++)
                {
                    var variable = new MutableVariableDefinition(locals[i])
                    {
                        Index = i
                    };
                    method.Body.Variables.Add(variable);
                }
            }

            // Read IL
            ReadIL(method.Body, body.GetILBytes());

            // Read exception handlers
            foreach (var region in body.ExceptionRegions)
            {
                var handler = new MutableExceptionHandler
                {
                    HandlerType = (MutableExceptionHandlerType)region.Kind,
                    TryStart = GetInstructionAtOffset(method.Body, region.TryOffset),
                    TryEnd = GetInstructionAtOffset(method.Body, region.TryOffset + region.TryLength),
                    HandlerStart = GetInstructionAtOffset(method.Body, region.HandlerOffset),
                    HandlerEnd = GetInstructionAtOffset(method.Body, region.HandlerOffset + region.HandlerLength),
                };

                if (region.Kind == ExceptionRegionKind.Catch)
                {
                    handler.CatchType = ReadTypeReference(region.CatchType);
                }
                else if (region.Kind == ExceptionRegionKind.Filter)
                {
                    handler.FilterStart = GetInstructionAtOffset(method.Body, region.FilterOffset);
                }

                method.Body.ExceptionHandlers.Add(handler);
            }
        }

        private void ReadIL(MutableMethodBody body, byte[] ilBytes)
        {
            if (ilBytes == null || ilBytes.Length == 0)
                return;

            var instructions = new List<MutableInstruction>();
            var offsetToInstruction = new Dictionary<int, MutableInstruction>();
            
            int position = 0;
            while (position < ilBytes.Length)
            {
                int offset = position;
                var opCode = ReadOpCode(ilBytes, ref position);
                var instruction = new MutableInstruction(opCode)
                {
                    Offset = offset
                };

                // Read operand
                instruction.Operand = ReadOperand(opCode, ilBytes, ref position, body);

                instructions.Add(instruction);
                offsetToInstruction[offset] = instruction;
            }

            // Resolve branch targets
            foreach (var instruction in instructions)
            {
                if (instruction.Operand is int targetOffset)
                {
                    if (instruction.OpCode.OperandType == MutableOperandType.InlineBrTarget ||
                        instruction.OpCode.OperandType == MutableOperandType.ShortInlineBrTarget)
                    {
                        var absoluteTarget = instruction.Offset + instruction.GetSize() + targetOffset;
                        if (offsetToInstruction.TryGetValue(absoluteTarget, out var target))
                        {
                            instruction.Operand = target;
                        }
                    }
                }
                else if (instruction.Operand is int[] switchTargets)
                {
                    var targets = new MutableInstruction[switchTargets.Length];
                    var baseOffset = instruction.Offset + instruction.GetSize();
                    for (int i = 0; i < switchTargets.Length; i++)
                    {
                        var absoluteTarget = baseOffset + switchTargets[i];
                        if (offsetToInstruction.TryGetValue(absoluteTarget, out var target))
                        {
                            targets[i] = target;
                        }
                    }
                    instruction.Operand = targets;
                }
            }

            body.Instructions.AddRange(instructions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MutableOpCode ReadOpCode(byte[] ilBytes, ref int position)
        {
            byte b = ilBytes[position++];
            if (b == 0xFE)
            {
                return MutableOpCodeLookup.GetTwoByteOpCode(ilBytes[position++]);
            }
            return MutableOpCodeLookup.GetSingleByteOpCode(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MutableOpCode GetOpCode(short value)
        {
            // Map opcode value to MutableOpCode
            // This is a simplified version - a full implementation would use a lookup table
            return MutableOpCodeLookup.GetOpCode(value);
        }

        private object ReadOperand(MutableOpCode opCode, byte[] ilBytes, ref int position, MutableMethodBody body)
        {
            switch (opCode.OperandType)
            {
                case MutableOperandType.InlineNone:
                    return null;
                    
                case MutableOperandType.ShortInlineBrTarget:
                    return (int)(sbyte)ilBytes[position++];
                    
                case MutableOperandType.InlineBrTarget:
                    return ReadInt32(ilBytes, ref position);
                    
                case MutableOperandType.ShortInlineI:
                    return opCode.Name == "ldc.i4.s" ? (object)(sbyte)ilBytes[position++] : (object)ilBytes[position++];
                    
                case MutableOperandType.InlineI:
                    return ReadInt32(ilBytes, ref position);
                    
                case MutableOperandType.InlineI8:
                    return ReadInt64(ilBytes, ref position);
                    
                case MutableOperandType.ShortInlineR:
                    return ReadSingle(ilBytes, ref position);
                    
                case MutableOperandType.InlineR:
                    return ReadDouble(ilBytes, ref position);
                    
                case MutableOperandType.InlineString:
                    var stringToken = ReadInt32(ilBytes, ref position);
                    var userStringHandle = MetadataTokens.UserStringHandle(stringToken & 0x00FFFFFF);
                    return _metadataReader.GetUserString(userStringHandle);
                    
                case MutableOperandType.InlineMethod:
                    var methodToken = ReadInt32(ilBytes, ref position);
                    return ResolveMethodToken(methodToken);
                    
                case MutableOperandType.InlineField:
                    var fieldToken = ReadInt32(ilBytes, ref position);
                    return ResolveFieldToken(fieldToken);
                    
                case MutableOperandType.InlineType:
                    var typeToken = ReadInt32(ilBytes, ref position);
                    return ResolveTypeToken(typeToken);
                    
                case MutableOperandType.InlineTok:
                    var token = ReadInt32(ilBytes, ref position);
                    return ResolveToken(token);
                    
                case MutableOperandType.InlineSig:
                    return ReadInt32(ilBytes, ref position); // Signature token
                    
                case MutableOperandType.ShortInlineVar:
                    var varIndex = ilBytes[position++];
                    return varIndex < body.Variables.Count ? body.Variables[varIndex] : null;
                    
                case MutableOperandType.InlineVar:
                    var varIndex2 = ReadUInt16(ilBytes, ref position);
                    return varIndex2 < body.Variables.Count ? body.Variables[varIndex2] : null;
                    
                case MutableOperandType.ShortInlineArg:
                    return ilBytes[position++]; // Argument index
                    
                case MutableOperandType.InlineArg:
                    return ReadUInt16(ilBytes, ref position); // Argument index
                    
                case MutableOperandType.InlineSwitch:
                    var count = ReadInt32(ilBytes, ref position);
                    var targets = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        targets[i] = ReadInt32(ilBytes, ref position);
                    }
                    return targets;
                    
                default:
                    return null;
            }
        }

        private int ReadInt32(byte[] bytes, ref int position)
        {
            var value = BitConverter.ToInt32(bytes, position);
            position += 4;
            return value;
        }

        private long ReadInt64(byte[] bytes, ref int position)
        {
            var value = BitConverter.ToInt64(bytes, position);
            position += 8;
            return value;
        }

        private ushort ReadUInt16(byte[] bytes, ref int position)
        {
            var value = BitConverter.ToUInt16(bytes, position);
            position += 2;
            return value;
        }

        private float ReadSingle(byte[] bytes, ref int position)
        {
            var value = BitConverter.ToSingle(bytes, position);
            position += 4;
            return value;
        }

        private double ReadDouble(byte[] bytes, ref int position)
        {
            var value = BitConverter.ToDouble(bytes, position);
            position += 8;
            return value;
        }

        private MutableMethodReference ResolveMethodToken(int token)
        {
            var handle = MetadataTokens.EntityHandle(token);
            if (handle.Kind == HandleKind.MethodDefinition)
            {
                var methodHandle = (MethodDefinitionHandle)handle;
                if (_methodDefCache.TryGetValue(methodHandle, out var method))
                    return method;
            }
            else if (handle.Kind == HandleKind.MemberReference)
            {
                return ResolveMemberReference((MemberReferenceHandle)handle) as MutableMethodReference;
            }
            else if (handle.Kind == HandleKind.MethodSpecification)
            {
                return ResolveMethodSpecification((MethodSpecificationHandle)handle);
            }
            return null;
        }

        private MutableFieldReference ResolveFieldToken(int token)
        {
            var handle = MetadataTokens.EntityHandle(token);
            if (handle.Kind == HandleKind.FieldDefinition)
            {
                var fieldHandle = (FieldDefinitionHandle)handle;
                if (_fieldDefCache.TryGetValue(fieldHandle, out var field))
                    return field;
            }
            else if (handle.Kind == HandleKind.MemberReference)
            {
                return ResolveMemberReference((MemberReferenceHandle)handle) as MutableFieldReference;
            }
            return null;
        }

        private MutableTypeReference ResolveTypeToken(int token)
        {
            var handle = MetadataTokens.EntityHandle(token);
            return ReadTypeReference(handle);
        }

        private object ResolveToken(int token)
        {
            var handle = MetadataTokens.EntityHandle(token);
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:
                    return ReadTypeReference(handle);
                case HandleKind.MethodDefinition:
                case HandleKind.MemberReference:
                    return ResolveMethodToken(token);
                case HandleKind.MethodSpecification:
                    return ResolveMethodToken(token);
                case HandleKind.FieldDefinition:
                    return ResolveFieldToken(token);
                default:
                    return null;
            }
        }

        private MutableMethodReference ResolveMethodSpecification(MethodSpecificationHandle handle)
        {
            if (_methodSpecCache.TryGetValue(handle, out var cached))
                return cached;

            var spec = _metadataReader.GetMethodSpecification(handle);
            var elementMethod = ResolveMethodHandle(spec.Method);
            if (elementMethod == null)
                return null;

            var genericArgs = spec.DecodeSignature(
                new TypeProvider(_module, _metadataReader, _asmRefCache),
                new GenericContext(elementMethod.DeclaringType, elementMethod));
            var instance = new MutableGenericInstanceMethod(elementMethod);
            foreach (var arg in genericArgs)
            {
                instance.GenericArguments.Add(arg);
            }

            _methodSpecCache[handle] = instance;
            return instance;
        }

        private MutableMethodReference ResolveMethodHandle(EntityHandle handle)
        {
            if (handle.Kind == HandleKind.MethodDefinition)
            {
                if (_methodDefCache.TryGetValue((MethodDefinitionHandle)handle, out var method))
                    return method;
            }
            else if (handle.Kind == HandleKind.MemberReference)
            {
                return ResolveMemberReference((MemberReferenceHandle)handle) as MutableMethodReference;
            }

            return null;
        }

        private object ResolveMemberReference(MemberReferenceHandle handle)
        {
            if (_memberRefCache.TryGetValue(handle, out var cached))
                return cached;

            var memberRef = _metadataReader.GetMemberReference(handle);
            var name = _metadataReader.GetString(memberRef.Name);
            var declaringType = ReadTypeReference(memberRef.Parent);

            object result;
            if (memberRef.GetKind() == MemberReferenceKind.Method)
            {
                var method = new MutableMethodReference(name, null, declaringType);
                var sig = memberRef.DecodeMethodSignature(
                    new TypeProvider(_module, _metadataReader, _asmRefCache),
                    new GenericContext(declaringType, method));

                method.ReturnType = sig.ReturnType;
                method.HasThis = sig.Header.IsInstance;
                method.Parameters.Clear();
                method.GenericParameters.Clear();

                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                {
                    method.Parameters.Add(new MutableParameterDefinition($"param{i}", ParameterAttributes.None, sig.ParameterTypes[i])
                    {
                        Index = i
                    });
                }
                for (int i = 0; i < sig.GenericParameterCount; i++)
                {
                    method.GenericParameters.Add(new MutableGenericParameter($"T{i}", method)
                    {
                        Position = i
                    });
                }
                result = method;
            }
            else
            {
                var fieldType = memberRef.DecodeFieldSignature(new TypeProvider(_module, _metadataReader, _asmRefCache), null);
                result = new MutableFieldReference(name, fieldType, declaringType);
            }

            _memberRefCache[handle] = result;
            return result;
        }

        private MutableInstruction GetInstructionAtOffset(MutableMethodBody body, int offset)
        {
            foreach (var instruction in body.Instructions)
            {
                if (instruction.Offset == offset)
                    return instruction;
            }
            return null;
        }

        private MutablePropertyDefinition ReadPropertyDefinition(PropertyDefinitionHandle handle, MutableTypeDefinition declaringType)
        {
            var propDef = _metadataReader.GetPropertyDefinition(handle);
            var name = _metadataReader.GetString(propDef.Name);
            var sig = propDef.DecodeSignature(new TypeProvider(_module, _metadataReader, _asmRefCache), null);

            var prop = new MutablePropertyDefinition(name, (PropertyAttributes)0, sig.ReturnType)
            {
                DeclaringType = declaringType,
                MetadataToken = MetadataTokens.GetToken(handle)
            };

            var accessors = propDef.GetAccessors();
            if (!accessors.Getter.IsNil && _methodDefCache.TryGetValue(accessors.Getter, out var getter))
            {
                prop.GetMethod = getter;
                getter.SemanticsAttributes |= MutableMethodSemanticsAttributes.Getter;
            }
            if (!accessors.Setter.IsNil && _methodDefCache.TryGetValue(accessors.Setter, out var setter))
            {
                prop.SetMethod = setter;
                setter.SemanticsAttributes |= MutableMethodSemanticsAttributes.Setter;
            }

            _propertyDefCache[handle] = prop;
            return prop;
        }

        private MutableEventDefinition ReadEventDefinition(EventDefinitionHandle handle, MutableTypeDefinition declaringType)
        {
            var evtDef = _metadataReader.GetEventDefinition(handle);
            var name = _metadataReader.GetString(evtDef.Name);
            var eventType = ReadTypeReference(evtDef.Type);

            var evt = new MutableEventDefinition(name, (EventAttributes)0, eventType)
            {
                DeclaringType = declaringType,
                MetadataToken = MetadataTokens.GetToken(handle)
            };

            var accessors = evtDef.GetAccessors();
            if (!accessors.Adder.IsNil && _methodDefCache.TryGetValue(accessors.Adder, out var adder))
            {
                evt.AddMethod = adder;
                adder.SemanticsAttributes |= MutableMethodSemanticsAttributes.AddOn;
            }
            if (!accessors.Remover.IsNil && _methodDefCache.TryGetValue(accessors.Remover, out var remover))
            {
                evt.RemoveMethod = remover;
                remover.SemanticsAttributes |= MutableMethodSemanticsAttributes.RemoveOn;
            }
            if (!accessors.Raiser.IsNil && _methodDefCache.TryGetValue(accessors.Raiser, out var raiser))
            {
                evt.InvokeMethod = raiser;
                raiser.SemanticsAttributes |= MutableMethodSemanticsAttributes.Fire;
            }

            _eventDefCache[handle] = evt;
            return evt;
        }

        private MutableGenericParameter ReadGenericParameter(GenericParameterHandle handle, object owner)
        {
            var gpDef = _metadataReader.GetGenericParameter(handle);
            var name = _metadataReader.GetString(gpDef.Name);

            var gp = new MutableGenericParameter(name, owner)
            {
                Position = gpDef.Index,
                GenericParameterAttributes = (GenericParameterAttributes)gpDef.Attributes
            };

            _genericParameterCache[handle] = gp;

            foreach (var constraintHandle in gpDef.GetConstraints())
            {
                var constraint = _metadataReader.GetGenericParameterConstraint(constraintHandle);
                var constraintType = ReadTypeReference(constraint.Type);
                if (constraintType != null)
                {
                    gp.Constraints.Add(new MutableGenericParameterConstraint(constraintType));
                }
            }

            return gp;
        }

        private MutableTypeReference ReadTypeReference(EntityHandle handle)
        {
            if (handle.IsNil)
                return null;

            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    var typeDefHandle = (TypeDefinitionHandle)handle;
                    return _typeDefCache.TryGetValue(typeDefHandle, out var typeDef) ? typeDef : null;

                case HandleKind.TypeReference:
                    var typeRefHandle = (TypeReferenceHandle)handle;
                    if (_typeRefCache.TryGetValue(typeRefHandle, out var cached))
                        return cached;

                    var typeRef = _metadataReader.GetTypeReference(typeRefHandle);
                    var ns = _metadataReader.GetString(typeRef.Namespace);
                    var name = _metadataReader.GetString(typeRef.Name);
                    var reference = new MutableTypeReference(ns, name, _module);

                    // Resolve scope
                    if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
                    {
                        var asmRefHandle = (AssemblyReferenceHandle)typeRef.ResolutionScope;
                        if (_asmRefCache.TryGetValue(asmRefHandle, out var asmRef))
                        {
                            reference.Scope = asmRef;
                        }
                    }

                    _typeRefCache[typeRefHandle] = reference;
                    return reference;

                case HandleKind.TypeSpecification:
                    var typeSpecHandle = (TypeSpecificationHandle)handle;
                    var typeSpec = _metadataReader.GetTypeSpecification(typeSpecHandle);
                    return typeSpec.DecodeSignature(new TypeProvider(_module, _metadataReader, _asmRefCache), null);

                default:
                    return null;
            }
        }

        private bool IsValueType(TypeDefinition typeDef)
        {
            if (typeDef.BaseType.IsNil)
                return false;

            var baseType = ReadTypeReference(typeDef.BaseType);
            if (baseType == null)
                return false;

            return baseType.FullName == "System.ValueType" || baseType.FullName == "System.Enum";
        }

        private bool IsEnum(TypeDefinition typeDef)
        {
            if (typeDef.BaseType.IsNil)
                return false;

            var baseType = ReadTypeReference(typeDef.BaseType);
            return baseType?.FullName == "System.Enum";
        }

        private object ReadConstant(Constant constant)
        {
            var reader = _metadataReader.GetBlobReader(constant.Value);
            
            switch (constant.TypeCode)
            {
                case ConstantTypeCode.Boolean: return reader.ReadBoolean();
                case ConstantTypeCode.Char: return reader.ReadChar();
                case ConstantTypeCode.SByte: return reader.ReadSByte();
                case ConstantTypeCode.Byte: return reader.ReadByte();
                case ConstantTypeCode.Int16: return reader.ReadInt16();
                case ConstantTypeCode.UInt16: return reader.ReadUInt16();
                case ConstantTypeCode.Int32: return reader.ReadInt32();
                case ConstantTypeCode.UInt32: return reader.ReadUInt32();
                case ConstantTypeCode.Int64: return reader.ReadInt64();
                case ConstantTypeCode.UInt64: return reader.ReadUInt64();
                case ConstantTypeCode.Single: return reader.ReadSingle();
                case ConstantTypeCode.Double: return reader.ReadDouble();
                case ConstantTypeCode.String: return reader.ReadUTF16(reader.Length);
                case ConstantTypeCode.NullReference: return null;
                default: return null;
            }
        }

        private void ReadCustomAttributes()
        {
            var provider = new MutableAttributeTypeProvider(this);
            foreach (var handle in _metadataReader.CustomAttributes)
            {
                try
                {
                    var ca = _metadataReader.GetCustomAttribute(handle);
                    var attr = CreateCustomAttribute(ca, provider);
                    if (attr == null)
                        continue;

                    AddCustomAttributeToParent(ca.Parent, attr);
                }
                catch
                {
                    // Skip attributes that fail to decode
                }
            }
        }

        private MutableCustomAttribute CreateCustomAttribute(CustomAttribute attribute, MutableAttributeTypeProvider provider)
        {
            var ctor = ResolveAttributeConstructor(attribute.Constructor, out var attrType);
            if (ctor == null || attrType == null)
                return null;

            var result = new MutableCustomAttribute(ctor);

            try
            {
                var value = attribute.DecodeValue(provider);
                foreach (var arg in value.FixedArguments)
                {
                    result.ConstructorArguments.Add(ConvertTypedArgument(arg));
                }

                foreach (var named in value.NamedArguments)
                {
                    var converted = ConvertTypedArgument(named.Value);
                    var namedArg = new MutableCustomAttributeNamedArgument(named.Name, converted,
                        named.Kind == CustomAttributeNamedArgumentKind.Field);
                    if (named.Kind == CustomAttributeNamedArgumentKind.Field)
                        result.Fields.Add(namedArg);
                    else
                        result.Properties.Add(namedArg);
                }
            }
            catch
            {
                // Best-effort decoding only
            }

            return result;
        }

        private void AddCustomAttributeToParent(EntityHandle parent, MutableCustomAttribute attr)
        {
            switch (parent.Kind)
            {
                case HandleKind.AssemblyDefinition:
                    _assembly.CustomAttributes.Add(attr);
                    break;
                case HandleKind.ModuleDefinition:
                    _module.CustomAttributes.Add(attr);
                    if (attr.AttributeTypeName == typeof(ObfuscateAssemblyAttribute).FullName)
                    {
                        bool exists = false;
                        for (int i = 0; i < _assembly.CustomAttributes.Count; i++)
                        {
                            if (_assembly.CustomAttributes[i].AttributeTypeName == attr.AttributeTypeName)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                            _assembly.CustomAttributes.Add(attr);
                    }
                    break;
                case HandleKind.TypeDefinition:
                    if (_typeDefCache.TryGetValue((TypeDefinitionHandle)parent, out var type))
                        type.CustomAttributes.Add(attr);
                    break;
                case HandleKind.MethodDefinition:
                    if (_methodDefCache.TryGetValue((MethodDefinitionHandle)parent, out var method))
                        method.CustomAttributes.Add(attr);
                    break;
                case HandleKind.FieldDefinition:
                    if (_fieldDefCache.TryGetValue((FieldDefinitionHandle)parent, out var field))
                        field.CustomAttributes.Add(attr);
                    break;
                case HandleKind.PropertyDefinition:
                    if (_propertyDefCache.TryGetValue((PropertyDefinitionHandle)parent, out var prop))
                        prop.CustomAttributes.Add(attr);
                    break;
                case HandleKind.EventDefinition:
                    if (_eventDefCache.TryGetValue((EventDefinitionHandle)parent, out var evt))
                        evt.CustomAttributes.Add(attr);
                    break;
                case HandleKind.Parameter:
                    if (_parameterDefCache.TryGetValue((ParameterHandle)parent, out var param))
                        param.CustomAttributes.Add(attr);
                    break;
                case HandleKind.GenericParameter:
                    if (_genericParameterCache.TryGetValue((GenericParameterHandle)parent, out var genericParam))
                        genericParam.CustomAttributes.Add(attr);
                    break;
            }
        }

        private MutableMethodReference ResolveAttributeConstructor(EntityHandle constructor, out MutableTypeReference attributeType)
        {
            attributeType = null;
            switch (constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                {
                    var handle = (MethodDefinitionHandle)constructor;
                    if (_methodDefCache.TryGetValue(handle, out var method))
                    {
                        attributeType = method.DeclaringType;
                        return method;
                    }

                    var def = _metadataReader.GetMethodDefinition(handle);
                    attributeType = ReadTypeReference(def.GetDeclaringType());
                    var sig = def.DecodeSignature(new TypeProvider(_module, _metadataReader, _asmRefCache), null);
                    return CreateMethodReference(_metadataReader.GetString(def.Name), sig, attributeType);
                }
                case HandleKind.MemberReference:
                {
                    var memberRef = _metadataReader.GetMemberReference((MemberReferenceHandle)constructor);
                    attributeType = ReadTypeReference(memberRef.Parent);
                    var sig = memberRef.DecodeMethodSignature(new TypeProvider(_module, _metadataReader, _asmRefCache), null);
                    return CreateMethodReference(_metadataReader.GetString(memberRef.Name), sig, attributeType);
                }
                case HandleKind.MethodSpecification:
                {
                    var spec = _metadataReader.GetMethodSpecification((MethodSpecificationHandle)constructor);
                    return ResolveAttributeConstructor(spec.Method, out attributeType);
                }
                default:
                    return null;
            }
        }

        private static MutableMethodReference CreateMethodReference(string name, MethodSignature<MutableTypeReference> sig, MutableTypeReference declaringType)
        {
            if (declaringType == null)
                return null;

            var method = new MutableMethodReference(name, sig.ReturnType, declaringType)
            {
                HasThis = sig.Header.IsInstance
            };

            for (int i = 0; i < sig.ParameterTypes.Length; i++)
            {
                method.Parameters.Add(new MutableParameterDefinition($"param{i}", ParameterAttributes.None, sig.ParameterTypes[i])
                {
                    Index = i
                });
            }

            return method;
        }

        private static MutableCustomAttributeArgument ConvertTypedArgument(CustomAttributeTypedArgument<MutableTypeReference> arg)
        {
            return new MutableCustomAttributeArgument(arg.Type, ConvertTypedValue(arg.Value));
        }

        private static MutableCustomAttributeArgument ConvertTypedArgument(object arg)
        {
            if (arg is CustomAttributeTypedArgument<MutableTypeReference> typed)
                return ConvertTypedArgument(typed);

            if (arg is CustomAttributeTypedArgument<object> typedObj)
                return new MutableCustomAttributeArgument(typedObj.Type as MutableTypeReference, ConvertTypedValue(typedObj.Value));

            if (arg != null)
            {
                var argType = arg.GetType();
                var typeProp = argType.GetProperty("Type");
                var valueProp = argType.GetProperty("Value");
                if (typeProp != null && valueProp != null)
                {
                    var typeValue = typeProp.GetValue(arg) as MutableTypeReference;
                    var value = valueProp.GetValue(arg);
                    return new MutableCustomAttributeArgument(typeValue, ConvertTypedValue(value));
                }
            }

            return new MutableCustomAttributeArgument(null, arg);
        }

        private static object ConvertTypedValue(object value)
        {
            if (value is ImmutableArray<CustomAttributeTypedArgument<MutableTypeReference>> array)
            {
                var list = new List<MutableCustomAttributeArgument>(array.Length);
                foreach (var item in array)
                {
                    list.Add(ConvertTypedArgument(item));
                }
                return list;
            }

            if (value is ImmutableArray<object> objArray)
            {
                var list = new List<MutableCustomAttributeArgument>(objArray.Length);
                foreach (var item in objArray)
                {
                    list.Add(ConvertTypedArgument(item));
                }
                return list;
            }

            return value;
        }

        private sealed class MutableAttributeTypeProvider : ICustomAttributeTypeProvider<MutableTypeReference>
        {
            private readonly MutableAssemblyReader _reader;

            public MutableAttributeTypeProvider(MutableAssemblyReader reader)
            {
                _reader = reader;
            }

            public MutableTypeReference GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                var typeSystem = _reader._module.TypeSystem;
                if (typeSystem == null)
                {
                    return typeCode switch
                    {
                        PrimitiveTypeCode.Void => new MutableTypeReference("System", "Void", _reader._module),
                        PrimitiveTypeCode.Boolean => new MutableTypeReference("System", "Boolean", _reader._module),
                        PrimitiveTypeCode.Char => new MutableTypeReference("System", "Char", _reader._module),
                        PrimitiveTypeCode.SByte => new MutableTypeReference("System", "SByte", _reader._module),
                        PrimitiveTypeCode.Byte => new MutableTypeReference("System", "Byte", _reader._module),
                        PrimitiveTypeCode.Int16 => new MutableTypeReference("System", "Int16", _reader._module),
                        PrimitiveTypeCode.UInt16 => new MutableTypeReference("System", "UInt16", _reader._module),
                        PrimitiveTypeCode.Int32 => new MutableTypeReference("System", "Int32", _reader._module),
                        PrimitiveTypeCode.UInt32 => new MutableTypeReference("System", "UInt32", _reader._module),
                        PrimitiveTypeCode.Int64 => new MutableTypeReference("System", "Int64", _reader._module),
                        PrimitiveTypeCode.UInt64 => new MutableTypeReference("System", "UInt64", _reader._module),
                        PrimitiveTypeCode.Single => new MutableTypeReference("System", "Single", _reader._module),
                        PrimitiveTypeCode.Double => new MutableTypeReference("System", "Double", _reader._module),
                        PrimitiveTypeCode.IntPtr => new MutableTypeReference("System", "IntPtr", _reader._module),
                        PrimitiveTypeCode.UIntPtr => new MutableTypeReference("System", "UIntPtr", _reader._module),
                        PrimitiveTypeCode.String => new MutableTypeReference("System", "String", _reader._module),
                        PrimitiveTypeCode.Object => new MutableTypeReference("System", "Object", _reader._module),
                        PrimitiveTypeCode.TypedReference => new MutableTypeReference("System", "TypedReference", _reader._module),
                        _ => new MutableTypeReference("System", "Object", _reader._module)
                    };
                }

                return typeCode switch
                {
                    PrimitiveTypeCode.Void => typeSystem.Void,
                    PrimitiveTypeCode.Boolean => typeSystem.Boolean,
                    PrimitiveTypeCode.Char => typeSystem.Char,
                    PrimitiveTypeCode.SByte => typeSystem.SByte,
                    PrimitiveTypeCode.Byte => typeSystem.Byte,
                    PrimitiveTypeCode.Int16 => typeSystem.Int16,
                    PrimitiveTypeCode.UInt16 => typeSystem.UInt16,
                    PrimitiveTypeCode.Int32 => typeSystem.Int32,
                    PrimitiveTypeCode.UInt32 => typeSystem.UInt32,
                    PrimitiveTypeCode.Int64 => typeSystem.Int64,
                    PrimitiveTypeCode.UInt64 => typeSystem.UInt64,
                    PrimitiveTypeCode.Single => typeSystem.Single,
                    PrimitiveTypeCode.Double => typeSystem.Double,
                    PrimitiveTypeCode.IntPtr => typeSystem.IntPtr,
                    PrimitiveTypeCode.UIntPtr => typeSystem.UIntPtr,
                    PrimitiveTypeCode.String => typeSystem.String,
                    PrimitiveTypeCode.Object => typeSystem.Object,
                    PrimitiveTypeCode.TypedReference => typeSystem.TypedReference,
                    _ => typeSystem.Object
                };
            }

            public MutableTypeReference GetSystemType()
            {
                var typeSystem = _reader._module.TypeSystem;
                if (typeSystem != null)
                    return typeSystem.Import(typeof(Type));

                return new MutableTypeReference("System", "Type", _reader._module);
            }

            public MutableTypeReference GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return _reader.ReadTypeReference(handle);
            }

            public MutableTypeReference GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return _reader.ReadTypeReference(handle);
            }

            public MutableTypeReference GetSZArrayType(MutableTypeReference elementType)
            {
                return new MutableArrayType(elementType);
            }

            public MutableTypeReference GetTypeFromSerializedName(string name)
            {
                return _reader.ParseSerializedTypeName(name);
            }

            public PrimitiveTypeCode GetUnderlyingEnumType(MutableTypeReference type)
            {
                return PrimitiveTypeCode.Int32;
            }

            public bool IsSystemType(MutableTypeReference type)
            {
                return type?.FullName == "System.Type";
            }

            public MutableTypeReference GetEnumType(MutableTypeReference underlyingType, MutableTypeReference type)
            {
                return type;
            }

            public MutableTypeReference GetGenericInstantiation(MutableTypeReference genericType, ImmutableArray<MutableTypeReference> typeArguments)
            {
                if (typeArguments.IsDefaultOrEmpty)
                    return genericType;

                var instance = new MutableGenericInstanceType(genericType);
                foreach (var arg in typeArguments)
                {
                    instance.GenericArguments.Add(arg);
                }
                return instance;
            }
        }

        private MutableTypeReference ParseSerializedTypeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var typeName = name;
            var commaIndex = typeName.IndexOf(',');
            if (commaIndex >= 0)
                typeName = typeName.Substring(0, commaIndex);

            if (typeName.EndsWith("[]", StringComparison.Ordinal))
            {
                var element = ParseSerializedTypeName(typeName.Substring(0, typeName.Length - 2));
                return element == null ? null : new MutableArrayType(element);
            }

            if (typeName.EndsWith("&", StringComparison.Ordinal))
            {
                var element = ParseSerializedTypeName(typeName.Substring(0, typeName.Length - 1));
                return element == null ? null : new MutableByReferenceType(element);
            }

            if (typeName.EndsWith("*", StringComparison.Ordinal))
            {
                var element = ParseSerializedTypeName(typeName.Substring(0, typeName.Length - 1));
                return element == null ? null : new MutablePointerType(element);
            }

            var nestedParts = typeName.Split(new[] { '+', '/' }, StringSplitOptions.None);
            var rootPart = nestedParts[0];
            var lastDot = rootPart.LastIndexOf('.');
            var ns = lastDot >= 0 ? rootPart.Substring(0, lastDot) : string.Empty;
            var rootName = lastDot >= 0 ? rootPart.Substring(lastDot + 1) : rootPart;

            MutableTypeReference current = new MutableTypeReference(ns, rootName, _module);
            for (int i = 1; i < nestedParts.Length; i++)
            {
                var nested = new MutableTypeReference(string.Empty, nestedParts[i], _module)
                {
                    DeclaringType = current
                };
                current = nested;
            }

            return current;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _peReader?.Dispose();
        }
    }

    /// <summary>
    /// Opcode lookup table optimized for fast single-byte opcode lookup.
    /// </summary>
    internal static class MutableOpCodeLookup
    {
        // Single-byte opcodes (0x00-0xFF) - direct array indexing for O(1) lookup
        private static readonly MutableOpCode[] _singleByteOpCodes = new MutableOpCode[256];
        
        // Two-byte opcodes (0xFExx) - dictionary for less frequent lookups
        private static readonly Dictionary<byte, MutableOpCode> _twoByteOpCodes = new Dictionary<byte, MutableOpCode>();

        static MutableOpCodeLookup()
        {
            // Register all opcodes
            var fields = typeof(MutableOpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(MutableOpCode))
                {
                    var opCode = (MutableOpCode)field.GetValue(null);
                    short value = opCode.Value;
                    
                    if ((value & 0xFF00) == 0xFE00)
                    {
                        // Two-byte opcode (0xFE prefix)
                        _twoByteOpCodes[(byte)(value & 0xFF)] = opCode;
                    }
                    else if (value >= 0 && value <= 255)
                    {
                        // Single-byte opcode
                        _singleByteOpCodes[value] = opCode;
                    }
                }
            }
        }

        /// <summary>
        /// Fast lookup for single-byte opcodes (0x00-0xFF).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MutableOpCode GetSingleByteOpCode(byte value)
        {
            var opCode = _singleByteOpCodes[value];
            return opCode.Name != null ? opCode : MutableOpCodes.Nop;
        }

        /// <summary>
        /// Lookup for two-byte opcodes (0xFExx).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MutableOpCode GetTwoByteOpCode(byte secondByte)
        {
            return _twoByteOpCodes.TryGetValue(secondByte, out var opCode) ? opCode : MutableOpCodes.Nop;
        }

        /// <summary>
        /// General lookup (for compatibility).
        /// </summary>
        public static MutableOpCode GetOpCode(short value)
        {
            if ((value & 0xFF00) == 0xFE00)
                return GetTwoByteOpCode((byte)(value & 0xFF));
            
            if (value >= 0 && value <= 255)
                return GetSingleByteOpCode((byte)value);
            
            return MutableOpCodes.Nop; // Fallback
        }
    }

    internal readonly struct GenericContext
    {
        public GenericContext(object typeOwner, object methodOwner)
        {
            TypeOwner = typeOwner;
            MethodOwner = methodOwner;
        }

        public object TypeOwner { get; }
        public object MethodOwner { get; }
    }

    /// <summary>
    /// Type provider for decoding signatures.
    /// </summary>
    internal class TypeProvider : ISignatureTypeProvider<MutableTypeReference, object>
    {
        private readonly MutableModuleDefinition _module;
        private readonly MetadataReader _reader;
        private readonly Dictionary<AssemblyReferenceHandle, MutableAssemblyNameReference> _asmRefCache;

        public TypeProvider(MutableModuleDefinition module, MetadataReader reader, 
            Dictionary<AssemblyReferenceHandle, MutableAssemblyNameReference> asmRefCache)
        {
            _module = module;
            _reader = reader;
            _asmRefCache = asmRefCache;
        }

        public MutableTypeReference GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            if (_module.TypeSystem == null)
            {
                // TypeSystem not initialized yet - create temporary reference
                var (ns, name) = GetPrimitiveTypeName(typeCode);
                return new MutableTypeReference(ns, name, _module) { IsValueType = typeCode != PrimitiveTypeCode.String && typeCode != PrimitiveTypeCode.Object };
            }

            switch (typeCode)
            {
                case PrimitiveTypeCode.Void: return _module.TypeSystem.Void;
                case PrimitiveTypeCode.Boolean: return _module.TypeSystem.Boolean;
                case PrimitiveTypeCode.Char: return _module.TypeSystem.Char;
                case PrimitiveTypeCode.SByte: return _module.TypeSystem.SByte;
                case PrimitiveTypeCode.Byte: return _module.TypeSystem.Byte;
                case PrimitiveTypeCode.Int16: return _module.TypeSystem.Int16;
                case PrimitiveTypeCode.UInt16: return _module.TypeSystem.UInt16;
                case PrimitiveTypeCode.Int32: return _module.TypeSystem.Int32;
                case PrimitiveTypeCode.UInt32: return _module.TypeSystem.UInt32;
                case PrimitiveTypeCode.Int64: return _module.TypeSystem.Int64;
                case PrimitiveTypeCode.UInt64: return _module.TypeSystem.UInt64;
                case PrimitiveTypeCode.Single: return _module.TypeSystem.Single;
                case PrimitiveTypeCode.Double: return _module.TypeSystem.Double;
                case PrimitiveTypeCode.IntPtr: return _module.TypeSystem.IntPtr;
                case PrimitiveTypeCode.UIntPtr: return _module.TypeSystem.UIntPtr;
                case PrimitiveTypeCode.String: return _module.TypeSystem.String;
                case PrimitiveTypeCode.Object: return _module.TypeSystem.Object;
                case PrimitiveTypeCode.TypedReference: return _module.TypeSystem.TypedReference;
                default:
                    var (ns2, name2) = GetPrimitiveTypeName(typeCode);
                    return new MutableTypeReference(ns2, name2, _module);
            }
        }

        private (string ns, string name) GetPrimitiveTypeName(PrimitiveTypeCode typeCode)
        {
            return typeCode switch
            {
                PrimitiveTypeCode.Void => ("System", "Void"),
                PrimitiveTypeCode.Boolean => ("System", "Boolean"),
                PrimitiveTypeCode.Char => ("System", "Char"),
                PrimitiveTypeCode.SByte => ("System", "SByte"),
                PrimitiveTypeCode.Byte => ("System", "Byte"),
                PrimitiveTypeCode.Int16 => ("System", "Int16"),
                PrimitiveTypeCode.UInt16 => ("System", "UInt16"),
                PrimitiveTypeCode.Int32 => ("System", "Int32"),
                PrimitiveTypeCode.UInt32 => ("System", "UInt32"),
                PrimitiveTypeCode.Int64 => ("System", "Int64"),
                PrimitiveTypeCode.UInt64 => ("System", "UInt64"),
                PrimitiveTypeCode.Single => ("System", "Single"),
                PrimitiveTypeCode.Double => ("System", "Double"),
                PrimitiveTypeCode.IntPtr => ("System", "IntPtr"),
                PrimitiveTypeCode.UIntPtr => ("System", "UIntPtr"),
                PrimitiveTypeCode.String => ("System", "String"),
                PrimitiveTypeCode.Object => ("System", "Object"),
                PrimitiveTypeCode.TypedReference => ("System", "TypedReference"),
                _ => ("System", "Object")
            };
        }

        public MutableTypeReference GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var typeDef = reader.GetTypeDefinition(handle);
            var ns = reader.GetString(typeDef.Namespace);
            var name = reader.GetString(typeDef.Name);
            return new MutableTypeReference(ns, name, _module)
            {
                IsValueType = rawTypeKind == 0x11
            };
        }

        public MutableTypeReference GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var typeRef = reader.GetTypeReference(handle);
            var ns = reader.GetString(typeRef.Namespace);
            var name = reader.GetString(typeRef.Name);
            var result = new MutableTypeReference(ns, name, _module)
            {
                IsValueType = rawTypeKind == 0x11
            };

            if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                var asmRefHandle = (AssemblyReferenceHandle)typeRef.ResolutionScope;
                if (_asmRefCache.TryGetValue(asmRefHandle, out var asmRef))
                {
                    result.Scope = asmRef;
                }
            }

            return result;
        }

        public MutableTypeReference GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            var typeSpec = reader.GetTypeSpecification(handle);
            return typeSpec.DecodeSignature(this, genericContext);
        }

        public MutableTypeReference GetSZArrayType(MutableTypeReference elementType) => new MutableArrayType(elementType);
        public MutableTypeReference GetArrayType(MutableTypeReference elementType, ArrayShape shape) => new MutableArrayType(elementType, shape.Rank);
        public MutableTypeReference GetByReferenceType(MutableTypeReference elementType) => new MutableByReferenceType(elementType);
        public MutableTypeReference GetPointerType(MutableTypeReference elementType) => new MutablePointerType(elementType);
        
        public MutableTypeReference GetGenericInstantiation(MutableTypeReference genericType, System.Collections.Immutable.ImmutableArray<MutableTypeReference> typeArguments)
        {
            var instance = new MutableGenericInstanceType(genericType);
            instance.GenericArguments.AddRange(typeArguments);
            return instance;
        }

        public MutableTypeReference GetGenericTypeParameter(object genericContext, int index)
        {
            object owner = genericContext is GenericContext ctx ? ctx.TypeOwner : genericContext;
            return new MutableGenericParameter($"T{index}", owner) { Position = index };
        }

        public MutableTypeReference GetGenericMethodParameter(object genericContext, int index)
        {
            object owner = genericContext is GenericContext ctx ? ctx.MethodOwner : genericContext;
            return new MutableGenericParameter($"M{index}", owner)
            {
                Position = index,
                IsMethodParameter = true
            };
        }

        public MutableTypeReference GetFunctionPointerType(MethodSignature<MutableTypeReference> signature)
        {
            return new MutableTypeReference("", "FnPtr", _module);
        }

        public MutableTypeReference GetModifiedType(MutableTypeReference modifier, MutableTypeReference unmodifiedType, bool isRequired)
        {
            return new MutableModifiedType(modifier, unmodifiedType, isRequired);
        }

        public MutableTypeReference GetPinnedType(MutableTypeReference elementType)
        {
            return elementType;
        }
    }
}
