using Mono.Cecil;
using System;
using System.Linq;
using System.Runtime.Caching;

namespace Obfuscar.Helpers
{
    public static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Checks if a type has attributes that are compiler-generated and embedded
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if the type has compiler-generated and embedded attributes</returns>
        public static bool HasCompilerGeneratedAttributes(this TypeDefinition type)
        {
            if (type == null || !type.HasCustomAttributes)
                return false;

            bool hasCompilerGenerated = false;
            bool hasEmbedded = false;

            foreach (var attribute in type.CustomAttributes)
            {
                // Check if the attribute itself is compiler-generated
                if (attribute.AttributeType.Name == "CompilerGeneratedAttribute" && 
                    attribute.AttributeType.Namespace == "System.Runtime.CompilerServices")
                {
                    hasCompilerGenerated = true;
                }

                // Check if the attribute name or namespace contains "Embedded"
                if (attribute.AttributeType.Name == "EmbeddedAttribute" && 
                    attribute.AttributeType.Namespace == "Microsoft.CodeAnalysis")
                {
                    hasEmbedded = true;
                }

                if (hasCompilerGenerated && hasEmbedded)
                    return true;
            }
            
            return hasCompilerGenerated && hasEmbedded;
        }
        public static bool IsTypePublic(this TypeDefinition type)
        {
            if (type.DeclaringType == null)
                return type.IsPublic;

            if (type.IsNestedFamily || type.IsNestedFamilyOrAssembly || type.IsNestedPublic)
                return IsTypePublic(type.DeclaringType);

            return false;
        }

        private static CacheItemPolicy policy = new CacheItemPolicy {SlidingExpiration = TimeSpan.FromMinutes(5)};

        public static bool IsResourcesType(this TypeDefinition type)
        {
            if (MemoryCache.Default.Contains(type.FullName))
                return (bool) MemoryCache.Default[type.FullName];

            var generated = type.CustomAttributes.FirstOrDefault(attribute =>
                attribute.AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute");
            var result = false;
            if (generated == null)
            {
                result = type.IsFormOrUserControl();
            }
            else
            {
                var name = generated.ConstructorArguments[0].Value.ToString();
                result = name == "System.Resources.Tools.StronglyTypedResourceBuilder";
            }

            MemoryCache.Default.Add(type.FullName, result, policy);
            return result;
        }

        private static bool IsFormOrUserControl(this TypeDefinition type)
        {
            if (type == null)
                return false;

            if (type.FullName == "System.Windows.Forms.Form" || type.FullName == "System.Windows.Forms.UserControl")
                return true;

            if (type.BaseType != null)
            {
                var baseTypeFullName = type.BaseType.FullName;
                if (baseTypeFullName == "System.Object")
                {
                    // Check if this is a UWP .winmd file
                    var module = type.BaseType.Module;
                    if (module?.FileName != null && module.FileName.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                    {
                        // IMPORTANT: Resolve call below fails for UWP .winmd files.
                        return false;
                    }
                }

                try
                {
                    var resolved = type.BaseType.Resolve();
                    if (resolved != null)
                        return resolved.IsFormOrUserControl();
                }
                catch (Mono.Cecil.AssemblyResolutionException)
                {
                    // If we can't resolve the base type assembly, assume it's not a Form/UserControl
                    return false;
                }
            }

            return false;
        }
    }
}
