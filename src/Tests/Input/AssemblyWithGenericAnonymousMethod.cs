#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TestClasses
{
    // Reproducer for #607.
    // Key elements from the issue:
    // 1. Generic methods contain lambdas -> compiler emits generic <>c__N<T> display classes.
    // 2. Nullable reference types enabled -> compiler emits/embeds NullableAttribute.
    // 3. [Obfuscation] applied to the declaring types.

    public static class GenericExtensionMethods
    {
        // Lambda inside a generic method, no captures -> <>c__N<T>.
        public static int CountNonNull<T>(this IEnumerable<T> source)
            where T : class
        {
            Func<T, bool> isNotNull = x => x != null;
            int count = 0;
            foreach (var item in source)
            {
                if (isNotNull(item))
                    count++;
            }

            return count;
        }

        // Second generic lambda in the same class -> another display class.
        public static List<T> Materialize<T>(this IEnumerable<T> source)
            where T : class
        {
            Func<T, T> identity = x => x;
            var result = new List<T>();
            foreach (var item in source)
            {
                if (identity(item) != null)
                    result.Add(item);
            }

            return result;
        }

        // Nullable-aware generic method.
        public static T? GetOrDefault<T>(this IDictionary<string, object> dict, string key)
            where T : class, new()
        {
            var props = typeof(T).GetProperties();
            return new T();
        }
    }


    public static class AsyncExtensionMethods
    {
        public static async Task<string?> DescribeAsync<T>(this Task<T> task)
            where T : class
        {
            Func<T, string?> describe = x => x?.ToString();
            var value = await task;
            return describe(value);
        }
    }

    public static class GenericAnonymousMethodEntryPoint
    {
        public static string Test()
        {
            var list = new List<string> { "a", "b" };
            var count = list.CountNonNull();
            var materialized = list.Materialize();

            var dict = new Dictionary<string, object> { { "key", "value" } };
            var config = dict.GetOrDefault<SimpleConfig>("key");

            var registrar = new Registrar();
            registrar.Register(new ServiceLocator());

            return "ok";
        }
    }

    public interface IParent
    {
    }

    public interface IChild : IParent
    {
    }

    public class ServiceLocator
    {
        public T? GetService<T>()
            where T : class
        {
            return null;
        }
    }

    // Mirrors the reported snippet:
    //   services.AddSingleton<IParent>(sp => sp.GetService<IChild>());
    // The lambda body calls a generic method with a reference-type argument.
    public class Registrar
    {
        public void Register(ServiceLocator locator)
        {
            Func<ServiceLocator, IParent?> factory = sp => sp.GetService<IChild>();
            var item = factory(locator);
            _ = item;
        }
    }

    public class SimpleConfig
    {
        public string? Name { get; set; }
    }

    // Double-generic nesting: generic method inside generic class, lambda inside.
    public class GenericOuter<T>
        where T : class
    {
        public void Register<U>(IEnumerable<U> items)
            where U : class
        {
            Func<U, bool> predicate = x => x != null;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                }
            }
        }

        public void RegisterWithCapture<U>(IEnumerable<U> items, T seed)
            where U : class
        {
            Func<U, T, bool> predicate = (x, s) => x != null && s != null;
            foreach (var item in items)
            {
                if (predicate(item, seed))
                {
                }
            }
        }
    }
}
