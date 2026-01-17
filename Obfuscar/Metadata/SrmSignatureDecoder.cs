using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Immutable;
using Mono.Cecil;

namespace Obfuscar.Metadata
{
    // Minimal signature decoder: converts SRM blobs into Mono.Cecil.TypeReference stubs.
    // This intentionally avoids ambiguous type names by fully qualifying Mono.Cecil types in code.
    internal static class SrmSignatureDecoder
    {
        public static Mono.Cecil.TypeReference DecodeType(Mono.Cecil.ModuleDefinition module, MetadataReader md, BlobHandle signature)
        {
            if (signature.IsNil)
                return module.TypeSystem.Object;

            var reader = md.GetBlobReader(signature);
            try
            {
                var provider = new SimpleTypeProvider(module, md);
                var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                var type = decoder.DecodeType(ref reader);
                return type ?? module.TypeSystem.Object;
            }
            catch
            {
                return module.TypeSystem.Object;
            }
        }

        public sealed class MethodSignatureResult
        {
            public Mono.Cecil.TypeReference ReturnType { get; set; }
            public Mono.Cecil.TypeReference[] ParameterTypes { get; set; }
        }

        public static MethodSignatureResult DecodeMethodSignature(Mono.Cecil.ModuleDefinition module, MetadataReader md, BlobHandle signature)
        {
            var empty = new MethodSignatureResult { ReturnType = module.TypeSystem.Object, ParameterTypes = new Mono.Cecil.TypeReference[0] };
            if (signature.IsNil)
                return empty;
            var reader = md.GetBlobReader(signature);
            try
            {
                var provider = new SimpleTypeProvider(module, md);
                var decoder = new System.Reflection.Metadata.Ecma335.SignatureDecoder<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, md, module);
                // Method signature layout: callingconv, (generic param count), param count, return type, params...
                byte callingConv = reader.ReadByte();
                if ((callingConv & 0x10) != 0)
                {
                    // generic method: skip generic param count
                    _ = reader.ReadCompressedInteger();
                }
                int paramCount = reader.ReadCompressedInteger();
                var returnType = decoder.DecodeType(ref reader) ?? module.TypeSystem.Object;
                var parameters = new Mono.Cecil.TypeReference[paramCount];
                for (int i = 0; i < paramCount; i++)
                {
                    parameters[i] = decoder.DecodeType(ref reader) ?? module.TypeSystem.Object;
                }
                return new MethodSignatureResult { ReturnType = returnType, ParameterTypes = parameters };
            }
            catch
            {
                return empty;
            }
        }

        public static Mono.Cecil.TypeReference[] DecodeLocalVariables(Mono.Cecil.ModuleDefinition module, MetadataReader md, StandaloneSignatureHandle handle)
        {
            if (handle.IsNil)
                return Array.Empty<Mono.Cecil.TypeReference>();
            try
            {
                var ss = md.GetStandaloneSignature(handle);
                var provider = new SimpleTypeProvider(module, md);
                var arr = ss.DecodeLocalSignature<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>(provider, module);
                if (arr.IsDefaultOrEmpty)
                    return Array.Empty<Mono.Cecil.TypeReference>();
                var res = new Mono.Cecil.TypeReference[arr.Length];
                for (int i = 0; i < arr.Length; i++) res[i] = arr[i] ?? module.TypeSystem.Object;
                return res;
            }
            catch
            {
                return Array.Empty<Mono.Cecil.TypeReference>();
            }
        }

        internal sealed class SimpleTypeProvider : ISignatureTypeProvider<Mono.Cecil.TypeReference, Mono.Cecil.ModuleDefinition>
        {
            private readonly Mono.Cecil.ModuleDefinition module;
            private readonly MetadataReader md;

            public SimpleTypeProvider(Mono.Cecil.ModuleDefinition module, MetadataReader md)
            {
                this.module = module;
                this.md = md;
            }

            public Mono.Cecil.TypeReference GetArrayType(Mono.Cecil.TypeReference elementType, ArrayShape shape)
            {
                // Create multi-dimensional array type
                // Mono.Cecil ArrayType rank is read-only; it's set via constructor
                if (shape.Rank == 0 || shape.Rank == 1)
                    return new Mono.Cecil.ArrayType(elementType); // rank 1 array
                // For rank > 1, create ArrayType with default rank (1) as a simplification
                return new Mono.Cecil.ArrayType(elementType);
            }

            public Mono.Cecil.TypeReference GetByReferenceType(Mono.Cecil.TypeReference elementType)
            {
                // Create ref/out parameter type
                return new Mono.Cecil.ByReferenceType(elementType);
            }

            public Mono.Cecil.TypeReference GetFunctionPointerType(MethodSignature<Mono.Cecil.TypeReference> signature)
            {
                // Function pointers are rare in metadata; fallback to object
                return module.TypeSystem.Object;
            }

            public Mono.Cecil.TypeReference GetGenericInstantiation(Mono.Cecil.TypeReference genericType, ImmutableArray<Mono.Cecil.TypeReference> typeArguments)
            {
                // Create generic instance type with type arguments
                if (typeArguments.IsDefaultOrEmpty || genericType == null)
                    return genericType ?? module.TypeSystem.Object;
                var genericInstance = new Mono.Cecil.GenericInstanceType(genericType);
                foreach (var arg in typeArguments)
                {
                    genericInstance.GenericArguments.Add(arg ?? module.TypeSystem.Object);
                }
                return genericInstance;
            }

            public Mono.Cecil.TypeReference GetGenericMethodParameter(Mono.Cecil.ModuleDefinition provider, int index)
            {
                // Method generic parameter (e.g., T in void Foo<T>(T t))
                // Return a GenericParameter representing !!index (method generic parameter)
                // We need to create a dummy MethodReference as owner so the GenericParameter
                // correctly identifies as GenericParameterType.Method
                var dummyType = module.Types.Count > 0 ? module.Types[0] : new Mono.Cecil.TypeDefinition("", "DummyType", Mono.Cecil.TypeAttributes.NotPublic);
                var dummyMethod = new Mono.Cecil.MethodDefinition("DummyMethod", Mono.Cecil.MethodAttributes.Private, module.TypeSystem.Void);
                dummyType.Methods.Add(dummyMethod);
                var gp = new Mono.Cecil.GenericParameter("T" + index, dummyMethod);
                return gp;
            }

            public Mono.Cecil.TypeReference GetGenericTypeParameter(Mono.Cecil.ModuleDefinition provider, int index)
            {
                // Type generic parameter (e.g., T in class Foo<T>)
                // Return a GenericParameter representing !index (type generic parameter)
                var owner = module.Types.Count > 0 ? module.Types[0] : null;
                var gp = new Mono.Cecil.GenericParameter("T" + index, owner);
                return gp;
            }

            public Mono.Cecil.TypeReference GetModifiedType(Mono.Cecil.TypeReference unmodifiedType, Mono.Cecil.TypeReference modifier, bool isRequired)
            {
                // Managed pointers (modopt/modreq); for simplicity, return unmodified type
                return unmodifiedType;
            }

            public Mono.Cecil.TypeReference GetPinnedType(Mono.Cecil.TypeReference elementType)
            {
                // Pinned types in local signatures; return element type for analysis
                return elementType;
            }

            public Mono.Cecil.TypeReference GetPointerType(Mono.Cecil.TypeReference elementType)
            {
                // Unmanaged pointer
                return new Mono.Cecil.PointerType(elementType);
            }

            public Mono.Cecil.TypeReference GetPrimitiveType(PrimitiveTypeCode typeCode)
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

            public Mono.Cecil.TypeReference GetSZArrayType(Mono.Cecil.TypeReference elementType)
            {
                // Single-dimensional zero-based array (most common)
                return new Mono.Cecil.ArrayType(elementType);
            }

            public Mono.Cecil.TypeReference GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var td = reader.GetTypeDefinition(handle);
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);
                
                // Try to find the existing TypeDefinition in the module
                // This is important so that when types are renamed, the references are updated too
                string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
                var existingType = module.GetType(fullName);
                if (existingType != null)
                    return existingType;
                
                // Also check nested types and types with generic arity suffix
                foreach (var type in module.Types)
                {
                    if (type.Name == name && type.Namespace == ns)
                        return type;
                    // Check nested types
                    var nested = FindNestedType(type, name, ns);
                    if (nested != null)
                        return nested;
                }
                
                // Fallback to creating a TypeReference if not found
                return new Mono.Cecil.TypeReference(ns, name, module, module);
            }
            
            private static Mono.Cecil.TypeDefinition FindNestedType(Mono.Cecil.TypeDefinition parent, string name, string ns)
            {
                foreach (var nested in parent.NestedTypes)
                {
                    if (nested.Name == name)
                        return nested;
                    var found = FindNestedType(nested, name, ns);
                    if (found != null)
                        return found;
                }
                return null;
            }

            public Mono.Cecil.TypeReference GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var tr = reader.GetTypeReference(handle);
                var name = reader.GetString(tr.Name);
                var ns = reader.GetString(tr.Namespace);
                return new Mono.Cecil.TypeReference(ns, name, module, module);
            }
            public Mono.Cecil.TypeReference GetTypeFromSpecification(MetadataReader reader, Mono.Cecil.ModuleDefinition genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                return module.TypeSystem.Object;
            }
        }
    }
}
