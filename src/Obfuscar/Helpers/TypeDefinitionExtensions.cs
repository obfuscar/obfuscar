using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Helpers
{
    public static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Checks if a type has attributes that are compiler-generated and embedded
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the type has compiler-generated and embedded attributes</returns>
        public static bool HasCompilerGeneratedAttributes(this ITypeDefinition type)
        {
            if (type == null || type.CustomAttributes == null)
                return false;

            bool hasCompilerGenerated = false;
            bool hasEmbedded = false;

            foreach (var attribute in type.CustomAttributes)
            {
                // Check if the attribute itself is compiler-generated
                if (attribute.AttributeTypeName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                {
                    hasCompilerGenerated = true;
                }

                // Check if the attribute name or namespace contains "Embedded"
                if (attribute.AttributeTypeName == "Microsoft.CodeAnalysis.EmbeddedAttribute")
                {
                    hasEmbedded = true;
                }

                if (hasCompilerGenerated && hasEmbedded)
                    return true;
            }
            
            return hasCompilerGenerated && hasEmbedded;
        }
        public static bool IsTypePublic(this ITypeDefinition type)
        {
            if (type.DeclaringType == null)
                return type.IsPublic;

            var visibility = type.Attributes & System.Reflection.TypeAttributes.VisibilityMask;
            if (visibility == System.Reflection.TypeAttributes.NestedPublic ||
                visibility == System.Reflection.TypeAttributes.NestedFamily ||
                visibility == System.Reflection.TypeAttributes.NestedFamORAssem)
                return IsTypePublic(type.DeclaringType);

            return false;
        }

        private static readonly object _resourcesTypeCacheLock = new object();
        private static readonly Dictionary<string, bool> _resourcesTypeCache =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        public static bool IsResourcesType(this ITypeDefinition type)
        {
            if (type == null || string.IsNullOrEmpty(type.FullName))
                return false;

            lock (_resourcesTypeCacheLock)
            {
                if (_resourcesTypeCache.TryGetValue(type.FullName, out bool cached))
                    return cached;
            }

            var generated = type.CustomAttributes.FirstOrDefault(attribute =>
                attribute.AttributeTypeName == "System.CodeDom.Compiler.GeneratedCodeAttribute");
            var result = false;
            if (generated == null)
            {
                result = type.IsFormOrUserControl();
            }
            else
            {
                var name = generated.ConstructorArguments?.FirstOrDefault()?.Value?.ToString();
                result = name == "System.Resources.Tools.StronglyTypedResourceBuilder";
            }

            lock (_resourcesTypeCacheLock)
            {
                _resourcesTypeCache[type.FullName] = result;
            }

            return result;
        }

        private static bool IsFormOrUserControl(this ITypeDefinition type)
        {
            if (type == null)
                return false;

            if (type.FullName == "System.Windows.Forms.Form" || type.FullName == "System.Windows.Forms.UserControl")
                return true;

            if (!string.IsNullOrEmpty(type.BaseTypeFullName))
            {
                if (type.BaseTypeFullName == "System.Windows.Forms.Form" ||
                    type.BaseTypeFullName == "System.Windows.Forms.UserControl")
                    return true;
            }

            return false;
        }
    }
}
