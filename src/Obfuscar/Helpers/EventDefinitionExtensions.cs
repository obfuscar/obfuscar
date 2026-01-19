using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Helpers
{
    internal static class EventDefinitionExtensions
    {
        public static bool IsAddPublic(this IEventDefinition eventDefinition)
        {
            return eventDefinition?.AddMethod.IsPublic() ?? false;
        }

        public static bool IsRemovePublic(this IEventDefinition eventDefinition)
        {
            return eventDefinition?.RemoveMethod.IsPublic() ?? false;
        }

        public static bool IsPublic(this IEventDefinition eventDefinition)
        {
            return eventDefinition.IsAddPublic() || eventDefinition.IsRemovePublic();
        }
    }
}
