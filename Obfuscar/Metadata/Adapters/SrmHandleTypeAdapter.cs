using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    // SRM-backed adapter using TypeDefinitionHandle and MetadataReader
    public class SrmHandleTypeAdapter : IType
    {
        private readonly MetadataReader md;
        private readonly TypeDefinitionHandle handle;
        private readonly string scope;

        public SrmHandleTypeAdapter(MetadataReader md, TypeDefinitionHandle handle, string scope = null)
        {
            this.md = md;
            this.handle = handle;
            this.scope = scope ?? string.Empty;
        }

        private TypeDefinition GetDefinition() => md.GetTypeDefinition(handle);

        public string FullName => BuildFullName(handle);

        public string Scope => scope;

        public string Name => md.GetString(GetDefinition().Name);

        public string Namespace => BuildNamespace(handle);

        public string BaseTypeFullName => GetBaseTypeFullName(handle);

        public IEnumerable<string> InterfaceTypeFullNames
        {
            get
            {
                foreach (var ifaceHandle in GetDefinition().GetInterfaceImplementations())
                {
                    var iface = md.GetInterfaceImplementation(ifaceHandle);
                    var name = GetTypeNameFromHandle(iface.Interface);
                    if (!string.IsNullOrEmpty(name))
                        yield return name;
                }
            }
        }

        public bool IsPublic
        {
            get
            {
                var attrs = GetDefinition().Attributes;
                return (attrs & TypeAttributes.Public) != 0 || (attrs & TypeAttributes.NestedPublic) != 0;
            }
        }

        public bool IsSerializable => (GetDefinition().Attributes & TypeAttributes.Serializable) != 0;

        public bool IsSealed => (GetDefinition().Attributes & TypeAttributes.Sealed) != 0;

        public bool IsAbstract => (GetDefinition().Attributes & TypeAttributes.Abstract) != 0;

        public bool IsEnum => string.Equals(GetBaseTypeFullName(handle), "System.Enum", System.StringComparison.Ordinal);

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attrHandle in GetDefinition().GetCustomAttributes())
                {
                    var typeName = ResolveAttributeTypeFullName(attrHandle);
                    if (!string.IsNullOrEmpty(typeName))
                        yield return typeName;
                }
            }
        }

        public IEnumerable<IField> Fields
        {
            get
            {
                foreach (var fh in GetDefinition().GetFields())
                {
                    yield return new SrmHandleFieldAdapter(md, fh);
                }
            }
        }

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
                    var methodDef = md.GetMethodDefinition((MethodDefinitionHandle)constructorHandle);
                    return GetTypeNameFromHandle(methodDef.GetDeclaringType());
                case HandleKind.MemberReference:
                    var memberRef = md.GetMemberReference((MemberReferenceHandle)constructorHandle);
                    return GetTypeNameFromHandle(memberRef.Parent);
                case HandleKind.MethodSpecification:
                    var spec = md.GetMethodSpecification((MethodSpecificationHandle)constructorHandle);
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
                    return GetTypeFullName((TypeDefinitionHandle)handle);
                case HandleKind.TypeReference:
                    return GetTypeFullName((TypeReferenceHandle)handle);
                case HandleKind.TypeSpecification:
                    return DecodeTypeSpecification((TypeSpecificationHandle)handle);
                default:
                    return string.Empty;
            }
        }

        private string GetTypeFullName(TypeDefinitionHandle handle)
        {
            return BuildFullName(handle);
        }

        private string GetTypeFullName(TypeReferenceHandle handle)
        {
            var tr = md.GetTypeReference(handle);
            var name = md.GetString(tr.Name);
            var ns = md.GetString(tr.Namespace);
            return ComposeTypeName(ns, name);
        }

        private string DecodeTypeSpecification(TypeSpecificationHandle handle)
        {
            try
            {
                var spec = md.GetTypeSpecification(handle);
                var reader = md.GetBlobReader(spec.Signature);
                var provider = new SrmTypeNameProvider(md);
                var decoder = new SignatureDecoder<string, object>(provider, md, null);
                return decoder.DecodeType(ref reader) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ComposeTypeName(string ns, string name)
        {
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        private string BuildFullName(TypeDefinitionHandle typeHandle)
        {
            var td = md.GetTypeDefinition(typeHandle);
            var name = md.GetString(td.Name);
            var declaring = td.GetDeclaringType();
            if (!declaring.IsNil)
            {
                return BuildFullName(declaring) + "/" + name;
            }

            var ns = md.GetString(td.Namespace);
            return ComposeTypeName(ns, name);
        }

        private string BuildNamespace(TypeDefinitionHandle typeHandle)
        {
            var td = md.GetTypeDefinition(typeHandle);
            var ns = md.GetString(td.Namespace);
            if (!string.IsNullOrEmpty(ns))
            {
                return ns;
            }

            var declaring = td.GetDeclaringType();
            return declaring.IsNil ? string.Empty : BuildNamespace(declaring);
        }

        private string GetBaseTypeFullName(TypeDefinitionHandle typeHandle)
        {
            var td = md.GetTypeDefinition(typeHandle);
            var baseType = td.BaseType;
            if (baseType.IsNil)
                return string.Empty;

            switch (baseType.Kind)
            {
                case HandleKind.TypeDefinition:
                    return BuildFullName((TypeDefinitionHandle)baseType);
                case HandleKind.TypeReference:
                    return GetTypeFullName((TypeReferenceHandle)baseType);
                case HandleKind.TypeSpecification:
                    return DecodeTypeSpecification((TypeSpecificationHandle)baseType);
                default:
                    return string.Empty;
            }
        }
    }
}
