using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Helpers
{
    internal static class PropertyDefinitionExtensions
    {
        public static bool IsGetterPublic(this IPropertyDefinition propertyDefinition)
        {
            return propertyDefinition?.GetMethod.IsPublic() ?? false;
        }

        public static bool IsSetterPublic(this IPropertyDefinition propertyDefinition)
        {
            return propertyDefinition?.SetMethod.IsPublic() ?? false;
        }

        public static bool IsPublic(this IPropertyDefinition propertyDefinition)
        {
            return propertyDefinition.IsGetterPublic() || propertyDefinition.IsSetterPublic();
        }
    }
}
