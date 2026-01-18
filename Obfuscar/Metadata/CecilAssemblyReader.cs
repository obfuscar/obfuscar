using System;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Adapters;

namespace Obfuscar.Metadata
{
    public class CecilAssemblyReader : IAssemblyReader
    {
        private IAssembly assemblyAbstraction;

        [Obsolete("Use Assembly property instead. This is for migration compatibility only.")]
        public AssemblyDefinition AssemblyDefinition { get; private set; }

        /// <summary>
        /// Gets the assembly through the Cecil-free abstraction.
        /// </summary>
        public IAssembly Assembly
        {
            get
            {
                if (assemblyAbstraction == null && AssemblyDefinition != null)
                {
                    assemblyAbstraction = new CecilAssemblyAdapter(AssemblyDefinition);
                }
                return assemblyAbstraction;
            }
        }

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
            assemblyAbstraction = null;
        }
    }
}
