using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Obfuscar.Metadata.Abstractions;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace Obfuscar.Metadata.Adapters
{
    // SRM-backed adapter using MethodDefinitionHandle and MetadataReader
    public class SrmHandleMethodAdapter : IMethod
    {
        private readonly MetadataReader md;
        private readonly MethodDefinitionHandle handle;
        private readonly string[] parameterTypeFullNames;

        public SrmHandleMethodAdapter(MetadataReader md, MethodDefinitionHandle handle)
        {
            this.md = md;
            this.handle = handle;
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
                    var provider = new SrmTypeNameProvider(md);
                    var decoder = new SignatureDecoder<string, object>(provider, md, null);
                    var ret = decoder.DecodeType(ref reader);
                    return ret ?? string.Empty;
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
                var provider = new SrmTypeNameProvider(md);
                var decoder = new SignatureDecoder<string, object>(provider, md, null);
                var list = new List<string>(paramCount);
                for (int i = 0; i < paramCount; i++)
                {
                    var next = decoder.DecodeType(ref reader);
                    list.Add(next ?? string.Empty);
                }

                return list.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
