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
        }

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

            // Create assembly with a module name based on the file name
            string moduleName = Path.GetFileName(path);
            materializedAssembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(an, moduleName, Mono.Cecil.ModuleKind.Dll);

            var module = materializedAssembly.MainModule;

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

            // Populate top-level types (minimal stub TypeDefinition for lookups)
            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);

                // skip <Module> synthetic type
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ns))
                    continue;
                var typeDef = new Mono.Cecil.TypeDefinition(ns, name, Mono.Cecil.TypeAttributes.NotPublic);
                module.Types.Add(typeDef);

                // Populate methods for this type
                foreach (var mh in td.GetMethods())
                {
                    var mdMethod = md.GetMethodDefinition(mh);
                    var mname = md.GetString(mdMethod.Name);
                    var matt = (Mono.Cecil.MethodAttributes)mdMethod.Attributes;
                    var mdef = new Mono.Cecil.MethodDefinition(mname, matt, module.TypeSystem.Object);
                    typeDef.Methods.Add(mdef);

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

                // Populate properties (stub) for this type
                foreach (var ph in td.GetProperties())
                {
                    var mdProp = md.GetPropertyDefinition(ph);
                    var pname = md.GetString(mdProp.Name);
                    var patt = (Mono.Cecil.PropertyAttributes)mdProp.Attributes;
                    var pdef = new Mono.Cecil.PropertyDefinition(pname, patt, module.TypeSystem.Object);
                    typeDef.Properties.Add(pdef);
                }
            }

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
                        operand = start + rel8 + 1;
                        break;
                    case OperandType.InlineBrTarget:
                        int rel32 = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        operand = start + rel32 + 4 + 1;
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
                        for (int i = 0; i < count; i++)
                        {
                            int rel = BitConverter.ToInt32(il, offset);
                            offset += 4;
                            targets[i] = start + rel + 4 + 1;
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
                        // Parent type
                        string pns = null; string pname = null;
                        if (mr.Parent.Kind == HandleKind.TypeReference)
                        {
                            var ptr = md.GetTypeReference((TypeReferenceHandle)mr.Parent);
                            pns = md.GetString(ptr.Namespace);
                            pname = md.GetString(ptr.Name);
                        }
                        else if (mr.Parent.Kind == HandleKind.TypeDefinition)
                        {
                            var ptd = md.GetTypeDefinition((TypeDefinitionHandle)mr.Parent);
                            pns = md.GetString(ptd.Namespace);
                            pname = md.GetString(ptd.Name);
                        }
                        Mono.Cecil.TypeReference parent = pns != null ? new Mono.Cecil.TypeReference(pns, pname, module, module) : module.TypeSystem.Object;
                        // Heuristic: if signature blob indicates field/member? We default to MethodReference
                        return new Mono.Cecil.MethodReference(name, module.TypeSystem.Object) { DeclaringType = parent };
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
