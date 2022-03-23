using Mono.Cecil;

namespace ObfuscarTest.FluentAssertions.Mono.Cecil
{
    /// <summary>
    /// Contains a number of methods to assert that an
    /// <see cref="FieldDefinition" /> is in the expected state.
    /// </summary>
    public static class FieldDefinitionExtensions
    {
        /// <summary>
        /// Returns an <see cref="FieldDefinitionAssertions" /> object that can be used to assert the
        /// current <see cref="FieldDefinition" />.
        /// </summary>
        public static FieldDefinitionAssertions Should(this FieldDefinition instance) =>
            new FieldDefinitionAssertions(instance);
    }
}
