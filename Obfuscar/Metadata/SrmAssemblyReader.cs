using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using Mono.Cecil;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using System.Reflection.Metadata.Ecma335;

namespace Obfuscar.Metadata
{
    // Minimal SRM-based reader. Read-only: exposes MetadataReader and PEReader for callers.
    public class SrmAssemblyReader : IAssemblyReader
    {
        private readonly string path;
        private readonly Dictionary<Mono.Cecil.TypeDefinition, TypeDefinitionHandle> typeDefinitionHandles =
            new Dictionary<Mono.Cecil.TypeDefinition, TypeDefinitionHandle>();
        private readonly Dictionary<Mono.Cecil.MethodDefinition, MethodDefinitionHandle> methodDefinitionHandles =
            new Dictionary<Mono.Cecil.MethodDefinition, MethodDefinitionHandle>();
        private readonly Dictionary<Mono.Cecil.FieldDefinition, FieldDefinitionHandle> fieldDefinitionHandles =
            new Dictionary<Mono.Cecil.FieldDefinition, FieldDefinitionHandle>();
        private readonly Dictionary<Mono.Cecil.PropertyDefinition, PropertyDefinitionHandle> propertyDefinitionHandles =
            new Dictionary<Mono.Cecil.PropertyDefinition, PropertyDefinitionHandle>();
        private readonly Dictionary<Mono.Cecil.EventDefinition, EventDefinitionHandle> eventDefinitionHandles =
            new Dictionary<Mono.Cecil.EventDefinition, EventDefinitionHandle>();

        public SrmAssemblyReader(string path)
        {
            this.path = path;

            // Load the assembly bytes into memory so PEReader can operate
            var bytes = File.ReadAllBytes(path);
            peStream = new MemoryStream(bytes, false);
            PeReader = new PEReader(peStream);
            if (!PeReader.HasMetadata)
                throw new BadImageFormatException("PE image has no metadata.");

            MetadataReader = PeReader.GetMetadataReader();
            // Try load portable PDB next to assembly (simple strategy)
            try
            {
                var pdbPath = Path.ChangeExtension(path, ".pdb");
                if (File.Exists(pdbPath))
                {
                    var pdbStream = File.OpenRead(pdbPath);
                    pdbProvider = System.Reflection.Metadata.MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                    pdbReader = pdbProvider.GetMetadataReader();
                }
            }
            catch
            {
                // continue without PDB
            }
        }

        // PDB reader/provider (if a portable PDB exists alongside the assembly)
        private System.Reflection.Metadata.MetadataReaderProvider pdbProvider;
        private System.Reflection.Metadata.MetadataReader pdbReader;
        public System.Reflection.Metadata.MetadataReader PdbReader => pdbReader;

        // Materialized AssemblyDefinition (lazy)
        private Mono.Cecil.AssemblyDefinition materializedAssembly;

        public Mono.Cecil.AssemblyDefinition AssemblyDefinition => materializedAssembly ?? throw new NotSupportedException("AssemblyDefinition not materialized yet. Call CreateAssemblyDefinition().");

        public MetadataReader MetadataReader { get; private set; }

        public PEReader PeReader { get; private set; }

        private MemoryStream peStream;

        public Mono.Cecil.AssemblyDefinition CreateAssemblyDefinition()
        {
            if (materializedAssembly != null)
                return materializedAssembly;

            var md = MetadataReader;
            var asmDefHandle = md.GetAssemblyDefinition();
            string asmName = md.GetString(asmDefHandle.Name);

            // Build Version
            var ver = asmDefHandle.Version;
            var version = new Version(ver.Major, ver.Minor, ver.Build, ver.Revision);

            var an = new Mono.Cecil.AssemblyNameDefinition(asmName, version);

            // Populate public key (if present) so callers can detect signed assemblies
            try
            {
                if (!asmDefHandle.PublicKey.IsNil)
                {
                    var pkReader = md.GetBlobReader(asmDefHandle.PublicKey);
                    an.PublicKey = pkReader.ReadBytes(pkReader.Length);
                }
            }
            catch
            {
                // best-effort: leave public key null if we can't read it
            }

            // Create assembly with a module name based on the file name
            string moduleName = Path.GetFileName(path);
            materializedAssembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(an, moduleName, Mono.Cecil.ModuleKind.Dll);

            var module = materializedAssembly.MainModule;

            // Populate module MVID from metadata and set StrongNameSigned flag from PE header
            try
            {
                var modDef = md.GetModuleDefinition();
                if (!modDef.Mvid.IsNil)
                {
                    module.Mvid = md.GetGuid(modDef.Mvid);
                }

                // Use PE header CorFlags to determine strong-name signed state
                try
                {
                    var corFlags = PeReader.PEHeaders.CorHeader?.Flags ?? 0;
                    if ((corFlags & System.Reflection.PortableExecutable.CorFlags.StrongNameSigned) != 0)
                    {
                        module.Attributes |= Mono.Cecil.ModuleAttributes.StrongNameSigned;
                    }
                }
                catch
                {
                    // ignore
                }
            }
            catch
            {
                // best-effort
            }

            // ModuleDefinition.TypeSystem is initialized by Mono.Cecil; avoid assigning to it directly.

            // Populate assembly references
            foreach (var handle in md.AssemblyReferences)
            {
                var aref = md.GetAssemblyReference(handle);
                var name = md.GetString(aref.Name);
                var av = aref.Version;
                var refVer = new Version(av.Major, av.Minor, av.Build, av.Revision);
                var anref = new AssemblyNameReference(name, refVer);
                module.AssemblyReferences.Add(anref);
            }

            // Remove the default <Module> type that Cecil creates, so we don't have duplicates
            var defaultModule = module.Types.FirstOrDefault(t => string.IsNullOrEmpty(t.Name) && string.IsNullOrEmpty(t.Namespace));
            if (defaultModule != null)
                module.Types.Remove(defaultModule);

            // Map from TypeDefinitionHandle to Cecil TypeDefinition
            var typeMap = new Dictionary<System.Reflection.Metadata.TypeDefinitionHandle, Mono.Cecil.TypeDefinition>();
            var propertyMap = new Dictionary<System.Reflection.Metadata.PropertyDefinitionHandle, Mono.Cecil.PropertyDefinition>();
            
            // First pass: Create all type definitions (both top-level and nested)
            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);

                // skip <Module> synthetic type
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ns))
                    continue;
                    
                // Use actual type attributes from metadata
                var typeDef = new Mono.Cecil.TypeDefinition(ns, name, (Mono.Cecil.TypeAttributes)td.Attributes);
                typeMap[th] = typeDef;
                typeDefinitionHandles[typeDef] = th;
            }
            
            // Second pass: Establish nesting relationships and add to module
            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);

                // skip <Module> synthetic type
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ns))
                    continue;
                    
                if (!typeMap.TryGetValue(th, out var typeDef))
                    continue;
                
                // Check if this is a nested type
                var declaringTypeHandle = td.GetDeclaringType();
                if (!declaringTypeHandle.IsNil && typeMap.TryGetValue(declaringTypeHandle, out var parentType))
                {
                    // This is a nested type
                    parentType.NestedTypes.Add(typeDef);
                }
                else
                {
                    // This is a top-level type
                    module.Types.Add(typeDef);
                }
            }
            
            // Third pass: Populate type members
            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);

                // skip <Module> synthetic type
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ns))
                    continue;
                    
                if (!typeMap.TryGetValue(th, out var typeDef))
                    continue;
                
                // Set base type
                var baseTypeHandle = td.BaseType;
                if (!baseTypeHandle.IsNil)
                {
                    try
                    {
                        typeDef.BaseType = ResolveTypeHandle(module, md, baseTypeHandle);
                    }
                    catch
                    {
                        // Best-effort - leave as null if we can't resolve
                    }
                }

                // Populate interface implementations
                foreach (var ifaceHandle in td.GetInterfaceImplementations())
                {
                    try
                    {
                        var ifaceImpl = md.GetInterfaceImplementation(ifaceHandle);
                        var ifaceTypeRef = ResolveTypeHandle(module, md, ifaceImpl.Interface);
                        if (ifaceTypeRef != null)
                        {
                            typeDef.Interfaces.Add(new Mono.Cecil.InterfaceImplementation(ifaceTypeRef));
                        }
                    }
                    catch
                    {
                        // Best-effort - skip if we can't resolve
                    }
                }

                // Populate generic parameters for this type
                foreach (var gpHandle in td.GetGenericParameters())
                {
                    var gpDef = md.GetGenericParameter(gpHandle);
                    var gpName = md.GetString(gpDef.Name);
                    var gp = new Mono.Cecil.GenericParameter(gpName, typeDef);
                    typeDef.GenericParameters.Add(gp);
                }

                // Populate methods for this type
                foreach (var mh in td.GetMethods())
                {
                    var mdMethod = md.GetMethodDefinition(mh);
                    var mname = md.GetString(mdMethod.Name);
                    var matt = (Mono.Cecil.MethodAttributes)mdMethod.Attributes;
                    var mdef = new Mono.Cecil.MethodDefinition(mname, matt, module.TypeSystem.Object);
                    
                    // Populate generic parameters for this method
                    foreach (var gpHandle in mdMethod.GetGenericParameters())
                    {
                        var gpDef = md.GetGenericParameter(gpHandle);
                        var gpName = md.GetString(gpDef.Name);
                        var gp = new Mono.Cecil.GenericParameter(gpName, mdef);
                        mdef.GenericParameters.Add(gp);
                    }
                    
                    // Try to decode signature for return type and parameters
                    try
                    {
                        var sigHandle = mdMethod.Signature;
                        if (!sigHandle.IsNil)
                        {
                            var returnType = SrmSignatureDecoder.DecodeType(module, md, sigHandle);
                            if (returnType != null)
                                mdef.ReturnType = returnType;
                        }
                    }
                    catch
                    {
                        // best-effort: leave as object
                    }
                    typeDef.Methods.Add(mdef);
                    methodDefinitionHandles[mdef] = mh;

                    // Populate method parameters from signature (simple extraction)
                    try
                    {
                        var sigHandle = mdMethod.Signature;
                        if (!sigHandle.IsNil)
                        {
                            var msig = SrmSignatureDecoder.DecodeMethodSignature(module, md, sigHandle);
                            if (msig.ParameterTypes.Length > 0)
                            {
                                // Get parameter names from Parameter table
                                var paramNames = new Dictionary<int, (string name, Mono.Cecil.ParameterAttributes attrs)>();
                                foreach (var paramHandle in mdMethod.GetParameters())
                                {
                                    var param = md.GetParameter(paramHandle);
                                    var paramSeq = param.SequenceNumber;
                                    var paramName = md.GetString(param.Name);
                                    var paramAttrs = (Mono.Cecil.ParameterAttributes)param.Attributes;
                                    paramNames[paramSeq] = (paramName, paramAttrs);
                                }
                                
                                for (int i = 0; i < msig.ParameterTypes.Length; i++)
                                {
                                    var ptype = msig.ParameterTypes[i];
                                    var pname = "param" + (i + 1);
                                    var pattrs = Mono.Cecil.ParameterAttributes.None;
                                    
                                    // Parameter sequence is 1-based (0 is return type)
                                    if (paramNames.TryGetValue(i + 1, out var paramInfo))
                                    {
                                        pname = paramInfo.name;
                                        pattrs = paramInfo.attrs;
                                    }
                                    
                                    var pdef = new Mono.Cecil.ParameterDefinition(pname, pattrs, ptype ?? module.TypeSystem.Object);
                                    mdef.Parameters.Add(pdef);
                                }
                            }
                            if (msig.ReturnType != null)
                                mdef.ReturnType = msig.ReturnType;
                        }
                    }
                    catch { }

                    // Try to populate method body if there is an RVA
                    int rva = mdMethod.RelativeVirtualAddress;
                    if (rva != 0)
                    {
                        try
                        {
                            var bodyBlock = PeReader.GetMethodBody(rva);
                            if (bodyBlock != null)
                            {
                                var ilBytes = bodyBlock.GetILBytes();
                                if (ilBytes != null && ilBytes.Length > 0)
                                {
                                    var instructions = DecodeIL(module, md, ilBytes.ToArray());

                                    var mbody = new Mono.Cecil.Cil.MethodBody(mdef);
                                    // copy MaxStack if available
                                    try { mbody.MaxStackSize = bodyBlock.MaxStack; } catch { }
                                    foreach (var instr in instructions)
                                        mbody.Instructions.Add(instr);

                                    // Local variable decoding: first try from MethodBodyBlock.LocalSignature (always available)
                                    // then fallback to MethodDebugInformation if not present
                                    bool localsPopulated = false;
                                    try
                                    {
                                        var bodyLocalSig = bodyBlock.LocalSignature;
                                        if (!bodyLocalSig.IsNil)
                                        {
                                            var locals = SrmSignatureDecoder.DecodeLocalVariables(module, md, bodyLocalSig);
                                            foreach (var lt in locals)
                                            {
                                                var v = new Mono.Cecil.Cil.VariableDefinition(lt ?? module.TypeSystem.Object);
                                                mbody.Variables.Add(v);
                                            }
                                            localsPopulated = true;
                                        }
                                    }
                                    catch { }
                                    
                                    // Fallback: try from MethodDebugInformation local signature
                                    if (!localsPopulated)
                                    {
                                        try
                                        {
                                            var methodHandle = (MethodDefinitionHandle)mh;
                                            var debugHandle = md.GetMethodDebugInformation(methodHandle);
                                            var localSig = debugHandle.LocalSignature;
                                            if (!localSig.IsNil)
                                            {
                                                var locals = SrmSignatureDecoder.DecodeLocalVariables(module, md, localSig);
                                                foreach (var lt in locals)
                                                {
                                                    var v = new Mono.Cecil.Cil.VariableDefinition(lt ?? module.TypeSystem.Object);
                                                    mbody.Variables.Add(v);
                                                }
                                            }
                                        }
                                        catch { }
                                    }

                                    // If we have a portable PDB loaded, try to get local variable names and scopes
                                    try
                                    {
                                        if (pdbReader != null)
                                        {
                                            var methodHandle = (MethodDefinitionHandle)mh;

                                            // Build an instruction offset map for mapping offsets -> Instruction
                                            var instrMap = mbody.Instructions.ToDictionary(i => i.Offset, i => i);

                                            // Iterate local scopes from the PDB and find those that belong to this method
                                            foreach (var scopeHandle in pdbReader.LocalScopes)
                                            {
                                                try
                                                {
                                                    var scope = pdbReader.GetLocalScope(scopeHandle);
                                                    if (!scope.Method.Equals(methodHandle))
                                                        continue;

                                                    // Only attach debug scopes/variable names if MethodDebugInformation already exists
                                                    var mdDebug = mdef.DebugInformation;
                                                    if (mdDebug == null)
                                                        continue;

                                                    // Map start/end instruction by offset
                                                    instrMap.TryGetValue(scope.StartOffset, out var sInstr);
                                                    instrMap.TryGetValue(scope.StartOffset + scope.Length, out var eInstr);
                                                    var sd = new Mono.Cecil.Cil.ScopeDebugInformation(sInstr, eInstr);

                                                    // Add variables declared in this scope
                                                    foreach (var lvHandle in scope.GetLocalVariables())
                                                    {
                                                        try
                                                        {
                                                            var lv = pdbReader.GetLocalVariable(lvHandle);
                                                            int localIndex = lv.Index;
                                                            if (localIndex >= 0 && localIndex < mbody.Variables.Count)
                                                            {
                                                                var vdef = mbody.Variables[localIndex];
                                                                var lvName = pdbReader.GetString(lv.Name);
                                                                var vinfo = new Mono.Cecil.Cil.VariableDebugInformation(vdef, lvName);
                                                                sd.Variables.Add(vinfo);
                                                            }
                                                        }
                                                        catch { }
                                                    }

                                                    // Attach scope to method debug info
                                                    mdDebug.Scope = sd;
                                                    mdef.DebugInformation = mdDebug;
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    catch { }

                                    // Exception handlers from method body
                                    try
                                    {
                                        foreach (var eh in bodyBlock.ExceptionRegions)
                                        {
                                            // Map exception region kind to Cecil's ExceptionHandlerType
                                            Mono.Cecil.Cil.ExceptionHandlerType handlerType;
                                            switch (eh.Kind)
                                            {
                                                case ExceptionRegionKind.Catch:
                                                    handlerType = Mono.Cecil.Cil.ExceptionHandlerType.Catch;
                                                    break;
                                                case ExceptionRegionKind.Filter:
                                                    handlerType = Mono.Cecil.Cil.ExceptionHandlerType.Filter;
                                                    break;
                                                case ExceptionRegionKind.Finally:
                                                    handlerType = Mono.Cecil.Cil.ExceptionHandlerType.Finally;
                                                    break;
                                                case ExceptionRegionKind.Fault:
                                                    handlerType = Mono.Cecil.Cil.ExceptionHandlerType.Fault;
                                                    break;
                                                default:
                                                    handlerType = Mono.Cecil.Cil.ExceptionHandlerType.Catch;
                                                    break;
                                            }
                                            
                                            var handler = new Mono.Cecil.Cil.ExceptionHandler(handlerType);
                                            // map offsets to instructions
                                            var map = mbody.Instructions.ToDictionary(i => i.Offset, i => i);
                                            
                                            // For Try/Handler end, if exact offset not found, find the instruction just after the range
                                            Instruction FindInstruction(int offset, bool isEnd)
                                            {
                                                if (map.TryGetValue(offset, out var instr))
                                                    return instr;
                                                if (isEnd)
                                                {
                                                    // End offsets point one past the last instruction
                                                    // Find the first instruction at or after this offset
                                                    return mbody.Instructions.FirstOrDefault(i => i.Offset >= offset);
                                                }
                                                return null;
                                            }
                                            
                                            handler.TryStart = FindInstruction(eh.TryOffset, false);
                                            handler.TryEnd = FindInstruction(eh.TryOffset + eh.TryLength, true);
                                            handler.HandlerStart = FindInstruction(eh.HandlerOffset, false);
                                            handler.HandlerEnd = FindInstruction(eh.HandlerOffset + eh.HandlerLength, true);
                                            
                                            if (eh.Kind == ExceptionRegionKind.Filter)
                                            {
                                                handler.FilterStart = FindInstruction(eh.FilterOffset, false);
                                            }
                                            
                                            try
                                            {
                                                var catchHandle = eh.CatchType;
                                                if (!catchHandle.IsNil)
                                                {
                                                    var catchToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(catchHandle);
                                                    var catchType = ResolveMetadataToken(module, md, (int)catchToken);
                                                    handler.CatchType = catchType as Mono.Cecil.TypeReference;
                                                }
                                            }
                                            catch { }
                                            mbody.ExceptionHandlers.Add(handler);
                                        }
                                    }
                                    catch { }

                                    mdef.Body = mbody;
                                    
                                    // Fix up variable and parameter operands now that we have the collections populated
                                    FixupVariableAndParameterOperands(mdef);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is OverflowException)
                                throw;
                        }
                    }
                }

                // Populate fields for this type
                foreach (var fh in td.GetFields())
                {
                    var mdField = md.GetFieldDefinition(fh);
                    var fname = md.GetString(mdField.Name);
                    var fatt = (Mono.Cecil.FieldAttributes)mdField.Attributes;
                    
                    // Decode field type from signature
                    Mono.Cecil.TypeReference fieldType = module.TypeSystem.Object;
                    try
                    {
                        var sigBlob = md.GetBlobReader(mdField.Signature);
                        byte header = sigBlob.ReadByte(); // Skip FIELD header (0x06)
                        if (header == 0x06)
                        {
                            var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md, typeDef);
                            var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                            fieldType = decoder.DecodeType(ref sigBlob) ?? module.TypeSystem.Object;
                        }
                    }
                    catch
                    {
                        // Fall back to Object on decode failure
                    }
                    
                    var fdef = new Mono.Cecil.FieldDefinition(fname, fatt, fieldType);
                    typeDef.Fields.Add(fdef);
                    fieldDefinitionHandles[fdef] = fh;
                }

                // Populate properties (stub) for this type, linking getter/setter methods
                // DEBUG: Log property count being read
                var propHandles = td.GetProperties();
                foreach (var ph in propHandles)
                {
                    var mdProp = md.GetPropertyDefinition(ph);
                    var pname = md.GetString(mdProp.Name);
                    var patt = (Mono.Cecil.PropertyAttributes)mdProp.Attributes;
                    
                    // Decode property type from signature
                    Mono.Cecil.TypeReference propType = module.TypeSystem.Object;
                    try
                    {
                        var sigBlob = md.GetBlobReader(mdProp.Signature);
                        byte header = sigBlob.ReadByte(); // Read calling convention (PROPERTY header)
                        if ((header & 0x08) != 0) // PROPERTY signature
                        {
                            int paramCount = sigBlob.ReadCompressedInteger();
                            var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md, typeDef);
                            var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                            propType = decoder.DecodeType(ref sigBlob) ?? module.TypeSystem.Object;
                        }
                    }
                    catch
                    {
                        // Fall back to Object on decode failure
                    }
                    
                    var pdef = new Mono.Cecil.PropertyDefinition(pname, patt, propType);
                    
                    // Link getter and setter methods
                    var accessors = mdProp.GetAccessors();
                    if (!accessors.Getter.IsNil)
                    {
                        var getterMethod = md.GetMethodDefinition(accessors.Getter);
                        var getterName = md.GetString(getterMethod.Name);
                        foreach (var m in typeDef.Methods)
                        {
                            if (m.Name == getterName)
                            {
                                pdef.GetMethod = m;
                                SetMethodSemanticsAttributes(m, Mono.Cecil.MethodSemanticsAttributes.Getter);
                                break;
                            }
                        }
                    }
                    if (!accessors.Setter.IsNil)
                    {
                        var setterMethod = md.GetMethodDefinition(accessors.Setter);
                        var setterName = md.GetString(setterMethod.Name);
                        foreach (var m in typeDef.Methods)
                        {
                            if (m.Name == setterName)
                            {
                                pdef.SetMethod = m;
                                SetMethodSemanticsAttributes(m, Mono.Cecil.MethodSemanticsAttributes.Setter);
                                break;
                            }
                        }
                    }
                    
                    typeDef.Properties.Add(pdef);
                    propertyMap[ph] = pdef;
                    propertyDefinitionHandles[pdef] = ph;
                }

                // Populate events for this type, linking add/remove methods
                foreach (var eh in td.GetEvents())
                {
                    var mdEvent = md.GetEventDefinition(eh);
                    var ename = md.GetString(mdEvent.Name);
                    var eatt = (Mono.Cecil.EventAttributes)mdEvent.Attributes;
                    var edef = new Mono.Cecil.EventDefinition(ename, eatt, module.TypeSystem.Object);
                    
                    // Link add and remove methods
                    var accessors = mdEvent.GetAccessors();
                    if (!accessors.Adder.IsNil)
                    {
                        var adderMethod = md.GetMethodDefinition(accessors.Adder);
                        var adderName = md.GetString(adderMethod.Name);
                        foreach (var m in typeDef.Methods)
                        {
                            if (m.Name == adderName)
                            {
                                edef.AddMethod = m;
                                SetMethodSemanticsAttributes(m, Mono.Cecil.MethodSemanticsAttributes.AddOn);
                                break;
                            }
                        }
                    }
                    if (!accessors.Remover.IsNil)
                    {
                        var removerMethod = md.GetMethodDefinition(accessors.Remover);
                        var removerName = md.GetString(removerMethod.Name);
                        foreach (var m in typeDef.Methods)
                        {
                            if (m.Name == removerName)
                            {
                                edef.RemoveMethod = m;
                                SetMethodSemanticsAttributes(m, Mono.Cecil.MethodSemanticsAttributes.RemoveOn);
                                break;
                            }
                        }
                    }
                    if (!accessors.Raiser.IsNil)
                    {
                        var raiserMethod = md.GetMethodDefinition(accessors.Raiser);
                        var raiserName = md.GetString(raiserMethod.Name);
                        foreach (var m in typeDef.Methods)
                        {
                            if (m.Name == raiserName)
                            {
                                edef.InvokeMethod = m;
                                SetMethodSemanticsAttributes(m, Mono.Cecil.MethodSemanticsAttributes.Fire);
                                break;
                            }
                        }
                    }
                    
                    typeDef.Events.Add(edef);
                    eventDefinitionHandles[edef] = eh;
                }
            }

            // Populate custom attributes on all entities
            PopulateCustomAttributes(module, md, propertyMap);

            return materializedAssembly;
        }

        public bool TryGetTypeHandle(Mono.Cecil.TypeDefinition type, out TypeDefinitionHandle handle)
        {
            if (type == null)
            {
                handle = default;
                return false;
            }

            return typeDefinitionHandles.TryGetValue(type, out handle);
        }

        public bool TryGetMethodHandle(Mono.Cecil.MethodDefinition method, out MethodDefinitionHandle handle)
        {
            if (method == null)
            {
                handle = default;
                return false;
            }

            return methodDefinitionHandles.TryGetValue(method, out handle);
        }

        public bool TryGetFieldHandle(Mono.Cecil.FieldDefinition field, out FieldDefinitionHandle handle)
        {
            if (field == null)
            {
                handle = default;
                return false;
            }

            return fieldDefinitionHandles.TryGetValue(field, out handle);
        }

        public bool TryGetPropertyHandle(Mono.Cecil.PropertyDefinition property, out PropertyDefinitionHandle handle)
        {
            if (property == null)
            {
                handle = default;
                return false;
            }

            return propertyDefinitionHandles.TryGetValue(property, out handle);
        }

        public bool TryGetEventHandle(Mono.Cecil.EventDefinition evt, out EventDefinitionHandle handle)
        {
            if (evt == null)
            {
                handle = default;
                return false;
            }

            return eventDefinitionHandles.TryGetValue(evt, out handle);
        }

        // Helper to set SemanticsAttributes on a MethodDefinition and mark it as ready.
        // Cecil's SemanticsAttributes getter resets the value to None if sem_attrs_ready is false.
        private static void SetMethodSemanticsAttributes(Mono.Cecil.MethodDefinition method, Mono.Cecil.MethodSemanticsAttributes value)
        {
            // First set the field via the public setter
            method.SemanticsAttributes = value;
            
            // Then set sem_attrs_ready = true via reflection so the getter doesn't reset it
            var semAttrsReadyField = typeof(Mono.Cecil.MethodDefinition).GetField("sem_attrs_ready", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (semAttrsReadyField != null)
            {
                semAttrsReadyField.SetValue(method, true);
            }
        }

        // Basic IL decoder: converts raw IL bytes into a list of Cecil Instructions.
        private static List<Instruction> DecodeIL(Mono.Cecil.ModuleDefinition module, MetadataReader md, byte[] il)
        {
            var result = new List<Instruction>();
            int offset = 0;
            while (offset < il.Length)
            {
                int start = offset;
                OpCode op;
                byte code = il[offset++];
                if (code == 0xFE)
                {
                    // two-byte opcode
                    byte code2 = il[offset++];
                    ushort val = (ushort)((code << 8) | code2);
                    op = OpCodes.Nop; // fallback
                    // map common two-byte opcodes
                    op = GetOpCode(val);
                }
                else
                {
                    op = GetOpCode(code);
                }

                object operand = null;
                switch (op.OperandType)
                {
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineI:
                        operand = (sbyte)il[offset++];
                        break;
                    case OperandType.InlineI:
                        operand = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        break;
                    case OperandType.InlineI8:
                        operand = BitConverter.ToInt64(il, offset);
                        offset += 8;
                        break;
                    case OperandType.InlineString:
                        int strToken = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        try
                        {
                            // User string token format: 0x70XXXXXX where XXXXXX is heap offset
                            // MetadataTokens.UserStringHandle expects just the offset (lower 24 bits)
                            int heapOffset = strToken & 0x00FFFFFF;
                            var strHandle = System.Reflection.Metadata.Ecma335.MetadataTokens.UserStringHandle(heapOffset);
                            operand = md.GetUserString(strHandle);
                        }
                        catch
                        {
                            operand = string.Empty;
                        }
                        break;
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.InlineField:
                        int token = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        // attempt to resolve metadata token to a Cecil reference
                        try
                        {
                            operand = ResolveMetadataToken(module, md, token);
                        }
                        catch
                        {
                            operand = token;
                        }
                        break;
                    case OperandType.ShortInlineBrTarget:
                        sbyte rel8 = (sbyte)il[offset++];
                        // Branch target is relative to the END of the instruction (current offset)
                        operand = offset + rel8;
                        break;
                    case OperandType.InlineBrTarget:
                        int rel32 = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        // Branch target is relative to the END of the instruction (current offset)
                        operand = offset + rel32;
                        break;
                    case OperandType.ShortInlineVar:
                        operand = il[offset++];
                        break;
                    case OperandType.InlineVar:
                        operand = BitConverter.ToUInt16(il, offset);
                        offset += 2;
                        break;
                    case OperandType.ShortInlineR:
                        operand = BitConverter.ToSingle(il, offset);
                        offset += 4;
                        break;
                    case OperandType.InlineR:
                        operand = BitConverter.ToDouble(il, offset);
                        offset += 8;
                        break;
                    case OperandType.InlineSwitch:
                        int count = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        var targets = new int[count];
                        // Calculate base offset after reading all switch targets
                        int baseOffset = offset + (count * 4);
                        for (int i = 0; i < count; i++)
                        {
                            int rel = BitConverter.ToInt32(il, offset);
                            offset += 4;
                            // Switch targets are relative to the END of the switch instruction
                            targets[i] = baseOffset + rel;
                        }
                        operand = targets;
                        break;
                    case OperandType.InlineSig:
                    default:
                        break;
                }

                // Use reflection to create Instruction since Instruction.Create validates operand types
                // and we're building from raw IL where the structure is already validated
                var instruction = CreateInstruction(op, operand);
                instruction.Offset = start;
                result.Add(instruction);
            }

            // Link branch targets: replace numeric offsets with Instruction references
            var map = result.ToDictionary(i => i.Offset, i => i);
            foreach (var instr in result)
            {
                // Only process branch instructions
                if (instr.OpCode.OperandType == OperandType.ShortInlineBrTarget ||
                    instr.OpCode.OperandType == OperandType.InlineBrTarget)
                {
                    if (instr.Operand is int off)
                    {
                        if (map.TryGetValue(off, out var target))
                            instr.Operand = target;
                    }
                }
                else if (instr.OpCode.OperandType == OperandType.InlineSwitch)
                {
                    if (instr.Operand is int[] offs)
                    {
                        var targets = new Instruction[offs.Length];
                        for (int j = 0; j < offs.Length; j++)
                        {
                            if (map.TryGetValue(offs[j], out var t))
                                targets[j] = t;
                            else
                            {
                                // Find the closest instruction or use the first one as fallback
                                targets[j] = result.FirstOrDefault(x => x.Offset >= offs[j]) ?? result[0];
                            }
                        }
                        instr.Operand = targets;
                    }
                }
            }

            return result;
        }

        private static object ResolveMetadataToken(Mono.Cecil.ModuleDefinition module, MetadataReader md, int token)
        {
            var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(token);
            switch (handle.Kind)
            {
                case HandleKind.TypeReference:
                    {
                        var tr = md.GetTypeReference(System.Reflection.Metadata.Ecma335.MetadataTokens.TypeReferenceHandle(token));
                        var name = md.GetString(tr.Name);
                        var ns = md.GetString(tr.Namespace);
                        var scope = GetResolutionScope(module, md, tr.ResolutionScope);
                        var tref = new Mono.Cecil.TypeReference(ns, name, module, scope);
                        return tref;
                    }
                case HandleKind.TypeDefinition:
                    {
                        var td = md.GetTypeDefinition(System.Reflection.Metadata.Ecma335.MetadataTokens.TypeDefinitionHandle(token));
                        var name = md.GetString(td.Name);
                        var ns = md.GetString(td.Namespace);
                        var tref = new Mono.Cecil.TypeReference(ns, name, module, module);
                        return tref;
                    }
                case HandleKind.FieldDefinition:
                    {
                        var fd = md.GetFieldDefinition(System.Reflection.Metadata.Ecma335.MetadataTokens.FieldDefinitionHandle(token));
                        var name = md.GetString(fd.Name);
                        // Try to find declaring type by scanning type definitions (best-effort)
                        var declType = FindDeclaringTypeForMember(md, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(System.Reflection.Metadata.Ecma335.MetadataTokens.FieldDefinitionHandle(token)));
                        Mono.Cecil.TypeReference parent = declType.ns != null ? new Mono.Cecil.TypeReference(declType.ns, declType.name, module, module) : module.TypeSystem.Object;
                        
                        // Decode field type from signature
                        Mono.Cecil.TypeReference fieldType = module.TypeSystem.Object;
                        try
                        {
                            var sigBlob = md.GetBlobReader(fd.Signature);
                            byte header = sigBlob.ReadByte(); // Skip FIELD header (0x06)
                            if (header == 0x06)
                            {
                                var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md);
                                var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                                fieldType = decoder.DecodeType(ref sigBlob) ?? module.TypeSystem.Object;
                            }
                        }
                        catch { }
                        
                        return new Mono.Cecil.FieldReference(name, fieldType, parent);
                    }
                case HandleKind.MethodDefinition:
                    {
                        var mdDef = md.GetMethodDefinition(System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(token));
                        var name = md.GetString(mdDef.Name);
                        var declType = FindDeclaringTypeForMember(md, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(token)));
                        Mono.Cecil.TypeReference parent = declType.ns != null ? new Mono.Cecil.TypeReference(declType.ns, declType.name, module, module) : module.TypeSystem.Object;
                        // Decode proper return type from method signature
                        var methodSig = SrmSignatureDecoder.DecodeMethodSignature(module, md, mdDef.Signature);
                        return new Mono.Cecil.MethodReference(name, methodSig.ReturnType) { DeclaringType = parent };
                    }
                case HandleKind.MemberReference:
                    {
                        var mr = md.GetMemberReference(System.Reflection.Metadata.Ecma335.MetadataTokens.MemberReferenceHandle(token));
                        var name = md.GetString(mr.Name);
                        
                        // Resolve parent type properly with correct scope
                        Mono.Cecil.TypeReference parent = null;
                        if (mr.Parent.Kind == HandleKind.TypeReference)
                        {
                            var ptr = md.GetTypeReference((TypeReferenceHandle)mr.Parent);
                            var pns = md.GetString(ptr.Namespace);
                            var pname = md.GetString(ptr.Name);
                            var scope = GetResolutionScope(module, md, ptr.ResolutionScope);
                            parent = new Mono.Cecil.TypeReference(pns, pname, module, scope);
                        }
                        else if (mr.Parent.Kind == HandleKind.TypeDefinition)
                        {
                            var ptd = md.GetTypeDefinition((TypeDefinitionHandle)mr.Parent);
                            var pns = md.GetString(ptd.Namespace);
                            var pname = md.GetString(ptd.Name);
                            parent = new Mono.Cecil.TypeReference(pns, pname, module, module);
                        }
                        else if (mr.Parent.Kind == HandleKind.TypeSpecification)
                        {
                            // Handle generic type specification - decode the signature
                            parent = ResolveTypeHandle(module, md, mr.Parent) ?? module.TypeSystem.Object;
                        }
                        else
                        {
                            parent = module.TypeSystem.Object;
                        }
                        
                        // Decode the signature to determine if this is a field or method reference
                        var sigBlob = md.GetBlobReader(mr.Signature);
                        var firstByte = sigBlob.ReadByte();
                        
                        // Field signature starts with 0x06 (FIELD)
                        if (firstByte == 0x06)
                        {
                            // Decode the actual field type
                            Mono.Cecil.TypeReference fieldType = module.TypeSystem.Object;
                            try
                            {
                                var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md);
                                var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                                fieldType = decoder.DecodeType(ref sigBlob) ?? module.TypeSystem.Object;
                            }
                            catch { }
                            return new Mono.Cecil.FieldReference(name, fieldType, parent);
                        }
                        
                        // Method signature - decode parameters and return type properly
                        var methodSig = SrmSignatureDecoder.DecodeMethodSignature(module, md, mr.Signature);
                        var mref = new Mono.Cecil.MethodReference(name, methodSig.ReturnType) { DeclaringType = parent };
                        
                        // Parse calling convention from first byte
                        bool hasThis = (firstByte & 0x20) != 0;
                        mref.HasThis = hasThis;
                        
                        // Check for generic method
                        if ((firstByte & 0x10) != 0)
                        {
                            // Re-read signature to get generic param count
                            var sigBlob2 = md.GetBlobReader(mr.Signature);
                            sigBlob2.ReadByte(); // skip calling convention
                            var genParamCount = sigBlob2.ReadCompressedInteger();
                            for (int i = 0; i < genParamCount; i++)
                            {
                                mref.GenericParameters.Add(new Mono.Cecil.GenericParameter("T" + i, mref));
                            }
                        }
                        
                        // Add parameters from decoded signature
                        for (int i = 0; i < methodSig.ParameterTypes.Length; i++)
                        {
                            mref.Parameters.Add(new Mono.Cecil.ParameterDefinition(methodSig.ParameterTypes[i]));
                        }
                        
                        return mref;
                    }
                case HandleKind.MethodSpecification:
                    {
                        var ms = md.GetMethodSpecification((MethodSpecificationHandle)handle);
                        
                        // Get the generic method being instantiated
                        var elementMethodHandle = ms.Method;
                        var elementMethod = ResolveMetadataToken(module, md, MetadataTokens.GetToken(elementMethodHandle)) as Mono.Cecil.MethodReference;
                        if (elementMethod == null)
                            return token;
                        
                        // Decode the generic arguments
                        var sigReader = md.GetBlobReader(ms.Signature);
                        byte header = sigReader.ReadByte();
                        if (header != 0x0A) // GENRICINST
                            return token;
                        
                        int argCount = sigReader.ReadCompressedInteger();
                        var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md);
                        var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                        
                        var genericInstance = new Mono.Cecil.GenericInstanceMethod(elementMethod);
                        for (int i = 0; i < argCount; i++)
                        {
                            var argType = decoder.DecodeType(ref sigReader) ?? module.TypeSystem.Object;
                            genericInstance.GenericArguments.Add(argType);
                        }
                        
                        return genericInstance;
                    }
                case HandleKind.TypeSpecification:
                    {
                        // TypeSpecification is for generic instantiations, arrays, pointers, and generic parameters
                        // Decode the signature blob to get the actual type
                        var tsHandle = (TypeSpecificationHandle)handle;
                        var ts = md.GetTypeSpecification(tsHandle);
                        var sigBlob = ts.Signature;
                        if (!sigBlob.IsNil)
                        {
                            var decodedType = SrmSignatureDecoder.DecodeType(module, md, sigBlob);
                            if (decodedType != null)
                                return decodedType;
                        }
                        return module.TypeSystem.Object;
                    }
                default:
                    // fallback: return numeric token
                    return token;
            }
        }

        private static (string ns, string name) FindDeclaringTypeForMember(MetadataReader md, int memberToken)
        {
            // Best-effort: scan types to find member with matching row id
            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                foreach (var fh in td.GetFields())
                {
                    if (System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(fh) == (uint)memberToken)
                        return (md.GetString(td.Namespace), md.GetString(td.Name));
                }
                foreach (var mh in td.GetMethods())
                {
                    if (System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(mh) == (uint)memberToken)
                        return (md.GetString(td.Namespace), md.GetString(td.Name));
                }
            }
            return (null, null);
        }

        private static Mono.Cecil.IMetadataScope GetResolutionScope(Mono.Cecil.ModuleDefinition module, MetadataReader md, EntityHandle scope)
        {
            if (scope.Kind == HandleKind.AssemblyReference)
            {
                var aref = md.GetAssemblyReference((AssemblyReferenceHandle)scope);
                var name = md.GetString(aref.Name);
                var av = aref.Version;
                var refVer = new Version(av.Major, av.Minor, av.Build, av.Revision);
                var anref = new Mono.Cecil.AssemblyNameReference(name, refVer);
                // add to module assembly references if missing
                if (!module.AssemblyReferences.Any(a => a.Name == anref.Name))
                    module.AssemblyReferences.Add(anref);
                return anref;
            }
            // fallback
            return module;
        }

        // Cache for reflection access to Instruction's internal constructor
        private static ConstructorInfo _instructionCtor;
        
        /// <summary>
        /// Creates an Instruction using reflection to bypass validation in Instruction.Create methods.
        /// This is needed because we're decoding raw IL and the structure is already valid.
        /// </summary>
        private static Instruction CreateInstruction(OpCode opcode, object operand)
        {
            if (_instructionCtor == null)
            {
                // Find the internal constructor: Instruction(OpCode opcode, object operand)
                _instructionCtor = typeof(Instruction).GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(OpCode), typeof(object) },
                    null);
            }
            
            if (_instructionCtor != null)
            {
                return (Instruction)_instructionCtor.Invoke(new object[] { opcode, operand });
            }
            
            // Fallback: try finding the (int, OpCode) constructor and set Operand via property
            var altCtor = typeof(Instruction).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(OpCode) },
                null);
            if (altCtor != null)
            {
                var instruction = (Instruction)altCtor.Invoke(new object[] { 0, opcode });
                instruction.Operand = operand;
                return instruction;
            }
            
            // Last resort: use Create and hope it works
            throw new InvalidOperationException("Cannot create Instruction - no suitable constructor found");
        }

        /// <summary>
        /// After populating variables and parameters, fix up instructions that have integer operands
        /// which should be VariableDefinition or ParameterDefinition objects.
        /// </summary>
        private static void FixupVariableAndParameterOperands(Mono.Cecil.MethodDefinition method)
        {
            if (method.Body == null) return;
            
            var body = method.Body;
            
            foreach (var instr in body.Instructions)
            {
                switch (instr.OpCode.OperandType)
                {
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineVar:
                        if (instr.Operand is byte b && b < body.Variables.Count)
                        {
                            instr.Operand = body.Variables[b];
                        }
                        else if (instr.Operand is ushort u && u < body.Variables.Count)
                        {
                            instr.Operand = body.Variables[u];
                        }
                        else if (instr.Operand is int i && i >= 0 && i < body.Variables.Count)
                        {
                            instr.Operand = body.Variables[i];
                        }
                        break;
                        
                    case OperandType.ShortInlineArg:
                    case OperandType.InlineArg:
                        int argIndex = -1;
                        if (instr.Operand is byte ab)
                            argIndex = ab;
                        else if (instr.Operand is ushort au)
                            argIndex = au;
                        else if (instr.Operand is int ai)
                            argIndex = ai;
                            
                        if (argIndex >= 0)
                        {
                            // For instance methods, arg 0 is 'this', so we need to adjust
                            if (method.HasThis)
                            {
                                if (argIndex == 0)
                                {
                                    // 'this' parameter - use method.Body.ThisParameter
                                    instr.Operand = body.ThisParameter;
                                }
                                else if (argIndex - 1 < method.Parameters.Count)
                                {
                                    instr.Operand = method.Parameters[argIndex - 1];
                                }
                            }
                            else if (argIndex < method.Parameters.Count)
                            {
                                instr.Operand = method.Parameters[argIndex];
                            }
                        }
                        break;
                }
            }
        }

        private static OpCode GetOpCode(int code)
        {
            // Very small mapping: fallback to search in OpCodes set
            foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var oc = (OpCode)fi.GetValue(null);
                if (oc.Value == code)
                    return oc;
            }
            return OpCodes.Nop;
        }

        // Populate custom attributes for types, methods, fields
        private static void PopulateCustomAttributes(Mono.Cecil.ModuleDefinition module, MetadataReader md,
            Dictionary<System.Reflection.Metadata.PropertyDefinitionHandle, Mono.Cecil.PropertyDefinition> propertyMap)
        {
            try
            {
                // Iterate through custom attributes and attach them to their parent entities
                foreach (var caHandle in md.CustomAttributes)
                {
                    try
                    {
                        var ca = md.GetCustomAttribute(caHandle);
                        var parent = ca.Parent;
                        var constructor = ca.Constructor;
                        
                        // Get the attribute type
                        string attrTypeName = null;
                        string attrTypeNamespace = null;
                        
                        if (constructor.Kind == System.Reflection.Metadata.HandleKind.MemberReference)
                        {
                            var mr = md.GetMemberReference((System.Reflection.Metadata.MemberReferenceHandle)constructor);
                            if (mr.Parent.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
                            {
                                var tr = md.GetTypeReference((System.Reflection.Metadata.TypeReferenceHandle)mr.Parent);
                                attrTypeName = md.GetString(tr.Name);
                                attrTypeNamespace = md.GetString(tr.Namespace);
                            }
                        }
                        else if (constructor.Kind == System.Reflection.Metadata.HandleKind.MethodDefinition)
                        {
                            var mh = (System.Reflection.Metadata.MethodDefinitionHandle)constructor;
                            var md_method = md.GetMethodDefinition(mh);
                            // Find declaring type
                            var declType = FindDeclaringTypeForMember(md, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(mh));
                            attrTypeName = declType.name;
                            attrTypeNamespace = declType.ns;
                        }

                        if (attrTypeName == null)
                            continue;

                        // Create the attribute type reference with proper scope
                        Mono.Cecil.IMetadataScope attrScope = module;
                        if (constructor.Kind == System.Reflection.Metadata.HandleKind.MemberReference)
                        {
                            var mr = md.GetMemberReference((System.Reflection.Metadata.MemberReferenceHandle)constructor);
                            if (mr.Parent.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
                            {
                                var tr = md.GetTypeReference((System.Reflection.Metadata.TypeReferenceHandle)mr.Parent);
                                attrScope = GetResolutionScope(module, md, tr.ResolutionScope);
                            }
                        }
                        
                        var attrTypeRef = new Mono.Cecil.TypeReference(attrTypeNamespace, attrTypeName, module, attrScope);
                        
                        // Create MethodReference for constructor with proper parameters
                        var ctorRef = new Mono.Cecil.MethodReference(".ctor", module.TypeSystem.Void, attrTypeRef);
                        ctorRef.HasThis = true;
                        
                        // Decode constructor signature to get parameter types
                        if (constructor.Kind == System.Reflection.Metadata.HandleKind.MemberReference)
                        {
                            var mr = md.GetMemberReference((System.Reflection.Metadata.MemberReferenceHandle)constructor);
                            try
                            {
                                var sigBlob = md.GetBlobReader(mr.Signature);
                                var header = sigBlob.ReadSignatureHeader();
                                var paramCount = sigBlob.ReadCompressedInteger();
                                
                                // Skip return type (void for .ctor)
                                SkipSignatureType(ref sigBlob);
                                
                                // Read parameter types
                                for (int i = 0; i < paramCount; i++)
                                {
                                    var paramType = DecodeSignatureType(module, md, ref sigBlob);
                                    ctorRef.Parameters.Add(new Mono.Cecil.ParameterDefinition(paramType ?? module.TypeSystem.Object));
                                }
                            }
                            catch { }
                        }
                        else if (constructor.Kind == System.Reflection.Metadata.HandleKind.MethodDefinition)
                        {
                            // For method definitions (custom attributes in the same assembly), decode signature from MethodDef
                            var mh = (System.Reflection.Metadata.MethodDefinitionHandle)constructor;
                            var md_method = md.GetMethodDefinition(mh);
                            try
                            {
                                var sigBlob = md.GetBlobReader(md_method.Signature);
                                var header = sigBlob.ReadSignatureHeader();
                                var paramCount = sigBlob.ReadCompressedInteger();
                                
                                // Skip return type (void for .ctor)
                                SkipSignatureType(ref sigBlob);
                                
                                // Read parameter types
                                for (int i = 0; i < paramCount; i++)
                                {
                                    var paramType = DecodeSignatureType(module, md, ref sigBlob);
                                    ctorRef.Parameters.Add(new Mono.Cecil.ParameterDefinition(paramType ?? module.TypeSystem.Object));
                                }
                            }
                            catch { }
                        }
                        
                        var attrDef = new Mono.Cecil.CustomAttribute(ctorRef);
                        
                        // Decode custom attribute value blob for constructor arguments and named arguments
                        try
                        {
                            var valueBlob = md.GetBlobReader(ca.Value);
                            if (valueBlob.Length >= 2)
                            {
                                var prolog = valueBlob.ReadUInt16();
                                if (prolog == 0x0001)
                                {
                                    // Read constructor arguments based on parameter types
                                    foreach (var param in ctorRef.Parameters)
                                    {
                                        var argValue = DecodeCustomAttributeArgumentValue(module, md, ref valueBlob, param.ParameterType);
                                        attrDef.ConstructorArguments.Add(new Mono.Cecil.CustomAttributeArgument(param.ParameterType, argValue));
                                    }
                                    
                                    // Read named arguments (fields and properties)
                                    if (valueBlob.RemainingBytes >= 2)
                                    {
                                        var numNamed = valueBlob.ReadUInt16();
                                        for (int i = 0; i < numNamed && valueBlob.RemainingBytes > 0; i++)
                                        {
                                            var kind = valueBlob.ReadByte(); // 0x53 = FIELD, 0x54 = PROPERTY
                                            var namedArgType = DecodeSerializationType(module, md, ref valueBlob);
                                            var namedArgName = ReadSerializedString(ref valueBlob);
                                            var namedArgValue = DecodeCustomAttributeArgumentValue(module, md, ref valueBlob, namedArgType);
                                            
                                            if (namedArgName != null && namedArgType != null)
                                            {
                                                var namedArg = new Mono.Cecil.CustomAttributeNamedArgument(
                                                    namedArgName,
                                                    new Mono.Cecil.CustomAttributeArgument(namedArgType, namedArgValue));
                                                    
                                                if (kind == 0x53) // FIELD
                                                    attrDef.Fields.Add(namedArg);
                                                else if (kind == 0x54) // PROPERTY
                                                    attrDef.Properties.Add(namedArg);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        // Attach to parent entity
                        if (parent.Kind == System.Reflection.Metadata.HandleKind.AssemblyDefinition)
                        {
                            // Assembly level attribute
                            var assemblyDef = module.Assembly;
                            assemblyDef.CustomAttributes.Add(attrDef);
                            continue;
                        }

                        if (parent.Kind == System.Reflection.Metadata.HandleKind.TypeDefinition)
                        {
                            var tdHandle = (System.Reflection.Metadata.TypeDefinitionHandle)parent;
                            var td = md.GetTypeDefinition(tdHandle);
                            var typeName = md.GetString(td.Name);
                            var typeNs = md.GetString(td.Namespace);
                            // Skip <Module> type
                            if (string.IsNullOrEmpty(typeName) && string.IsNullOrEmpty(typeNs))
                                continue;
                            var cecilType = module.Types.FirstOrDefault(t => t.Name == typeName && t.Namespace == typeNs);
                            if (cecilType != null)
                                cecilType.CustomAttributes.Add(attrDef);
                        }
                        else if (parent.Kind == System.Reflection.Metadata.HandleKind.MethodDefinition)
                        {
                            var mhParent = (System.Reflection.Metadata.MethodDefinitionHandle)parent;
                            var mdParent = md.GetMethodDefinition(mhParent);
                            var parentType = FindDeclaringTypeForMember(md, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(mhParent));
                            var cecilType = module.Types.FirstOrDefault(t => t.Name == parentType.name && t.Namespace == parentType.ns);
                            if (cecilType != null)
                            {
                                var methodName = md.GetString(mdParent.Name);
                                var cecilMethod = cecilType.Methods.FirstOrDefault(m => m.Name == methodName);
                                if (cecilMethod != null)
                                    cecilMethod.CustomAttributes.Add(attrDef);
                            }
                        }
                        else if (parent.Kind == System.Reflection.Metadata.HandleKind.FieldDefinition)
                        {
                            var fhParent = (System.Reflection.Metadata.FieldDefinitionHandle)parent;
                            var fdParent = md.GetFieldDefinition(fhParent);
                            var parentType = FindDeclaringTypeForMember(md, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(fhParent));
                            var cecilType = module.Types.FirstOrDefault(t => t.Name == parentType.name && t.Namespace == parentType.ns);
                            if (cecilType != null)
                            {
                                var fieldName = md.GetString(fdParent.Name);
                                var cecilField = cecilType.Fields.FirstOrDefault(f => f.Name == fieldName);
                                if (cecilField != null)
                                    cecilField.CustomAttributes.Add(attrDef);
                            }
                        }
                        else if (parent.Kind == System.Reflection.Metadata.HandleKind.PropertyDefinition)
                        {
                            var propHandle = (System.Reflection.Metadata.PropertyDefinitionHandle)parent;
                            if (propertyMap.TryGetValue(propHandle, out var cecilProp))
                            {
                                cecilProp.CustomAttributes.Add(attrDef);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Skip a type in a signature blob (ECMA-335 II.23.2.12)
        /// </summary>
        private static void SkipSignatureType(ref BlobReader blob)
        {
            var typeCode = blob.ReadCompressedInteger();
            
            switch (typeCode)
            {
                case 0x01: // ELEMENT_TYPE_VOID
                case 0x02: // ELEMENT_TYPE_BOOLEAN
                case 0x03: // ELEMENT_TYPE_CHAR
                case 0x04: // ELEMENT_TYPE_I1
                case 0x05: // ELEMENT_TYPE_U1
                case 0x06: // ELEMENT_TYPE_I2
                case 0x07: // ELEMENT_TYPE_U2
                case 0x08: // ELEMENT_TYPE_I4
                case 0x09: // ELEMENT_TYPE_U4
                case 0x0A: // ELEMENT_TYPE_I8
                case 0x0B: // ELEMENT_TYPE_U8
                case 0x0C: // ELEMENT_TYPE_R4
                case 0x0D: // ELEMENT_TYPE_R8
                case 0x0E: // ELEMENT_TYPE_STRING
                case 0x18: // ELEMENT_TYPE_I (native int)
                case 0x19: // ELEMENT_TYPE_U (native uint)
                case 0x1C: // ELEMENT_TYPE_OBJECT
                    break; // Primitive types - nothing more to skip
                    
                case 0x0F: // ELEMENT_TYPE_PTR
                case 0x10: // ELEMENT_TYPE_BYREF
                case 0x45: // ELEMENT_TYPE_PINNED
                case 0x1D: // ELEMENT_TYPE_SZARRAY
                    SkipSignatureType(ref blob); // Skip the element type
                    break;
                    
                case 0x11: // ELEMENT_TYPE_VALUETYPE
                case 0x12: // ELEMENT_TYPE_CLASS
                    blob.ReadCompressedInteger(); // Skip TypeDefOrRef coded index
                    break;
                    
                case 0x14: // ELEMENT_TYPE_ARRAY
                    SkipSignatureType(ref blob); // Element type
                    var rank = blob.ReadCompressedInteger();
                    var numSizes = blob.ReadCompressedInteger();
                    for (int i = 0; i < numSizes; i++)
                        blob.ReadCompressedInteger();
                    var numLoBounds = blob.ReadCompressedInteger();
                    for (int i = 0; i < numLoBounds; i++)
                        blob.ReadCompressedSignedInteger();
                    break;
                    
                case 0x15: // ELEMENT_TYPE_GENERICINST
                    SkipSignatureType(ref blob); // Skip CLASS or VALUETYPE
                    var genArgCount = blob.ReadCompressedInteger();
                    for (int i = 0; i < genArgCount; i++)
                        SkipSignatureType(ref blob);
                    break;
                    
                case 0x13: // ELEMENT_TYPE_VAR (generic type parameter)
                case 0x1E: // ELEMENT_TYPE_MVAR (generic method parameter)
                    blob.ReadCompressedInteger(); // Skip the index
                    break;
                    
                case 0x1B: // ELEMENT_TYPE_FNPTR
                    // Function pointer - skip method signature
                    var fHeader = blob.ReadSignatureHeader();
                    var fParamCount = blob.ReadCompressedInteger();
                    SkipSignatureType(ref blob); // Return type
                    for (int i = 0; i < fParamCount; i++)
                        SkipSignatureType(ref blob);
                    break;
                    
                case 0x41: // ELEMENT_TYPE_SENTINEL
                    // Just a marker, nothing to skip
                    break;
                    
                default:
                    // Unknown type - can't reliably skip
                    break;
            }
        }
        
        /// <summary>
        /// Decode a type from a signature blob (ECMA-335 II.23.2.12)
        /// </summary>
        private static Mono.Cecil.TypeReference DecodeSignatureType(Mono.Cecil.ModuleDefinition module, MetadataReader md, ref BlobReader blob)
        {
            var typeCode = blob.ReadCompressedInteger();
            
            switch (typeCode)
            {
                case 0x01: return module.TypeSystem.Void;
                case 0x02: return module.TypeSystem.Boolean;
                case 0x03: return module.TypeSystem.Char;
                case 0x04: return module.TypeSystem.SByte;
                case 0x05: return module.TypeSystem.Byte;
                case 0x06: return module.TypeSystem.Int16;
                case 0x07: return module.TypeSystem.UInt16;
                case 0x08: return module.TypeSystem.Int32;
                case 0x09: return module.TypeSystem.UInt32;
                case 0x0A: return module.TypeSystem.Int64;
                case 0x0B: return module.TypeSystem.UInt64;
                case 0x0C: return module.TypeSystem.Single;
                case 0x0D: return module.TypeSystem.Double;
                case 0x0E: return module.TypeSystem.String;
                case 0x18: return module.TypeSystem.IntPtr;
                case 0x19: return module.TypeSystem.UIntPtr;
                case 0x1C: return module.TypeSystem.Object;
                    
                case 0x11: // ELEMENT_TYPE_VALUETYPE
                case 0x12: // ELEMENT_TYPE_CLASS
                    var typeToken = blob.ReadCompressedInteger();
                    // TypeDefOrRefOrSpecEncoded: low 2 bits encode table, rest is row
                    var tableType = typeToken & 0x3;
                    var rowNumber = typeToken >> 2;
                    
                    if (tableType == 0) // TypeDef
                    {
                        var tdHandle = MetadataTokens.TypeDefinitionHandle(rowNumber);
                        if (!tdHandle.IsNil)
                        {
                            var td = md.GetTypeDefinition(tdHandle);
                            var name = md.GetString(td.Name);
                            var ns = md.GetString(td.Namespace);
                            var existing = module.Types.FirstOrDefault(t => t.Name == name && t.Namespace == ns);
                            if (existing != null)
                                return existing;
                            return new Mono.Cecil.TypeReference(ns, name, module, module);
                        }
                    }
                    else if (tableType == 1) // TypeRef
                    {
                        var trHandle = MetadataTokens.TypeReferenceHandle(rowNumber);
                        if (!trHandle.IsNil)
                        {
                            var tr = md.GetTypeReference(trHandle);
                            var name = md.GetString(tr.Name);
                            var ns = md.GetString(tr.Namespace);
                            
                            // Find the scope (assembly reference)
                            Mono.Cecil.IMetadataScope scope = module;
                            var resScope = tr.ResolutionScope;
                            if (resScope.Kind == HandleKind.AssemblyReference)
                            {
                                var arHandle = (AssemblyReferenceHandle)resScope;
                                var ar = md.GetAssemblyReference(arHandle);
                                var arName = md.GetString(ar.Name);
                                scope = module.AssemblyReferences.FirstOrDefault(a => a.Name == arName) ?? (Mono.Cecil.IMetadataScope)module;
                            }
                            
                            return new Mono.Cecil.TypeReference(ns, name, module, scope);
                        }
                    }
                    return module.TypeSystem.Object;
                    
                case 0x1D: // ELEMENT_TYPE_SZARRAY
                    var elemType = DecodeSignatureType(module, md, ref blob);
                    return new Mono.Cecil.ArrayType(elemType ?? module.TypeSystem.Object);
                    
                case 0x0F: // ELEMENT_TYPE_PTR
                    var ptrElemType = DecodeSignatureType(module, md, ref blob);
                    return new Mono.Cecil.PointerType(ptrElemType ?? module.TypeSystem.Object);
                    
                case 0x10: // ELEMENT_TYPE_BYREF
                    var byRefElemType = DecodeSignatureType(module, md, ref blob);
                    return new Mono.Cecil.ByReferenceType(byRefElemType ?? module.TypeSystem.Object);
                    
                default:
                    // For other complex types, skip and return Object
                    return module.TypeSystem.Object;
            }
        }
        
        /// <summary>
        /// Decode serialization type from custom attribute named argument (ECMA-335 II.23.3 FieldOrPropType)
        /// </summary>
        private static Mono.Cecil.TypeReference DecodeSerializationType(Mono.Cecil.ModuleDefinition module, MetadataReader md, ref BlobReader blob)
        {
            var elementType = blob.ReadByte();
            
            switch (elementType)
            {
                case 0x02: return module.TypeSystem.Boolean;
                case 0x03: return module.TypeSystem.Char;
                case 0x04: return module.TypeSystem.SByte;
                case 0x05: return module.TypeSystem.Byte;
                case 0x06: return module.TypeSystem.Int16;
                case 0x07: return module.TypeSystem.UInt16;
                case 0x08: return module.TypeSystem.Int32;
                case 0x09: return module.TypeSystem.UInt32;
                case 0x0A: return module.TypeSystem.Int64;
                case 0x0B: return module.TypeSystem.UInt64;
                case 0x0C: return module.TypeSystem.Single;
                case 0x0D: return module.TypeSystem.Double;
                case 0x0E: return module.TypeSystem.String;
                case 0x50: // SERIALIZATION_TYPE_TYPE
                    return new Mono.Cecil.TypeReference("System", "Type", module, module.TypeSystem.CoreLibrary);
                case 0x51: // SERIALIZATION_TYPE_TAGGED_OBJECT (System.Object boxed)
                    return module.TypeSystem.Object;
                case 0x1D: // ELEMENT_TYPE_SZARRAY
                    var elemType = DecodeSerializationType(module, md, ref blob);
                    return new Mono.Cecil.ArrayType(elemType ?? module.TypeSystem.Object);
                case 0x55: // SERIALIZATION_TYPE_ENUM
                    // Read enum type name as SerString
                    var enumTypeName = ReadSerializedString(ref blob);
                    if (!string.IsNullOrEmpty(enumTypeName))
                    {
                        // Parse the type name (could be "Namespace.Type, Assembly")
                        var typeName = enumTypeName.Split(',')[0].Trim();
                        var lastDot = typeName.LastIndexOf('.');
                        if (lastDot > 0)
                        {
                            var ns = typeName.Substring(0, lastDot);
                            var name = typeName.Substring(lastDot + 1);
                            return new Mono.Cecil.TypeReference(ns, name, module, module);
                        }
                        return new Mono.Cecil.TypeReference("", typeName, module, module);
                    }
                    return module.TypeSystem.Int32; // Default for unknown enum
                default:
                    return module.TypeSystem.Object;
            }
        }
        
        /// <summary>
        /// Read a SerString from a blob (ECMA-335 II.24.2.4)
        /// </summary>
        private static string ReadSerializedString(ref BlobReader blob)
        {
            if (blob.RemainingBytes == 0)
                return null;
            var firstByte = blob.ReadByte();
            if (firstByte == 0xFF)
                return null;
            // Put the byte back (it's part of the compressed length)
            blob.Offset--;
            var strLen = blob.ReadCompressedInteger();
            if (strLen == 0)
                return string.Empty;
            if (strLen > blob.RemainingBytes)
                return null;
            var strBytes = blob.ReadBytes(strLen);
            return System.Text.Encoding.UTF8.GetString(strBytes);
        }
        
        /// <summary>
        /// Decode a custom attribute argument value from a blob (ECMA-335 II.23.3)
        /// </summary>
        private static object DecodeCustomAttributeArgumentValue(Mono.Cecil.ModuleDefinition module, MetadataReader md, ref BlobReader blob, Mono.Cecil.TypeReference paramType)
        {
            if (paramType == null)
                return null;
                
            var typeName = paramType.FullName;
            
            switch (typeName)
            {
                case "System.Boolean":
                    return blob.ReadByte() != 0;
                case "System.Char":
                    return (char)blob.ReadUInt16();
                case "System.SByte":
                    return blob.ReadSByte();
                case "System.Byte":
                    return blob.ReadByte();
                case "System.Int16":
                    return blob.ReadInt16();
                case "System.UInt16":
                    return blob.ReadUInt16();
                case "System.Int32":
                    return blob.ReadInt32();
                case "System.UInt32":
                    return blob.ReadUInt32();
                case "System.Int64":
                    return blob.ReadInt64();
                case "System.UInt64":
                    return blob.ReadUInt64();
                case "System.Single":
                    return blob.ReadSingle();
                case "System.Double":
                    return blob.ReadDouble();
                case "System.String":
                    // SerString - starts with length prefix or 0xFF for null
                    if (blob.RemainingBytes == 0)
                        return null;
                    var firstByte = blob.ReadByte();
                    if (firstByte == 0xFF)
                        return null;
                    // Put the byte back (it's part of the compressed length)
                    blob.Offset--;
                    var strLen = blob.ReadCompressedInteger();
                    if (strLen == 0)
                        return string.Empty;
                    var strBytes = blob.ReadBytes(strLen);
                    return System.Text.Encoding.UTF8.GetString(strBytes);
                case "System.Type":
                    // SerString containing type name
                    if (blob.RemainingBytes == 0)
                        return null;
                    var typeFirstByte = blob.ReadByte();
                    if (typeFirstByte == 0xFF)
                        return null;
                    blob.Offset--;
                    var typeNameLen = blob.ReadCompressedInteger();
                    if (typeNameLen == 0)
                        return null;
                    var typeNameBytes = blob.ReadBytes(typeNameLen);
                    var typeNameStr = System.Text.Encoding.UTF8.GetString(typeNameBytes);
                    // Return as TypeReference - this is a simplified version
                    return new Mono.Cecil.TypeReference("", typeNameStr, module, module);
                case "System.Object":
                    // ELEMENT_TYPE_OBJECT - boxed value with type indicator
                    // First byte indicates the actual type
                    if (blob.RemainingBytes == 0)
                        return null;
                    var objTypeCode = blob.ReadByte();
                    if (objTypeCode == 0x51) // SERIALIZATION_TYPE_TYPE
                    {
                        // It's a Type argument - read as SerString
                        if (blob.RemainingBytes == 0)
                            return null;
                        var fb = blob.ReadByte();
                        if (fb == 0xFF)
                            return null;
                        blob.Offset--;
                        var len = blob.ReadCompressedInteger();
                        if (len == 0)
                            return null;
                        var bytes = blob.ReadBytes(len);
                        return System.Text.Encoding.UTF8.GetString(bytes);
                    }
                    // For other boxed types, decode based on element type
                    var boxedType = GetTypeFromElementType(module, objTypeCode);
                    if (boxedType != null)
                        return DecodeCustomAttributeArgumentValue(module, md, ref blob, boxedType);
                    return null;
                default:
                    // Check if it's an enum
                    if (paramType is Mono.Cecil.TypeDefinition td && td.IsEnum)
                    {
                        // Read the underlying value based on enum's underlying type
                        return blob.ReadInt32(); // Most enums are int32
                    }
                    // Check if it's an array
                    if (paramType is Mono.Cecil.ArrayType)
                    {
                        // Read array length
                        var arrLen = blob.ReadInt32();
                        if (arrLen == -1)
                            return null;
                        // For now, skip array content
                        return new object[0];
                    }
                    return null;
            }
        }
        
        /// <summary>
        /// Get TypeReference from element type code (for boxed values in custom attributes)
        /// </summary>
        private static Mono.Cecil.TypeReference GetTypeFromElementType(Mono.Cecil.ModuleDefinition module, byte elementType)
        {
            switch (elementType)
            {
                case 0x02: return module.TypeSystem.Boolean;
                case 0x03: return module.TypeSystem.Char;
                case 0x04: return module.TypeSystem.SByte;
                case 0x05: return module.TypeSystem.Byte;
                case 0x06: return module.TypeSystem.Int16;
                case 0x07: return module.TypeSystem.UInt16;
                case 0x08: return module.TypeSystem.Int32;
                case 0x09: return module.TypeSystem.UInt32;
                case 0x0A: return module.TypeSystem.Int64;
                case 0x0B: return module.TypeSystem.UInt64;
                case 0x0C: return module.TypeSystem.Single;
                case 0x0D: return module.TypeSystem.Double;
                case 0x0E: return module.TypeSystem.String;
                default: return null;
            }
        }

        /// <summary>
        /// Resolves an EntityHandle (TypeDef, TypeRef, or TypeSpec) to a Cecil TypeReference
        /// </summary>
        private static Mono.Cecil.TypeReference ResolveTypeHandle(Mono.Cecil.ModuleDefinition module, MetadataReader md, EntityHandle handle)
        {
            if (handle.IsNil)
                return null;
            
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    var tdHandle = (TypeDefinitionHandle)handle;
                    var td = md.GetTypeDefinition(tdHandle);
                    var tdName = md.GetString(td.Name);
                    var tdNs = md.GetString(td.Namespace);
                    // Return a reference to the type we've already added to the module
                    var existing = module.Types.FirstOrDefault(t => t.Name == tdName && t.Namespace == tdNs);
                    if (existing != null)
                        return existing;
                    return new Mono.Cecil.TypeReference(tdNs, tdName, module, module);
                    
                case HandleKind.TypeReference:
                    var trHandle = (TypeReferenceHandle)handle;
                    var tr = md.GetTypeReference(trHandle);
                    var trName = md.GetString(tr.Name);
                    var trNs = md.GetString(tr.Namespace);
                    
                    // Find the scope (assembly reference)
                    Mono.Cecil.IMetadataScope scope = module;
                    var resScope = tr.ResolutionScope;
                    if (resScope.Kind == HandleKind.AssemblyReference)
                    {
                        var arHandle = (AssemblyReferenceHandle)resScope;
                        var ar = md.GetAssemblyReference(arHandle);
                        var arName = md.GetString(ar.Name);
                        scope = module.AssemblyReferences.FirstOrDefault(a => a.Name == arName) ?? (Mono.Cecil.IMetadataScope)module;
                    }
                    
                    return new Mono.Cecil.TypeReference(trNs, trName, module, scope);
                    
                case HandleKind.TypeSpecification:
                    // TypeSpec is for generic instantiations, arrays, pointers, etc.
                    // Decode the signature blob to get the actual type
                    var tsHandle = (TypeSpecificationHandle)handle;
                    var ts = md.GetTypeSpecification(tsHandle);
                    var sigBlob = ts.Signature;
                    if (!sigBlob.IsNil)
                    {
                        var decodedType = SrmSignatureDecoder.DecodeType(module, md, sigBlob);
                        if (decodedType != null)
                            return decodedType;
                    }
                    return module.TypeSystem.Object;
                    
                default:
                    return module.TypeSystem.Object;
            }
        }

        public void Dispose()
        {
            MetadataReader = null;
            PeReader?.Dispose();
            PeReader = null;
            if (peStream != null)
            {
                peStream.Dispose();
                peStream = null;
            }
        }
    }
}
