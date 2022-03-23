using Mono.Cecil;

namespace ObfuscarTest.FluentAssertions.Mono.Cecil
{
    /// <summary>
    /// Contains a number of methods to assert that an
    /// <see cref="PropertyDefinition" /> is in the expected state.
    /// </summary>
    public static class PropertyDefinitionExtensions
    {
        /// <summary>
        /// Returns an <see cref="PropertyDefinitionAssertions" /> object that can be used to assert the
        /// current <see cref="PropertyDefinition" />.
        /// </summary>
        public static PropertyDefinitionAssertions Should(this PropertyDefinition instance) =>
            new PropertyDefinitionAssertions(instance);
    }
}
