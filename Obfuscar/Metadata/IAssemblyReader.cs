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
        // Default factory returns a Cecil-based reader. Can be swapped to SRM-based reader for testing.
        public static Func<string, IAssemblyReader> Create = path => new CecilAssemblyReader(path);
    }
}
