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
                                var ilBytes = bodyBlock.GetILBytes().ToArray();
                                var instructions = DecodeIL(module, md, ilBytes);

                                var mbody = new Mono.Cecil.Cil.MethodBody(mdef);
                                // copy MaxStack if available
                                try { mbody.MaxStackSize = bodyBlock.MaxStack; } catch { }
                                foreach (var instr in instructions)
                                    mbody.Instructions.Add(instr);

                                    // Local variable decoding: try to use MethodDebugInformation local signature when available
                                    try
                                    {
                                        // method handle
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
                                            var handler = new Mono.Cecil.Cil.ExceptionHandler(Mono.Cecil.Cil.ExceptionHandlerType.Catch);
                                            // map offsets to instructions
                                            var map = mbody.Instructions.ToDictionary(i => i.Offset, i => i);
                                            if (map.TryGetValue(eh.TryOffset, out var tstart)) handler.TryStart = tstart;
                                            if (map.TryGetValue(eh.TryOffset + eh.TryLength, out var tend)) handler.TryEnd = tend;
                                            if (map.TryGetValue(eh.HandlerOffset, out var hstart)) handler.HandlerStart = hstart;
                                            if (map.TryGetValue(eh.HandlerOffset + eh.HandlerLength, out var hend)) handler.HandlerEnd = hend;
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
                            }
                        }
                        catch
                        {
                            // best-effort: leave method without body
                        }
                    }
                }

                // Populate fields for this type
                foreach (var fh in td.GetFields())
                {
                    var mdField = md.GetFieldDefinition(fh);
                    var fname = md.GetString(mdField.Name);
                    var fatt = (Mono.Cecil.FieldAttributes)mdField.Attributes;
                    var fdef = new Mono.Cecil.FieldDefinition(fname, fatt, module.TypeSystem.Object);
                    typeDef.Fields.Add(fdef);
                }

                // Populate properties (stub) for this type, linking getter/setter methods
                foreach (var ph in td.GetProperties())
                {
                    var mdProp = md.GetPropertyDefinition(ph);
                    var pname = md.GetString(mdProp.Name);
                    var patt = (Mono.Cecil.PropertyAttributes)mdProp.Attributes;
                    var pdef = new Mono.Cecil.PropertyDefinition(pname, patt, module.TypeSystem.Object);
                    
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
                                break;
                            }
                        }
                    }
                    
                    typeDef.Properties.Add(pdef);
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
                                break;
                            }
                        }
                    }
                    
                    typeDef.Events.Add(edef);
                }
            }

            // Populate custom attributes on all entities
            PopulateCustomAttributes(module, md);

            return materializedAssembly;
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
                        operand = il[offset++];
                        break;
                    case OperandType.InlineI:
                        operand = BitConverter.ToInt32(il, offset);
                        offset += 4;
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

                var instruction = Instruction.Create(op);
                instruction.Offset = start;
                if (operand != null)
                    instruction.Operand = operand;
                result.Add(instruction);
            }

            // Link branch targets: replace numeric offsets with Instruction references
            var map = result.ToDictionary(i => i.Offset, i => i);
            foreach (var instr in result)
            {
                if (instr.Operand is int off)
                {
                    if (map.TryGetValue(off, out var target))
                        instr.Operand = target;
                }
                else if (instr.Operand is int[] offs)
                {
                    var targets = offs.Select(o => map.TryGetValue(o, out var t) ? t : null).ToArray();
                    instr.Operand = targets;
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
                        return new Mono.Cecil.FieldReference(name, module.TypeSystem.Object, parent);
                    }
                case HandleKind.MethodDefinition:
                    {
                        var mdDef = md.GetMethodDefinition(System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(token));
                        var name = md.GetString(mdDef.Name);
                        var declType = FindDeclaringTypeForMember(md, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(token)));
                        Mono.Cecil.TypeReference parent = declType.ns != null ? new Mono.Cecil.TypeReference(declType.ns, declType.name, module, module) : module.TypeSystem.Object;
                        return new Mono.Cecil.MethodReference(name, module.TypeSystem.Object) { DeclaringType = parent };
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
                            // Handle generic type specification
                            parent = module.TypeSystem.Object;
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
                            return new Mono.Cecil.FieldReference(name, module.TypeSystem.Object, parent);
                        }
                        
                        // Method signature - decode parameters and return type
                        var mref = new Mono.Cecil.MethodReference(name, module.TypeSystem.Object) { DeclaringType = parent };
                        
                        // Parse calling convention
                        bool hasThis = (firstByte & 0x20) != 0;
                        mref.HasThis = hasThis;
                        
                        // Check for generic method
                        if ((firstByte & 0x10) != 0)
                        {
                            var genParamCount = sigBlob.ReadCompressedInteger();
                            for (int i = 0; i < genParamCount; i++)
                            {
                                mref.GenericParameters.Add(new Mono.Cecil.GenericParameter("T" + i, mref));
                            }
                        }
                        
                        var paramCount = sigBlob.ReadCompressedInteger();
                        // Skip return type (we already set it to Object)
                        
                        return mref;
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
        private static void PopulateCustomAttributes(Mono.Cecil.ModuleDefinition module, MetadataReader md)
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
                    // For now, return Object as a placeholder
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
