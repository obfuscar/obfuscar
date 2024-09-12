using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class PropertyDefinitionExtensions
    {
        public static bool IsPublic(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.IsGetterPublic() || propertyDefinition.IsSetterPublic();
        }

        public static bool IsInternal(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.IsGetterInternal() || propertyDefinition.IsSetterInternal();
        }
        
        private static bool IsGetterPublic(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.GetMethod.IsPublic();
        }

        private static bool IsSetterPublic(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.SetMethod.IsPublic();
        }
        
        private static bool IsGetterInternal(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.GetMethod.IsInternal();
        }

        private static bool IsSetterInternal(this PropertyDefinition propertyDefinition)
        {
            return propertyDefinition.SetMethod.IsInternal();
        }
    }
}
