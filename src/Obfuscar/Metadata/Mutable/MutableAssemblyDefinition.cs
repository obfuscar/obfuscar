using System;
using System.Collections.Generic;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents an assembly definition in the mutable object model.
    /// This replaces legacy Mono.Cecil.AssemblyDefinition.
    /// </summary>
    public class MutableAssemblyDefinition : IDisposable
    {
        /// <summary>
        /// Creates a new assembly definition.
        /// </summary>
        public MutableAssemblyDefinition(MutableAssemblyNameDefinition name)
        {
            Name = name;
            Modules = new List<MutableModuleDefinition>();
            CustomAttributes = new List<MutableCustomAttribute>();
            SecurityDeclarations = new List<MutableSecurityDeclaration>();
        }

        /// <summary>
        /// The name of the assembly.
        /// </summary>
        public MutableAssemblyNameDefinition Name { get; set; }

        /// <summary>
        /// The full name of the assembly.
        /// </summary>
        public string FullName => Name?.FullName ?? string.Empty;

        /// <summary>
        /// The modules in this assembly.
        /// </summary>
        public List<MutableModuleDefinition> Modules { get; }

        /// <summary>
        /// The main module of the assembly.
        /// </summary>
        public MutableModuleDefinition MainModule => Modules.Count > 0 ? Modules[0] : null;

        /// <summary>
        /// Custom attributes applied to this assembly.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Security declarations on this assembly.
        /// </summary>
        public List<MutableSecurityDeclaration> SecurityDeclarations { get; }

        /// <summary>
        /// The entry point method (for executables).
        /// </summary>
        public MutableMethodDefinition EntryPoint { get; set; }

        /// <summary>
        /// Writes the assembly to the specified file path.
        /// </summary>
        public void Write(string outputPath)
        {
            Write(outputPath, null);
        }

        /// <summary>
        /// Writes the assembly to the specified file path with writer parameters.
        /// </summary>
        public void Write(string outputPath, MutableWriterParameters parameters)
        {
            var writer = new MutableAssemblyWriter(this, parameters);
            writer.Write(outputPath);
        }

        /// <summary>
        /// Creates a new assembly definition.
        /// </summary>
        public static MutableAssemblyDefinition CreateAssembly(MutableAssemblyNameDefinition assemblyName, string moduleName, MutableModuleKind kind)
        {
            var assembly = new MutableAssemblyDefinition(assemblyName);
            var module = new MutableModuleDefinition(moduleName, kind);
            module.Assembly = assembly;
            assembly.Modules.Add(module);
            return assembly;
        }

        /// <summary>
        /// Reads an assembly from the specified file path.
        /// </summary>
        public static MutableAssemblyDefinition ReadAssembly(string fileName)
        {
            return ReadAssembly(fileName, null);
        }

        /// <summary>
        /// Reads an assembly from the specified file path with reader parameters.
        /// </summary>
        public static MutableAssemblyDefinition ReadAssembly(string fileName, MutableReaderParameters parameters)
        {
            var reader = new MutableAssemblyReader();
            return reader.Read(fileName, parameters);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var module in Modules)
            {
                module.Dispose();
            }
        }
    }

    /// <summary>
    /// Represents an assembly name definition.
    /// </summary>
    public class MutableAssemblyNameDefinition
    {
        /// <summary>
        /// Creates a new assembly name definition.
        /// </summary>
        public MutableAssemblyNameDefinition(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// The simple name of the assembly.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The version of the assembly.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// The culture of the assembly.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The public key of the assembly.
        /// </summary>
        public byte[] PublicKey { get; set; }

        /// <summary>
        /// The public key token of the assembly.
        /// </summary>
        public byte[] PublicKeyToken { get; set; }

        /// <summary>
        /// The hash algorithm used for the assembly.
        /// </summary>
        public System.Configuration.Assemblies.AssemblyHashAlgorithm HashAlgorithm { get; set; }

        /// <summary>
        /// The assembly attributes.
        /// </summary>
        public System.Reflection.AssemblyNameFlags Attributes { get; set; }

        /// <summary>
        /// The full name of the assembly.
        /// </summary>
        public string FullName
        {
            get
            {
                var result = Name;
                if (Version != null)
                    result += $", Version={Version}";
                if (!string.IsNullOrEmpty(Culture))
                    result += $", Culture={Culture}";
                else
                    result += ", Culture=neutral";
                if (PublicKeyToken != null && PublicKeyToken.Length > 0)
                    result += $", PublicKeyToken={BitConverter.ToString(PublicKeyToken).Replace("-", "").ToLowerInvariant()}";
                else
                    result += ", PublicKeyToken=null";
                return result;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }

    /// <summary>
    /// Represents an assembly name reference.
    /// </summary>
    public class MutableAssemblyNameReference
    {
        /// <summary>
        /// Creates a new assembly name reference.
        /// </summary>
        public MutableAssemblyNameReference(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// The simple name of the assembly.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The version of the assembly.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// The culture of the assembly.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The public key of the assembly.
        /// </summary>
        public byte[] PublicKey { get; set; }

        /// <summary>
        /// The public key token of the assembly.
        /// </summary>
        public byte[] PublicKeyToken { get; set; }

        /// <summary>
        /// The assembly attributes.
        /// </summary>
        public System.Reflection.AssemblyNameFlags Attributes { get; set; }

        /// <summary>
        /// The full name of the assembly.
        /// </summary>
        public string FullName
        {
            get
            {
                var result = Name;
                if (Version != null)
                    result += $", Version={Version}";
                if (!string.IsNullOrEmpty(Culture))
                    result += $", Culture={Culture}";
                else
                    result += ", Culture=neutral";
                if (PublicKeyToken != null && PublicKeyToken.Length > 0)
                    result += $", PublicKeyToken={BitConverter.ToString(PublicKeyToken).Replace("-", "").ToLowerInvariant()}";
                else
                    result += ", PublicKeyToken=null";
                return result;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }

    /// <summary>
    /// Security declaration on an assembly or type.
    /// </summary>
    public class MutableSecurityDeclaration
    {
        /// <summary>
        /// The security action.
        /// </summary>
        public System.Security.Permissions.SecurityAction Action { get; set; }

        /// <summary>
        /// Security attributes.
        /// </summary>
        public List<MutableSecurityAttribute> SecurityAttributes { get; } = new List<MutableSecurityAttribute>();
    }

    /// <summary>
    /// Security attribute.
    /// </summary>
    public class MutableSecurityAttribute
    {
        /// <summary>
        /// The attribute type.
        /// </summary>
        public MutableTypeReference AttributeType { get; set; }

        /// <summary>
        /// Named arguments.
        /// </summary>
        public List<MutableCustomAttributeNamedArgument> Properties { get; } = new List<MutableCustomAttributeNamedArgument>();
    }

    /// <summary>
    /// Writer parameters for saving assemblies.
    /// </summary>
    public class MutableWriterParameters
    {
        /// <summary>
        /// Strong name key blob for signing.
        /// </summary>
        public byte[] StrongNameKeyBlob { get; set; }

        /// <summary>
        /// Whether to write symbols.
        /// </summary>
        public bool WriteSymbols { get; set; }
    }

    /// <summary>
    /// Reader parameters for loading assemblies.
    /// </summary>
    public class MutableReaderParameters
    {
        /// <summary>
        /// Whether to read symbols.
        /// </summary>
        public bool ReadSymbols { get; set; }

        /// <summary>
        /// Assembly resolver.
        /// </summary>
        public IMutableAssemblyResolver AssemblyResolver { get; set; }

        /// <summary>
        /// Whether to read in memory.
        /// </summary>
        public bool InMemory { get; set; }
    }

    /// <summary>
    /// Assembly resolver interface.
    /// </summary>
    public interface IMutableAssemblyResolver
    {
        /// <summary>
        /// Resolves an assembly name.
        /// </summary>
        MutableAssemblyDefinition Resolve(MutableAssemblyNameReference name);

        /// <summary>
        /// Resolves an assembly name with parameters.
        /// </summary>
        MutableAssemblyDefinition Resolve(MutableAssemblyNameReference name, MutableReaderParameters parameters);
    }
}
