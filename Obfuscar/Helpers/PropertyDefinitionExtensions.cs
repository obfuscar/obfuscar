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

        public static bool IsGetterAccessible(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.GetMethod.IsAccessible();
        }

        public static bool IsSetterAccessible(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.SetMethod.IsAccessible();
        }

        public static bool IsAccessible(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.IsSetterAccessible() ||
                   propertyDefinition.IsGetterAccessible();
        }
    }
}
