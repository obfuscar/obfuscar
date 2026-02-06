using System;

namespace TestClasses
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ObjectKeyAttribute : Attribute
    {
        public ObjectKeyAttribute(object key)
        {
            Key = key;
        }

        public object Key { get; }
    }

    public sealed class NeedsKeyedDependency
    {
        public NeedsKeyedDependency([ObjectKey("my-key")] object dependency)
        {
        }
    }

    public static class ConstructorParameterObjectAttributeEntryPoint
    {
        public static string Execute()
        {
            var constructor = typeof(NeedsKeyedDependency).GetConstructors()[0];
            var parameter = constructor.GetParameters()[0];
            var attribute = (ObjectKeyAttribute)Attribute.GetCustomAttribute(parameter, typeof(ObjectKeyAttribute));
            return attribute?.Key?.ToString() ?? "<null>";
        }
    }
}
