using System;

namespace ImportTypeUsingStaticOuterClassTest.Models
{
    public class OuterClass
    {
        public struct NestedType1
        {
            public int? Property1 { get; set; }
            public string Property2 { get; set; }
        }

        public class NestedType2
        {
            public int? Property1 { get; set; }
            public string Property2 { get; set; }
        }
    }  
}
