using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Helpers
{
    internal static class FieldDefinitionExtensions
    {
        public static bool IsPublic(this IField field)
        {
            return field != null && field.IsFieldPublic();
        }
    }
}
