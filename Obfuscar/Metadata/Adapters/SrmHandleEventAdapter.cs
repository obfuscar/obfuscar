using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class SrmHandleEventAdapter : IEvent
    {
        private readonly MetadataReader md;
        private readonly EventDefinitionHandle handle;
        private string eventTypeFullName;
        private string declaringTypeFullName;
        private MethodAttributes? addAttributes;
        private MethodAttributes? removeAttributes;

        public SrmHandleEventAdapter(MetadataReader md, EventDefinitionHandle handle)
        {
            this.md = md;
            this.handle = handle;
        }

        internal EventDefinitionHandle Handle => handle;

        private EventDefinition GetDefinition() => md.GetEventDefinition(handle);

        public string Name => md.GetString(GetDefinition().Name);

        public string EventTypeFullName
        {
            get
            {
                if (eventTypeFullName != null)
                    return eventTypeFullName;

                eventTypeFullName = ResolveEventTypeFullName();
                return eventTypeFullName;
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

        public MethodAttributes AddMethodAttributes
        {
            get
            {
                if (addAttributes.HasValue)
                    return addAttributes.Value;

                addAttributes = ResolveMethodAttributes(GetDefinition().GetAccessors().Adder);
                return addAttributes.Value;
            }
        }

        public MethodAttributes RemoveMethodAttributes
        {
            get
            {
                if (removeAttributes.HasValue)
                    return removeAttributes.Value;

                removeAttributes = ResolveMethodAttributes(GetDefinition().GetAccessors().Remover);
                return removeAttributes.Value;
            }
        }

        public bool IsRuntimeSpecialName =>
            (GetDefinition().Attributes & System.Reflection.EventAttributes.RTSpecialName) != 0;

        public bool IsPublic
        {
            get
            {
                var accessors = GetDefinition().GetAccessors();
                return IsMethodPublic(accessors.Adder) || IsMethodPublic(accessors.Remover);
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

        private string ResolveEventTypeFullName()
        {
            var typeHandle = GetDefinition().Type;
            return GetTypeNameFromHandle(typeHandle);
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
                foreach (var evtHandle in typeDef.GetEvents())
                {
                    if (evtHandle == handle)
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
                case HandleKind.TypeSpecification:
                    return DecodeTypeSpecification((TypeSpecificationHandle)handle);
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

        private string DecodeTypeSpecification(TypeSpecificationHandle typeHandle)
        {
            try
            {
                var spec = md.GetTypeSpecification(typeHandle);
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
    }
}
