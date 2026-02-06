using System;
using System.Reflection;

namespace TestClasses
{
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class TestAttribute : Attribute
    {
        public bool testField;
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class AttributeNamedArgumentEntryPoint
    {
        [Test(testField = true)]
        public static string FieldToReproduceProblem;

        public static bool Execute()
        {
            var attribute = (TestAttribute)Attribute.GetCustomAttribute(
                typeof(AttributeNamedArgumentEntryPoint).GetField(nameof(FieldToReproduceProblem)),
                typeof(TestAttribute));

            return attribute.testField;
        }
    }
}
