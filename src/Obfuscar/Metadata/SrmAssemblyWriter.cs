using System;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar.Metadata
{
    /// <summary>
    /// SRM-backed writer that emits assemblies from the mutable object model.
    /// </summary>
    public class SrmAssemblyWriter : IAssemblyWriter
    {
        public void Write(MutableAssemblyDefinition assembly, string outputPath)
        {
            Write(assembly, outputPath, null);
        }

        public void Write(MutableAssemblyDefinition assembly, string outputPath, MutableWriterParameters parameters)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            assembly.Write(outputPath, parameters);
        }

        public void Dispose()
        {
        }
    }
}
