using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace TestClasses
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class ClassB
    {
        [WithTypesCustom(typeof(ClassC))]
        public object ObjectWithAttr;
    }
}
