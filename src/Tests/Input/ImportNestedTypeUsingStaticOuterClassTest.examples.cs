using System;
using System.Reflection;
using static ImportTypeUsingStaticOuterClassTest.Models.OuterClass;

namespace ImportTypeUsingStaticOuterClassTest.Examples
{
    [Obfuscation(ApplyToMembers = false)]
    public class Example_1
    {
        public NestedType1 CreateNestedTypeInstance()
        {
            return new NestedType1
            {
                Property1 = 1,
                Property2 = "NestedType1"
            };
        }
    }

    [Obfuscation(ApplyToMembers = false)]
    public class Example_2
    {
        public virtual NestedType2 CreateNestedTypeInstance()
        {
            return new NestedType2
            {
                Property1 = 2,
                Property2 = "NestedType2"
            };
        }
    }
	
	[Obfuscation(ApplyToMembers = false)]
    public class Example_3 : Example_2
    {
        public override NestedType2 CreateNestedTypeInstance()
        {
            return new NestedType2
            {
                Property1 = 3,
                Property2 = "NestedType2"
            };
        }
    }
}
