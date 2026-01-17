using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Adapters
{
    // SRM-backed adapter using TypeDefinitionHandle and MetadataReader
    public class SrmHandleTypeAdapter : IType
    {
        private readonly MetadataReader md;
        private readonly TypeDefinitionHandle handle;

        public SrmHandleTypeAdapter(MetadataReader md, TypeDefinitionHandle handle)
        {
            this.md = md;
            this.handle = handle;
        }

        private TypeDefinition GetDefinition() => md.GetTypeDefinition(handle);

        public string FullName
        {
            get
            {
                var td = GetDefinition();
                var name = md.GetString(td.Name);
                var ns = md.GetString(td.Namespace);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }
        }

        public string Name => md.GetString(GetDefinition().Name);
        public string Namespace => md.GetString(GetDefinition().Namespace);

        public IEnumerable<IField> Fields
        {
            get
            {
                foreach (var fh in GetDefinition().GetFields())
                {
                    yield return new SrmHandleFieldAdapter(md, fh);
                }
            }
        }
    }
}
