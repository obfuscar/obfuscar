using Mono.Cecil;

namespace ObfuscarTest.FluentAssertions.Mono.Cecil
{
    /// <summary>
    /// Contains a number of methods to assert that an
    /// <see cref="EventDefinition" /> is in the expected state.
    /// </summary>
    public static class EventDefinitionExtensions
    {
        /// <summary>
        /// Returns an <see cref="EventDefinitionAssertions" /> object that can be used to assert the
        /// current <see cref="EventDefinition" />.
        /// </summary>
        public static EventDefinitionAssertions Should(this EventDefinition instance) =>
            new EventDefinitionAssertions(instance);
    }
}
