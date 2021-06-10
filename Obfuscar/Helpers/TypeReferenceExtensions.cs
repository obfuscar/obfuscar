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

        public static string GetGenericFullName(this TypeReference type)
        {
            string fullName = null;
            while (type.IsNested)
            {
                if (fullName == null)
                    fullName = type.GetGenericName();
                else
                    fullName = type.GetGenericName() + "/" + fullName;
                type = type.DeclaringType;
            }
            
            if (fullName == null)
                fullName = type.Namespace + "." + type.GetGenericName();
            else
                fullName = type.Namespace + "." + type.GetGenericName() + "/" + fullName;
            return fullName;
        }

        public static string GetGenericName(this TypeReference type)
        {
            if (!type.ContainsGenericParameter && !type.HasGenericParameters)
            {
                return type.Name;
            }

            // IMPORTANT: Type A[] and T[] should have the same code.
            var nameMaker = new NameMaker();
            for (int i = 0; i < type.GenericParameters.Count; i++)
            {
                GenericParameter genericName = type.GenericParameters[i];
                genericName.Name = nameMaker.UniqueName(i, null, genericName.Name);
            }

            type.GetElementType().Name = nameMaker.UniqueName(type.GenericParameters.Count, null, type.Name);
            return type.Name;
        }
    }
}
