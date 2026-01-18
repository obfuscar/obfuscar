using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Obfuscar;
using Obfuscar.Metadata.Abstractions;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

namespace Obfuscar.Metadata.Adapters
{
    // SRM-backed adapter using MethodDefinitionHandle and MetadataReader
    public class SrmHandleMethodAdapter : IMethod
    {
        private readonly MetadataReader md;
        private readonly MethodDefinitionHandle handle;
        private readonly string[] parameterTypeFullNames;
        private readonly Mono.Cecil.ModuleDefinition module;
        private MethodSemantics? semanticsAttributes;

        public SrmHandleMethodAdapter(Mono.Cecil.ModuleDefinition module, MetadataReader md, MethodDefinitionHandle handle)
        {
            this.md = md;
            this.handle = handle;
            this.module = module;
            this.parameterTypeFullNames = ExtractParameterTypeFullNames();
        }

        private MethodDefinition GetDefinition() => md.GetMethodDefinition(handle);

        public string Name => md.GetString(GetDefinition().Name);

        public string ReturnTypeFullName
        {
            get
            {
                var sig = GetDefinition().Signature;
                if (sig.IsNil) return string.Empty;
                try
                {
                    var reader = md.GetBlobReader(sig);
                    // Skip calling convention and generic param count if present
                    byte callingConv = reader.ReadByte();
                    if ((callingConv & 0x10) != 0)
                        _ = reader.ReadCompressedInteger();
                    _ = reader.ReadCompressedInteger(); // param count
                    if (module == null)
                    {
                        return string.Empty;
                    }
                    var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md);
                    var decoder = new SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                    var ret = decoder.DecodeType(ref reader) ?? module.TypeSystem.Object;
                    return TypeNameCache.GetTypeName(ret);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string DeclaringTypeFullName
        {
            get
            {
                var td = md.GetTypeDefinition(GetDefinition().GetDeclaringType());
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }
        }

        public MethodAttributes Attributes => (MethodAttributes) GetDefinition().Attributes;

        public MethodSemantics SemanticsAttributes
        {
            get
            {
                if (semanticsAttributes.HasValue)
                    return semanticsAttributes.Value;

                semanticsAttributes = ResolveSemanticsAttributes();
                return semanticsAttributes.Value;
            }
        }

        public bool IsRuntime => (GetDefinition().ImplAttributes & MethodImplAttributes.Runtime) != 0;

        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;

        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

        public bool IsFamilyOrAssembly =>
            (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

        public IReadOnlyList<string> ParameterTypeFullNames => parameterTypeFullNames;

        private string[] ExtractParameterTypeFullNames()
        {
            var sig = GetDefinition().Signature;
            if (sig.IsNil)
                return Array.Empty<string>();

            try
            {
                var reader = md.GetBlobReader(sig);
                byte callingConv = reader.ReadByte();
                if ((callingConv & 0x10) != 0)
                    _ = reader.ReadCompressedInteger();
                int paramCount = reader.ReadCompressedInteger();
                var provider = new SrmSignatureDecoder.SimpleTypeProvider(module, md);
                var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                _ = decoder.DecodeType(ref reader);
                var list = new List<string>(paramCount);
                for (int i = 0; i < paramCount; i++)
                {
                    var next = decoder.DecodeType(ref reader) ?? module.TypeSystem.Object;
                    list.Add(TypeNameCache.GetTypeName(next));
                }

                return list.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private MethodSemantics ResolveSemanticsAttributes()
        {
            foreach (var propHandle in md.PropertyDefinitions)
            {
                var accessors = md.GetPropertyDefinition(propHandle).GetAccessors();
                if (!accessors.Getter.IsNil && accessors.Getter == handle)
                    return MethodSemantics.Getter;
                if (!accessors.Setter.IsNil && accessors.Setter == handle)
                    return MethodSemantics.Setter;
            }

            foreach (var evtHandle in md.EventDefinitions)
            {
                var accessors = md.GetEventDefinition(evtHandle).GetAccessors();
                if (!accessors.Adder.IsNil && accessors.Adder == handle)
                    return MethodSemantics.AddOn;
                if (!accessors.Remover.IsNil && accessors.Remover == handle)
                    return MethodSemantics.RemoveOn;
                if (!accessors.Raiser.IsNil && accessors.Raiser == handle)
                    return MethodSemantics.Fire;
            }

            return MethodSemantics.Other;
        }
    }
}
