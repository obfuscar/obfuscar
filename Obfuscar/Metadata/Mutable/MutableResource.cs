namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Base class for embedded resources in a module.
    /// This replaces Mono.Cecil.Resource.
    /// </summary>
    public abstract class MutableResource
    {
        /// <summary>
        /// The name of the resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The resource attributes.
        /// </summary>
        public MutableManifestResourceAttributes Attributes { get; set; }

        /// <summary>
        /// The type of resource.
        /// </summary>
        public abstract MutableResourceType ResourceType { get; }
    }

    /// <summary>
    /// Represents an embedded resource.
    /// This replaces Mono.Cecil.EmbeddedResource.
    /// </summary>
    public class MutableEmbeddedResource : MutableResource
    {
        private byte[] _data;

        /// <summary>
        /// Creates a new embedded resource.
        /// </summary>
        public MutableEmbeddedResource(string name, MutableManifestResourceAttributes attributes, byte[] data)
        {
            Name = name;
            Attributes = attributes;
            _data = data;
        }

        /// <inheritdoc/>
        public override MutableResourceType ResourceType => MutableResourceType.Embedded;

        /// <summary>
        /// Gets the resource data.
        /// </summary>
        public byte[] GetResourceData()
        {
            return _data;
        }

        /// <summary>
        /// Sets the resource data.
        /// </summary>
        public void SetResourceData(byte[] data)
        {
            _data = data;
        }
    }

    /// <summary>
    /// Represents a linked resource.
    /// This replaces Mono.Cecil.LinkedResource.
    /// </summary>
    public class MutableLinkedResource : MutableResource
    {
        /// <summary>
        /// Creates a new linked resource.
        /// </summary>
        public MutableLinkedResource(string name, MutableManifestResourceAttributes attributes, string file)
        {
            Name = name;
            Attributes = attributes;
            File = file;
        }

        /// <inheritdoc/>
        public override MutableResourceType ResourceType => MutableResourceType.Linked;

        /// <summary>
        /// The linked file name.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The file hash.
        /// </summary>
        public byte[] Hash { get; set; }
    }

    /// <summary>
    /// Represents an assembly-linked resource.
    /// This replaces Mono.Cecil.AssemblyLinkedResource.
    /// </summary>
    public class MutableAssemblyLinkedResource : MutableResource
    {
        /// <summary>
        /// Creates a new assembly-linked resource.
        /// </summary>
        public MutableAssemblyLinkedResource(string name, MutableManifestResourceAttributes attributes, MutableAssemblyNameReference assembly)
        {
            Name = name;
            Attributes = attributes;
            Assembly = assembly;
        }

        /// <inheritdoc/>
        public override MutableResourceType ResourceType => MutableResourceType.AssemblyLinked;

        /// <summary>
        /// The assembly that contains the resource.
        /// </summary>
        public MutableAssemblyNameReference Assembly { get; set; }
    }

    /// <summary>
    /// Resource types.
    /// </summary>
    public enum MutableResourceType
    {
        /// <summary>Embedded resource.</summary>
        Embedded,
        /// <summary>Linked resource.</summary>
        Linked,
        /// <summary>Assembly-linked resource.</summary>
        AssemblyLinked,
    }

    /// <summary>
    /// Manifest resource attributes.
    /// </summary>
    public enum MutableManifestResourceAttributes
    {
        /// <summary>Public resource.</summary>
        Public = 1,
        /// <summary>Private resource.</summary>
        Private = 2,
    }
}
