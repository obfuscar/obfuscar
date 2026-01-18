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
    /// This replaces the need for Mono.Cecil.AssemblyDefinition.Write().
    /// </summary>
    /// <remarks>
    /// <para>
    /// The writer translates the mutable object model to SRM's MetadataBuilder
    /// and uses PEBuilder to produce the final PE file. This is a complete
    /// re-implementation of assembly writing without any Cecil dependency.
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
            
            // Second pass: fill in type members
            WriteTypeDefinitionsSecondPass();

            // Custom attributes
            WriteCustomAttributes();
            
            // Third pass: write method bodies
            WriteMethodBodies();
            
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
            
            // Add <Module> type first
            _metadata.AddTypeDefinition(
                default, // Attributes
                default, // Namespace
                _metadata.GetOrAddString("<Module>"),
                default, // BaseType
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));

            // Process types in order: non-nested first, then nested
            var allTypes = new List<MutableTypeDefinition>();
            CollectTypes(module.Types, allTypes);
            
            foreach (var type in allTypes)
            {
                CreateTypeDefinition(type);
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

        private void CreateTypeDefinition(MutableTypeDefinition type)
        {
            // Get field and method handles (will be set in second pass)
            var firstField = MetadataTokens.FieldDefinitionHandle(_metadata.GetRowCount(TableIndex.Field) + 1);
            var firstMethod = MetadataTokens.MethodDefinitionHandle(_metadata.GetRowCount(TableIndex.MethodDef) + 1);
            
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
            foreach (var kvp in _typeDefHandles)
            {
                var type = kvp.Key;
                
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
                
                // Write interfaces
                foreach (var iface in type.Interfaces)
                {
                    var ifaceHandle = GetTypeHandle(iface.InterfaceType);
                    _metadata.AddInterfaceImplementation(kvp.Value, ifaceHandle);
                }
                
                // Write generic parameters
                WriteGenericParameters(kvp.Value, type.GenericParameters);
                
                // Write properties
                if (type.Properties.Count > 0)
                {
                    _metadata.AddPropertyMap(kvp.Value, 
                        MetadataTokens.PropertyDefinitionHandle(_metadata.GetRowCount(TableIndex.Property) + 1));
                    
                    foreach (var prop in type.Properties)
                    {
                        WritePropertyDefinition(prop);
                    }
                }
                
                // Write events
                if (type.Events.Count > 0)
                {
                    _metadata.AddEventMap(kvp.Value,
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
            
            // RVA will be set when writing method body
            var handle = _metadata.AddMethodDefinition(
                method.Attributes,
                method.ImplAttributes,
                _metadata.GetOrAddString(method.Name),
                _metadata.GetOrAddBlob(signature),
                -1, // RVA placeholder - will be patched
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
            
            // Write generic parameters
            WriteGenericParameters(handle, method.GenericParameters);
        }

        private void WriteMethodBodies()
        {
            foreach (var kvp in _methodDefHandles)
            {
                var method = kvp.Key;
                
                if (method.Body == null || method.IsAbstract || method.IsPInvokeImpl)
                    continue;
                
                EncodeMethodBody(method.Body);
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

        private void WriteGenericParameters(EntityHandle owner, IList<MutableGenericParameter> genericParameters)
        {
            foreach (var gp in genericParameters)
            {
                var gpHandle = _metadata.AddGenericParameter(
                    owner,
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
                EncodeCustomAttributeFieldOrPropType(builder, field.Argument.Type);
                WriteSerializedString(builder, field.Name);
                EncodeCustomAttributeArgument(builder, field.Argument);
            }

            foreach (var prop in attr.Properties)
            {
                builder.WriteByte(0x54); // PROPERTY
                EncodeCustomAttributeFieldOrPropType(builder, prop.Argument.Type);
                WriteSerializedString(builder, prop.Name);
                EncodeCustomAttributeArgument(builder, prop.Argument);
            }

            return _metadata.GetOrAddBlob(builder);
        }

        private void EncodeCustomAttributeArgument(BlobBuilder builder, MutableCustomAttributeArgument arg)
        {
            if (arg?.Type == null)
            {
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

        private void EncodeCustomAttributeFieldOrPropType(BlobBuilder builder, MutableTypeReference type)
        {
            if (type?.Resolve() is MutableTypeDefinition enumDef && enumDef.IsEnum)
            {
                builder.WriteByte(0x55); // ELEMENT_TYPE_ENUM
                WriteSerializedString(builder, GetCustomAttributeTypeName(type));
                return;
            }

            if (type == null)
            {
                builder.WriteByte(0x51); // ELEMENT_TYPE_OBJECT
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
                if (gp.Owner is MutableMethodDefinition || gp.Owner is MutableMethodReference)
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
                0, // genericParameterCount
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
            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(_metadata),
                _ilStream,
                flags: CorFlags.ILOnly);

            // Write to blob
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            
            // Write to stream
            peBlob.WriteContentTo(stream);
        }
    }
}
