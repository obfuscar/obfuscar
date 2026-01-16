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
    /// Cecil-based assembly writer (uses Mono.Cecil's built-in Write functionality).
    /// This is the current implementation and remains the most complete.
    /// </summary>
    public sealed class CecilAssemblyWriter : IAssemblyWriter
    {
        public void Write(AssemblyDefinition assembly, string outputPath)
        {
            assembly.Write(outputPath);
        }

        public void Write(AssemblyDefinition assembly, string outputPath, WriterParameters parameters)
        {
            assembly.Write(outputPath, parameters);
        }

        public void Dispose()
        {
            // No resources to clean up
        }
    }

    /// <summary>
    /// Factory for creating assembly writers.
    /// </summary>
    public static class AssemblyWriterFactory
    {
        /// <summary>
        /// Create a writer using the default implementation (Cecil-based).
        /// </summary>
        public static IAssemblyWriter CreateWriter()
        {
            return new CecilAssemblyWriter();
        }
    }
}
