using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class MethodDefinitionExtensions
    {
        public static bool IsPublic(this MethodDefinition method)
        {
            return method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
        }
    }
}
