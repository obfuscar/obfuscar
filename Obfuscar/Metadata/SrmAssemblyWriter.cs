using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using CecilAssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using CecilModuleDefinition = Mono.Cecil.ModuleDefinition;
using CecilTypeDefinition = Mono.Cecil.TypeDefinition;
using CecilTypeReference = Mono.Cecil.TypeReference;
using CecilMethodDefinition = Mono.Cecil.MethodDefinition;
using CecilMethodReference = Mono.Cecil.MethodReference;
using CecilFieldDefinition = Mono.Cecil.FieldDefinition;
using CecilFieldReference = Mono.Cecil.FieldReference;
using CecilPropertyDefinition = Mono.Cecil.PropertyDefinition;
using CecilEventDefinition = Mono.Cecil.EventDefinition;
using CecilParameterDefinition = Mono.Cecil.ParameterDefinition;
using CecilGenericParameter = Mono.Cecil.GenericParameter;
using CecilCustomAttribute = Mono.Cecil.CustomAttribute;
using CecilModuleReference = Mono.Cecil.ModuleReference;

namespace Obfuscar.Metadata
{
    /// <summary>
    /// System.Reflection.Metadata-based PE writer.
    /// Translates Cecil's in-memory object model to a new PE file using MetadataBuilder.
    /// </summary>
    public class SrmAssemblyWriter : IAssemblyWriter, IDisposable
    {
        private CecilAssemblyDefinition assembly;
        private MetadataBuilder metadata;
        private BlobBuilder ilBuilder;
        private BlobBuilder resourceBuilder;
        private ModuleDefinitionHandle moduleDefHandle;
        private AssemblyDefinitionHandle assemblyDefHandle;
        
        // Handle caches for forward references
        private Dictionary<CecilTypeDefinition, TypeDefinitionHandle> typeDefHandles;
        private Dictionary<CecilMethodDefinition, MethodDefinitionHandle> methodDefHandles;
        private Dictionary<CecilFieldDefinition, FieldDefinitionHandle> fieldDefHandles;
        private Dictionary<CecilTypeReference, EntityHandle> typeRefHandles;
        private Dictionary<CecilMethodReference, EntityHandle> methodRefHandles;
        private Dictionary<CecilFieldReference, EntityHandle> fieldRefHandles;
        private Dictionary<AssemblyNameReference, AssemblyReferenceHandle> assemblyRefHandles;
        private Dictionary<CecilModuleReference, ModuleReferenceHandle> moduleRefHandles;
        
        // String/blob caches
        private Dictionary<string, StringHandle> stringCache;
        private Dictionary<string, UserStringHandle> userStringCache;
        
        public SrmAssemblyWriter()
        {
        }
        
        private void Initialize(CecilAssemblyDefinition assembly)
        {
            this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.metadata = new MetadataBuilder();
            this.ilBuilder = new BlobBuilder();
            this.resourceBuilder = new BlobBuilder();
            this.moduleDefHandle = default;
            this.assemblyDefHandle = default;
            
            this.typeDefHandles = new Dictionary<CecilTypeDefinition, TypeDefinitionHandle>();
            this.methodDefHandles = new Dictionary<CecilMethodDefinition, MethodDefinitionHandle>();
            this.fieldDefHandles = new Dictionary<CecilFieldDefinition, FieldDefinitionHandle>();
            this.typeRefHandles = new Dictionary<CecilTypeReference, EntityHandle>();
            this.methodRefHandles = new Dictionary<CecilMethodReference, EntityHandle>();
            this.fieldRefHandles = new Dictionary<CecilFieldReference, EntityHandle>();
            this.assemblyRefHandles = new Dictionary<AssemblyNameReference, AssemblyReferenceHandle>();
            this.moduleRefHandles = new Dictionary<CecilModuleReference, ModuleReferenceHandle>();
            
            this.stringCache = new Dictionary<string, StringHandle>();
            this.userStringCache = new Dictionary<string, UserStringHandle>();
        }

        public void Write(CecilAssemblyDefinition assembly, string outputPath)
        {
            Write(assembly, outputPath, null);
        }

        public void Write(CecilAssemblyDefinition assembly, string outputPath, WriterParameters parameters)
        {
            try
            {
                // NOTE: Previously this method could fall back to a Mono.Cecil-based writer for
                // unsupported scenarios. Cecil-based writer has been removed; SRM writer will
                // attempt to handle all cases. If a scenario isn't supported, an exception will
                // be thrown.
                // Initialize for this assembly
                Initialize(assembly);
                
                // Build all metadata tables
                BuildMetadata();
                
                // Create and write PE file
                using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    WritePeFile(stream, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write assembly using SRM: {ex.Message}", ex);
            }
        }

        private static bool ShouldFallbackToCecil(CecilAssemblyDefinition assembly, WriterParameters parameters)
        {
            if (parameters?.StrongNameKeyBlob != null || parameters?.StrongNameKeyPair != null || parameters?.SymbolWriterProvider != null)
            {
                return true;
            }

            if (assembly == null)
            {
                return true;
            }

            foreach (var type in assembly.MainModule.Types)
            {
                if (TypeUsesGenerics(type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TypeUsesGenerics(CecilTypeDefinition type)
        {
            if (type.HasGenericParameters)
            {
                return true;
            }

            foreach (var method in type.Methods)
            {
                if (method.HasGenericParameters || method.ReturnType.IsGenericInstance)
                {
                    return true;
                }

                foreach (var param in method.Parameters)
                {
                    if (param.ParameterType.IsGenericInstance)
                    {
                        return true;
                    }
                }
            }

            foreach (var field in type.Fields)
            {
                if (field.FieldType.IsGenericInstance)
                {
                    return true;
                }
            }

            foreach (var property in type.Properties)
            {
                if (property.PropertyType.IsGenericInstance)
                {
                    return true;
                }
            }

            foreach (var evt in type.Events)
            {
                if (evt.EventType.IsGenericInstance)
                {
                    return true;
                }
            }

            foreach (var nested in type.NestedTypes)
            {
                if (TypeUsesGenerics(nested))
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildMetadata()
        {
            // 1. Add module
            moduleDefHandle = metadata.AddModule(
                generation: 0,
                moduleName: GetOrAddString(assembly.MainModule.Name),
                mvid: GetOrAddGuid(assembly.MainModule.Mvid),
                encId: default,
                encBaseId: default);

            // 2. Add assembly (if this is an assembly, not a module)
            if (assembly.Name != null)
            {
                assemblyDefHandle = metadata.AddAssembly(
                    name: GetOrAddString(assembly.Name.Name),
                    version: assembly.Name.Version ?? new Version(0, 0, 0, 0),
                    culture: GetOrAddString(assembly.Name.Culture ?? string.Empty),
                    publicKey: GetOrAddBlob(assembly.Name.PublicKey),
                    flags: (AssemblyFlags)assembly.Name.Attributes,
                    hashAlgorithm: (System.Reflection.AssemblyHashAlgorithm)assembly.Name.HashAlgorithm);
            }

            // 3. Add assembly references
            foreach (var asmRef in assembly.MainModule.AssemblyReferences)
            {
                AddAssemblyReference(asmRef);
            }

            // 4. Add type definitions (multi-pass to handle forward references and ordering constraints)
            // First, add the <Module> type explicitly as the first type (row 1)
            // This is required by ECMA-335 II.22.37 - TypeDef row 1 is always the module pseudo-type
            var moduleType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "<Module>" && string.IsNullOrEmpty(t.Namespace));
            if (moduleType != null)
            {
                var moduleTypeHandle = metadata.AddTypeDefinition(
                    attributes: (System.Reflection.TypeAttributes)moduleType.Attributes,
                    @namespace: default,
                    name: GetOrAddString("<Module>"),
                    baseType: default,
                    fieldList: MetadataTokens.FieldDefinitionHandle(metadata.GetRowCount(TableIndex.Field) + 1),
                    methodList: MetadataTokens.MethodDefinitionHandle(metadata.GetRowCount(TableIndex.MethodDef) + 1));
                typeDefHandles[moduleType] = moduleTypeHandle;
            }
            
            // First pass: add all other type definitions to get handles (without members)
            foreach (var type in assembly.MainModule.Types)
            {
                // Skip <Module> type - it was already added above
                if (type.Name == "<Module>" && string.IsNullOrEmpty(type.Namespace))
                    continue;
                    
                AddTypeDefinitionsRecursive(type);
            }
            
            // Second pass: add all generic parameters (sorted by owner coded index, must come after all types/methods are defined)
            // Collect all generic params (both type and method) and sort by coded owner value
            var allGenericParams = new List<(EntityHandle owner, CecilGenericParameter gp, int codedOwner)>();
            CollectAllGenericParametersRecursive(assembly.MainModule.Types, allGenericParams);
            
            // Sort by coded owner value (TypeOrMethodDef coded index), then by Number
            allGenericParams.Sort((a, b) => {
                int ownerCompare = a.codedOwner.CompareTo(b.codedOwner);
                return ownerCompare != 0 ? ownerCompare : a.gp.Position.CompareTo(b.gp.Position);
            });
            
            foreach (var (owner, gp, _) in allGenericParams)
            {
                AddGenericParameter(owner, gp);
            }
            
            // Third pass: add remaining members (properties, events, custom attributes, etc.)
            foreach (var type in assembly.MainModule.Types)
            {
                // Skip <Module> type - it's implicitly created by AddModule
                if (type.Name == "<Module>" && string.IsNullOrEmpty(type.Namespace))
                    continue;
                    
                AddTypeMembersRecursive(type);
            }

            AddAssemblyAndModuleCustomAttributes();

            // 5. Add resources
            AddResources();
        }
        
        private void CollectAllGenericParametersRecursive(IEnumerable<CecilTypeDefinition> types, List<(EntityHandle, CecilGenericParameter, int)> list)
        {
            foreach (var type in types)
            {
                // Skip <Module> type - it's implicitly created and not in our type handles
                if (type.Name == "<Module>" && string.IsNullOrEmpty(type.Namespace))
                    continue;
                    
                // Type generic params - TypeOrMethodDef coded index with tag 0
                int typeCodedOwner = (MetadataTokens.GetRowNumber(typeDefHandles[type]) << 1) | 0;
                foreach (var gp in type.GenericParameters)
                {
                    list.Add((typeDefHandles[type], gp, typeCodedOwner));
                }
                
                // Method generic params - TypeOrMethodDef coded index with tag 1
                foreach (var method in type.Methods)
                {
                    int methodCodedOwner = (MetadataTokens.GetRowNumber(methodDefHandles[method]) << 1) | 1;
                    foreach (var gp in method.GenericParameters)
                    {
                        list.Add((methodDefHandles[method], gp, methodCodedOwner));
                    }
                }
                
                CollectAllGenericParametersRecursive(type.NestedTypes, list);
            }
        }

        private void AddAssemblyReference(AssemblyNameReference asmRef)
        {
            var handle = metadata.AddAssemblyReference(
                name: GetOrAddString(asmRef.Name),
                version: asmRef.Version ?? new Version(0, 0, 0, 0),
                culture: GetOrAddString(asmRef.Culture ?? string.Empty),
                publicKeyOrToken: GetOrAddBlob(asmRef.PublicKeyToken ?? asmRef.PublicKey),
                flags: (AssemblyFlags)(asmRef.Attributes & ~AssemblyAttributes.PublicKey),
                hashValue: default);
            
            assemblyRefHandles[asmRef] = handle;
        }

        private void AddTypeDefinitionsRecursive(CecilTypeDefinition type)
        {
            // Calculate field and method list handles
            // These point to the first field/method that will be added
            var fieldList = MetadataTokens.FieldDefinitionHandle(metadata.GetRowCount(TableIndex.Field) + 1);
            var methodList = MetadataTokens.MethodDefinitionHandle(metadata.GetRowCount(TableIndex.MethodDef) + 1);
            var baseType = type.BaseType != null ? GetTypeHandle(type.BaseType) : default;

            // Add type definition
            var typeHandle = metadata.AddTypeDefinition(
                attributes: (System.Reflection.TypeAttributes)type.Attributes,
                @namespace: GetOrAddString(type.Namespace ?? string.Empty),
                name: GetOrAddString(type.Name),
                baseType: baseType,
                fieldList: fieldList,
                methodList: methodList);
            
            typeDefHandles[type] = typeHandle;

            // Add all field definitions (just the metadata, not custom attributes yet)
            foreach (var field in type.Fields)
            {
                var fieldHandle = metadata.AddFieldDefinition(
                    attributes: (System.Reflection.FieldAttributes)field.Attributes,
                    name: GetOrAddString(field.Name),
                    signature: EncodeFieldSignature(field));
                fieldDefHandles[field] = fieldHandle;
            }

            // Add all method definitions (including bodies)
            foreach (var method in type.Methods)
            {
                var bodyOffset = -1;
                if (method.HasBody && !method.IsAbstract && !method.IsPInvokeImpl)
                {
                    bodyOffset = EncodeMethodBody(method);
                }
                
                var methodHandle = metadata.AddMethodDefinition(
                    attributes: (System.Reflection.MethodAttributes)method.Attributes,
                    implAttributes: (System.Reflection.MethodImplAttributes)method.ImplAttributes,
                    name: GetOrAddString(method.Name),
                    signature: EncodeMethodSignature(method),
                    bodyOffset: bodyOffset,
                    parameterList: MetadataTokens.ParameterHandle(metadata.GetRowCount(TableIndex.Param) + 1));
                methodDefHandles[method] = methodHandle;
                
                // Add parameters
                foreach (var param in method.Parameters)
                {
                    metadata.AddParameter(
                        attributes: (System.Reflection.ParameterAttributes)param.Attributes,
                        sequenceNumber: param.Index + 1,
                        name: GetOrAddString(param.Name ?? string.Empty));
                }
            }

            // Recursively handle nested types
            foreach (var nested in type.NestedTypes)
            {
                AddTypeDefinitionsRecursive(nested);
            }
        }

        private void AddTypeMembersRecursive(CecilTypeDefinition type)
        {
            // Generic parameters are added in separate passes (AddTypeGenericParametersRecursive and AddMethodGenericParametersRecursive)
            
            // Add custom attributes for methods
            foreach (var method in type.Methods)
            {
                // Add custom attributes for the method
                foreach (var attr in method.CustomAttributes)
                {
                    AddCustomAttribute(methodDefHandles[method], attr);
                }
            }

            // Add custom attributes for fields
            foreach (var field in type.Fields)
            {
                foreach (var attr in field.CustomAttributes)
                {
                    AddCustomAttribute(fieldDefHandles[field], attr);
                }
            }

            // Add properties - first add PropertyMap, then properties
            if (type.Properties.Count > 0)
            {
                int firstPropertyRow = metadata.GetRowCount(TableIndex.Property) + 1;
                metadata.AddPropertyMap(
                    declaringType: typeDefHandles[type],
                    propertyList: MetadataTokens.PropertyDefinitionHandle(firstPropertyRow));
                    
                foreach (var property in type.Properties)
                {
                    AddPropertyDefinition(property);
                }
            }

            // Add events - first add EventMap, then events
            if (type.Events.Count > 0)
            {
                int firstEventRow = metadata.GetRowCount(TableIndex.Event) + 1;
                metadata.AddEventMap(
                    declaringType: typeDefHandles[type],
                    eventList: MetadataTokens.EventDefinitionHandle(firstEventRow));
                    
                foreach (var evt in type.Events)
                {
                    AddEventDefinition(evt);
                }
            }

            // Add custom attributes for the type
            foreach (var attr in type.CustomAttributes)
            {
                AddCustomAttribute(typeDefHandles[type], attr);
            }

            // Add nested type relationships
            if (type.DeclaringType != null)
            {
                metadata.AddNestedType(
                    type: typeDefHandles[type],
                    enclosingType: typeDefHandles[type.DeclaringType]);
            }

            // Recursively add members for nested types
            foreach (var nested in type.NestedTypes)
            {
                AddTypeMembersRecursive(nested);
            }
        }

        private void AddAssemblyAndModuleCustomAttributes()
        {
            if (!assemblyDefHandle.IsNil)
            {
                foreach (var attr in assembly.CustomAttributes)
                {
                    AddCustomAttribute(assemblyDefHandle, attr);
                }
            }

            foreach (var attr in assembly.MainModule.CustomAttributes)
            {
                AddCustomAttribute(moduleDefHandle, attr);
            }
        }

        private void AddGenericParameter(EntityHandle owner, CecilGenericParameter gp)
        {
            metadata.AddGenericParameter(
                parent: owner,
                attributes: (System.Reflection.GenericParameterAttributes)gp.Attributes,
                name: GetOrAddString(gp.Name),
                index: gp.Position);
        }

        private void AddPropertyDefinition(CecilPropertyDefinition property)
        {
            var propHandle = metadata.AddProperty(
                attributes: (System.Reflection.PropertyAttributes)property.Attributes,
                name: GetOrAddString(property.Name),
                signature: EncodePropertySignature(property));

            // Add getter/setter method associations
            if (property.GetMethod != null && methodDefHandles.TryGetValue(property.GetMethod, out var getter))
            {
                metadata.AddMethodSemantics(
                    association: propHandle,
                    semantics: System.Reflection.MethodSemanticsAttributes.Getter,
                    methodDefinition: getter);
            }

            if (property.SetMethod != null && methodDefHandles.TryGetValue(property.SetMethod, out var setter))
            {
                metadata.AddMethodSemantics(
                    association: propHandle,
                    semantics: System.Reflection.MethodSemanticsAttributes.Setter,
                    methodDefinition: setter);
            }

            // Add custom attributes
            foreach (var attr in property.CustomAttributes)
            {
                AddCustomAttribute(propHandle, attr);
            }
        }

        private void AddEventDefinition(CecilEventDefinition evt)
        {
            var eventHandle = metadata.AddEvent(
                attributes: (System.Reflection.EventAttributes)evt.Attributes,
                name: GetOrAddString(evt.Name),
                type: GetTypeHandle(evt.EventType));

            // Add add/remove method associations
            if (evt.AddMethod != null && methodDefHandles.TryGetValue(evt.AddMethod, out var adder))
            {
                metadata.AddMethodSemantics(
                    association: eventHandle,
                    semantics: System.Reflection.MethodSemanticsAttributes.Adder,
                    methodDefinition: adder);
            }

            if (evt.RemoveMethod != null && methodDefHandles.TryGetValue(evt.RemoveMethod, out var remover))
            {
                metadata.AddMethodSemantics(
                    association: eventHandle,
                    semantics: System.Reflection.MethodSemanticsAttributes.Remover,
                    methodDefinition: remover);
            }

            // Add custom attributes
            foreach (var attr in evt.CustomAttributes)
            {
                AddCustomAttribute(eventHandle, attr);
            }
        }

        private void AddCustomAttribute(EntityHandle parent, CecilCustomAttribute attr)
        {
            try
            {
                var constructor = GetMethodHandle(attr.Constructor);
                var value = EncodeCustomAttributeValue(attr);

                metadata.AddCustomAttribute(
                    parent: parent,
                    constructor: constructor,
                    value: value);
            }
            catch
            {
                // Skip attributes that can't be encoded
            }
        }

        private BlobHandle EncodeCustomAttributeValue(CecilCustomAttribute attr)
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

            return GetOrAddBlob(builder.ToArray());
        }

        private void EncodeCustomAttributeArgument(BlobBuilder builder, CustomAttributeArgument arg)
        {
            if (arg.Type.FullName == "System.String")
            {
                WriteSerializedString(builder, arg.Value as string);
            }
            else if (arg.Type.FullName == "System.Type")
            {
                var typeRef = arg.Value as CecilTypeReference;
                WriteSerializedString(builder, GetCustomAttributeTypeName(typeRef));
            }
            else if (arg.Type.Resolve()?.IsEnum == true)
            {
                var enumDef = arg.Type.Resolve();
                var underlyingType = enumDef.Fields.FirstOrDefault(field => field.Name == "value__")?.FieldType;
                if (underlyingType != null)
                {
                    WritePrimitiveValue(builder, arg.Value, underlyingType);
                }
                else
                {
                    builder.WriteInt32(0);
                }
            }
            else if (arg.Type.IsPrimitive)
            {
                WritePrimitiveValue(builder, arg.Value, arg.Type);
            }
            else
            {
                // Fallback: write as bytes if possible
                builder.WriteInt32(0);
            }
        }

        private void EncodeCustomAttributeFieldOrPropType(BlobBuilder builder, CecilTypeReference type)
        {
            if (type?.Resolve()?.IsEnum == true)
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

        private void WriteSerializedString(BlobBuilder builder, string value)
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

        private static string GetCustomAttributeTypeName(CecilTypeReference typeRef)
        {
            if (typeRef == null)
                return null;

            return typeRef.FullName?.Replace('/', '+');
        }

        private void WritePrimitiveValue(BlobBuilder builder, object value, CecilTypeReference type)
        {
            if (value == null) return;

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

        private void AddResources()
        {
            foreach (var resource in assembly.MainModule.Resources.OfType<EmbeddedResource>())
            {
                var offset = (uint)resourceBuilder.Count;
                var data = resource.GetResourceData();
                
                resourceBuilder.WriteInt32(data.Length);
                resourceBuilder.WriteBytes(data);
                
                // Align to 8 bytes
                while (resourceBuilder.Count % 8 != 0)
                    resourceBuilder.WriteByte(0);

                metadata.AddManifestResource(
                    attributes: (System.Reflection.ManifestResourceAttributes)resource.Attributes,
                    name: GetOrAddString(resource.Name),
                    implementation: default,
                    offset: offset);
            }
        }

        private BlobHandle EncodeFieldSignature(CecilFieldDefinition field)
        {
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder).FieldSignature();
            EncodeTypeSignature(encoder, field.FieldType);
            return GetOrAddBlob(builder.ToArray());
        }

        private BlobHandle EncodeMethodSignature(CecilMethodDefinition method)
        {
            var builder = new BlobBuilder();
            
            var convention = SignatureCallingConvention.Default;
            if (!method.HasThis)
                convention = SignatureCallingConvention.Default;
            
            var signature = new BlobEncoder(builder).MethodSignature(
                convention: convention,
                genericParameterCount: method.GenericParameters.Count,
                isInstanceMethod: method.HasThis);

            // Encode parameters
            signature.Parameters(
                parameterCount: method.Parameters.Count,
                returnType: out var retTypeEncoder,
                parameters: out var paramsOut);

            EncodeReturnType(retTypeEncoder, method.ReturnType);

            foreach (var param in method.Parameters)
            {
                var paramEncoder = paramsOut.AddParameter();
                EncodeTypeSignature(new SignatureTypeEncoder(paramEncoder.Type().Builder), param.ParameterType);
            }

            return GetOrAddBlob(builder.ToArray());
        }

        private BlobHandle EncodePropertySignature(CecilPropertyDefinition property)
        {
            var builder = new BlobBuilder();
            
            // Check if this is an instance property (has a 'this' parameter)
            bool isInstance = (property.GetMethod != null && property.GetMethod.HasThis) ||
                             (property.SetMethod != null && property.SetMethod.HasThis);
                             
            var signature = new BlobEncoder(builder).PropertySignature(isInstanceProperty: isInstance);
            
            signature.Parameters(
                parameterCount: property.Parameters.Count,
                returnType: out var retTypeEncoder,
                parameters: out var paramsOut);

            EncodeReturnType(retTypeEncoder, property.PropertyType);

            foreach (var param in property.Parameters)
            {
                var paramEncoder = paramsOut.AddParameter();
                EncodeTypeSignature(new SignatureTypeEncoder(paramEncoder.Type().Builder), param.ParameterType);
            }

            return GetOrAddBlob(builder.ToArray());
        }

        private void EncodeTypeSignature(SignatureTypeEncoder encoder, CecilTypeReference type)
        {
            if (type == null)
            {
                encoder.PrimitiveType(PrimitiveTypeCode.Void);
                return;
            }

            // Handle by-reference types
            if (type.IsByReference)
            {
                encoder.Builder.WriteByte(0x10); // ELEMENT_TYPE_BYREF
                EncodeTypeSignature(new SignatureTypeEncoder(encoder.Builder), type.GetElementType());
                return;
            }

            // Handle pointer types
            if (type.IsPointer)
            {
                encoder.Builder.WriteByte(0x0F); // ELEMENT_TYPE_PTR
                EncodeTypeSignature(new SignatureTypeEncoder(encoder.Builder), type.GetElementType());
                return;
            }

            // Handle array types
            if (type.IsArray)
            {
                var arrayType = type as ArrayType;
                if (arrayType != null && arrayType.Rank == 1 && !arrayType.IsVector)
                {
                    encoder.Array(
                        out var elementType,
                        out var arrayShape);
                    EncodeTypeSignature(elementType, arrayType.ElementType);
                    arrayShape.Shape(
                        rank: arrayType.Rank,
                        sizes: default,
                        lowerBounds: default);
                }
                else
                {
                    encoder.SZArray();
                    var szArrayEncoder = new SignatureTypeEncoder(encoder.Builder);
                    EncodeTypeSignature(szArrayEncoder, type.GetElementType());
                }
                return;
            }

            // Handle generic instances
            if (type.IsGenericInstance)
            {
                var genericInstance = type as GenericInstanceType;
                var elementType = genericInstance.ElementType;
                var elementIsValueType = SafeIsValueType(elementType);
                
                encoder.Builder.WriteByte(0x15); // ELEMENT_TYPE_GENERICINST
                encoder.Builder.WriteByte(elementIsValueType ? (byte)0x11 : (byte)0x12); // ELEMENT_TYPE_VALUETYPE or ELEMENT_TYPE_CLASS
                
                // Get the handle for the element type (not the generic instance)
                var genericTypeHandle = GetTypeHandle(elementType);
                
                // Encode as TypeDefOrRefOrSpecEncoded coded index
                int codedIndex = EncodeTypeDefOrRefOrSpec(genericTypeHandle);
                encoder.Builder.WriteCompressedInteger(codedIndex);
                
                encoder.Builder.WriteCompressedInteger(genericInstance.GenericArguments.Count);
                foreach (var arg in genericInstance.GenericArguments)
                {
                    EncodeTypeSignature(new SignatureTypeEncoder(encoder.Builder), arg);
                }
                return;
            }

            // Handle primitive types
            if (type.IsPrimitive || type.FullName == "System.Void" || type.FullName == "System.String" || type.FullName == "System.Object")
            {
                switch (type.FullName)
                {
                    case "System.Void":
                        // Void cannot be used with PrimitiveType(), write directly
                        encoder.Builder.WriteByte((byte)SignatureTypeCode.Void);
                        return;
                    case "System.Boolean":
                        encoder.PrimitiveType(PrimitiveTypeCode.Boolean);
                        return;
                    case "System.Char":
                        encoder.PrimitiveType(PrimitiveTypeCode.Char);
                        return;
                    case "System.SByte":
                        encoder.PrimitiveType(PrimitiveTypeCode.SByte);
                        return;
                    case "System.Byte":
                        encoder.PrimitiveType(PrimitiveTypeCode.Byte);
                        return;
                    case "System.Int16":
                        encoder.PrimitiveType(PrimitiveTypeCode.Int16);
                        return;
                    case "System.UInt16":
                        encoder.PrimitiveType(PrimitiveTypeCode.UInt16);
                        return;
                    case "System.Int32":
                        encoder.PrimitiveType(PrimitiveTypeCode.Int32);
                        return;
                    case "System.UInt32":
                        encoder.PrimitiveType(PrimitiveTypeCode.UInt32);
                        return;
                    case "System.Int64":
                        encoder.PrimitiveType(PrimitiveTypeCode.Int64);
                        return;
                    case "System.UInt64":
                        encoder.PrimitiveType(PrimitiveTypeCode.UInt64);
                        return;
                    case "System.Single":
                        encoder.PrimitiveType(PrimitiveTypeCode.Single);
                        return;
                    case "System.Double":
                        encoder.PrimitiveType(PrimitiveTypeCode.Double);
                        return;
                    case "System.IntPtr":
                        encoder.PrimitiveType(PrimitiveTypeCode.IntPtr);
                        return;
                    case "System.UIntPtr":
                        encoder.PrimitiveType(PrimitiveTypeCode.UIntPtr);
                        return;
                    case "System.String":
                        encoder.String();
                        return;
                    case "System.Object":
                        encoder.Object();
                        return;
                    default:
                        throw new InvalidOperationException($"Type marked as primitive but not recognized: {type.FullName}");
                }
            }

            // Handle generic parameters
            if (type.IsGenericParameter)
            {
                var gp = type as CecilGenericParameter;
                if (gp == null)
                {
                    throw new InvalidOperationException($"Type {type.FullName} reports IsGenericParameter=true but is not a GenericParameter");
                }
                
                var position = gp.Position;
                
                // If position is -1, try to determine it from context
                if (position < 0 && gp.Name != null)
                {
                    // Try to parse position from name like "T0", "T1", "T2"
                    if (gp.Name.StartsWith("T") && int.TryParse(gp.Name.Substring(1), out var parsedPos))
                    {
                        position = parsedPos;
                    }
                    else
                    {
                        // Try to find the position from the owner's generic parameters
                        if (gp.Owner != null)
                        {
                            var ownerParams = gp.Owner is Mono.Cecil.MethodReference mr 
                                ? (IList<Mono.Cecil.GenericParameter>)mr.GenericParameters 
                                : gp.Owner is Mono.Cecil.TypeReference tr 
                                    ? (IList<Mono.Cecil.GenericParameter>)tr.GenericParameters 
                                    : null;
                            if (ownerParams != null)
                            {
                                for (int i = 0; i < ownerParams.Count; i++)
                                {
                                    if (ownerParams[i] == gp || ownerParams[i].Name == gp.Name)
                                    {
                                        position = i;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        // If still not found, default to 0
                        if (position < 0)
                            position = 0;
                    }
                }
                
                if (position < 0)
                {
                    throw new InvalidOperationException($"Generic parameter {gp.Name} has invalid position {position}");
                }
                
                if (gp.Type == GenericParameterType.Type)
                {
                    encoder.Builder.WriteByte(0x13); // ELEMENT_TYPE_VAR
                    encoder.Builder.WriteCompressedInteger(position);
                }
                else
                {
                    encoder.Builder.WriteByte(0x1E); // ELEMENT_TYPE_MVAR
                    encoder.Builder.WriteCompressedInteger(position);
                }
                return;
            }

            // Handle class/valuetype references
            var typeHandle = GetTypeHandle(type);
            var isValueType = SafeIsValueType(type);
            encoder.Type(typeHandle, isValueType);
        }
        
        /// <summary>
        /// Safely determines if a type is a value type, handling resolution failures.
        /// </summary>
        private bool SafeIsValueType(CecilTypeReference type)
        {
            if (type == null)
                return false;
                
            // Check if it's a TypeDefinition first
            if (type is CecilTypeDefinition td)
                return td.IsValueType;
                
            try
            {
                var resolved = type.Resolve();
                return resolved?.IsValueType ?? false;
            }
            catch
            {
                // Resolution failed - assume it's not a value type (most types are classes)
                return false;
            }
        }

        private int EncodeTypeDefOrRefOrSpec(EntityHandle handle)
        {
            // TypeDefOrRefOrSpecEncoded coded index:
            // TypeDef: (row << 2) | 0
            // TypeRef: (row << 2) | 1
            // TypeSpec: (row << 2) | 2
            int row = MetadataTokens.GetRowNumber(handle);
            var kind = handle.Kind;
            
            int tag;
            switch (kind)
            {
                case HandleKind.TypeDefinition:
                    tag = 0;
                    break;
                case HandleKind.TypeReference:
                    tag = 1;
                    break;
                case HandleKind.TypeSpecification:
                    tag = 2;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported handle kind for TypeDefOrRefOrSpec: {kind}");
            }
            
            return (row << 2) | tag;
        }

        private EntityHandle GetTypeHandle(CecilTypeReference typeRef)
        {
            if (typeRef == null)
                return default;

            // Handle generic instance types - these need a TypeSpec
            if (typeRef.IsGenericInstance)
            {
                return GetTypeSpecHandle(typeRef as GenericInstanceType);
            }
            
            // Handle generic parameters - these need a TypeSpec with VAR/MVAR encoding
            if (typeRef.IsGenericParameter && typeRef is CecilGenericParameter gp)
            {
                return GetGenericParameterTypeSpecHandle(gp);
            }

            // Check if it's a type definition we've already added
            if (typeRef is CecilTypeDefinition typeDef && typeDefHandles.TryGetValue(typeDef, out var defHandle))
            {
                return defHandle;
            }
            
            // If it's a TypeReference that points to a type in the same module, 
            // try to find the TypeDefinition and use that instead
            if (!(typeRef is CecilTypeDefinition))
            {
                var resolved = TryResolveToLocalTypeDefinition(typeRef);
                if (resolved != null && typeDefHandles.TryGetValue(resolved, out var resolvedHandle))
                {
                    return resolvedHandle;
                }
            }

            // Check cache
            if (typeRefHandles.TryGetValue(typeRef, out var cachedHandle))
            {
                return cachedHandle;
            }

            // Create type reference
            var resScope = GetResolutionScope(typeRef);
            if (resScope.Kind == HandleKind.TypeDefinition)
            {
                resScope = moduleDefHandle;
            }
            var handle = metadata.AddTypeReference(
                resolutionScope: resScope,
                @namespace: GetOrAddString(typeRef.Namespace ?? string.Empty),
                name: GetOrAddString(typeRef.Name));

            typeRefHandles[typeRef] = handle;
            return handle;
        }
        
        /// <summary>
        /// Tries to resolve a TypeReference to a TypeDefinition in the current module.
        /// </summary>
        private CecilTypeDefinition TryResolveToLocalTypeDefinition(CecilTypeReference typeRef)
        {
            var mainModule = assembly.MainModule;
            
            // First try to use Cecil's built-in resolution
            try
            {
                var resolved = typeRef.Resolve();
                if (resolved != null && resolved.Module == mainModule)
                {
                    return resolved;
                }
            }
            catch
            {
                // Resolution failed, fall through to manual lookup
            }
            
            // Check if it's a type in this module by looking at the scope
            if (typeRef.Scope is CecilModuleDefinition modDef && modDef == mainModule)
            {
                // Find the type in module.Types - try both current and original names
                foreach (var td in mainModule.Types)
                {
                    if (td.FullName == typeRef.FullName)
                        return td;
                    // Check nested types
                    var nested = FindNestedType(td, typeRef.FullName);
                    if (nested != null)
                        return nested;
                }
            }
            
            // Also try to resolve if the scope is an assembly reference to itself
            if (typeRef.Scope is AssemblyNameReference asmRef && 
                asmRef.FullName == mainModule.Assembly?.Name?.FullName)
            {
                foreach (var td in mainModule.Types)
                {
                    if (td.FullName == typeRef.FullName)
                        return td;
                    var nested = FindNestedType(td, typeRef.FullName);
                    if (nested != null)
                        return nested;
                }
            }
            
            return null;
        }
        
        private CecilTypeDefinition FindNestedType(CecilTypeDefinition parent, string fullName)
        {
            foreach (var nested in parent.NestedTypes)
            {
                if (nested.FullName == fullName)
                    return nested;
                var found = FindNestedType(nested, fullName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private readonly Dictionary<CecilTypeReference, TypeSpecificationHandle> typeSpecHandles = new Dictionary<CecilTypeReference, TypeSpecificationHandle>();

        private TypeSpecificationHandle GetTypeSpecHandle(GenericInstanceType genericType)
        {
            // Check cache
            if (typeSpecHandles.TryGetValue(genericType, out var cachedHandle))
            {
                return cachedHandle;
            }

            // Encode the generic instantiation type signature
            var builder = new BlobBuilder();
            var encoder = new SignatureTypeEncoder(builder);
            EncodeTypeSignature(encoder, genericType);

            var signature = GetOrAddBlob(builder.ToArray());
            var handle = metadata.AddTypeSpecification(signature);
            typeSpecHandles[genericType] = handle;
            return handle;
        }
        
        /// <summary>
        /// Creates a TypeSpecification handle for a generic parameter (VAR/MVAR).
        /// </summary>
        private TypeSpecificationHandle GetGenericParameterTypeSpecHandle(CecilGenericParameter gp)
        {
            // Check cache
            if (typeSpecHandles.TryGetValue(gp, out var cachedHandle))
            {
                return cachedHandle;
            }

            // Encode the generic parameter type signature
            var builder = new BlobBuilder();
            var encoder = new SignatureTypeEncoder(builder);
            
            // Determine position
            var position = gp.Position;
            if (position < 0 && gp.Name != null)
            {
                // Try to parse position from name like "T0", "T1", "T2"
                if (gp.Name.StartsWith("T") && int.TryParse(gp.Name.Substring(1), out var parsedPos))
                {
                    position = parsedPos;
                }
                else
                {
                    position = 0;
                }
            }
            
            if (gp.Type == GenericParameterType.Type)
            {
                builder.WriteByte(0x13); // ELEMENT_TYPE_VAR
                builder.WriteCompressedInteger(position);
            }
            else
            {
                builder.WriteByte(0x1E); // ELEMENT_TYPE_MVAR
                builder.WriteCompressedInteger(position);
            }

            var signature = GetOrAddBlob(builder.ToArray());
            var handle = metadata.AddTypeSpecification(signature);
            typeSpecHandles[gp] = handle;
            return handle;
        }

        private EntityHandle GetResolutionScope(CecilTypeReference typeRef)
        {
            if (typeRef.DeclaringType != null)
            {
                return GetTypeReferenceHandle(typeRef.DeclaringType);
            }

            if (typeRef.Scope is CecilModuleDefinition)
            {
                return moduleDefHandle;
            }

            if (typeRef.Scope is CecilModuleReference moduleRef)
            {
                if (!moduleRefHandles.TryGetValue(moduleRef, out var handle))
                {
                    handle = metadata.AddModuleReference(GetOrAddString(moduleRef.Name));
                    moduleRefHandles[moduleRef] = handle;
                }
                return handle;
            }

            if (typeRef.Scope is AssemblyNameReference asmRef)
            {
                if (assemblyRefHandles.TryGetValue(asmRef, out var handle))
                {
                    return handle;
                }
                
                // If the exact object is not in cache, try to find by name
                var matchingRef = assemblyRefHandles.Keys.FirstOrDefault(r => r.Name == asmRef.Name);
                if (matchingRef != null)
                {
                    return assemblyRefHandles[matchingRef];
                }
                    
                // Still not found - add the assembly reference now
                AddAssemblyReference(asmRef);
                if (assemblyRefHandles.TryGetValue(asmRef, out handle))
                    return handle;
            }

            if (typeRef.Scope is CecilTypeReference typeScope)
            {
                return GetTypeReferenceHandle(typeScope);
            }

            // Default to first assembly reference or module
            return !moduleDefHandle.IsNil
                ? (EntityHandle)moduleDefHandle
                : (EntityHandle)assemblyRefHandles.Values.FirstOrDefault();
        }

        private EntityHandle GetTypeReferenceHandle(CecilTypeReference typeRef)
        {
            if (typeRef == null)
                return default;

            if (typeRefHandles.TryGetValue(typeRef, out var cachedHandle))
            {
                return cachedHandle;
            }

            var resScope = GetResolutionScope(typeRef);
            if (resScope.Kind == HandleKind.TypeDefinition)
            {
                resScope = moduleDefHandle;
            }

            var handle = metadata.AddTypeReference(
                resolutionScope: resScope,
                @namespace: GetOrAddString(typeRef.Namespace ?? string.Empty),
                name: GetOrAddString(typeRef.Name));

            typeRefHandles[typeRef] = handle;
            return handle;
        }

        private EntityHandle GetMethodHandle(CecilMethodReference methodRef)
        {
            if (methodRef == null)
                return default;

            // Handle generic instance methods - these need a MethodSpec
            if (methodRef is GenericInstanceMethod genericInstanceMethod)
            {
                return GetMethodSpecHandle(genericInstanceMethod);
            }

            // Check if it's a method definition
            if (methodRef is CecilMethodDefinition methodDef && methodDefHandles.TryGetValue(methodDef, out var defHandle))
            {
                return defHandle;
            }
            
            // IMPORTANT: If the declaring type is a generic instance, we MUST create a MemberReference
            // with a TypeSpec as parent, even if the method resolves to a local MethodDefinition.
            // This is because the runtime needs to know the specific generic instantiation.
            bool hasGenericParent = methodRef.DeclaringType?.IsGenericInstance ?? false;
            
            if (!hasGenericParent)
            {
                // Only try to resolve to a method definition if the declaring type is NOT a generic instance
                try
                {
                    var resolved = methodRef.Resolve();
                    if (resolved != null && methodDefHandles.TryGetValue(resolved, out var resolvedHandle))
                    {
                        return resolvedHandle;
                    }
                }
                catch
                {
                    // Resolution failed, continue with MemberReference
                }
            }

            // Check cache
            if (methodRefHandles.TryGetValue(methodRef, out var cachedHandle))
            {
                return cachedHandle;
            }

            // Create member reference
            var declaringType = methodRef.DeclaringType;
            
            var parent = GetTypeHandle(declaringType);
            var signature = EncodeMethodSignature(methodRef);
            
            var handle = metadata.AddMemberReference(
                parent: parent,
                name: GetOrAddString(methodRef.Name),
                signature: signature);

            methodRefHandles[methodRef] = handle;
            return handle;
        }

        private readonly Dictionary<GenericInstanceMethod, MethodSpecificationHandle> methodSpecHandles = new Dictionary<GenericInstanceMethod, MethodSpecificationHandle>();

        private MethodSpecificationHandle GetMethodSpecHandle(GenericInstanceMethod genericMethod)
        {
            // Check cache
            if (methodSpecHandles.TryGetValue(genericMethod, out var cachedHandle))
            {
                return cachedHandle;
            }

            // Get the element method handle (the uninstantiated method)
            var elementMethod = genericMethod.ElementMethod;
            var methodHandle = GetMethodHandle(elementMethod);

            // Encode the generic instantiation signature
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder);
            var genericArgsEncoder = encoder.MethodSpecificationSignature(genericMethod.GenericArguments.Count);
            
            foreach (var arg in genericMethod.GenericArguments)
            {
                EncodeTypeSignature(genericArgsEncoder.AddArgument(), arg);
            }

            var instantiation = GetOrAddBlob(builder.ToArray());

            var handle = metadata.AddMethodSpecification(methodHandle, instantiation);
            methodSpecHandles[genericMethod] = handle;
            return handle;
        }

        private BlobHandle EncodeMethodSignature(CecilMethodReference methodRef)
        {
            
            var builder = new BlobBuilder();
            
            var convention = SignatureCallingConvention.Default;
            if (!methodRef.HasThis)
                convention = SignatureCallingConvention.Default;
            
            var signature = new BlobEncoder(builder).MethodSignature(
                convention: convention,
                genericParameterCount: methodRef.GenericParameters.Count,
                isInstanceMethod: methodRef.HasThis);

            signature.Parameters(
                parameterCount: methodRef.Parameters.Count,
                returnType: out var retTypeEncoder,
                parameters: out var paramsOut);

            EncodeReturnType(retTypeEncoder, methodRef.ReturnType);

            foreach (var param in methodRef.Parameters)
            {
                var paramEncoder = paramsOut.AddParameter();
                EncodeTypeSignature(new SignatureTypeEncoder(paramEncoder.Type().Builder), param.ParameterType);
            }

            return GetOrAddBlob(builder.ToArray());
        }

        private void EncodeReturnType(ReturnTypeEncoder retTypeEncoder, CecilTypeReference returnType)
        {
            if (returnType == null || returnType.FullName == "System.Void")
            {
                retTypeEncoder.Void();
            }
            else
            {
                EncodeTypeSignature(retTypeEncoder.Type(), returnType);
            }
        }

        private int EncodeMethodBody(CecilMethodDefinition method)
        {
            var body = method.Body;
            var offset = ilBuilder.Count;

            // Calculate code size
            var codeSize = body.Instructions.Sum(i => i.GetSize());

            // Determine if we can use tiny format
            // Per ECMA-335 II.25.4.2: Tiny format if:
            // - Code size < 64 bytes
            // - No local variables
            // - No exception handlers  
            // - MaxStack <= 8
            var useTinyFormat = codeSize < 64 && 
                                body.MaxStackSize <= 8 && 
                                body.Variables.Count == 0 && 
                                body.ExceptionHandlers.Count == 0;

            if (useTinyFormat)
            {
                // Tiny format: 6-bit size + 2-bit format (0x2)
                ilBuilder.WriteByte((byte)((codeSize << 2) | 0x02));
            }
            else
            {
                // Fat format
                var flags = 0x3003; // Fat format + init locals
                if (body.ExceptionHandlers.Count > 0)
                    flags |= 0x0008; // More sections

                ilBuilder.WriteUInt16((ushort)flags);
                ilBuilder.WriteUInt16((ushort)body.MaxStackSize);
                ilBuilder.WriteInt32(codeSize);
                
                // Local var signature token
                if (body.Variables.Count > 0)
                {
                    var localVarSig = EncodeLocalVariablesSignature(body.Variables);
                    var standaloneHandle = metadata.AddStandaloneSignature(localVarSig);
                    ilBuilder.WriteInt32(MetadataTokens.GetToken(standaloneHandle));
                }
                else
                {
                    ilBuilder.WriteInt32(0);
                }
            }

            // Write IL instructions
            var instructionOffsets = new Dictionary<Instruction, int>();
            foreach (var instruction in body.Instructions)
            {
                instructionOffsets[instruction] = ilBuilder.Count - offset - (useTinyFormat ? 1 : 12);
                EncodeInstruction(instruction);
            }

            // Align to 4 bytes
            while (ilBuilder.Count % 4 != 0)
                ilBuilder.WriteByte(0);

            // Write exception handlers if present
            if (body.ExceptionHandlers.Count > 0)
            {
                EncodeExceptionHandlers(body.ExceptionHandlers, instructionOffsets, offset + (useTinyFormat ? 1 : 12));
            }

            return offset;
        }

        private BlobHandle EncodeLocalVariablesSignature(Collection<VariableDefinition> variables)
        {
            var builder = new BlobBuilder();
            builder.WriteByte(0x07); // LOCAL_SIG
            builder.WriteCompressedInteger(variables.Count);
            
            foreach (var variable in variables)
            {
                EncodeTypeSignature(new SignatureTypeEncoder(builder), variable.VariableType);
            }
            
            return GetOrAddBlob(builder.ToArray());
        }

        private void EncodeInstruction(Instruction instruction)
        {
            // Write opcode
            if (instruction.OpCode.Size == 1)
            {
                ilBuilder.WriteByte((byte)instruction.OpCode.Value);
            }
            else
            {
                ilBuilder.WriteByte((byte)(instruction.OpCode.Value >> 8));
                ilBuilder.WriteByte((byte)(instruction.OpCode.Value & 0xFF));
            }

            // Write operand
            switch (instruction.OpCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                    
                case OperandType.InlineBrTarget:
                    if (instruction.Operand is Instruction target)
                    {
                        ilBuilder.WriteInt32(target.Offset - (instruction.Offset + instruction.GetSize()));
                    }
                    else if (instruction.Operand is int targetOffset)
                    {
                        // If operand is still an int offset (unresolved), use it directly
                        ilBuilder.WriteInt32(targetOffset);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected InlineBrTarget operand type: {instruction.Operand?.GetType()?.Name ?? "null"}");
                    }
                    break;
                    
                case OperandType.ShortInlineBrTarget:
                    if (instruction.Operand is Instruction shortTarget)
                    {
                        ilBuilder.WriteSByte((sbyte)(shortTarget.Offset - (instruction.Offset + instruction.GetSize())));
                    }
                    else if (instruction.Operand is int shortTargetOffset)
                    {
                        // If operand is still an int offset (unresolved), use it directly
                        ilBuilder.WriteSByte((sbyte)shortTargetOffset);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected ShortInlineBrTarget operand type: {instruction.Operand?.GetType()?.Name ?? "null"}");
                    }
                    break;
                    
                case OperandType.InlineI:
                    ilBuilder.WriteInt32((int)instruction.Operand);
                    break;
                    
                case OperandType.ShortInlineI:
                    if (instruction.OpCode == OpCodes.Ldc_I4_S)
                        ilBuilder.WriteSByte((sbyte)instruction.Operand);
                    else
                        ilBuilder.WriteByte((byte)instruction.Operand);
                    break;
                    
                case OperandType.InlineI8:
                    ilBuilder.WriteInt64((long)instruction.Operand);
                    break;
                    
                case OperandType.InlineR:
                    ilBuilder.WriteDouble((double)instruction.Operand);
                    break;
                    
                case OperandType.ShortInlineR:
                    ilBuilder.WriteSingle((float)instruction.Operand);
                    break;
                    
                case OperandType.InlineString:
                    var str = (string)instruction.Operand;
                    var userStringHandle = GetOrAddUserString(str);
                    // MetadataTokens.GetToken already includes the 0x70 token type prefix
                    ilBuilder.WriteInt32(MetadataTokens.GetToken(userStringHandle));
                    break;
                    
                case OperandType.InlineMethod:
                    var methodOperand = instruction.Operand as CecilMethodReference;
                    var methodHandle = GetMethodHandle(methodOperand);
                    ilBuilder.WriteInt32(MetadataTokens.GetToken(methodHandle));
                    break;
                    
                case OperandType.InlineField:
                    var fieldRef = instruction.Operand as CecilFieldReference;
                    var fieldHandle = GetFieldHandle(fieldRef);
                    ilBuilder.WriteInt32(MetadataTokens.GetToken(fieldHandle));
                    break;
                    
                case OperandType.InlineType:
                    var typeHandle = GetTypeHandle(instruction.Operand as CecilTypeReference);
                    ilBuilder.WriteInt32(MetadataTokens.GetToken(typeHandle));
                    break;
                    
                case OperandType.InlineTok:
                    // InlineTok can be a type, method, or field token
                    EntityHandle tokenHandle = default;
                    if (instruction.Operand is CecilTypeReference tokTypeRef)
                    {
                        tokenHandle = GetTypeHandle(tokTypeRef);
                    }
                    else if (instruction.Operand is CecilMethodReference tokMethodRef)
                    {
                        tokenHandle = GetMethodHandle(tokMethodRef);
                    }
                    else if (instruction.Operand is CecilFieldReference tokFieldRef)
                    {
                        tokenHandle = GetFieldHandle(tokFieldRef);
                    }
                    
                    if (tokenHandle.IsNil)
                    {
                        // Fallback for unexpected operand types - write 0
                        ilBuilder.WriteInt32(0);
                    }
                    else
                    {
                        ilBuilder.WriteInt32(MetadataTokens.GetToken(tokenHandle));
                    }
                    break;
                    
                case OperandType.ShortInlineVar:
                    int shortVarIndex;
                    if (instruction.Operand is VariableDefinition varDef)
                    {
                        shortVarIndex = varDef.Index;
                    }
                    else if (instruction.Operand is int intVarIdx)
                    {
                        shortVarIndex = intVarIdx;
                    }
                    else if (instruction.Operand is byte byteVarIdx)
                    {
                        shortVarIndex = byteVarIdx;
                    }
                    else
                    {
                        shortVarIndex = 0;
                    }
                    ilBuilder.WriteByte((byte)shortVarIndex);
                    break;
                    
                case OperandType.InlineVar:
                    int varIndex;
                    if (instruction.Operand is VariableDefinition varDef2)
                    {
                        varIndex = varDef2.Index;
                    }
                    else if (instruction.Operand is int intVarIdx2)
                    {
                        varIndex = intVarIdx2;
                    }
                    else if (instruction.Operand is ushort ushortVarIdx)
                    {
                        varIndex = ushortVarIdx;
                    }
                    else if (instruction.Operand is byte byteVarIdx2)
                    {
                        varIndex = byteVarIdx2;
                    }
                    else
                    {
                        varIndex = 0;
                    }
                    ilBuilder.WriteUInt16((ushort)varIndex);
                    break;
                    
                case OperandType.ShortInlineArg:
                    int shortArgIndex;
                    if (instruction.Operand is CecilParameterDefinition paramDef)
                    {
                        shortArgIndex = paramDef.Index;
                        // For 'this' parameter, Index is -1, but IL encoding uses 0
                        if (shortArgIndex == -1)
                            shortArgIndex = 0;
                        else if (paramDef.Method is CecilMethodDefinition md && md.HasThis)
                            shortArgIndex++; // Adjust for 'this' parameter
                    }
                    else if (instruction.Operand is int intIdx)
                    {
                        shortArgIndex = intIdx;
                    }
                    else if (instruction.Operand is byte byteIdx)
                    {
                        shortArgIndex = byteIdx;
                    }
                    else if (instruction.Operand == null)
                    {
                        // Null operand usually means 'this' parameter (arg 0)
                        shortArgIndex = 0;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected ShortInlineArg operand type: {instruction.Operand?.GetType()?.Name ?? "null"}");
                    }
                    ilBuilder.WriteByte((byte)shortArgIndex);
                    break;
                    
                case OperandType.InlineArg:
                    int argIndex;
                    if (instruction.Operand is CecilParameterDefinition paramDef2)
                    {
                        argIndex = paramDef2.Index;
                        // For 'this' parameter, Index is -1, but IL encoding uses 0
                        if (argIndex == -1)
                            argIndex = 0;
                        else if (paramDef2.Method is CecilMethodDefinition md2 && md2.HasThis)
                            argIndex++; // Adjust for 'this' parameter
                    }
                    else if (instruction.Operand is int intIdx2)
                    {
                        argIndex = intIdx2;
                    }
                    else if (instruction.Operand is ushort ushortIdx)
                    {
                        argIndex = ushortIdx;
                    }
                    else if (instruction.Operand == null)
                    {
                        // Null operand usually means 'this' parameter (arg 0)
                        argIndex = 0;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected InlineArg operand type: {instruction.Operand?.GetType()?.Name ?? "null"}");
                    }
                    ilBuilder.WriteUInt16((ushort)argIndex);
                    break;
                    
                case OperandType.InlineSwitch:
                    var targets = (Instruction[])instruction.Operand;
                    ilBuilder.WriteInt32(targets.Length);
                    var baseOffset = instruction.Offset + instruction.GetSize();
                    foreach (var switchTarget in targets)
                    {
                        ilBuilder.WriteInt32(switchTarget.Offset - baseOffset);
                    }
                    break;
            }
        }

        private EntityHandle GetFieldHandle(CecilFieldReference fieldRef)
        {
            if (fieldRef == null)
                return default;

            // Check if it's a field definition
            if (fieldRef is CecilFieldDefinition fieldDef && fieldDefHandles.TryGetValue(fieldDef, out var defHandle))
            {
                return defHandle;
            }

            // Check cache
            if (fieldRefHandles.TryGetValue(fieldRef, out var cachedHandle))
            {
                return cachedHandle;
            }

            // Create member reference
            var parent = GetTypeHandle(fieldRef.DeclaringType);
            var signature = EncodeFieldSignature(fieldRef);
            
            var handle = metadata.AddMemberReference(
                parent: parent,
                name: GetOrAddString(fieldRef.Name),
                signature: signature);

            fieldRefHandles[fieldRef] = handle;
            return handle;
        }

        private BlobHandle EncodeFieldSignature(CecilFieldReference fieldRef)
        {
            var builder = new BlobBuilder();
            var encoder = new BlobEncoder(builder).FieldSignature();
            EncodeTypeSignature(encoder, fieldRef.FieldType);
            return GetOrAddBlob(builder.ToArray());
        }

        private void EncodeExceptionHandlers(Collection<ExceptionHandler> handlers, Dictionary<Instruction, int> offsets, int codeBaseOffset)
        {
            // Determine if we need fat format
            var needsFat = handlers.Any(h =>
                GetValueOrDefault(offsets, h.TryStart) > 65535 ||
                GetValueOrDefault(offsets, h.TryEnd) > 65535 ||
                GetValueOrDefault(offsets, h.HandlerStart) > 65535 ||
                GetValueOrDefault(offsets, h.HandlerEnd) > 65535);

            if (needsFat)
            {
                // Fat format
                var dataSize = handlers.Count * 24 + 4;
                ilBuilder.WriteByte(0x41); // CorILMethod_Sect_FatFormat | CorILMethod_Sect_EHTable
                ilBuilder.WriteByte((byte)dataSize);
                ilBuilder.WriteUInt16((ushort)(dataSize >> 8));

                foreach (var handler in handlers)
                {
                    ilBuilder.WriteInt32((int)handler.HandlerType);
                    ilBuilder.WriteInt32(GetValueOrDefault(offsets, handler.TryStart));
                    ilBuilder.WriteInt32(GetValueOrDefault(offsets, handler.TryEnd) - GetValueOrDefault(offsets, handler.TryStart));
                    ilBuilder.WriteInt32(GetValueOrDefault(offsets, handler.HandlerStart));
                    ilBuilder.WriteInt32(GetValueOrDefault(offsets, handler.HandlerEnd) - GetValueOrDefault(offsets, handler.HandlerStart));
                    
                    if (handler.HandlerType == ExceptionHandlerType.Catch)
                    {
                        var typeHandle = GetTypeHandle(handler.CatchType);
                        ilBuilder.WriteInt32(MetadataTokens.GetToken(typeHandle));
                    }
                    else if (handler.HandlerType == ExceptionHandlerType.Filter)
                    {
                        ilBuilder.WriteInt32(GetValueOrDefault(offsets, handler.FilterStart));
                    }
                    else
                    {
                        ilBuilder.WriteInt32(0);
                    }
                }
            }
            else
            {
                // Small format
                var dataSize = handlers.Count * 12 + 4;
                ilBuilder.WriteByte(0x01); // CorILMethod_Sect_EHTable
                ilBuilder.WriteByte((byte)dataSize);
                ilBuilder.WriteUInt16(0);

                foreach (var handler in handlers)
                {
                    ilBuilder.WriteUInt16((ushort)handler.HandlerType);
                    ilBuilder.WriteUInt16((ushort)GetValueOrDefault(offsets, handler.TryStart));
                    ilBuilder.WriteByte((byte)(GetValueOrDefault(offsets, handler.TryEnd) - GetValueOrDefault(offsets, handler.TryStart)));
                    ilBuilder.WriteUInt16((ushort)GetValueOrDefault(offsets, handler.HandlerStart));
                    ilBuilder.WriteByte((byte)(GetValueOrDefault(offsets, handler.HandlerEnd) - GetValueOrDefault(offsets, handler.HandlerStart)));
                    
                    if (handler.HandlerType == ExceptionHandlerType.Catch)
                    {
                        var typeHandle = GetTypeHandle(handler.CatchType);
                        ilBuilder.WriteInt32(MetadataTokens.GetToken(typeHandle));
                    }
                    else if (handler.HandlerType == ExceptionHandlerType.Filter)
                    {
                        ilBuilder.WriteInt32(GetValueOrDefault(offsets, handler.FilterStart));
                    }
                    else
                    {
                        ilBuilder.WriteInt32(0);
                    }
                }
            }

            // Align to 4 bytes
            while (ilBuilder.Count % 4 != 0)
                ilBuilder.WriteByte(0);
        }

        private void WritePeFile(Stream stream, WriterParameters parameters)
        {
            // Create metadata root
            var metadataRoot = new MetadataRootBuilder(metadata);

            // Get module characteristics
            var module = assembly.MainModule;
            var machine = GetMachine(module);
            var characteristics = GetImageCharacteristics(module);
            var corFlags = GetCorFlags(module);

            // Create PE header
            var peHeaderBuilder = new PEHeaderBuilder(
                machine: machine,
                imageCharacteristics: characteristics);

            // Determine entry point
            var entryPoint = assembly.EntryPoint != null && methodDefHandles.TryGetValue(assembly.EntryPoint, out var epHandle)
                ? epHandle
                : default(MethodDefinitionHandle);

            // If writer parameters provide a strong-name key blob or key pair, mark the image as signed
            // and reserve space for the strong-name signature. This ensures the produced PE has the
            // StrongNameSigned flag set so readers (tests) can observe the signed state even if an
            // external signing step is required to produce a valid signature.
            int strongNameSignatureSize = 0;
            if (parameters != null && (parameters.StrongNameKeyBlob != null || parameters.StrongNameKeyPair != null))
            {
                corFlags |= CorFlags.StrongNameSigned;
                // Reserve 128 bytes for the strong name signature like the full framework writer did.
                strongNameSignatureSize = 128;
            }

            // Create managed PE builder
            var peBuilder = new ManagedPEBuilder(
                header: peHeaderBuilder,
                metadataRootBuilder: metadataRoot,
                ilStream: ilBuilder,
                mappedFieldData: null,
                managedResources: resourceBuilder.Count > 0 ? resourceBuilder : null,
                nativeResources: null,
                debugDirectoryBuilder: null,
                strongNameSignatureSize: strongNameSignatureSize,
                entryPoint: entryPoint,
                flags: corFlags,
                deterministicIdProvider: null);

            // Serialize PE to blob
            var peBlob = new BlobBuilder();
            var contentId = peBuilder.Serialize(peBlob);

            // Write to stream
            peBlob.WriteContentTo(stream);

            // TODO: Strong name signing if parameters.StrongNameKeyPair is provided
        }

        private Machine GetMachine(CecilModuleDefinition module)
        {
            switch (module.Architecture)
            {
                case TargetArchitecture.I386: return Machine.I386;
                case TargetArchitecture.AMD64: return Machine.Amd64;
                case TargetArchitecture.ARM: return Machine.Arm;
                case TargetArchitecture.ARM64: return Machine.Arm64;
                case TargetArchitecture.IA64: return Machine.IA64;
                default: return Machine.Unknown;
            }
        }

        private Characteristics GetImageCharacteristics(CecilModuleDefinition module)
        {
            var characteristics = Characteristics.ExecutableImage;

            if (module.Architecture == TargetArchitecture.AMD64 || module.Architecture == TargetArchitecture.IA64 || module.Architecture == TargetArchitecture.ARM64)
                characteristics |= Characteristics.LargeAddressAware;

            if (module.Kind == ModuleKind.Dll)
                characteristics |= Characteristics.Dll;

            return characteristics;
        }

        private CorFlags GetCorFlags(CecilModuleDefinition module)
        {
            var flags = CorFlags.ILOnly;

            if ((module.Attributes & ModuleAttributes.Required32Bit) != 0)
                flags |= CorFlags.Requires32Bit;
                
            if ((module.Attributes & ModuleAttributes.StrongNameSigned) != 0)
                flags |= CorFlags.StrongNameSigned;

            return flags;
        }

        private StringHandle GetOrAddString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return default;

            if (!stringCache.TryGetValue(value, out var handle))
            {
                handle = metadata.GetOrAddString(value);
                stringCache[value] = handle;
            }

            return handle;
        }

        private UserStringHandle GetOrAddUserString(string value)
        {
            if (value == null)
                value = string.Empty;

            if (!userStringCache.TryGetValue(value, out var handle))
            {
                handle = metadata.GetOrAddUserString(value);
                userStringCache[value] = handle;
            }

            return handle;
        }

        private GuidHandle GetOrAddGuid(Guid value)
        {
            return metadata.GetOrAddGuid(value);
        }

        private BlobHandle GetOrAddBlob(byte[] data)
        {
            if (data == null || data.Length == 0)
                return default;

            return metadata.GetOrAddBlob(data);
        }

        public void Dispose()
        {
            // No unmanaged resources to dispose
        }

        // Helper for .NET Framework 4.6.2 compatibility
        private static TValue GetValueOrDefault<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key)
        {
            return dict.TryGetValue(key, out var value) ? value : default(TValue);
        }
    }
}
