using System.Collections.Generic;
using System.Linq;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    // SRM adapter: currently wraps a Mono.Cecil.TypeDefinition materialized by SrmAssemblyReader
    public class SrmTypeAdapter : IType
    {
        private readonly Mono.Cecil.TypeDefinition typeDef;

        public SrmTypeAdapter(Mono.Cecil.TypeDefinition typeDef)
        {
            this.typeDef = typeDef;
        }

        public string FullName => typeDef.FullName;
        public string Name => typeDef.Name;
        public string Namespace => typeDef.Namespace;

        public IEnumerable<IField> Fields => typeDef.Fields.Select(f => new SrmFieldAdapter(f));
    }
}
