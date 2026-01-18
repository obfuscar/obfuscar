using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class CecilTypeAdapter : IType
    {
        private readonly TypeDefinition type;

        public CecilTypeAdapter(TypeDefinition type)
        {
            this.type = type;
        }

        public string Scope => type?.GetScopeName() ?? string.Empty;

        public string FullName => type?.FullName ?? string.Empty;

        public string Name => type?.Name ?? string.Empty;

        public string Namespace => type?.Namespace ?? string.Empty;

        public string BaseTypeFullName => type?.BaseType?.FullName ?? string.Empty;

        public IEnumerable<string> InterfaceTypeFullNames =>
            type?.Interfaces?.Select(iface => iface.InterfaceType.FullName) ?? Enumerable.Empty<string>();

        public bool IsPublic => type != null && type.IsTypePublic();

        public bool IsSerializable => type?.IsSerializable == true;

        public bool IsSealed => type?.IsSealed == true;

        public bool IsAbstract => type?.IsAbstract == true;

        public bool IsEnum => type?.IsEnum == true;

        public IEnumerable<string> CustomAttributeTypeFullNames =>
            type?.CustomAttributes?.Select(attr => attr.AttributeType.FullName) ?? Enumerable.Empty<string>();

        public IEnumerable<IField> Fields =>
            type?.Fields?.Select(field => new CecilFieldAdapter(field)) ?? Enumerable.Empty<IField>();
    }
}
