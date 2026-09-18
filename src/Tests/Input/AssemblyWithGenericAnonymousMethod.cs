using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestClasses
{
    public static class GenericExtensionMethods
    {
        public static T GetOrDefault<T>(this IDictionary<string, object> dict, string key)
            where T : class, new()
        {
            var props = typeof(T).GetProperties();
            return new T();
        }

        public static async Task<TResult> MapAsync<TInput, TResult>(
            this Task<TInput> task, Func<TInput, TResult> mapper)
        {
            var input = await task;
            return mapper(input);
        }
    }

    public static class AnotherGenericExtensions
    {
        public static List<T> FilterNulls<T>(this List<T> source)
            where T : class
        {
            var result = new List<T>();
            foreach (var x in source)
                if (x != null) result.Add(x);
            return result;
        }

        public static void ForEach<T>(this List<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }

    public static class GenericAnonymousMethodEntryPoint
    {
        public static string Test()
        {
            var dict = new Dictionary<string, object> { { "key", "value" } };
            var result = dict.GetOrDefault<SimpleConfig>("key");

            var list = new List<string> { "a", "b" };
            var filtered = list.FilterNulls();
            filtered.ForEach(x => { var _ = x.Length; });

            return "ok";
        }
    }

    public class SimpleConfig
    {
        public string Name { get; set; } = "";
    }
}
