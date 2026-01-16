using System;
using System.IO;
using Mono.Cecil;

namespace Obfuscar.Metadata
{
    public interface IAssemblyReader : IDisposable
    {
        AssemblyDefinition AssemblyDefinition { get; }
    }

    public static class AssemblyReaderFactory
    {
        // Default factory returns a Cecil-based reader for now.
        // Can be switched to SRM-based reader via AssemblyReaderFactory.Create or environment variable OBFUSCAR_USE_SRM.
        private static Func<string, IAssemblyReader> defaultFactory = null;

        public static Func<string, IAssemblyReader> Create
        {
            get
            {
                if (defaultFactory != null)
                    return defaultFactory;

                // Default: SRM-based reader
                return defaultFactory = path => new SrmAssemblyReader(path);
            }
            set => defaultFactory = value;
        }
    }
}
