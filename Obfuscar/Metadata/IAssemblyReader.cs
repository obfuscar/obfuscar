using System;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata
{
    public interface IAssemblyReader : IDisposable
    {
        /// <summary>
        /// Gets the assembly abstraction (Cecil-free interface).
        /// </summary>
        IAssembly Assembly { get; }

        /// <summary>
        /// Gets the underlying Cecil AssemblyDefinition for migration compatibility.
        /// This property will be removed once Cecil is fully eliminated.
        /// </summary>
        [Obsolete("Use Assembly property instead. This is for migration compatibility only.")]
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
