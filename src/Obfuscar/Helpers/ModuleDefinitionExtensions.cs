using System.Collections.Generic;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Helpers
{
    public static class ModuleDefinitionExtensions
    {
        public static IEnumerable<MutableTypeDefinition> GetTypes(this MutableModuleDefinition module)
        {
            foreach (var type in module.Types)
            {
                yield return type;
                foreach (var nested in GetNestedTypes(type))
                    yield return nested;
            }
        }

        private static IEnumerable<MutableTypeDefinition> GetNestedTypes(MutableTypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
            {
                yield return nested;
                foreach (var deeper in GetNestedTypes(nested))
                    yield return deeper;
            }
        }
    }
}
