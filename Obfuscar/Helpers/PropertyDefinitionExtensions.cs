using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class PropertyDefinitionExtensions
    {
        public static bool IsGetterPublic(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.GetMethod.IsPublic();
        }

        public static bool IsSetterPublic(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.SetMethod.IsPublic();
        }

        public static bool IsPublic(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.IsGetterPublic() || propertyDefinition.IsSetterPublic();
        }

        public static bool IsGetterPublicOrInternal(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.GetMethod.IsPublicOrInternal();
        }

        public static bool IsSetterPublicOrInternal(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.SetMethod.IsPublicOrInternal();
        }

        public static bool IsPublicOrInternal(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.IsSetterPublicOrInternal() ||
                   propertyDefinition.IsGetterPublicOrInternal();
        }
    }
}
