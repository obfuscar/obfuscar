using System;
using System.Collections.Generic;
using LeXtudio.Metadata.Abstractions;

namespace Obfuscar.Helpers
{
    internal static class ObfuscationExtensions
    {
        private static bool? GetMarkedToRename(IEnumerable<ICustomAttribute> attributes, bool fromMember)
        {
            if (attributes == null)
                return null;

#pragma warning disable 618
            var obfuscarObfuscate = typeof(ObfuscateAttribute).FullName;
#pragma warning restore 618
            var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

            foreach (var attr in attributes)
            {
                if (attr == null || string.IsNullOrEmpty(attr.AttributeTypeName))
                    continue;

                if (attr.AttributeTypeName == obfuscarObfuscate)
                {
                    var val = Helper.GetAttributePropertyByName(attr, "ShouldObfuscate");
                    if (val == null)
                        return true;
                    try { return Convert.ToBoolean(val); } catch { return true; }
                }

                if (attr.AttributeTypeName == reflectionObfuscate)
                {
                    var applyToMembersObj = Helper.GetAttributePropertyByName(attr, "ApplyToMembers");
                    var excludeObj = Helper.GetAttributePropertyByName(attr, "Exclude");

                    bool applyToMembers = applyToMembersObj == null ? true : Convert.ToBoolean(applyToMembersObj);
                    bool exclude = excludeObj == null ? true : Convert.ToBoolean(excludeObj);
                    var rename = !exclude;

                    if (fromMember && !applyToMembers)
                        continue;

                    return rename;
                }
            }

            return null;
        }

        public static bool? MarkedToRename(this ITypeDefinition type)
        {
            var result = GetMarkedToRename(type?.CustomAttributes, false);
            if (result != null)
                return result;

            // If not found on this type, check declaring types (nested types inherit parent's attribute semantics)
            var parent = type?.DeclaringType;
            while (parent != null)
            {
                var parentResult = GetMarkedToRename(parent?.CustomAttributes, true);
                if (parentResult != null)
                    return parentResult;
                parent = parent.DeclaringType;
            }

            return null;
        }

        public static bool? MarkedToRenameForMembers(this ITypeDefinition type)
        {
            var result = GetMarkedToRename(type?.CustomAttributes, true);
            if (result != null)
                return result;

            // If not found on this type, check declaring types for member-affecting attributes
            var parent = type?.DeclaringType;
            while (parent != null)
            {
                var parentResult = GetMarkedToRename(parent?.CustomAttributes, true);
                if (parentResult != null)
                    return parentResult;
                parent = parent.DeclaringType;
            }

            return null;
        }

        public static bool? MarkedToRename(this IMethodDefinition method)
        {
            var result = GetMarkedToRename(method?.CustomAttributes, false);
            if (result != null)
                return result;

            return method?.DeclaringType?.MarkedToRenameForMembers();
        }

        public static bool? MarkedToRename(this IFieldDefinition field)
        {
            var result = GetMarkedToRename(field?.CustomAttributes, false);
            if (result != null)
                return result;

            // Declaring type is not available on IFieldDefinition; callers should
            // check the declaring type (e.g., FieldKey.DeclaringType) when needed.
            return null;
        }

        public static bool? MarkedToRename(this IPropertyDefinition property)
        {
            var result = GetMarkedToRename(property?.CustomAttributes, false);
            if (result != null)
                return result;

            return property?.DeclaringType?.MarkedToRenameForMembers();
        }

        public static bool? MarkedToRename(this IEventDefinition evt)
        {
            var result = GetMarkedToRename(evt?.CustomAttributes, false);
            if (result != null)
                return result;

            return evt?.DeclaringType?.MarkedToRenameForMembers();
        }
    }
}
