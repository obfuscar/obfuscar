using System;
using Mono.Cecil;

namespace Obfuscar.Metadata
{
    public class CecilAssemblyReader : IAssemblyReader
    {
        public AssemblyDefinition AssemblyDefinition { get; private set; }

        public CecilAssemblyReader(string path)
        {
            // Use the same default reading parameters as before
            var readerParameters = new ReaderParameters { ReadSymbols = false };
            AssemblyDefinition = AssemblyDefinition.ReadAssembly(path, readerParameters);
        }

        public void Dispose()
        {
            AssemblyDefinition?.Dispose();
            AssemblyDefinition = null;
        }
    }
}
