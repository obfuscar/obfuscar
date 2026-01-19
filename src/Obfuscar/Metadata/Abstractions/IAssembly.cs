using System;
using System.Collections.Generic;

namespace Obfuscar.Metadata.Abstractions
{
    /// <summary>
    /// Abstraction for an assembly, replacing Cecil's AssemblyDefinition.
    /// </summary>
    public interface IAssembly : IDisposable
    {
        /// <summary>Assembly name (without extension).</summary>
        string Name { get; }

        /// <summary>Full assembly name including version and public key token.</summary>
        string FullName { get; }

        /// <summary>Assembly version.</summary>
        Version Version { get; }

        /// <summary>Main module of the assembly.</summary>
        IModule MainModule { get; }

        /// <summary>Public key of the assembly (if signed).</summary>
        byte[] PublicKey { get; }

        /// <summary>Custom attributes on the assembly.</summary>
        IEnumerable<string> CustomAttributeTypeFullNames { get; }

        /// <summary>Whether the assembly has ObfuscateAssemblyAttribute(true).</summary>
        bool IsMarkedForObfuscation { get; }
    }
}
