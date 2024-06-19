using System;
using System.Collections.Generic;
using System.Linq;

namespace ObfuscarTestNet.Input
{
    class AssemblyWithCollectionExpression
    {
        int a, b, c;

        public void Test()
        {
            var value = new AssemblyWithCollectionArgument();
            value.Method([1, 2]);
            value.Method([value, value]);
        }
    }
}
