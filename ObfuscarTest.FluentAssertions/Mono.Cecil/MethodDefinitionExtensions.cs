using Mono.Cecil;

namespace ObfuscarTest.FluentAssertions.Mono.Cecil
{
    /// <summary>
    /// Contains a number of methods to assert that an
    /// <see cref="MethodDefinition" /> is in the expected state.
    /// </summary>
    public static class MethodDefinitionExtensions
    {
        /// <summary>
        /// Returns an <see cref="MethodDefinitionAssertions" /> object that can be used to assert the
        /// current <see cref="MethodDefinition" />.
        /// </summary>
        public static MethodDefinitionAssertions Should(this MethodDefinition instance) =>
            new MethodDefinitionAssertions(instance);
    }
}
