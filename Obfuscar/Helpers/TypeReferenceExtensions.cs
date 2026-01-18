using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Helpers
{
    static class TypeReferenceExtensions
    {
        /// <summary>
        /// Returns the simplified name for the assembly where a type can be found,
        /// for example, a type whose module is "Assembly.exe", "Assembly" would be 
        /// returned.
        /// </summary>
        public static string GetScopeName(this MutableTypeReference type)
        {
            if (type?.Module?.Assembly?.Name?.Name != null)
                return type.Module.Assembly.Name.Name;

            if (type?.Scope is MutableAssemblyNameReference asmRef)
                return asmRef.Name;

            if (type?.Scope is MutableAssemblyNameDefinition asmDef)
                return asmDef.Name;

            if (type?.Scope is MutableModuleDefinition module)
                return module.Assembly?.Name?.Name ?? module.Name;

            return type?.Scope?.ToString() ?? string.Empty;
        }

        public static string GetFullName(this MutableTypeReference type)
        {
            if (type == null)
                return string.Empty;

            string fullName = null;
            var current = type;
            while (current.DeclaringType != null)
            {
                if (fullName == null)
                    fullName = current.Name;
                else
                    fullName = current.Name + "/" + fullName;
                current = current.DeclaringType;
            }

            if (fullName == null)
                fullName = string.IsNullOrEmpty(current.Namespace)
                    ? current.Name
                    : current.Namespace + "." + current.Name;
            else
                fullName = string.IsNullOrEmpty(current.Namespace)
                    ? current.Name + "/" + fullName
                    : current.Namespace + "." + current.Name + "/" + fullName;
            return fullName;
        }

        public static MutableTypeReference GetElementType(this MutableTypeReference type)
        {
            return type switch
            {
                MutableArrayType arrayType => arrayType.ElementType,
                MutableByReferenceType byRefType => byRefType.ElementType,
                MutablePointerType pointerType => pointerType.ElementType,
                MutableGenericInstanceType genericType => genericType.ElementType,
                _ => type
            };
        }
    }
}
