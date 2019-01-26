using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class FieldDefinitionExtensions
    {
        public static bool IsPublic(this FieldDefinition field)
        {
            return field != null && (field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly);
        }
    }
}
