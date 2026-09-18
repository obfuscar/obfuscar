#nullable enable
using System;
using System.Collections.Generic;
using GenLib;

namespace GenApp
{
    public static class AppEntryPoint
    {
        public static string Test()
        {
            var registrar = new Registrar();
            registrar.Register(new ServiceLocator());
            registrar.Generic<string>(new List<string> { "a" });
            return "ok";
        }
    }

    public class Registrar
    {
        // Mirrors the reported snippet, but the types live in another assembly.
        public void Register(ServiceLocator locator)
        {
            Func<ServiceLocator, IParent?> factory = sp => sp.GetService<IChild>();
            _ = factory(locator);
        }

        // Generic method with lambda -> generic <>c__N<T> display class.
        public void Generic<T>(IEnumerable<T> items)
            where T : class
        {
            Func<T, bool> predicate = x => x != null;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                }
            }
        }
    }
}
