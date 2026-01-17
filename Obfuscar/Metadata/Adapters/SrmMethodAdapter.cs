using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace Obfuscar.Metadata.Adapters
{
    // SRM adapter: currently wraps a Mono.Cecil.MethodDefinition materialized by SrmAssemblyReader
    public class SrmMethodAdapter : IMethod
    {
        private readonly Mono.Cecil.MethodDefinition method;
        private readonly string[] parameterTypeFullNames;

        public SrmMethodAdapter(Mono.Cecil.MethodDefinition method)
        {
            this.method = method;
            this.parameterTypeFullNames = method.Parameters
                .Select(param => Helper.GetParameterTypeName(param))
                .ToArray();
        }

        public string Name => method.Name;
        public string ReturnTypeFullName => method.ReturnType?.FullName ?? string.Empty;
        public string DeclaringTypeFullName => method.DeclaringType?.FullName ?? string.Empty;
        public MethodAttributes Attributes => (MethodAttributes) method.Attributes;
        public IReadOnlyList<string> ParameterTypeFullNames => parameterTypeFullNames;
    }
}
