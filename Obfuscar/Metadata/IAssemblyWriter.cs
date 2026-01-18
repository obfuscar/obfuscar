using System;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Metadata
{
    /// <summary>
    /// Abstraction for writing/emitting assemblies.
    /// Uses the mutable object model backed by System.Reflection.Metadata.
    /// </summary>
    public interface IAssemblyWriter : IDisposable
    {
        /// <summary>
        /// Write the assembly definition to the specified output path.
        /// </summary>
        void Write(MutableAssemblyDefinition assembly, string outputPath);

        /// <summary>
        /// Write the assembly definition with optional writer parameters.
        /// </summary>
        void Write(MutableAssemblyDefinition assembly, string outputPath, MutableWriterParameters parameters);
    }

    /// <summary>
    /// Factory for creating assembly writers.
    /// </summary>
    public static class AssemblyWriterFactory
    {
        /// <summary>
        /// Create a writer using the default implementation.
        /// </summary>
        public static IAssemblyWriter CreateWriter()
        {
            return new SrmAssemblyWriter();
        }
    }
}
