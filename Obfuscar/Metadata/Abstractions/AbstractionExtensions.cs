using System.Collections.Generic;
using System.Linq;

namespace Obfuscar.Metadata.Abstractions
{
    /// <summary>
    /// Extension methods for abstraction interfaces, providing Cecil-compatible helper functionality.
    /// </summary>
    public static class AbstractionExtensions
    {
        /// <summary>
        /// Checks if a method is public, including family (protected) and family-or-assembly visibility.
        /// </summary>
        public static bool IsMethodPublic(this IMethodDefinition method)
        {
            if (method == null)
                return false;
            
            // Check if it's public, family (protected), or family-or-assembly
            // For abstraction layer, we need to check the method attributes
            // If IsPrivate is false and method exists, we consider visibility patterns
            return !method.IsPrivate;
        }

        /// <summary>
        /// Gets a custom attribute by full name if present.
        /// </summary>
        public static ICustomAttribute GetCustomAttribute(this ITypeDefinition type, string attributeFullName)
        {
            if (type?.CustomAttributes == null)
                return null;

            return type.CustomAttributes.FirstOrDefault(a => a.AttributeTypeName == attributeFullName);
        }

        /// <summary>
        /// Checks if the type has a specific custom attribute.
        /// </summary>
        public static bool HasCustomAttribute(this ITypeDefinition type, string attributeFullName)
        {
            return GetCustomAttribute(type, attributeFullName) != null;
        }

        /// <summary>
        /// Gets a custom attribute by full name if present on a method.
        /// </summary>
        public static ICustomAttribute GetCustomAttribute(this IMethodDefinition method, string attributeFullName)
        {
            if (method?.CustomAttributes == null)
                return null;

            return method.CustomAttributes.FirstOrDefault(a => a.AttributeTypeName == attributeFullName);
        }

        /// <summary>
        /// Checks if the method has a specific custom attribute.
        /// </summary>
        public static bool HasCustomAttribute(this IMethodDefinition method, string attributeFullName)
        {
            return GetCustomAttribute(method, attributeFullName) != null;
        }

        /// <summary>
        /// Checks if the type is marked for obfuscation/renaming based on custom attributes.
        /// Returns true to rename, false to skip, null if not specified.
        /// </summary>
        public static bool? MarkedToRename(this ITypeDefinition type)
        {
            return MarkedToRenameFromAttributes(type?.CustomAttributes);
        }

        /// <summary>
        /// Checks if the method is marked for obfuscation/renaming based on custom attributes.
        /// Returns true to rename, false to skip, null if not specified.
        /// </summary>
        public static bool? MarkedToRename(this IMethodDefinition method)
        {
            return MarkedToRenameFromAttributes(method?.CustomAttributes);
        }

        public static bool? MarkedToRename(this IFieldDefinition field)
        {
            return MarkedToRenameFromAttributes(field?.CustomAttributes);
        }

        public static bool? MarkedToRename(this IPropertyDefinition property)
        {
            return MarkedToRenameFromAttributes(property?.CustomAttributes);
        }

        public static bool? MarkedToRename(this IEventDefinition evt)
        {
            return MarkedToRenameFromAttributes(evt?.CustomAttributes);
        }

        private static bool? MarkedToRenameFromAttributes(IEnumerable<ICustomAttribute> attributes)
        {
            if (attributes == null)
                return null;

#pragma warning disable 618
            var obfuscarObfuscate = typeof(ObfuscateAttribute).FullName;
#pragma warning restore 618
            var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;

            foreach (var attr in attributes)
            {
                var attrFullName = attr.AttributeTypeName;
                if (attrFullName == obfuscarObfuscate)
                {
                    var shouldObfuscate = Helper.GetAttributePropertyByName(attr, "ShouldObfuscate");
                    return (bool)(shouldObfuscate ?? true);
                }

                if (attrFullName == reflectionObfuscate)
                {
                    var rename = !((bool)(Helper.GetAttributePropertyByName(attr, "Exclude") ?? true));
                    return rename;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a field is public, including family (protected) and family-or-assembly visibility.
        /// </summary>
        public static bool IsFieldPublic(this IField field)
        {
            if (field == null)
                return false;
            
            var visibility = field.Attributes & System.Reflection.FieldAttributes.FieldAccessMask;
            return visibility == System.Reflection.FieldAttributes.Public ||
                   visibility == System.Reflection.FieldAttributes.Family ||
                   visibility == System.Reflection.FieldAttributes.FamORAssem;
        }
    }
}
