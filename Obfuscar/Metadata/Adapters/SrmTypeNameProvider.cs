using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Obfuscar.Metadata.Adapters
{
    internal sealed class SrmTypeNameProvider : ISignatureTypeProvider<string, object>
    {
        private readonly MetadataReader md;

        public SrmTypeNameProvider(MetadataReader md)
        {
            this.md = md;
        }

        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[]";
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
        public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "System.Object";
    }
}
