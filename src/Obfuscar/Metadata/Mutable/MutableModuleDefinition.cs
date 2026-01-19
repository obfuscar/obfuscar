using System;
using System.Collections.Generic;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a module definition in the mutable object model.
    /// This replaces legacy Mono.Cecil.ModuleDefinition.
    /// </summary>
    public class MutableModuleDefinition : IDisposable
    {
        /// <summary>
        /// Creates a new module definition.
        /// </summary>
        public MutableModuleDefinition(string name, MutableModuleKind kind)
        {
            Name = name;
            Kind = kind;
            Types = new List<MutableTypeDefinition>();
            AssemblyReferences = new List<MutableAssemblyNameReference>();
            ModuleReferences = new List<MutableModuleReference>();
            Resources = new List<MutableResource>();
            CustomAttributes = new List<MutableCustomAttribute>();
            ExportedTypes = new List<MutableExportedType>();
            Mvid = Guid.NewGuid();
        }

        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The full path to the module on disk, if known.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The kind of module (Dll, Console, Windows, etc.).
        /// </summary>
        public MutableModuleKind Kind { get; set; }

        /// <summary>
        /// The assembly that contains this module.
        /// </summary>
        public MutableAssemblyDefinition Assembly { get; set; }

        /// <summary>
        /// The types defined in this module.
        /// </summary>
        public List<MutableTypeDefinition> Types { get; }

        /// <summary>
        /// Assembly references.
        /// </summary>
        public List<MutableAssemblyNameReference> AssemblyReferences { get; }

        /// <summary>
        /// Module references (for P/Invoke).
        /// </summary>
        public List<MutableModuleReference> ModuleReferences { get; }

        /// <summary>
        /// Resources embedded in this module.
        /// </summary>
        public List<MutableResource> Resources { get; }

        /// <summary>
        /// Custom attributes applied to this module.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Types forwarded to other assemblies.
        /// </summary>
        public List<MutableExportedType> ExportedTypes { get; }

        /// <summary>
        /// The module version ID.
        /// </summary>
        public Guid Mvid { get; set; }

        /// <summary>
        /// The runtime version string.
        /// </summary>
        public string RuntimeVersion { get; set; }

        /// <summary>
        /// The target runtime.
        /// </summary>
        public MutableTargetRuntime Runtime { get; set; }

        /// <summary>
        /// The entry point method.
        /// </summary>
        public MutableMethodDefinition EntryPoint { get; set; }

        /// <summary>
        /// Module characteristics.
        /// </summary>
        public MutableModuleCharacteristics Characteristics { get; set; }

        /// <summary>
        /// Module attributes.
        /// </summary>
        public MutableModuleAttributes Attributes { get; set; }

        /// <summary>
        /// The type system for this module.
        /// </summary>
        public MutableTypeSystem TypeSystem { get; private set; }

        /// <summary>
        /// Gets a type by full name.
        /// </summary>
        public MutableTypeDefinition GetType(string fullName)
        {
            foreach (var type in Types)
            {
                if (type.FullName == fullName)
                    return type;
                var nested = FindNestedType(type, fullName);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private MutableTypeDefinition FindNestedType(MutableTypeDefinition parent, string fullName)
        {
            foreach (var nested in parent.NestedTypes)
            {
                if (nested.FullName == fullName)
                    return nested;
                var found = FindNestedType(nested, fullName);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Gets a type by namespace and name.
        /// </summary>
        public MutableTypeDefinition GetType(string @namespace, string name)
        {
            foreach (var type in Types)
            {
                if (type.Namespace == @namespace && type.Name == name)
                    return type;
            }
            return null;
        }

        /// <summary>
        /// Imports a type reference.
        /// </summary>
        public MutableTypeReference ImportReference(Type type)
        {
            return TypeSystem.Import(type);
        }

        /// <summary>
        /// Imports a method reference.
        /// </summary>
        public MutableMethodReference ImportReference(System.Reflection.MethodInfo method)
        {
            return TypeSystem.Import(method);
        }

        /// <summary>
        /// Imports a constructor reference.
        /// </summary>
        public MutableMethodReference ImportReference(System.Reflection.ConstructorInfo constructor)
        {
            return TypeSystem.Import(constructor);
        }

        /// <summary>
        /// Imports a field reference.
        /// </summary>
        public MutableFieldReference ImportReference(System.Reflection.FieldInfo field)
        {
            return TypeSystem.Import(field);
        }

        /// <summary>
        /// Initializes the type system for this module.
        /// </summary>
        internal void InitializeTypeSystem()
        {
            TypeSystem = new MutableTypeSystem(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Cleanup resources
        }
    }

    /// <summary>
    /// Module kinds.
    /// </summary>
    public enum MutableModuleKind
    {
        /// <summary>A class library (DLL).</summary>
        Dll,
        /// <summary>A console application.</summary>
        Console,
        /// <summary>A Windows application.</summary>
        Windows,
        /// <summary>A .NET module (no assembly manifest).</summary>
        NetModule,
    }

    /// <summary>
    /// Module characteristics.
    /// </summary>
    [Flags]
    public enum MutableModuleCharacteristics
    {
        /// <summary>No characteristics.</summary>
        None = 0,
        /// <summary>High entropy ASLR.</summary>
        HighEntropyVA = 0x0020,
        /// <summary>Dynamic base.</summary>
        DynamicBase = 0x0040,
        /// <summary>No SEH.</summary>
        NoSEH = 0x0400,
        /// <summary>NX compatible.</summary>
        NXCompat = 0x0100,
        /// <summary>App container.</summary>
        AppContainer = 0x1000,
        /// <summary>Terminal server aware.</summary>
        TerminalServerAware = 0x8000,
    }

    /// <summary>
    /// Module attributes.
    /// </summary>
    [Flags]
    public enum MutableModuleAttributes
    {
        /// <summary>IL only.</summary>
        ILOnly = 1,
        /// <summary>32-bit required.</summary>
        Required32Bit = 2,
        /// <summary>IL library.</summary>
        ILLibrary = 4,
        /// <summary>Strong name signed.</summary>
        StrongNameSigned = 8,
        /// <summary>Prefer 32-bit.</summary>
        Preferred32Bit = 0x00020000,
    }

    /// <summary>
    /// Target runtime.
    /// </summary>
    public enum MutableTargetRuntime
    {
        /// <summary>.NET 1.0.</summary>
        Net_1_0,
        /// <summary>.NET 1.1.</summary>
        Net_1_1,
        /// <summary>.NET 2.0.</summary>
        Net_2_0,
        /// <summary>.NET 4.0.</summary>
        Net_4_0,
    }

    /// <summary>
    /// Module reference (for P/Invoke).
    /// </summary>
    public class MutableModuleReference
    {
        /// <summary>
        /// Creates a new module reference.
        /// </summary>
        public MutableModuleReference(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Exported type (type forwarder).
    /// </summary>
    public class MutableExportedType
    {
        /// <summary>
        /// The namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The scope (assembly where the type is defined).
        /// </summary>
        public object Scope { get; set; }
    }
}
