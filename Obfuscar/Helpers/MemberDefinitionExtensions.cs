using System.Collections.Generic;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Helpers
{
    internal static class MemberDefinitionExtensions
    {
        public static void CleanAttributes(this MutableTypeDefinition type)
        {
            if (type == null)
                return;
            CleanAttributes(type.CustomAttributes);
        }

        public static void CleanAttributes(this MutableMethodDefinition method)
        {
            if (method == null)
                return;
            CleanAttributes(method.CustomAttributes);
        }

        public static void CleanAttributes(this MutableFieldDefinition field)
        {
            if (field == null)
                return;
            CleanAttributes(field.CustomAttributes);
        }

        public static void CleanAttributes(this MutablePropertyDefinition property)
        {
            if (property == null)
                return;
            CleanAttributes(property.CustomAttributes);
        }

        public static void CleanAttributes(this MutableEventDefinition eventDefinition)
        {
            if (eventDefinition == null)
                return;
            CleanAttributes(eventDefinition.CustomAttributes);
        }

        private static void CleanAttributes(List<MutableCustomAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0)
                return;

            var reflectionObfuscate = typeof(System.Reflection.ObfuscationAttribute).FullName;
            for (int i = 0; i < attributes.Count; i++)
            {
                var attr = attributes[i];
                if (attr.AttributeTypeName == reflectionObfuscate)
                {
                    if ((bool)(Helper.GetAttributePropertyByName(attr, "StripAfterObfuscation") ?? true))
                    {
                        attributes.Remove(attr);
                    }
                }
            }
        }
    }
}
