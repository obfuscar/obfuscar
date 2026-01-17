using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    // SRM-backed adapter using FieldDefinitionHandle and MetadataReader
    public class SrmHandleFieldAdapter : IField
    {
        private readonly MetadataReader md;
        private readonly FieldDefinitionHandle handle;

        public SrmHandleFieldAdapter(MetadataReader md, FieldDefinitionHandle handle)
        {
            this.md = md;
            this.handle = handle;
        }

        private FieldDefinition GetDefinition() => md.GetFieldDefinition(handle);

        public string Name => md.GetString(GetDefinition().Name);

        public string FieldTypeFullName
        {
            get
            {
                var fd = GetDefinition();
                var sig = fd.Signature;
                if (sig.IsNil) return string.Empty;
                try
                {
                    var reader = md.GetBlobReader(sig);
                    var provider = new SrmTypeNameProvider(md);
                    var decoder = new SignatureDecoder<string, object>(provider, md, null);
                    var name = decoder.DecodeType(ref reader);
                    return name ?? string.Empty;
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

        public FieldAttributes Attributes => (FieldAttributes) GetDefinition().Attributes;

        public bool HasCustomAttributes => GetDefinition().GetCustomAttributes().Count > 0;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var handle in GetDefinition().GetCustomAttributes())
                {
                    var typeName = ResolveAttributeTypeFullName(handle);
                    if (!string.IsNullOrEmpty(typeName))
                        yield return typeName;
                }
            }
        }

        public bool IsStatic => (GetDefinition().Attributes & FieldAttributes.Static) != 0;

        private string ResolveAttributeTypeFullName(CustomAttributeHandle handle)
        {
            var attribute = md.GetCustomAttribute(handle);
            return GetTypeNameFromMethodConstructor(attribute.Constructor);
        }

        private string GetTypeNameFromMethodConstructor(EntityHandle constructorHandle)
        {
            switch (constructorHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    var methodDef = md.GetMethodDefinition((MethodDefinitionHandle) constructorHandle);
                    return GetTypeNameFromHandle(methodDef.GetDeclaringType());
                case HandleKind.MemberReference:
                    var memberRef = md.GetMemberReference((MemberReferenceHandle) constructorHandle);
                    return GetTypeNameFromHandle(memberRef.Parent);
                case HandleKind.MethodSpecification:
                    var spec = md.GetMethodSpecification((MethodSpecificationHandle) constructorHandle);
                    return GetTypeNameFromMethodConstructor(spec.Method);
                default:
                    return string.Empty;
            }
        }

        private string GetTypeNameFromHandle(EntityHandle handle)
        {
                switch (handle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        return GetTypeFullName((TypeDefinitionHandle) handle);
                    case HandleKind.TypeReference:
                        return GetTypeFullName((TypeReferenceHandle) handle);
                    default:
                        return string.Empty;
                }
        }

        private string GetTypeFullName(TypeDefinitionHandle handle)
        {
            var td = md.GetTypeDefinition(handle);
            var name = md.GetString(td.Name);
            var ns = md.GetString(td.Namespace);
            return ComposeTypeName(ns, name);
        }

        private string GetTypeFullName(TypeReferenceHandle handle)
        {
            var tr = md.GetTypeReference(handle);
            var name = md.GetString(tr.Name);
            var ns = md.GetString(tr.Namespace);
            return ComposeTypeName(ns, name);
        }

        private static string ComposeTypeName(string ns, string name)
        {
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        private sealed class TypeNameProvider : ISignatureTypeProvider<string, object>
        {
            private readonly MetadataReader md;

            public TypeNameProvider(MetadataReader md)
            {
                this.md = md;
            }

            public string GetArrayType(string elementType, ArrayShape shape)
            {
                return elementType + "[]";
            }

            public string GetByReferenceType(string elementType) => elementType + "&";

            public string GetFunctionPointerType(MethodSignature<string> signature) => "System.Object";

            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
            {
                if (typeArguments.IsDefaultOrEmpty)
                    return genericType;
                return genericType + "<" + string.Join(",", typeArguments) + ">";
            }

            public string GetGenericMethodParameter(object genericContext, int index) => "T" + index;

            public string GetGenericTypeParameter(object genericContext, int index) => "T" + index;

            public string GetModifiedType(string unmodifiedType, string modifier, bool isRequired) => unmodifiedType;

            public string GetPinnedType(string elementType) => elementType;

            public string GetPointerType(string elementType) => elementType + "*";

            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Void: return "System.Void";
                    case PrimitiveTypeCode.Boolean: return "System.Boolean";
                    case PrimitiveTypeCode.Char: return "System.Char";
                    case PrimitiveTypeCode.SByte: return "System.SByte";
                    case PrimitiveTypeCode.Byte: return "System.Byte";
                    case PrimitiveTypeCode.Int16: return "System.Int16";
                    case PrimitiveTypeCode.UInt16: return "System.UInt16";
                    case PrimitiveTypeCode.Int32: return "System.Int32";
                    case PrimitiveTypeCode.UInt32: return "System.UInt32";
                    case PrimitiveTypeCode.Int64: return "System.Int64";
                    case PrimitiveTypeCode.UInt64: return "System.UInt64";
                    case PrimitiveTypeCode.Single: return "System.Single";
                    case PrimitiveTypeCode.Double: return "System.Double";
                    case PrimitiveTypeCode.String: return "System.String";
                    case PrimitiveTypeCode.Object: return "System.Object";
                    default: return "System.Object";
                }
            }

            public string GetSZArrayType(string elementType) => elementType + "[]";

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var td = reader.GetTypeDefinition(handle);
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var tr = reader.GetTypeReference(handle);
                var name = reader.GetString(tr.Name);
                var ns = reader.GetString(tr.Namespace);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }

            public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                return "System.Object";
            }
        }
    }
}
