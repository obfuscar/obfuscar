using System;
using System.Collections.Generic;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    /// <summary>
    /// Cecil-backed IAssembly implementation.
    /// </summary>
    public class CecilAssemblyAdapter : IAssembly
    {
        private readonly AssemblyDefinition assembly;
        private readonly SrmAssemblyReader srmReader;
        private IModule mainModule;

        public CecilAssemblyAdapter(AssemblyDefinition assembly, SrmAssemblyReader srmReader = null)
        {
            this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.srmReader = srmReader;
        }

        public string Name => assembly.Name.Name;

        public string FullName => assembly.FullName;

        public Version Version => assembly.Name.Version;

        public IModule MainModule
        {
            get
            {
                if (mainModule == null)
                    mainModule = new CecilModuleAdapter(assembly.MainModule, srmReader);
                return mainModule;
            }
        }

        public byte[] PublicKey => assembly.Name.PublicKey;

        public IEnumerable<string> CustomAttributeTypeFullNames
        {
            get
            {
                foreach (var attr in assembly.CustomAttributes)
                {
                    yield return attr.AttributeType.FullName;
                }
            }
        }

        public bool IsMarkedForObfuscation
        {
            get
            {
                // Check for ObfuscateAssemblyAttribute
                foreach (var attr in assembly.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == "System.Reflection.ObfuscateAssemblyAttribute")
                    {
                        if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is bool val)
                        {
                            return val;
                        }
                    }
                }
                return false;
            }
        }

        public void Dispose()
        {
            assembly?.Dispose();
        }

        /// <summary>Get the underlying Cecil AssemblyDefinition (for migration compatibility).</summary>
        public AssemblyDefinition Definition => assembly;
    }

    /// <summary>
    /// Cecil-backed IModule implementation.
    /// </summary>
    public class CecilModuleAdapter : IModule
    {
        private readonly ModuleDefinition module;
        private readonly SrmAssemblyReader srmReader;

        public CecilModuleAdapter(ModuleDefinition module, SrmAssemblyReader srmReader = null)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.srmReader = srmReader;
        }

        public string Name => module.Name;

        public Abstractions.ModuleKind Kind
        {
            get
            {
                switch (module.Kind)
                {
                    case Mono.Cecil.ModuleKind.Dll:
                        return Abstractions.ModuleKind.Dll;
                    case Mono.Cecil.ModuleKind.Console:
                        return Abstractions.ModuleKind.Console;
                    case Mono.Cecil.ModuleKind.Windows:
                        return Abstractions.ModuleKind.Windows;
                    case Mono.Cecil.ModuleKind.NetModule:
                        return Abstractions.ModuleKind.NetModule;
                    default:
                        return Abstractions.ModuleKind.Dll;
                }
            }
        }

        public IEnumerable<ITypeDefinition> Types => GetAllTypes();

        public IEnumerable<ITypeDefinition> TopLevelTypes
        {
            get
            {
                foreach (var type in module.Types)
                {
                    yield return new CecilTypeDefinitionAdapter(type, srmReader);
                }
            }
        }

        public IEnumerable<IAssemblyReference> AssemblyReferences
        {
            get
            {
                foreach (var asmRef in module.AssemblyReferences)
                {
                    yield return new CecilAssemblyReferenceAdapter(asmRef);
                }
            }
        }

        public IEnumerable<IModuleReference> ModuleReferences
        {
            get
            {
                foreach (var modRef in module.ModuleReferences)
                {
                    yield return new CecilModuleReferenceAdapter(modRef);
                }
            }
        }

        public IEnumerable<IResource> Resources
        {
            get
            {
                foreach (var res in module.Resources)
                {
                    yield return new CecilResourceAdapter(res);
                }
            }
        }

        public IMethodDefinition EntryPoint
        {
            get
            {
                if (module.EntryPoint == null) return null;
                return new CecilMethodDefinitionAdapter(module.EntryPoint, srmReader);
            }
        }

        public bool IsMain => module.IsMain;

        public string RuntimeVersion => module.RuntimeVersion;

        /// <summary>Get the underlying Cecil ModuleDefinition (for migration compatibility).</summary>
        public ModuleDefinition Definition => module;

        private IEnumerable<ITypeDefinition> GetAllTypes()
        {
            foreach (var type in module.Types)
            {
                yield return new CecilTypeDefinitionAdapter(type, srmReader);
                foreach (var nested in GetNestedTypesRecursive(type))
                {
                    yield return nested;
                }
            }
        }

        private IEnumerable<ITypeDefinition> GetNestedTypesRecursive(TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
            {
                yield return new CecilTypeDefinitionAdapter(nested, srmReader);
                foreach (var deepNested in GetNestedTypesRecursive(nested))
                {
                    yield return deepNested;
                }
            }
        }
    }

    internal class CecilAssemblyReferenceAdapter : IAssemblyReference
    {
        private readonly AssemblyNameReference asmRef;

        public CecilAssemblyReferenceAdapter(AssemblyNameReference asmRef)
        {
            this.asmRef = asmRef;
        }

        public string Name => asmRef.Name;
        public string FullName => asmRef.FullName;
        public Version Version => asmRef.Version;
        public byte[] PublicKeyToken => asmRef.PublicKeyToken;
    }

    internal class CecilModuleReferenceAdapter : IModuleReference
    {
        private readonly ModuleReference modRef;

        public CecilModuleReferenceAdapter(ModuleReference modRef)
        {
            this.modRef = modRef;
        }

        public string Name => modRef.Name;
    }

    internal class CecilResourceAdapter : IResource
    {
        private readonly Resource resource;

        public CecilResourceAdapter(Resource resource)
        {
            this.resource = resource;
        }

        public string Name => resource.Name;

        public Abstractions.ResourceType ResourceType
        {
            get
            {
                switch (resource.ResourceType)
                {
                    case Mono.Cecil.ResourceType.Embedded:
                        return Abstractions.ResourceType.Embedded;
                    case Mono.Cecil.ResourceType.Linked:
                        return Abstractions.ResourceType.Linked;
                    case Mono.Cecil.ResourceType.AssemblyLinked:
                        return Abstractions.ResourceType.AssemblyLinked;
                    default:
                        return Abstractions.ResourceType.Embedded;
                }
            }
        }

        public bool IsPublic => resource.IsPublic;
    }
}
