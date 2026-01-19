using System;
using System.Collections.Generic;

namespace Obfuscar.Metadata.Abstractions
{
    /// <summary>
    /// Abstraction for a module, replacing Cecil's ModuleDefinition.
    /// </summary>
    public interface IModule
    {
        /// <summary>Module name.</summary>
        string Name { get; }

        /// <summary>Module kind (Dll, Exe, etc.).</summary>
        ModuleKind Kind { get; }

        /// <summary>All type definitions in the module (including nested types).</summary>
        IEnumerable<ITypeDefinition> Types { get; }

        /// <summary>Top-level type definitions only.</summary>
        IEnumerable<ITypeDefinition> TopLevelTypes { get; }

        /// <summary>Assembly references.</summary>
        IEnumerable<IAssemblyReference> AssemblyReferences { get; }

        /// <summary>Module references.</summary>
        IEnumerable<IModuleReference> ModuleReferences { get; }

        /// <summary>Resources embedded in the module.</summary>
        IEnumerable<IResource> Resources { get; }

        /// <summary>Entry point method (if exe).</summary>
        IMethodDefinition EntryPoint { get; }

        /// <summary>Whether the module is the main module.</summary>
        bool IsMain { get; }

        /// <summary>Target runtime.</summary>
        string RuntimeVersion { get; }
    }

    /// <summary>Module kind enumeration.</summary>
    public enum ModuleKind
    {
        Dll,
        Console,
        Windows,
        NetModule
    }

    /// <summary>Assembly reference abstraction.</summary>
    public interface IAssemblyReference
    {
        string Name { get; }
        string FullName { get; }
        Version Version { get; }
        byte[] PublicKeyToken { get; }
    }

    /// <summary>Module reference abstraction.</summary>
    public interface IModuleReference
    {
        string Name { get; }
    }

    /// <summary>Resource abstraction.</summary>
    public interface IResource
    {
        string Name { get; }
        ResourceType ResourceType { get; }
        bool IsPublic { get; }
    }

    /// <summary>Resource type enumeration.</summary>
    public enum ResourceType
    {
        Embedded,
        Linked,
        AssemblyLinked
    }
}
