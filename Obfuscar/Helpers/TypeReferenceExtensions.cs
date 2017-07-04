using Mono.Cecil;

namespace Obfuscar.Helpers
{
    static class TypeReferenceExtensions
    {
        /// <summary>
        /// Returns the simplified name for the assembly where a type can be found,
        /// for example, a type whose module is "Assembly.exe", "Assembly" would be 
        /// returned.
        /// </summary>
        public static string GetScopeName(this TypeReference type)
        {
            ModuleDefinition module = type.Scope as ModuleDefinition;
            if (module != null)
                return module.Assembly.Name.Name;
            else
                return type.Scope.Name;
        }

        public static string GetFullName(this TypeReference type)
        {
            string fullName = null;
            while (type.IsNested)
            {
                if (fullName == null)
                    fullName = type.Name;
                else
                    fullName = type.Name + "/" + fullName;
                type = type.DeclaringType;
            }

            if (fullName == null)
                fullName = type.Namespace + "." + type.Name;
            else
                fullName = type.Namespace + "." + type.Name + "/" + fullName;
            return fullName;
        }
    }
}
