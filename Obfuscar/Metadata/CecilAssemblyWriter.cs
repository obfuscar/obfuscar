using System;
using Mono.Cecil;

namespace Obfuscar.Metadata
{
    /// <summary>
    /// Cecil-based assembly writer that directly uses Mono.Cecil's assembly writing.
    /// This is a fallback/comparison writer.
    /// </summary>
    public class CecilAssemblyWriter : IAssemblyWriter
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
            // Nothing to dispose
        }
    }
}
