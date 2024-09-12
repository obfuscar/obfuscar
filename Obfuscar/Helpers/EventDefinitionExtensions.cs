using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class EventDefinitionExtensions
    {
        public static bool IsPublic(this EventDefinition eventDefinition)
        {
            return eventDefinition.IsAddPublic() || eventDefinition.IsRemovePublic();
        }
        public static bool IsInternal(this EventDefinition eventDefinition)
        {
            return eventDefinition.IsAddInternal() || eventDefinition.IsRemoveInternal();
        }
        
        private static bool IsAddPublic(this EventDefinition eventDefinition)
        {
            return eventDefinition.AddMethod.IsPublic();
        }

        private static bool IsRemovePublic(this EventDefinition eventDefinition)
        {
            return eventDefinition.RemoveMethod.IsPublic();
        }
        
        private static bool IsAddInternal(this EventDefinition eventDefinition)
        {
            return eventDefinition.AddMethod.IsInternal();
        }

        private static bool IsRemoveInternal(this EventDefinition eventDefinition)
        {
            return eventDefinition.RemoveMethod.IsInternal();
        }
    }
}
