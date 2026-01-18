using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Helpers
{
    internal static class MethodDefinitionExtensions
    {
        public static bool IsPublic(this IMethodDefinition method)
        {
            return method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
        }
    }
}
