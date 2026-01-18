using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class SrmHandlePropertyAdapter : IProperty
    {
        private readonly MetadataReader md;
        private readonly PropertyDefinitionHandle handle;
        private string propertyTypeFullName;
        private string declaringTypeFullName;
        private MethodAttributes? getterAttributes;
        private MethodAttributes? setterAttributes;

        public SrmHandlePropertyAdapter(MetadataReader md, PropertyDefinitionHandle handle)
        {
            this.md = md;
            this.handle = handle;
        }

        internal PropertyDefinitionHandle Handle => handle;

        private PropertyDefinition GetDefinition() => md.GetPropertyDefinition(handle);

        public string Name => md.GetString(GetDefinition().Name);

        public string PropertyTypeFullName
        {
            get
            {
                if (propertyTypeFullName != null)
                    return propertyTypeFullName;

                propertyTypeFullName = DecodePropertyTypeFullName();
                return propertyTypeFullName;
            }
        }

        public string DeclaringTypeFullName
        {
            get
            {
                if (declaringTypeFullName != null)
                    return declaringTypeFullName;

                declaringTypeFullName = ResolveDeclaringTypeFullName();
                return declaringTypeFullName;
            }
        }

        public MethodAttributes GetterMethodAttributes
        {
            get
            {
                if (getterAttributes.HasValue)
                    return getterAttributes.Value;

                getterAttributes = ResolveMethodAttributes(GetDefinition().GetAccessors().Getter);
                return getterAttributes.Value;
            }
        }

        public MethodAttributes SetterMethodAttributes
        {
            get
            {
                if (setterAttributes.HasValue)
                    return setterAttributes.Value;

                setterAttributes = ResolveMethodAttributes(GetDefinition().GetAccessors().Setter);
                return setterAttributes.Value;
            }
        }

        public bool IsRuntimeSpecialName =>
            (GetDefinition().Attributes & System.Reflection.PropertyAttributes.RTSpecialName) != 0;

        public bool IsPublic
        {
            get
            {
                return IsMethodPublic(GetDefinition().GetAccessors().Getter) ||
                       IsMethodPublic(GetDefinition().GetAccessors().Setter);
            }
        }

        public bool HasCustomAttributes => GetDefinition().GetCustomAttributes().Count > 0;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var caHandle in GetDefinition().GetCustomAttributes())
                {
                    var typeName = ResolveAttributeTypeFullName(caHandle);
                    if (!string.IsNullOrEmpty(typeName))
                        yield return typeName;
                }
            }
        }

        private string DecodePropertyTypeFullName()
        {
            var signature = GetDefinition().Signature;
            if (signature.IsNil)
                return string.Empty;

            try
            {
                var reader = md.GetBlobReader(signature);
                _ = reader.ReadSignatureHeader();
                _ = reader.ReadCompressedInteger(); // param count
                var provider = new SrmTypeNameProvider(md);
                var decoder = new SignatureDecoder<string, object>(provider, md, null);
                return decoder.DecodeType(ref reader) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private MethodAttributes ResolveMethodAttributes(MethodDefinitionHandle methodHandle)
        {
            if (methodHandle.IsNil)
                return 0;

            var method = md.GetMethodDefinition(methodHandle);
            return (MethodAttributes)method.Attributes;
        }

        private bool IsMethodPublic(MethodDefinitionHandle methodHandle)
        {
            if (methodHandle.IsNil)
                return false;

            var method = md.GetMethodDefinition(methodHandle);
            var attrs = (MethodAttributes)method.Attributes;
            return (attrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
        }

        private string ResolveDeclaringTypeFullName()
        {
            foreach (var typeHandle in md.TypeDefinitions)
            {
                var typeDef = md.GetTypeDefinition(typeHandle);
                foreach (var propHandle in typeDef.GetProperties())
                {
                    if (propHandle == handle)
                        return GetTypeFullName(typeHandle);
                }
            }

            return string.Empty;
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
                default:
                    return string.Empty;
            }
        }

        private string GetTypeFullName(TypeDefinitionHandle typeHandle)
        {
            var td = md.GetTypeDefinition(typeHandle);
            var name = md.GetString(td.Name);
            var declaring = td.GetDeclaringType();
            if (!declaring.IsNil)
                return GetTypeFullName(declaring) + "/" + name;

            var ns = md.GetString(td.Namespace);
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        private string GetTypeFullName(TypeReferenceHandle typeHandle)
        {
            var tr = md.GetTypeReference(typeHandle);
            var name = md.GetString(tr.Name);
            var ns = md.GetString(tr.Namespace);
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }
    }
}
