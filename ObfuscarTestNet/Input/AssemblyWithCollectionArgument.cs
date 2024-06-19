using System;
using System.Collections.Generic;
using System.Linq;

namespace ObfuscarTestNet.Input
{
    public class AssemblyWithCollectionArgument
    {
        int a, b;

        public void Method(IEnumerable<AssemblyWithCollectionArgument> items)
        {
            Console.WriteLine("Count: " + items.Count());
        }

        public void Method(IEnumerable<int> items)
        {
            Console.WriteLine("Count: " + items.Count());
        }

        public void Method(IEnumerable<string> items)
        {
            Console.WriteLine("Count: " + items.Count());
        }

        public void Test()
        {
            var value = new AssemblyWithCollectionArgument();
            value.Method([value, value]);
            value.Method([1, 2]);
            value.Method(["a", "b"]);
        }
    }
}
