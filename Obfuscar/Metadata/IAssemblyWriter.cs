using System;
using Mono.Cecil;

namespace Obfuscar.Metadata
{
    /// <summary>
    /// Abstraction for writing/emitting assemblies.
    /// For now, uses Mono.Cecil as the backend for PE writing.
    /// In the future, could be replaced with System.Reflection.Metadata.Ecma335-based implementation.
    /// </summary>
    public interface IAssemblyWriter : IDisposable
    {
        /// <summary>
        /// Write the assembly definition to the specified output path.
        /// </summary>
        void Write(AssemblyDefinition assembly, string outputPath);

        /// <summary>
        /// Write the assembly definition with optional writer parameters.
        /// </summary>
        void Write(AssemblyDefinition assembly, string outputPath, WriterParameters parameters);
    }

    /// <summary>
    /// Factory for creating assembly writers.
    /// </summary>
    public static class AssemblyWriterFactory
    {
        /// <summary>
        /// Create a writer using the default implementation (SRM-based).
        /// </summary>
        public static IAssemblyWriter CreateWriter()
        {
            return new SrmAssemblyWriter();
        }
    }
}
