using Mono.Cecil;

namespace Obfuscar.Helpers
{
    internal static class EventDefinitionExtensions
    {
        public static bool IsAddPublic(this EventDefinition eventDefinition)
        {
            return eventDefinition.AddMethod.IsPublic();
        }

        public static bool IsRemovePublic(this EventDefinition eventDefinition)
        {
            return eventDefinition.RemoveMethod.IsPublic();
        }

        public static bool IsPublic(this EventDefinition eventDefinition)
        {
            return eventDefinition.IsAddPublic() || eventDefinition.IsRemovePublic();
        }
    }
}
