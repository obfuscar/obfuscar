using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Writes mutable assemblies to PE files using System.Reflection.Metadata.
    /// This replaces the need for legacy Cecil AssemblyDefinition.Write().
    /// </summary>
    /// <remarks>
    /// <para>
    /// The writer translates the mutable object model to SRM's MetadataBuilder
    /// and uses PEBuilder to produce the final PE file. This is a complete
    /// Re-implementation of assembly writing without any Cecil dependency.
    /// </para>
    /// <para>
    /// The writing process:
    /// <list type="number">
    /// <item>Build metadata tables (types, methods, fields, etc.)</item>
    /// <item>Encode all IL method bodies</item>
    /// <item>Encode all resources</item>
    /// <item>Create PE headers and sections</item>
    /// <item>Write to output stream</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MutableAssemblyWriter
    {
        private readonly MutableAssemblyDefinition _assembly;
        private readonly MutableWriterParameters _parameters;
        
        private MetadataBuilder _metadata;
        private BlobBuilder _ilStream;
        private MethodBodyStreamEncoder _methodBodyEncoder;
        private AssemblyDefinitionHandle _assemblyDefHandle;
        private ModuleDefinitionHandle _moduleDefHandle;
        
        // Handle mappings for cross-references
        private Dictionary<MutableTypeDefinition, TypeDefinitionHandle> _typeDefHandles;
        private Dictionary<MutableMethodDefinition, MethodDefinitionHandle> _methodDefHandles;
        private Dictionary<MutableFieldDefinition, FieldDefinitionHandle> _fieldDefHandles;
        private Dictionary<MutablePropertyDefinition, PropertyDefinitionHandle> _propertyDefHandles;
        private Dictionary<MutableEventDefinition, EventDefinitionHandle> _eventDefHandles;
        private Dictionary<MutableParameterDefinition, ParameterHandle> _parameterHandles;
        private Dictionary<MutableGenericParameter, GenericParameterHandle> _genericParameterHandles;
        private Dictionary<MutableTypeReference, EntityHandle> _typeRefHandles;
        private Dictionary<MutableMethodReference, EntityHandle> _methodRefHandles;
        private Dictionary<MutableFieldReference, EntityHandle> _fieldRefHandles;
        private Dictionary<MutableAssemblyNameReference, AssemblyReferenceHandle> _asmRefHandles;
        private Dictionary<string, UserStringHandle> _userStringHandles;
        private List<MutableTypeDefinition> _orderedTypes;
        private Dictionary<MutableMethodDefinition, int> _methodBodyOffsets;

        /// <summary>
        /// Initializes a new writer for the specified assembly.
        /// </summary>
        public MutableAssemblyWriter(MutableAssemblyDefinition assembly, MutableWriterParameters parameters = null)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _parameters = parameters ?? new MutableWriterParameters();
        }

        /// <summary>
        /// Writes the assembly to the specified file path.
        /// </summary>
        public void Write(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            Write(stream);
        }

        /// <summary>
        /// Writes the assembly to the specified stream.
        /// </summary>
        public void Write(Stream stream)
        {
            InitializeBuilders();
            
            var module = _assembly.MainModule;
            
            // Create assembly definition
            if (_assembly.Name != null)
            {
                WriteAssemblyDefinition();
            }

            // Create module definition
            WriteModuleDefinition();
            
            // Write assembly references
            WriteAssemblyReferences();
            
            // First pass: create type definitions (forward declarations)
            WriteTypeDefinitionsFirstPass();

            // Precompute member handles for IL encoding.
            PrecomputeMemberHandles();

            // Encode method bodies before emitting method definitions.
            WriteMethodBodies();
            
            // Second pass: fill in type members
            WriteTypeDefinitionsSecondPass();

            // Write generic parameters after type/method definitions are available.
            WriteAllGenericParameters();

            // Custom attributes
            WriteCustomAttributes();
            
            // Create the PE file
            WritePEFile(stream);
        }

        private void InitializeBuilders()
        {
            _metadata = new MetadataBuilder();
            _ilStream = new BlobBuilder();
            _methodBodyEncoder = new MethodBodyStreamEncoder(_ilStream);
            _assemblyDefHandle = default;
            _moduleDefHandle = default;
            
            _typeDefHandles = new Dictionary<MutableTypeDefinition, TypeDefinitionHandle>();
            _methodDefHandles = new Dictionary<MutableMethodDefinition, MethodDefinitionHandle>();
            _fieldDefHandles = new Dictionary<MutableFieldDefinition, FieldDefinitionHandle>();
            _propertyDefHandles = new Dictionary<MutablePropertyDefinition, PropertyDefinitionHandle>();
            _eventDefHandles = new Dictionary<MutableEventDefinition, EventDefinitionHandle>();
            _parameterHandles = new Dictionary<MutableParameterDefinition, ParameterHandle>();
            _genericParameterHandles = new Dictionary<MutableGenericParameter, GenericParameterHandle>();
            _typeRefHandles = new Dictionary<MutableTypeReference, EntityHandle>();
            _methodRefHandles = new Dictionary<MutableMethodReference, EntityHandle>();
            _fieldRefHandles = new Dictionary<MutableFieldReference, EntityHandle>();
            _asmRefHandles = new Dictionary<MutableAssemblyNameReference, AssemblyReferenceHandle>();
            _userStringHandles = new Dictionary<string, UserStringHandle>();
            _orderedTypes = new List<MutableTypeDefinition>();
            _methodBodyOffsets = new Dictionary<MutableMethodDefinition, int>();
        }

        private void WriteAssemblyDefinition()
        {
            var name = _assembly.Name;
            _assemblyDefHandle = _metadata.AddAssembly(
                _metadata.GetOrAddString(name.Name),
                name.Version,
                _metadata.GetOrAddString(name.Culture ?? ""),
                name.PublicKey != null && name.PublicKey.Length > 0 
                    ? _metadata.GetOrAddBlob(name.PublicKey) 
                    : default,
                (AssemblyFlags)name.Attributes,
                (AssemblyHashAlgorithm)name.HashAlgorithm);
        }

        private void WriteModuleDefinition()
        {
            var module = _assembly.MainModule;
            _moduleDefHandle = _metadata.AddModule(
                0, // Generation
                _metadata.GetOrAddString(module.Name),
                _metadata.GetOrAddGuid(module.Mvid != Guid.Empty ? module.Mvid : Guid.NewGuid()),
                default, // EncId
                default); // EncBaseId
        }

        private void WriteAssemblyReferences()
        {
            var module = _assembly.MainModule;
            foreach (var asmRef in module.AssemblyReferences)
            {
                var handle = _metadata.AddAssemblyReference(
                    _metadata.GetOrAddString(asmRef.Name),
                    asmRef.Version,
                    _metadata.GetOrAddString(asmRef.Culture ?? ""),
                    asmRef.PublicKeyToken != null && asmRef.PublicKeyToken.Length > 0
                        ? _metadata.GetOrAddBlob(asmRef.PublicKeyToken)
                        : default,
                    (AssemblyFlags)asmRef.Attributes,
                    default); // HashValue
                
                _asmRefHandles[asmRef] = handle;
            }
        }

        private void WriteTypeDefinitionsFirstPass()
        {
            var module = _assembly.MainModule;
            
            // Process types in order: non-nested first, then nested
            _orderedTypes.Clear();
            CollectTypes(module.Types, _orderedTypes);

            // Ensure <Module> type (if present) is first.
            var moduleIndex = _orderedTypes.FindIndex(t => t.Name == "<Module>" && string.IsNullOrEmpty(t.Namespace));
            if (moduleIndex > 0)
            {
                var moduleType = _orderedTypes[moduleIndex];
                _orderedTypes.RemoveAt(moduleIndex);
                _orderedTypes.Insert(0, moduleType);
            }
            
            var fieldIndex = 1;
            var methodIndex = 1;
            foreach (var type in _orderedTypes)
            {
                CreateTypeDefinition(type, fieldIndex, methodIndex);
                fieldIndex += type.Fields.Count;
                methodIndex += type.Methods.Count;
            }
        }

        private void PrecomputeMemberHandles()
        {
            _fieldDefHandles.Clear();
            _methodDefHandles.Clear();

            var fieldIndex = 1;
            var methodIndex = 1;
            foreach (var type in _orderedTypes)
            {
                foreach (var field in type.Fields)
                {
                    _fieldDefHandles[field] = MetadataTokens.FieldDefinitionHandle(fieldIndex++);
                }

                foreach (var method in type.Methods)
                {
                    _methodDefHandles[method] = MetadataTokens.MethodDefinitionHandle(methodIndex++);
                }
            }
        }

        private void CollectTypes(IEnumerable<MutableTypeDefinition> types, List<MutableTypeDefinition> result)
        {
            foreach (var type in types)
            {
                result.Add(type);
                CollectTypes(type.NestedTypes, result);
            }
        }

        private void CreateTypeDefinition(MutableTypeDefinition type, int firstFieldIndex, int firstMethodIndex)
        {
            // Compute field and method list indices based on type order.
            var firstField = MetadataTokens.FieldDefinitionHandle(firstFieldIndex);
            var firstMethod = MetadataTokens.MethodDefinitionHandle(firstMethodIndex);
            
            var baseType = type.BaseType != null ? GetTypeHandle(type.BaseType) : default;
            
            var handle = _metadata.AddTypeDefinition(
                type.Attributes,
                _metadata.GetOrAddString(type.Namespace ?? ""),
                _metadata.GetOrAddString(type.Name),
                baseType,
                firstField,
                firstMethod);
            
            _typeDefHandles[type] = handle;

            if (type.PackingSize > 0 || type.ClassSize > 0)
            {
                _metadata.AddTypeLayout(handle, (ushort)type.PackingSize, (uint)type.ClassSize);
            }
            
            // Add nested type relationship
            if (type.DeclaringType is MutableTypeDefinition declaringTypeDef && _typeDefHandles.TryGetValue(declaringTypeDef, out var declaringHandle))
            {
                _metadata.AddNestedType(handle, declaringHandle);
            }
        }

        private void WriteTypeDefinitionsSecondPass()
        {
            foreach (var type in _orderedTypes)
            {
                if (!_typeDefHandles.TryGetValue(type, out var typeHandle))
                {
                    continue;
                }
                
                // Write fields
                foreach (var field in type.Fields)
                {
                    WriteFieldDefinition(field);
                }
                
                // Write methods (signatures only, bodies in third pass)
                foreach (var method in type.Methods)
                {
                    WriteMethodDefinition(method);
                }

                // Write explicit method implementations
                foreach (var impl in type.MethodImplementations)
                {
                    var bodyHandle = GetMethodHandleForImpl(impl.MethodBody);
                    var declarationHandle = GetMethodHandleForImpl(impl.MethodDeclaration);
                    if (bodyHandle.IsNil || declarationHandle.IsNil)
                        continue;

                    _metadata.AddMethodImplementation(typeHandle, bodyHandle, declarationHandle);
                }
                
                // Write interfaces
                foreach (var iface in type.Interfaces)
                {
                    var ifaceHandle = GetTypeHandle(iface.InterfaceType);
                    _metadata.AddInterfaceImplementation(typeHandle, ifaceHandle);
                }
                
                // Write properties
                if (type.Properties.Count > 0)
                {
                    _metadata.AddPropertyMap(typeHandle, 
                        MetadataTokens.PropertyDefinitionHandle(_metadata.GetRowCount(TableIndex.Property) + 1));
                    
                    foreach (var prop in type.Properties)
                    {
                        WritePropertyDefinition(prop);
                    }
                }
                
                // Write events
                if (type.Events.Count > 0)
                {
                    _metadata.AddEventMap(typeHandle,
                        MetadataTokens.EventDefinitionHandle(_metadata.GetRowCount(TableIndex.Event) + 1));
                    
                    foreach (var evt in type.Events)
                    {
                        WriteEventDefinition(evt);
                    }
                }
            }
        }

        private void WriteFieldDefinition(MutableFieldDefinition field)
        {
            var signature = EncodeFieldSignature(field.FieldType);
            
            var handle = _metadata.AddFieldDefinition(
                field.Attributes,
                _metadata.GetOrAddString(field.Name),
                _metadata.GetOrAddBlob(signature));
            
            _fieldDefHandles[field] = handle;
            
            // Write constant if present
            if (field.Constant != null)
            {
                _metadata.AddConstant(handle, field.Constant);
            }
            
            // Write initial value (RVA) if present
            if (field.InitialValue != null && field.InitialValue.Length > 0)
            {
                // RVA fields need special handling in PE builder
            }
        }

        private void WriteMethodDefinition(MutableMethodDefinition method)
        {
            var signature = EncodeMethodSignature(method);
            var bodyOffset = -1;
            if (CanWriteBody(method) && _methodBodyOffsets.TryGetValue(method, out var offset))
            {
                bodyOffset = offset;
            }
            
            // RVA derived from encoded method body (0 for abstract/external).
            var handle = _metadata.AddMethodDefinition(
                method.Attributes,
                method.ImplAttributes,
                _metadata.GetOrAddString(method.Name),
                _metadata.GetOrAddBlob(signature),
                bodyOffset,
                MetadataTokens.ParameterHandle(_metadata.GetRowCount(TableIndex.Param) + 1));
            
            _methodDefHandles[method] = handle;
            
            // Write parameters
            foreach (var param in method.Parameters)
            {
                var paramHandle = _metadata.AddParameter(
                    param.Attributes,
                    _metadata.GetOrAddString(param.Name ?? ""),
                    param.Index + 1); // Sequence number is 1-based

                _parameterHandles[param] = paramHandle;
                
                if (param.DefaultValue != null)
                {
                    _metadata.AddConstant(paramHandle, param.DefaultValue);
                }
            }
        }

        private void WriteMethodBodies()
        {
            _methodBodyOffsets.Clear();

            foreach (var kvp in _methodDefHandles)
            {
                var method = kvp.Key;
                var declaringType = method?.DeclaringType as MutableTypeDefinition;
                var isInterface = declaringType != null && declaringType.IsInterface;

                if (declaringType != null && declaringType.Namespace == "SkipVirtualMethodTest" && declaringType.Name == "Interface1")
                {
                    LogBodyIssue($"SkipVirtualMethodTest.Interface1::{method.Name} attrs={method.Attributes} isInterface={isInterface} bodyNull={method.Body == null}");
                }

                if (method?.Body != null && (method.IsAbstract || isInterface))
                {
                    LogBodyIssue($"Method has body but abstract/interface: {declaringType?.FullName}::{method.Name} attrs={method.Attributes} isInterface={isInterface} bodyInstr={method.Body.Instructions.Count}");
                }
                
                if (!CanWriteBody(method))
                    continue;

                if (method.IsAbstract || isInterface)
                {
                    LogBodyIssue($"Encoding body for abstract/interface: {declaringType?.FullName}::{method.Name} attrs={method.Attributes} isInterface={isInterface}");
                }
                
                var offset = EncodeMethodBody(method.Body);
                _methodBodyOffsets[method] = offset;
            }
        }

        private static bool CanWriteBody(MutableMethodDefinition method)
        {
            if (method == null || method.Body == null)
                return false;

            if (method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime)
                return false;

            if (method.DeclaringType is MutableTypeDefinition declaringType && declaringType.IsInterface)
                return false;

            // Only emit IL bodies for IL methods.
            if ((method.ImplAttributes & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL)
                return false;

            return true;
        }

        private static void LogBodyIssue(string message)
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("OBFUSCAR_DEBUG_BODIES"), "1", StringComparison.Ordinal))
                return;

            try
            {
                // Use the library logger instead of ad-hoc file logging.
                Obfuscar.LoggerService.Logger.LogDebug(message);
            }
            catch
            {
                // Ignore logging failures.
            }
        }

        private int EncodeMethodBody(MutableMethodBody body)
        {
            // Calculate locals signature if present
            StandaloneSignatureHandle localSig = default;
            if (body.Variables.Count > 0)
            {
                var localsEncoder = new BlobBuilder();
                var encoder = new BlobEncoder(localsEncoder);
                
                var localsBuilder = encoder.LocalVariableSignature(body.Variables.Count);
                foreach (var variable in body.Variables)
                {
                    EncodeTypeToBuilder(localsBuilder.AddVariable().Type(), variable.VariableType);
                }
                
                localSig = _metadata.AddStandaloneSignature(_metadata.GetOrAddBlob(localsEncoder));
            }
            
            // Encode IL
            var ilBytes = EncodeIL(body);
            var codeBuilder = new BlobBuilder();
            codeBuilder.WriteBytes(ilBytes);
            
            // Use MethodBodyStreamEncoder
            var bodyOffset = _methodBodyEncoder.AddMethodBody(
                new InstructionEncoder(codeBuilder, new ControlFlowBuilder()),
                body.MaxStackSize,
                localSig,
                body.InitLocals ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None);
            
            return bodyOffset;
        }

        private byte[] EncodeIL(MutableMethodBody body)
        {
            // First pass: calculate offsets
            int offset = 0;
            foreach (var instruction in body.Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
            
            // Second pass: encode
            var bytes = new List<byte>();
            
            foreach (var instruction in body.Instructions)
            {
                // Encode opcode
                if (instruction.OpCode.Size == 2)
                {
                    bytes.Add((byte)(instruction.OpCode.Value >> 8));
                    bytes.Add((byte)(instruction.OpCode.Value & 0xFF));
                }
                else
                {
                    bytes.Add((byte)instruction.OpCode.Value);
                }
                
                // Encode operand
                EncodeOperand(bytes, instruction);
            }
            
            return bytes.ToArray();
        }

        private void EncodeOperand(List<byte> bytes, MutableInstruction instruction)
        {
            switch (instruction.OpCode.OperandType)
            {
                case MutableOperandType.InlineNone:
                    break;
                    
                case MutableOperandType.ShortInlineBrTarget:
                    if (instruction.Operand is MutableInstruction target)
                    {
                        var off = target.Offset - (instruction.Offset + instruction.GetSize());
                        bytes.Add((byte)off);
                    }
                    else
                    {
                        bytes.Add(0);
                    }
                    break;
                    
                case MutableOperandType.InlineBrTarget:
                    if (instruction.Operand is MutableInstruction target2)
                    {
                        var off = target2.Offset - (instruction.Offset + instruction.GetSize());
                        WriteInt32(bytes, off);
                    }
                    else
                    {
                        WriteInt32(bytes, 0);
                    }
                    break;
                    
                case MutableOperandType.ShortInlineI:
                    if (instruction.Operand is sbyte sb)
                        bytes.Add((byte)sb);
                    else if (instruction.Operand is byte b)
                        bytes.Add(b);
                    else
                        bytes.Add(0);
                    break;
                    
                case MutableOperandType.InlineI:
                    WriteInt32(bytes, instruction.Operand is int i ? i : 0);
                    break;
                    
                case MutableOperandType.InlineI8:
                    WriteInt64(bytes, instruction.Operand is long l ? l : 0);
                    break;
                    
                case MutableOperandType.ShortInlineR:
                    var floatBytes = BitConverter.GetBytes(instruction.Operand is float f ? f : 0f);
                    bytes.AddRange(floatBytes);
                    break;
                    
                case MutableOperandType.InlineR:
                    var doubleBytes = BitConverter.GetBytes(instruction.Operand is double d ? d : 0d);
                    bytes.AddRange(doubleBytes);
                    break;
                    
                case MutableOperandType.InlineString:
                    var str = instruction.Operand as string ?? "";
                    var stringHandle = GetOrAddUserString(str);
                    WriteInt32(bytes, MetadataTokens.GetToken(stringHandle));
                    break;
                    
                case MutableOperandType.InlineMethod:
                    var methodHandle = GetMethodHandle(instruction.Operand as MutableMethodReference);
                    WriteInt32(bytes, MetadataTokens.GetToken(methodHandle));
                    break;
                    
                case MutableOperandType.InlineField:
                    var fieldHandle = GetFieldHandle(instruction.Operand as MutableFieldReference);
                    WriteInt32(bytes, MetadataTokens.GetToken(fieldHandle));
                    break;
                    
                case MutableOperandType.InlineType:
                    var typeHandle = GetTypeHandle(instruction.Operand as MutableTypeReference);
                    WriteInt32(bytes, MetadataTokens.GetToken(typeHandle));
                    break;
                    
                case MutableOperandType.InlineTok:
                    var tokenHandle = GetTokenHandle(instruction.Operand);
                    WriteInt32(bytes, MetadataTokens.GetToken(tokenHandle));
                    break;
                    
                case MutableOperandType.ShortInlineVar:
                    if (instruction.Operand is MutableVariableDefinition var1)
                        bytes.Add((byte)var1.Index);
                    else
                        bytes.Add(0);
                    break;
                    
                case MutableOperandType.InlineVar:
                    if (instruction.Operand is MutableVariableDefinition var2)
                        WriteInt16(bytes, (short)var2.Index);
                    else
                        WriteInt16(bytes, 0);
                    break;
                    
                case MutableOperandType.ShortInlineArg:
                    if (instruction.Operand is int argIndex)
                        bytes.Add((byte)argIndex);
                    else
                        bytes.Add(0);
                    break;
                    
                case MutableOperandType.InlineArg:
                    if (instruction.Operand is int argIndex2)
                        WriteInt16(bytes, (short)argIndex2);
                    else
                        WriteInt16(bytes, 0);
                    break;
                    
                case MutableOperandType.InlineSwitch:
                    if (instruction.Operand is MutableInstruction[] targets)
                    {
                        WriteInt32(bytes, targets.Length);
                        int baseOffset = instruction.Offset + instruction.GetSize();
                        foreach (var t in targets)
                        {
                            WriteInt32(bytes, t != null ? t.Offset - baseOffset : 0);
                        }
                    }
                    else
                    {
                        WriteInt32(bytes, 0);
                    }
                    break;
                    
                case MutableOperandType.InlineSig:
                    // Standalone signature
                    WriteInt32(bytes, instruction.Operand is int sigToken ? sigToken : 0);
                    break;
            }
        }

        private void WriteInt16(List<byte> bytes, short value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private void WriteInt32(List<byte> bytes, int value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private void WriteInt64(List<byte> bytes, long value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        private void WritePropertyDefinition(MutablePropertyDefinition prop)
        {
            var signature = EncodePropertySignature(prop);
            
            var handle = _metadata.AddProperty(
                (PropertyAttributes)0,
                _metadata.GetOrAddString(prop.Name),
                _metadata.GetOrAddBlob(signature));

            _propertyDefHandles[prop] = handle;
            
            // Add method semantics
            if (prop.GetMethod != null && _methodDefHandles.TryGetValue(prop.GetMethod, out var getHandle))
            {
                _metadata.AddMethodSemantics(handle, System.Reflection.MethodSemanticsAttributes.Getter, getHandle);
            }
            if (prop.SetMethod != null && _methodDefHandles.TryGetValue(prop.SetMethod, out var setHandle))
            {
                _metadata.AddMethodSemantics(handle, System.Reflection.MethodSemanticsAttributes.Setter, setHandle);
            }
        }

        private void WriteEventDefinition(MutableEventDefinition evt)
        {
            var eventTypeHandle = GetTypeHandle(evt.EventType);
            
            var handle = _metadata.AddEvent(
                (EventAttributes)0,
                _metadata.GetOrAddString(evt.Name),
                eventTypeHandle);

            _eventDefHandles[evt] = handle;
            
            if (evt.AddMethod != null && _methodDefHandles.TryGetValue(evt.AddMethod, out var addHandle))
            {
                _metadata.AddMethodSemantics(handle, System.Reflection.MethodSemanticsAttributes.Adder, addHandle);
            }
            if (evt.RemoveMethod != null && _methodDefHandles.TryGetValue(evt.RemoveMethod, out var removeHandle))
            {
                _metadata.AddMethodSemantics(handle, System.Reflection.MethodSemanticsAttributes.Remover, removeHandle);
            }
            if (evt.InvokeMethod != null && _methodDefHandles.TryGetValue(evt.InvokeMethod, out var raiseHandle))
            {
                _metadata.AddMethodSemantics(handle, System.Reflection.MethodSemanticsAttributes.Raiser, raiseHandle);
            }
        }

        private void WriteAllGenericParameters()
        {
            var entries = new List<(EntityHandle owner, MutableGenericParameter gp)>();

            foreach (var type in _orderedTypes)
            {
                if (_typeDefHandles.TryGetValue(type, out var typeHandle))
                {
                    foreach (var gp in type.GenericParameters)
                    {
                        entries.Add((typeHandle, gp));
                    }
                }

                foreach (var method in type.Methods)
                {
                    if (_methodDefHandles.TryGetValue(method, out var methodHandle))
                    {
                        foreach (var gp in method.GenericParameters)
                        {
                            entries.Add((methodHandle, gp));
                        }
                    }
                }
            }

            entries.Sort((left, right) =>
            {
                var leftCode = GetTypeOrMethodDefCode(left.owner);
                var rightCode = GetTypeOrMethodDefCode(right.owner);
                if (leftCode != rightCode)
                    return leftCode.CompareTo(rightCode);

                return left.gp.Position.CompareTo(right.gp.Position);
            });

            foreach (var entry in entries)
            {
                var gp = entry.gp;
                var gpHandle = _metadata.AddGenericParameter(
                    entry.owner,
                    (GenericParameterAttributes)gp.GenericParameterAttributes,
                    _metadata.GetOrAddString(gp.Name),
                    gp.Position);
                _genericParameterHandles[gp] = gpHandle;

                foreach (var constraint in gp.Constraints)
                {
                    var constraintType = GetTypeHandle(constraint.ConstraintType);
                    _metadata.AddGenericParameterConstraint(gpHandle, constraintType);
                }
            }
        }

        private static int GetTypeOrMethodDefCode(EntityHandle owner)
        {
            switch (owner.Kind)
            {
                case HandleKind.TypeDefinition:
                    return MetadataTokens.GetRowNumber((TypeDefinitionHandle)owner) << 1;
                case HandleKind.MethodDefinition:
                    return (MetadataTokens.GetRowNumber((MethodDefinitionHandle)owner) << 1) | 1;
                default:
                    return int.MaxValue;
            }
        }

        private void WriteCustomAttributes()
        {
            if (!_assemblyDefHandle.IsNil)
            {
                foreach (var attr in _assembly.CustomAttributes)
                {
                    AddCustomAttribute(_assemblyDefHandle, attr);
                }
            }

            if (!_moduleDefHandle.IsNil)
            {
                foreach (var attr in _assembly.MainModule.CustomAttributes)
                {
                    AddCustomAttribute(_moduleDefHandle, attr);
                }
            }

            foreach (var typeEntry in _typeDefHandles)
            {
                var type = typeEntry.Key;
                var typeHandle = typeEntry.Value;

                foreach (var attr in type.CustomAttributes)
                {
                    AddCustomAttribute(typeHandle, attr);
                }

                foreach (var method in type.Methods)
                {
                    if (_methodDefHandles.TryGetValue(method, out var methodHandle))
                    {
                        foreach (var attr in method.CustomAttributes)
                        {
                            AddCustomAttribute(methodHandle, attr);
                        }
                    }

                    foreach (var param in method.Parameters)
                    {
                        if (_parameterHandles.TryGetValue(param, out var paramHandle))
                        {
                            foreach (var attr in param.CustomAttributes)
                            {
                                AddCustomAttribute(paramHandle, attr);
                            }
                        }
                    }

                    foreach (var gp in method.GenericParameters)
                    {
                        if (_genericParameterHandles.TryGetValue(gp, out var gpHandle))
                        {
                            foreach (var attr in gp.CustomAttributes)
                            {
                                AddCustomAttribute(gpHandle, attr);
                            }
                        }
                    }
                }

                foreach (var field in type.Fields)
                {
                    if (_fieldDefHandles.TryGetValue(field, out var fieldHandle))
                    {
                        foreach (var attr in field.CustomAttributes)
                        {
                            AddCustomAttribute(fieldHandle, attr);
                        }
                    }
                }

                foreach (var gp in type.GenericParameters)
                {
                    if (_genericParameterHandles.TryGetValue(gp, out var gpHandle))
                    {
                        foreach (var attr in gp.CustomAttributes)
                        {
                            AddCustomAttribute(gpHandle, attr);
                        }
                    }
                }

                foreach (var prop in type.Properties)
                {
                    if (_propertyDefHandles.TryGetValue(prop, out var propHandle))
                    {
                        foreach (var attr in prop.CustomAttributes)
                        {
                            AddCustomAttribute(propHandle, attr);
                        }
                    }
                }

                foreach (var evt in type.Events)
                {
                    if (_eventDefHandles.TryGetValue(evt, out var evtHandle))
                    {
                        foreach (var attr in evt.CustomAttributes)
                        {
                            AddCustomAttribute(evtHandle, attr);
                        }
                    }
                }
            }
        }

        private void AddCustomAttribute(EntityHandle parent, MutableCustomAttribute attr)
        {
            try
            {
                var constructor = GetMethodHandle(attr.Constructor);
                var value = EncodeCustomAttributeValue(attr);

                _metadata.AddCustomAttribute(
                    parent: parent,
                    constructor: constructor,
                    value: value);
            }
            catch
            {
                if (string.Equals(Environment.GetEnvironmentVariable("OBFUSCAR_DEBUG_ATTRS"), "1", StringComparison.Ordinal))
                {
                    try
                    {
                        Obfuscar.LoggerService.Logger.LogDebug($"Custom attribute skipped: {attr?.AttributeTypeName}");
                    }
                    catch
                    {
                        // ignore logging failures
                    }
                }
                // Skip attributes that can't be encoded
            }
        }

        private BlobHandle EncodeCustomAttributeValue(MutableCustomAttribute attr)
        {
            var builder = new BlobBuilder();

            // Prolog
            builder.WriteUInt16(0x0001);

            // Fixed arguments
            foreach (var arg in attr.ConstructorArguments)
            {
                EncodeCustomAttributeArgument(builder, arg);
            }

            // Named arguments count
            builder.WriteUInt16((ushort)(attr.Fields.Count + attr.Properties.Count));

            // Named arguments
            foreach (var field in attr.Fields)
            {
                builder.WriteByte(0x53); // FIELD
                EncodeCustomAttributeFieldOrPropType(builder, field.Argument);
                WriteSerializedString(builder, field.Name);
                EncodeCustomAttributeArgument(builder, field.Argument);
            }

            foreach (var prop in attr.Properties)
            {
                builder.WriteByte(0x54); // PROPERTY
                EncodeCustomAttributeFieldOrPropType(builder, prop.Argument);
                WriteSerializedString(builder, prop.Name);
                EncodeCustomAttributeArgument(builder, prop.Argument);
            }

            return _metadata.GetOrAddBlob(builder);
        }

        private void EncodeCustomAttributeArgument(BlobBuilder builder, MutableCustomAttributeArgument arg)
        {
            if (arg?.Type == null)
            {
                if (arg?.Value is string stringValue)
                {
                    WriteSerializedString(builder, stringValue);
                    return;
                }

                if (TryWritePrimitiveValueFromObject(builder, arg?.Value))
                    return;

                builder.WriteInt32(0);
                return;
            }

            if (arg.Type.FullName == "System.String")
            {
                WriteSerializedString(builder, arg.Value as string);
                return;
            }

            if (arg.Type.FullName == "System.Type")
            {
                var typeRef = arg.Value as MutableTypeReference;
                WriteSerializedString(builder, GetCustomAttributeTypeName(typeRef));
                return;
            }

            if (arg.Value is IEnumerable<MutableCustomAttributeArgument> arrayArgs)
            {
                var list = arrayArgs.ToList();
                builder.WriteInt32(list.Count);
                foreach (var item in list)
                {
                    EncodeCustomAttributeArgument(builder, item);
                }
                return;
            }

            if (arg.Type.Resolve() is MutableTypeDefinition enumDef && enumDef.IsEnum)
            {
                var underlyingType = enumDef.Fields.FirstOrDefault(field => field.Name == "value__")?.FieldType;
                if (underlyingType != null)
                {
                    WritePrimitiveValue(builder, arg.Value, underlyingType);
                }
                else
                {
                    builder.WriteInt32(0);
                }
                return;
            }

            if (arg.Type.IsPrimitive)
            {
                WritePrimitiveValue(builder, arg.Value, arg.Type);
                return;
            }

            builder.WriteInt32(0);
        }

        private void EncodeCustomAttributeFieldOrPropType(BlobBuilder builder, MutableCustomAttributeArgument argument)
        {
            var type = argument?.Type;
            if (type == null)
            {
                if (argument?.Value is string)
                {
                    builder.WriteByte(0x0e);
                    return;
                }

                if (TryWritePrimitiveTypeFromObject(builder, argument?.Value))
                    return;

                builder.WriteByte(0x51); // ELEMENT_TYPE_OBJECT
                return;
            }

            if (type?.Resolve() is MutableTypeDefinition enumDef && enumDef.IsEnum)
            {
                builder.WriteByte(0x55); // ELEMENT_TYPE_ENUM
                WriteSerializedString(builder, GetCustomAttributeTypeName(type));
                return;
            }

            if (type.FullName == "System.Boolean") builder.WriteByte(0x02);
            else if (type.FullName == "System.Char") builder.WriteByte(0x03);
            else if (type.FullName == "System.SByte") builder.WriteByte(0x04);
            else if (type.FullName == "System.Byte") builder.WriteByte(0x05);
            else if (type.FullName == "System.Int16") builder.WriteByte(0x06);
            else if (type.FullName == "System.UInt16") builder.WriteByte(0x07);
            else if (type.FullName == "System.Int32") builder.WriteByte(0x08);
            else if (type.FullName == "System.UInt32") builder.WriteByte(0x09);
            else if (type.FullName == "System.Int64") builder.WriteByte(0x0a);
            else if (type.FullName == "System.UInt64") builder.WriteByte(0x0b);
            else if (type.FullName == "System.Single") builder.WriteByte(0x0c);
            else if (type.FullName == "System.Double") builder.WriteByte(0x0d);
            else if (type.FullName == "System.String") builder.WriteByte(0x0e);
            else if (type.FullName == "System.Type") builder.WriteByte(0x50);
            else builder.WriteByte(0x51); // ELEMENT_TYPE_OBJECT
        }

        private static void WriteSerializedString(BlobBuilder builder, string value)
        {
            if (value == null)
            {
                builder.WriteByte(0xFF);
            }
            else
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(value);
                builder.WriteCompressedInteger(bytes.Length);
                builder.WriteBytes(bytes);
            }
        }

        private static string GetCustomAttributeTypeName(MutableTypeReference typeRef)
        {
            if (typeRef == null)
                return null;

            return typeRef.FullName?.Replace('/', '+');
        }

        private static void WritePrimitiveValue(BlobBuilder builder, object value, MutableTypeReference type)
        {
            if (value == null || type == null)
                return;

            switch (type.FullName)
            {
                case "System.Boolean": builder.WriteBoolean(Convert.ToBoolean(value)); break;
                case "System.Byte": builder.WriteByte(Convert.ToByte(value)); break;
                case "System.SByte": builder.WriteSByte(Convert.ToSByte(value)); break;
                case "System.Int16": builder.WriteInt16(Convert.ToInt16(value)); break;
                case "System.UInt16": builder.WriteUInt16(Convert.ToUInt16(value)); break;
                case "System.Int32": builder.WriteInt32(Convert.ToInt32(value)); break;
                case "System.UInt32": builder.WriteUInt32(Convert.ToUInt32(value)); break;
                case "System.Int64": builder.WriteInt64(Convert.ToInt64(value)); break;
                case "System.UInt64": builder.WriteUInt64(Convert.ToUInt64(value)); break;
                case "System.Single": builder.WriteSingle(Convert.ToSingle(value)); break;
                case "System.Double": builder.WriteDouble(Convert.ToDouble(value)); break;
                case "System.Char": builder.WriteUInt16(Convert.ToUInt16(value)); break;
            }
        }

        private static bool TryWritePrimitiveTypeFromObject(BlobBuilder builder, object value)
        {
            if (value is bool) { builder.WriteByte(0x02); return true; }
            if (value is char) { builder.WriteByte(0x03); return true; }
            if (value is sbyte) { builder.WriteByte(0x04); return true; }
            if (value is byte) { builder.WriteByte(0x05); return true; }
            if (value is short) { builder.WriteByte(0x06); return true; }
            if (value is ushort) { builder.WriteByte(0x07); return true; }
            if (value is int) { builder.WriteByte(0x08); return true; }
            if (value is uint) { builder.WriteByte(0x09); return true; }
            if (value is long) { builder.WriteByte(0x0a); return true; }
            if (value is ulong) { builder.WriteByte(0x0b); return true; }
            if (value is float) { builder.WriteByte(0x0c); return true; }
            if (value is double) { builder.WriteByte(0x0d); return true; }
            if (value is Type || value is MutableTypeReference) { builder.WriteByte(0x50); return true; }
            return false;
        }

        private static bool TryWritePrimitiveValueFromObject(BlobBuilder builder, object value)
        {
            if (value is bool boolValue) { builder.WriteBoolean(boolValue); return true; }
            if (value is char charValue) { builder.WriteUInt16(charValue); return true; }
            if (value is sbyte sbyteValue) { builder.WriteSByte(sbyteValue); return true; }
            if (value is byte byteValue) { builder.WriteByte(byteValue); return true; }
            if (value is short shortValue) { builder.WriteInt16(shortValue); return true; }
            if (value is ushort ushortValue) { builder.WriteUInt16(ushortValue); return true; }
            if (value is int intValue) { builder.WriteInt32(intValue); return true; }
            if (value is uint uintValue) { builder.WriteUInt32(uintValue); return true; }
            if (value is long longValue) { builder.WriteInt64(longValue); return true; }
            if (value is ulong ulongValue) { builder.WriteUInt64(ulongValue); return true; }
            if (value is float floatValue) { builder.WriteSingle(floatValue); return true; }
            if (value is double doubleValue) { builder.WriteDouble(doubleValue); return true; }
            return false;
        }

        private BlobBuilder EncodeFieldSignature(MutableTypeReference fieldType)
        {
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder);
            EncodeTypeToBuilder(encoder.FieldSignature(), fieldType);
            return builder;
        }

        private BlobBuilder EncodeMethodSignature(MutableMethodDefinition method)
        {
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder);
            
            encoder.MethodSignature(
                SignatureCallingConvention.Default, 
                method.GenericParameters.Count, 
                !method.IsStatic)
                .Parameters(
                    method.Parameters.Count,
                    returnType => EncodeReturnType(returnType, method.ReturnType),
                    parameters =>
                    {
                        foreach (var param in method.Parameters)
                        {
                            EncodeTypeToBuilder(parameters.AddParameter().Type(), param.ParameterType);
                        }
                    });
            
            return builder;
        }

        private void EncodeReturnType(ReturnTypeEncoder encoder, MutableTypeReference type)
        {
            if (type == null || type.FullName == "System.Void")
            {
                encoder.Void();
            }
            else
            {
                EncodeTypeToBuilder(encoder.Type(), type);
            }
        }

        private BlobBuilder EncodePropertySignature(MutablePropertyDefinition prop)
        {
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder);
            
            // Property signature: PROPERTY hasThis paramCount returnType params...
            // For now, simple property with no parameters
            encoder.PropertySignature(!prop.GetMethod?.IsStatic ?? true)
                .Parameters(0, 
                    returnType => EncodeReturnType(returnType, prop.PropertyType),
                    parameters => { });
            
            return builder;
        }

        private void EncodeTypeToBuilder(SignatureTypeEncoder encoder, MutableTypeReference type)
        {
            if (type == null)
            {
                encoder.Object();
                return;
            }

            // Handle primitives
            var fullName = type.FullName;
            switch (fullName)
            {
                case "System.Void":
                    // Void should not appear in type signatures except return type
                    encoder.Builder.WriteByte((byte)SignatureTypeCode.Void);
                    return;
                case "System.Boolean": encoder.Boolean(); return;
                case "System.Char": encoder.Char(); return;
                case "System.SByte": encoder.SByte(); return;
                case "System.Byte": encoder.Byte(); return;
                case "System.Int16": encoder.Int16(); return;
                case "System.UInt16": encoder.UInt16(); return;
                case "System.Int32": encoder.Int32(); return;
                case "System.UInt32": encoder.UInt32(); return;
                case "System.Int64": encoder.Int64(); return;
                case "System.UInt64": encoder.UInt64(); return;
                case "System.Single": encoder.Single(); return;
                case "System.Double": encoder.Double(); return;
                case "System.IntPtr": encoder.IntPtr(); return;
                case "System.UIntPtr": encoder.UIntPtr(); return;
                case "System.String": encoder.String(); return;
                case "System.Object": encoder.Object(); return;
            }

            // Handle constructed types
            if (type is MutableGenericInstanceType genericInstance)
            {
                var elementHandle = GetTypeHandle(genericInstance.ElementType);
                var argsEncoder = encoder.GenericInstantiation(elementHandle, genericInstance.GenericArguments.Count, type.IsValueType);
                foreach (var arg in genericInstance.GenericArguments)
                {
                    EncodeTypeToBuilder(argsEncoder.AddArgument(), arg);
                }
                return;
            }
            
            if (type is MutableArrayType arrayType)
            {
                if (arrayType.Rank == 1)
                {
                    EncodeTypeToBuilder(encoder.SZArray(), arrayType.ElementType);
                }
                else
                {
                    encoder.Array(
                        e => EncodeTypeToBuilder(e, arrayType.ElementType), 
                        s => s.Shape(arrayType.Rank, System.Collections.Immutable.ImmutableArray<int>.Empty, System.Collections.Immutable.ImmutableArray<int>.Empty));
                }
                return;
            }
            
            if (type is MutableByReferenceType byRefType)
            {
                EncodeTypeToBuilder(encoder, byRefType.ElementType);
                return;
            }
            
            if (type is MutablePointerType pointerType)
            {
                EncodeTypeToBuilder(encoder.Pointer(), pointerType.ElementType);
                return;
            }
            
            if (type is MutableGenericParameter gp)
            {
                if (gp.IsMethodParameter || gp.Owner is MutableMethodDefinition || gp.Owner is MutableMethodReference)
                {
                    encoder.GenericMethodTypeParameter(gp.Position);
                }
                else
                {
                    encoder.GenericTypeParameter(gp.Position);
                }
                return;
            }

            // Regular type reference
            var handle = GetTypeHandle(type);
            encoder.Type(handle, type.IsValueType);
        }

        private EntityHandle GetTypeHandle(MutableTypeReference type)
        {
            if (type == null)
                return default;

            if (type is MutableTypeDefinition typeDef && _typeDefHandles.TryGetValue(typeDef, out var defHandle))
                return defHandle;

            if (_typeRefHandles.TryGetValue(type, out var existing))
                return existing;

            if (type is MutableGenericParameter)
            {
                var sig = new BlobBuilder();
                var sigEncoder = new BlobEncoder(sig);
                EncodeTypeToBuilder(sigEncoder.TypeSpecificationSignature(), type);

                var specHandle = _metadata.AddTypeSpecification(_metadata.GetOrAddBlob(sig));
                _typeRefHandles[type] = specHandle;
                return specHandle;
            }

            // Need to create a type reference or type spec
            if (type is MutableGenericInstanceType || type is MutableArrayType || 
                type is MutableByReferenceType || type is MutablePointerType)
            {
                // Create TypeSpec
                var sig = new BlobBuilder();
                var sigEncoder = new BlobEncoder(sig);
                EncodeTypeToBuilder(sigEncoder.TypeSpecificationSignature(), type);
                
                var specHandle = _metadata.AddTypeSpecification(_metadata.GetOrAddBlob(sig));
                _typeRefHandles[type] = specHandle;
                return specHandle;
            }

            // Create TypeRef
            EntityHandle resolutionScope = default;
            if (type.Scope is MutableAssemblyNameReference asmRef && _asmRefHandles.TryGetValue(asmRef, out var asmHandle))
            {
                resolutionScope = asmHandle;
            }

            var refHandle = _metadata.AddTypeReference(
                resolutionScope,
                _metadata.GetOrAddString(type.Namespace ?? ""),
                _metadata.GetOrAddString(type.Name));
            
            _typeRefHandles[type] = refHandle;
            return refHandle;
        }

        private EntityHandle GetMethodHandle(MutableMethodReference method)
        {
            if (method == null)
                return default;

            if (method is MutableGenericInstanceMethod genericInstance)
            {
                if (_methodRefHandles.TryGetValue(method, out var existingSpec))
                    return existingSpec;

                var elementHandle = GetMethodHandle(genericInstance.ElementMethod);
                var specSig = new BlobBuilder();
                var specEncoder = new BlobEncoder(specSig);
                var argsEncoder = specEncoder.MethodSpecificationSignature(genericInstance.GenericArguments.Count);
                foreach (var arg in genericInstance.GenericArguments)
                {
                    EncodeTypeToBuilder(argsEncoder.AddArgument(), arg);
                }

                var specHandle = _metadata.AddMethodSpecification(elementHandle, _metadata.GetOrAddBlob(specSig));
                _methodRefHandles[method] = specHandle;
                return specHandle;
            }

            if (method is MutableMethodDefinition methodDef && _methodDefHandles.TryGetValue(methodDef, out var defHandle))
                return defHandle;

            if (_methodRefHandles.TryGetValue(method, out var existing))
                return existing;

            // Create MemberRef
            var declaringHandle = GetTypeHandle(method.DeclaringType);
            var sig = EncodeMethodReferenceSignature(method);
            
            var refHandle = _metadata.AddMemberReference(
                declaringHandle,
                _metadata.GetOrAddString(method.Name),
                _metadata.GetOrAddBlob(sig));
            
            _methodRefHandles[method] = refHandle;
            return refHandle;
        }

        private EntityHandle GetFieldHandle(MutableFieldReference field)
        {
            if (field == null)
                return default;

            if (field is MutableFieldDefinition fieldDef && _fieldDefHandles.TryGetValue(fieldDef, out var defHandle))
                return defHandle;

            if (_fieldRefHandles.TryGetValue(field, out var existing))
                return existing;

            // Create MemberRef
            var declaringHandle = GetTypeHandle(field.DeclaringType);
            var sig = EncodeFieldSignature(field.FieldType);
            
            var refHandle = _metadata.AddMemberReference(
                declaringHandle,
                _metadata.GetOrAddString(field.Name),
                _metadata.GetOrAddBlob(sig));
            
            _fieldRefHandles[field] = refHandle;
            return refHandle;
        }

        private EntityHandle GetTokenHandle(object operand)
        {
            switch (operand)
            {
                case MutableTypeReference type: return GetTypeHandle(type);
                case MutableMethodReference method: return GetMethodHandle(method);
                case MutableFieldReference field: return GetFieldHandle(field);
                default: return default;
            }
        }

        private BlobBuilder EncodeMethodReferenceSignature(MutableMethodReference method)
        {
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder);
            
            encoder.MethodSignature(
                SignatureCallingConvention.Default,
                method.GenericParameters.Count,
                method.HasThis)
                .Parameters(
                    method.Parameters.Count,
                    returnType => EncodeReturnType(returnType, method.ReturnType),
                    parameters =>
                    {
                        foreach (var param in method.Parameters)
                        {
                            EncodeTypeToBuilder(parameters.AddParameter().Type(), param.ParameterType);
                        }
                    });
            
            return builder;
        }

        private EntityHandle GetMethodHandleForImpl(MutableMethodReference method)
        {
            if (method is MutableGenericInstanceMethod genericInstance)
                method = genericInstance.ElementMethod;

            var handle = GetMethodHandle(method);
            if (handle.Kind == HandleKind.MethodSpecification)
                return default;

            return handle;
        }

        private UserStringHandle GetOrAddUserString(string value)
        {
            if (_userStringHandles.TryGetValue(value, out var handle))
                return handle;
            
            handle = _metadata.GetOrAddUserString(value);
            _userStringHandles[value] = handle;
            return handle;
        }

        private void WritePEFile(Stream stream)
        {
            var module = _assembly.MainModule;
            
            // Determine PE characteristics
            var characteristics = Characteristics.ExecutableImage;
            if (module.Kind == MutableModuleKind.Dll)
            {
                characteristics |= Characteristics.Dll;
            }

            // Create PE builder
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: characteristics,
                subsystem: module.Kind == MutableModuleKind.Windows ? Subsystem.WindowsGui : Subsystem.WindowsCui);

            // Create managed PE builder
            var corFlags = CorFlags.ILOnly;
            if (module.Attributes.HasFlag(MutableModuleAttributes.Required32Bit))
                corFlags |= CorFlags.Requires32Bit;
            if (module.Attributes.HasFlag(MutableModuleAttributes.Preferred32Bit))
                corFlags |= CorFlags.Prefers32Bit;
            if (module.Attributes.HasFlag(MutableModuleAttributes.StrongNameSigned) || _parameters.StrongNameKeyBlob != null)
                corFlags |= CorFlags.StrongNameSigned;

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(_metadata),
                _ilStream,
                flags: corFlags);

            // Write to blob
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            
            // Write to stream
            peBlob.WriteContentTo(stream);
        }
    }
}
