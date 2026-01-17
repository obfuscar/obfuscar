using System.Collections.Generic;
using System.Linq;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    public class CecilTypeAdapter : IType
    {
        private readonly Mono.Cecil.TypeDefinition typeDef;

        public CecilTypeAdapter(Mono.Cecil.TypeDefinition typeDef)
        {
            this.typeDef = typeDef;
        }

        public string FullName => typeDef.FullName;
        public string Name => typeDef.Name;
        public string Namespace => typeDef.Namespace;

        public IEnumerable<IField> Fields => typeDef.Fields.Select(f => new CecilFieldAdapter(f));
    }
}
