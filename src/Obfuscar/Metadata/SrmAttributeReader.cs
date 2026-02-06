using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Obfuscar.Metadata
{
    internal static class SrmAttributeReader
    {
        public static bool? GetMarkedToRenameForType(MetadataReader md, TypeDefinitionHandle handle)
        {
            return GetMarkedToRename(md, md.GetTypeDefinition(handle).GetCustomAttributes(), false);
        }

        public static bool? GetMarkedToRenameForMethod(MetadataReader md, MethodDefinitionHandle handle)
        {
            var method = md.GetMethodDefinition(handle);
            var result = GetMarkedToRename(md, method.GetCustomAttributes(), false);
            if (result != null)
                return result;

            var declaring = method.GetDeclaringType();
            return declaring.IsNil ? null : GetMarkedToRenameForType(md, declaring, true);
        }

        public static bool? GetMarkedToRenameForField(MetadataReader md, FieldDefinitionHandle handle)
        {
            var field = md.GetFieldDefinition(handle);
            var result = GetMarkedToRename(md, field.GetCustomAttributes(), false);
            if (result != null)
                return result;

            var declaring = field.GetDeclaringType();
            return declaring.IsNil ? null : GetMarkedToRenameForType(md, declaring, true);
        }

        public static bool GetMarkedToRenameForAssembly(MetadataReader md)
        {
            var asm = md.GetAssemblyDefinition();
            foreach (var handle in asm.GetCustomAttributes())
            {
                var attribute = md.GetCustomAttribute(handle);
                var attrTypeName = GetAttributeTypeName(md, attribute.Constructor);
                if (attrTypeName == typeof(ObfuscateAssemblyAttribute).FullName)
                {
                    return GetNamedBool(md, attribute, "AssemblyIsPrivate", true);
                }
            }

            // IMPORTANT: assume it should be renamed.
            return true;
        }

        public static bool? GetMarkedToRenameForProperty(MetadataReader md, PropertyDefinitionHandle handle)
        {
            var prop = md.GetPropertyDefinition(handle);
            var result = GetMarkedToRename(md, prop.GetCustomAttributes(), false);
            if (result != null)
                return result;

            var declaring = FindPropertyDeclaringType(md, handle);
            return declaring.IsNil ? null : GetMarkedToRenameForType(md, declaring, true);
        }

        public static bool? GetMarkedToRenameForEvent(MetadataReader md, EventDefinitionHandle handle)
        {
            var evt = md.GetEventDefinition(handle);
            var result = GetMarkedToRename(md, evt.GetCustomAttributes(), false);
            if (result != null)
                return result;

            var declaring = FindEventDeclaringType(md, handle);
            return declaring.IsNil ? null : GetMarkedToRenameForType(md, declaring, true);
        }

        private static bool? GetMarkedToRenameForType(MetadataReader md, TypeDefinitionHandle handle, bool fromMember)
        {
            var type = md.GetTypeDefinition(handle);
            var result = GetMarkedToRename(md, type.GetCustomAttributes(), fromMember);
            if (result != null)
                return result;

            var declaring = type.GetDeclaringType();
            return declaring.IsNil ? null : GetMarkedToRenameForType(md, declaring, true);
        }

        private static TypeDefinitionHandle FindPropertyDeclaringType(MetadataReader md, PropertyDefinitionHandle handle)
        {
            foreach (var typeHandle in md.TypeDefinitions)
            {
                var typeDef = md.GetTypeDefinition(typeHandle);
                foreach (var propHandle in typeDef.GetProperties())
                {
                    if (propHandle == handle)
                        return typeHandle;
                }
            }

            return default;
        }

        private static TypeDefinitionHandle FindEventDeclaringType(MetadataReader md, EventDefinitionHandle handle)
        {
            foreach (var typeHandle in md.TypeDefinitions)
            {
                var typeDef = md.GetTypeDefinition(typeHandle);
                foreach (var evtHandle in typeDef.GetEvents())
                {
                    if (evtHandle == handle)
                        return typeHandle;
                }
            }

            return default;
        }

        private static bool? GetMarkedToRename(MetadataReader md, CustomAttributeHandleCollection handles, bool fromMember)
        {
#pragma warning disable 618
            var obfuscarObfuscate = typeof(ObfuscateAttribute).FullName;
#pragma warning restore 618
            var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

            foreach (var handle in handles)
            {
                var attribute = md.GetCustomAttribute(handle);
                var attrTypeName = GetAttributeTypeName(md, attribute.Constructor);
                if (attrTypeName == obfuscarObfuscate)
                {
                    var shouldObfuscate = GetNamedBool(md, attribute, "ShouldObfuscate", true);
                    return shouldObfuscate;
                }

                if (attrTypeName == reflectionObfuscate)
                {
                    var applyToMembers = GetNamedBool(md, attribute, "ApplyToMembers", true);
                    var exclude = GetNamedBool(md, attribute, "Exclude", true);
                    var rename = !exclude;

                    if (fromMember && !applyToMembers)
                        continue;

                    return rename;
                }
            }

            return null;
        }

        private static bool GetNamedBool(MetadataReader md, CustomAttribute attribute, string name, bool defaultValue)
        {
            var value = attribute.DecodeValue(new AttributeTypeNameProvider(md));
            foreach (var named in value.NamedArguments)
            {
                if (string.Equals(named.Name, name, StringComparison.Ordinal))
                {
                    if (named.Value is CustomAttributeTypedArgument<string> typed &&
                        typed.Value is bool typedBool)
                        return typedBool;

                    if (named.Value is bool boolValue)
                        return boolValue;
                }
            }

            return defaultValue;
        }

        private static string GetAttributeTypeName(MetadataReader md, EntityHandle constructorHandle)
        {
            switch (constructorHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    var methodDef = md.GetMethodDefinition((MethodDefinitionHandle)constructorHandle);
                    return GetTypeNameFromHandle(md, methodDef.GetDeclaringType());
                case HandleKind.MemberReference:
                    var memberRef = md.GetMemberReference((MemberReferenceHandle)constructorHandle);
                    return GetTypeNameFromHandle(md, memberRef.Parent);
                case HandleKind.MethodSpecification:
                    var spec = md.GetMethodSpecification((MethodSpecificationHandle)constructorHandle);
                    return GetAttributeTypeName(md, spec.Method);
                default:
                    return string.Empty;
            }
        }

        private static string GetTypeNameFromHandle(MetadataReader md, EntityHandle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return GetTypeFullName(md, (TypeDefinitionHandle)handle);
                case HandleKind.TypeReference:
                    return GetTypeFullName(md, (TypeReferenceHandle)handle);
                default:
                    return string.Empty;
            }
        }

        private static string GetTypeFullName(MetadataReader md, TypeDefinitionHandle handle)
        {
            var td = md.GetTypeDefinition(handle);
            var name = md.GetString(td.Name);
            var declaring = td.GetDeclaringType();
            if (!declaring.IsNil)
            {
                return GetTypeFullName(md, declaring) + "/" + name;
            }

            var ns = md.GetString(td.Namespace);
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        private static string GetTypeFullName(MetadataReader md, TypeReferenceHandle handle)
        {
            var tr = md.GetTypeReference(handle);
            var name = md.GetString(tr.Name);
            var ns = md.GetString(tr.Namespace);
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        private sealed class AttributeTypeNameProvider : ICustomAttributeTypeProvider<string>
        {
            private readonly MetadataReader md;

            public AttributeTypeNameProvider(MetadataReader md)
            {
                this.md = md;
            }

            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Boolean: return "System.Boolean";
                    case PrimitiveTypeCode.Byte: return "System.Byte";
                    case PrimitiveTypeCode.Char: return "System.Char";
                    case PrimitiveTypeCode.Double: return "System.Double";
                    case PrimitiveTypeCode.Int16: return "System.Int16";
                    case PrimitiveTypeCode.Int32: return "System.Int32";
                    case PrimitiveTypeCode.Int64: return "System.Int64";
                    case PrimitiveTypeCode.Object: return "System.Object";
                    case PrimitiveTypeCode.SByte: return "System.SByte";
                    case PrimitiveTypeCode.Single: return "System.Single";
                    case PrimitiveTypeCode.String: return "System.String";
                    case PrimitiveTypeCode.UInt16: return "System.UInt16";
                    case PrimitiveTypeCode.UInt32: return "System.UInt32";
                    case PrimitiveTypeCode.UInt64: return "System.UInt64";
                    case PrimitiveTypeCode.Void: return "System.Void";
                    default: return "System.Object";
                }
            }

            public string GetSystemType() => "System.Type";

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return GetTypeFullName(reader, handle);
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return GetTypeFullName(reader, handle);
            }

            public string GetSZArrayType(string elementType) => elementType + "[]";

            public string GetTypeFromSerializedName(string name) => name;

            public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

            public bool IsSystemType(string type) => type == "System.Type";

            public string GetEnumType(string underlyingType, string type) => type;

            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
            {
                return genericType;
            }
        }
    }
}
