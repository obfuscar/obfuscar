using System;
using System.Collections.Generic;

[assembly: System.Reflection.ObfuscationAttribute(Exclude = false, Feature = "all")]

namespace TestClasses
{
    /// <summary>
    /// Some attribute that accept type as parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AttributeWithTypeAttribute : Attribute
    {
        public Type PersistedType;

        public AttributeWithTypeAttribute(Type persistedType)
        {
            PersistedType = persistedType;
        }
    }

    /// <summary>
    /// Use list (from non-renamed assembly) with generic items class from renamed assembly
    /// To make sure we handle it recursively (going deeper)
    /// </summary>
    [AttributeWithType(typeof(List<ClassA>))]
    public class ClassWithGenericRenamedAttributeArgument
    {
        /// <summary>Dummy field to ensure the assemblies are copied into the output dir</summary>
        public static int Smth = -1;

        /// <summary>
        /// Reference ClassA directly to make sure compiler keep reference to the assembly A
        /// </summary>
        public static ClassA A { get; set; }
    }

}
