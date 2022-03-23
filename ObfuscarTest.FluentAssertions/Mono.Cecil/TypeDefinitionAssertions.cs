using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Mono.Cecil;

namespace ObfuscarTest.FluentAssertions.Mono.Cecil
{
    /// <summary>
    /// <see cref="TypeDefinition"/> assertions based on <inheritdoc cref="ReferenceTypeAssertions{TSubject,TAssertions}"/>
    /// </summary>
    public class TypeDefinitionAssertions : ReferenceTypeAssertions<TypeDefinition, TypeDefinitionAssertions>
    {
        /// <summary> Constructor </summary>
        /// <param name="instance">The instance to assert.</param>
        public TypeDefinitionAssertions(TypeDefinition instance)
            : base(instance)
        { }

        /// <inheritdoc cref = "ReferenceTypeAssertions{TSubject,TAssertions}.Identifier" />
        protected override string Identifier => "TypeDefinition";

        /// <summary>
        /// Assert the <see cref="ReferenceTypeAssertions{TSubject,TAssertions}.Subject"/> has a method with a specific name.
        /// </summary>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the
        /// assertion is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        /// <param name="name">The name of the method to find.</param>
        public AndConstraint<MethodDefinitionAssertions> HaveMethodWithName(string name,
            string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!string.IsNullOrEmpty(name))
                .ForCondition(Subject?.Methods.Any(method => method.Name == name) ?? false)
                .FailWith("Expected {context:" + Identifier + "} for type {0} to have method with name {1}.",
                    Subject, name);

            return new AndConstraint<MethodDefinitionAssertions>(new MethodDefinitionAssertions(
                Subject?.Methods.FirstOrDefault(method => method.Name == name)));
        }

        /// <summary>
        /// Assert the <see cref="ReferenceTypeAssertions{TSubject,TAssertions}.Subject"/> has a property with a specific name.
        /// </summary>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the
        /// assertion is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        /// <param name="name">The name of the property to find.</param>
        public AndConstraint<PropertyDefinitionAssertions> HavePropertyWithName(string name,
            string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!string.IsNullOrEmpty(name))
                .ForCondition(Subject?.Properties.Any(property => property.Name == name) ?? false)
                .FailWith("Expected {context:" + Identifier + "} for type {0} to have property with name {1}.",
                    Subject, name);

            return new AndConstraint<PropertyDefinitionAssertions>(new PropertyDefinitionAssertions(
                Subject?.Properties.FirstOrDefault(property => property.Name == name)));
        }

        /// <summary>
        /// Assert the <see cref="ReferenceTypeAssertions{TSubject,TAssertions}.Subject"/> has a field with a specific name.
        /// </summary>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the
        /// assertion is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        /// <param name="name">The name of the field to find.</param>
        public AndConstraint<FieldDefinitionAssertions> HaveFieldWithName(string name,
            string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!string.IsNullOrEmpty(name))
                .ForCondition(Subject?.Fields.Any(field => field.Name == name) ?? false)
                .FailWith("Expected {context:" + Identifier + "} for type {0} to have field with name {1}.",
                    Subject, name);

            return new AndConstraint<FieldDefinitionAssertions>(new FieldDefinitionAssertions(
                Subject?.Fields.FirstOrDefault(field => field.Name == name)));
        }

        /// <summary>
        /// Assert the <see cref="ReferenceTypeAssertions{TSubject,TAssertions}.Subject"/> has a field with a specific name.
        /// </summary>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the
        /// assertion is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        /// <param name="name">The name of the event to find.</param>
        public AndConstraint<EventDefinitionAssertions> HaveEventWithName(string name,
            string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!string.IsNullOrEmpty(name))
                .ForCondition(Subject?.Events.Any(@event => @event.Name == name) ?? false)
                .FailWith("Expected {context:" + Identifier + "} for type {0} to have field with name {1}.",
                    Subject, name);

            return new AndConstraint<EventDefinitionAssertions>(new EventDefinitionAssertions(
                Subject?.Events.FirstOrDefault(@event => @event.Name == name)));
        }
    }
}
