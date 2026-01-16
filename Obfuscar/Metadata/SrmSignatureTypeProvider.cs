using System;
using Mono.Cecil;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Collections.Immutable;

namespace Obfuscar.Metadata
{
    class SrmSignatureTypeProvider : ISignatureTypeProvider<TypeReference, ModuleDefinition>
    {
        private readonly ModuleDefinition module;

        public SrmSignatureTypeProvider(ModuleDefinition module)
        {
            this.module = module;
        }

        public TypeReference GetArrayType(TypeReference elementType, ArrayShape shape) => elementType;
        public TypeReference GetByReferenceType(TypeReference elementType) => elementType;
        public TypeReference GetFunctionPointerType(MethodSignature<TypeReference> signature) => module.TypeSystem.Object;
        public TypeReference GetGenericInstantiation(TypeReference genericType, ImmutableArray<TypeReference> typeArguments) => genericType;
        public TypeReference GetGenericMethodParameter(ModuleDefinition genericContext, int index) => module.TypeSystem.Object;
        public TypeReference GetGenericTypeParameter(ModuleDefinition genericContext, int index) => module.TypeSystem.Object;
        public TypeReference GetPinnedType(TypeReference elementType) => elementType;
        public TypeReference GetPointerType(TypeReference elementType) => elementType;
        public TypeReference GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Void: return module.TypeSystem.Void;
                case PrimitiveTypeCode.Boolean: return module.TypeSystem.Boolean;
                case PrimitiveTypeCode.Char: return module.TypeSystem.Char;
                case PrimitiveTypeCode.SByte: return module.TypeSystem.SByte;
                case PrimitiveTypeCode.Byte: return module.TypeSystem.Byte;
                case PrimitiveTypeCode.Int16: return module.TypeSystem.Int16;
                case PrimitiveTypeCode.UInt16: return module.TypeSystem.UInt16;
                case PrimitiveTypeCode.Int32: return module.TypeSystem.Int32;
                case PrimitiveTypeCode.UInt32: return module.TypeSystem.UInt32;
                case PrimitiveTypeCode.Int64: return module.TypeSystem.Int64;
                case PrimitiveTypeCode.UInt64: return module.TypeSystem.UInt64;
                case PrimitiveTypeCode.Single: return module.TypeSystem.Single;
                case PrimitiveTypeCode.Double: return module.TypeSystem.Double;
                case PrimitiveTypeCode.String: return module.TypeSystem.String;
                case PrimitiveTypeCode.Object: return module.TypeSystem.Object;
                default: return module.TypeSystem.Object;
            }
        }

        public TypeReference GetSZArrayType(TypeReference elementType) => elementType;

        public TypeReference GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var td = reader.GetTypeDefinition(handle);
            var name = reader.GetString(td.Name);
            var ns = reader.GetString(td.Namespace);
            return new TypeReference(ns, name, module, module);
        }

        public TypeReference GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var tr = reader.GetTypeReference(handle);
            var name = reader.GetString(tr.Name);
            var ns = reader.GetString(tr.Namespace);
            var scope = GetScopeFromResolution(reader, tr.ResolutionScope);
            return new TypeReference(ns, name, module, scope);
        }

        public TypeReference GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            // best-effort: return object
            return module.TypeSystem.Object;
        }

        private IMetadataScope GetScopeFromResolution(MetadataReader reader, EntityHandle scope)
        {
            if (scope.Kind == HandleKind.AssemblyReference)
            {
                var aref = reader.GetAssemblyReference((AssemblyReferenceHandle)scope);
                var name = reader.GetString(aref.Name);
                var ver = aref.Version;
                return new AssemblyNameReference(name, new Version(ver.Major, ver.Minor, ver.Build, ver.Revision));
            }
            return module;
        }
    }
}
