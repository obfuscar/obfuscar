using System;
using Mono.Cecil;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Adapters;

namespace Obfuscar.Metadata
{
    /// <summary>
    /// Migration bridge helpers for transitioning from Cecil to SRM.
    /// These methods allow code to gradually migrate while still being able
    /// to access underlying Cecil types when needed.
    /// </summary>
    /// <remarks>
    /// Usage: During migration, code that needs Cecil types can use these helpers:
    /// <code>
    /// ITypeDefinition type = ...;
    /// if (type.TryGetCecilDefinition(out var typeDef))
    /// {
    ///     // Use typeDef for Cecil-specific operations
    /// }
    /// </code>
    /// Once migration is complete, these helpers should be removed.
    /// </remarks>
    public static class MigrationBridge
    {
        /// <summary>
        /// Attempts to get the underlying Cecil TypeDefinition from an ITypeDefinition.
        /// Returns true if successful, false if the adapter is SRM-backed.
        /// </summary>
        public static bool TryGetCecilDefinition(this ITypeDefinition type, out TypeDefinition definition)
        {
            if (type == null)
            {
                definition = null;
                return false;
            }

            if (type is CecilTypeDefinitionAdapter adapter)
            {
                definition = adapter.Definition;
                return true;
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying Cecil MethodDefinition from an IMethodDefinition.
        /// </summary>
        public static bool TryGetCecilDefinition(this IMethodDefinition method, out MethodDefinition definition)
        {
            if (method == null)
            {
                definition = null;
                return false;
            }

            if (method is CecilMethodDefinitionAdapter adapter)
            {
                definition = adapter.Definition;
                return true;
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying Cecil FieldDefinition from an IFieldDefinition.
        /// </summary>
        public static bool TryGetCecilDefinition(this IFieldDefinition field, out FieldDefinition definition)
        {
            if (field == null)
            {
                definition = null;
                return false;
            }

            if (field is CecilFieldDefinitionAdapter adapter)
            {
                definition = adapter.Definition;
                return true;
            }

            // Also check CecilFieldAdapter for IField implementations
            if (field is CecilFieldAdapter fieldAdapter)
            {
                definition = fieldAdapter.Definition;
                return true;
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying Cecil PropertyDefinition from an IPropertyDefinition.
        /// </summary>
        public static bool TryGetCecilDefinition(this IPropertyDefinition property, out PropertyDefinition definition)
        {
            if (property == null)
            {
                definition = null;
                return false;
            }

            if (property is CecilPropertyDefinitionAdapter adapter)
            {
                definition = adapter.Definition;
                return true;
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying Cecil EventDefinition from an IEventDefinition.
        /// </summary>
        public static bool TryGetCecilDefinition(this IEventDefinition evt, out EventDefinition definition)
        {
            if (evt == null)
            {
                definition = null;
                return false;
            }

            if (evt is CecilEventDefinitionAdapter adapter)
            {
                definition = adapter.Definition;
                return true;
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// Gets the underlying Cecil TypeDefinition or throws if not available.
        /// Use only when you know the adapter is Cecil-backed.
        /// </summary>
        public static TypeDefinition GetCecilDefinition(this ITypeDefinition type)
        {
            if (type.TryGetCecilDefinition(out var def))
                return def;
            throw new InvalidOperationException($"Type {type.FullName} is not backed by Cecil");
        }

        /// <summary>
        /// Gets the underlying Cecil MethodDefinition or throws if not available.
        /// </summary>
        public static MethodDefinition GetCecilDefinition(this IMethodDefinition method)
        {
            if (method.TryGetCecilDefinition(out var def))
                return def;
            throw new InvalidOperationException($"Method {method.Name} is not backed by Cecil");
        }

        /// <summary>
        /// Gets the underlying Cecil FieldDefinition or throws if not available.
        /// </summary>
        public static FieldDefinition GetCecilDefinition(this IFieldDefinition field)
        {
            if (field.TryGetCecilDefinition(out var def))
                return def;
            throw new InvalidOperationException($"Field {field.Name} is not backed by Cecil");
        }

        /// <summary>
        /// Gets the underlying Cecil PropertyDefinition or throws if not available.
        /// </summary>
        public static PropertyDefinition GetCecilDefinition(this IPropertyDefinition property)
        {
            if (property.TryGetCecilDefinition(out var def))
                return def;
            throw new InvalidOperationException($"Property {property.Name} is not backed by Cecil");
        }

        /// <summary>
        /// Gets the underlying Cecil EventDefinition or throws if not available.
        /// </summary>
        public static EventDefinition GetCecilDefinition(this IEventDefinition evt)
        {
            if (evt.TryGetCecilDefinition(out var def))
                return def;
            throw new InvalidOperationException($"Event {evt.Name} is not backed by Cecil");
        }
    }
}
