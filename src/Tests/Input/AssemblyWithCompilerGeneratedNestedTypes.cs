using System.Collections.Generic;
using System.Linq;

namespace Issue602
{
    public class MyDictionaryA : Dictionary<string, string>
    {
        public static string Test()
        {
            var dict = new MyDictionaryA { { "a", "b" } };
            return string.Join(",", dict.Select(x => x.Key).ToArray());
        }
    }

    public class MyDictionaryB : Dictionary<string, string>
    {
        public static string Test()
        {
            var dict = new MyDictionaryB { { "c", "d" } };
            return string.Join(",", dict.Select(x => x.Key).ToArray());
        }
    }

    public class MyDictionaryC : Dictionary<string, string>
    {
        public static string Test()
        {
            var dict = new MyDictionaryC { { "e", "f" } };
            return string.Join(",", dict.Select(x => x.Key).ToArray());
        }
    }
}
