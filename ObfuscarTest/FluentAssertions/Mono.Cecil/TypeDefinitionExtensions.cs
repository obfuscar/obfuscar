using Mono.Cecil;

namespace ObfuscarTest.FluentAssertions.Mono.Cecil
{
    /// <summary>
    /// Contains a number of methods to assert that an
    /// <see cref="TypeDefinition" /> is in the expected state.
    /// </summary>
    public static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Returns an <see cref="TypeDefinitionAssertions" /> object that can be used to assert the
        /// current <see cref="TypeDefinition" />.
        /// </summary>
        public static TypeDefinitionAssertions Should(this TypeDefinition instance) =>
            new TypeDefinitionAssertions(instance);
    }
}
