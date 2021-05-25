using Mono.Cecil;
using System;
using System.Linq;
using System.Runtime.Caching;

namespace Obfuscar.Helpers
{
    internal static class TypeDefinitionExtensions
    {
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
                if (type.BaseType.FullName == "System.Object" &&
                    type.BaseType.Module.FileName.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                {
                    // IMPORTANT: Resolve call below fails for UWP .winmd files.
                    return false;
                }

                return type.BaseType.Resolve().IsFormOrUserControl();
            }

            return false;
        }
    }
}
