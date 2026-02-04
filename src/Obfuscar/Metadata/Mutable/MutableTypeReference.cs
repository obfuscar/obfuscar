using System;
using System.Collections.Generic;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a type reference in the mutable object model.
    /// This replaces the legacy Cecil TypeReference.
    /// </summary>
    public class MutableTypeReference : IType
    {
        protected string _cachedFullName;

        /// <summary>
        /// Creates a new type reference.
        /// </summary>
        public MutableTypeReference(string @namespace, string name, MutableModuleDefinition module)
        {
            Namespace = @namespace;
            Name = name;
            Module = module;
        }

        private string _namespace;
        private string _name;

        /// <summary>
        /// The namespace of the type.
        /// </summary>
        public string Namespace
        {
            get => _namespace;
            set
            {
                if (_namespace != value)
                {
                    _namespace = value;
                    _cachedFullName = null;
                    OnRenamed();
                }
            }
        }

        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    _cachedFullName = null;
                    OnRenamed();
                }
            }
        }

        /// <summary>
        /// Called when the type is renamed (Name or Namespace changed).
        /// Subclasses can override to update lookup maps, etc.
        /// </summary>
        protected virtual void OnRenamed() { }

        /// <summary>
        /// The full name of the type (namespace + name). Cached for performance.
        /// </summary>
        public virtual string FullName
        {
            get
            {
                if (_cachedFullName != null)
                    return _cachedFullName;

                if (DeclaringType == null)
                {
                    _cachedFullName = string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
                    return _cachedFullName;
                }

                var nested = Name;
                var current = DeclaringType;
                while (current?.DeclaringType != null)
                {
                    nested = current.Name + "/" + nested;
                    current = current.DeclaringType;
                }

                if (current == null)
                {
                    _cachedFullName = nested;
                    return _cachedFullName;
                }

                var root = string.IsNullOrEmpty(current.Namespace)
                    ? current.Name
                    : $"{current.Namespace}.{current.Name}";
                _cachedFullName = $"{root}/{nested}";
                return _cachedFullName;
            }
        }

        /// <summary>
        /// Invalidates the cached FullName. Call after changing Name, Namespace, or DeclaringType.
        /// </summary>
        public void InvalidateFullNameCache()
        {
            _cachedFullName = null;
        }

        /// <summary>
        /// The module this type reference is scoped to.
        /// </summary>
        public MutableModuleDefinition Module { get; set; }

        /// <summary>
        /// The scope (assembly or module) where this type is defined.
        /// </summary>
        public object Scope { get; set; }

        /// <summary>
        /// For nested types, the declaring type.
        /// </summary>
        public MutableTypeReference DeclaringType { get; set; }

        /// <summary>
        /// Whether this is a value type.
        /// </summary>
        public bool IsValueType { get; set; }

        /// <summary>
        /// Whether this is a primitive type.
        /// </summary>
        public bool IsPrimitive { get; set; }

        /// <summary>
        /// Whether this is a generic instance.
        /// </summary>
        public virtual bool IsGenericInstance => false;

        /// <summary>
        /// Whether this is an array type.
        /// </summary>
        public virtual bool IsArray => false;

        /// <summary>
        /// Whether this is a by-reference type.
        /// </summary>
        public virtual bool IsByReference => false;

        /// <summary>
        /// Whether this is a pointer type.
        /// </summary>
        public virtual bool IsPointer => false;

        /// <summary>
        /// Resolves this type reference to a type definition.
        /// </summary>
        public virtual MutableTypeDefinition Resolve()
        {
            return this as MutableTypeDefinition;
        }

        public virtual string BaseTypeFullName => string.Empty;

        public virtual IEnumerable<string> InterfaceTypeFullNames => Array.Empty<string>();

        public virtual bool IsPublic => false;

        public virtual bool IsSerializable => false;

        public virtual bool IsSealed => false;

        public virtual bool IsAbstract => false;

        public virtual bool IsEnum { get; set; }

        public virtual IEnumerable<string> CustomAttributeTypeFullNames => Array.Empty<string>();

        IEnumerable<IField> IType.Fields => Array.Empty<IField>();

        string IType.Scope
        {
            get
            {
                if (Scope is MutableAssemblyNameReference asmRef)
                    return asmRef.Name;

                if (Scope is MutableAssemblyNameDefinition asmDef)
                    return asmDef.Name;

                if (Scope is MutableModuleDefinition module)
                    return module.Assembly?.Name?.Name ?? module.Name;

                if (Module?.Assembly?.Name?.Name != null)
                    return Module.Assembly.Name.Name;

                return Scope?.ToString() ?? string.Empty;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }

    /// <summary>
    /// Represents a generic instance of a type.
    /// </summary>
    public class MutableGenericInstanceType : MutableTypeReference
    {
        /// <summary>
        /// Creates a new generic instance type.
        /// </summary>
        public MutableGenericInstanceType(MutableTypeReference elementType)
            : base(elementType.Namespace, elementType.Name, elementType.Module)
        {
            ElementType = elementType;
            GenericArguments = new System.Collections.Generic.List<MutableTypeReference>();
        }

        /// <summary>
        /// The element type (the generic type definition).
        /// </summary>
        public MutableTypeReference ElementType { get; }

        /// <summary>
        /// The generic type arguments.
        /// </summary>
        public System.Collections.Generic.List<MutableTypeReference> GenericArguments { get; }

        /// <inheritdoc/>
        public override bool IsGenericInstance => true;

        /// <inheritdoc/>
        public override string FullName
        {
            get
            {
                if (_cachedFullName != null)
                    return _cachedFullName;

                var args = string.Join(",", GenericArguments.ConvertAll(a => a?.FullName ?? "?"));
                _cachedFullName = $"{ElementType?.FullName ?? "?"}<{args}>";
                return _cachedFullName;
            }
        }
    }

    /// <summary>
    /// Represents an array type.
    /// </summary>
    public class MutableArrayType : MutableTypeReference
    {
        /// <summary>
        /// Creates a new array type.
        /// </summary>
        public MutableArrayType(MutableTypeReference elementType)
            : base(elementType.Namespace, elementType.Name + "[]", elementType.Module)
        {
            ElementType = elementType;
            Rank = 1;
        }

        /// <summary>
        /// Creates a new multi-dimensional array type.
        /// </summary>
        public MutableArrayType(MutableTypeReference elementType, int rank)
            : base(elementType.Namespace, elementType.Name + "[" + new string(',', rank - 1) + "]", elementType.Module)
        {
            ElementType = elementType;
            Rank = rank;
        }

        /// <summary>
        /// The element type.
        /// </summary>
        public MutableTypeReference ElementType { get; }

        /// <summary>
        /// The number of dimensions.
        /// </summary>
        public int Rank { get; }

        /// <inheritdoc/>
        public override bool IsArray => true;
    }

    /// <summary>
    /// Represents a by-reference type (ref T).
    /// </summary>
    public class MutableByReferenceType : MutableTypeReference
    {
        /// <summary>
        /// Creates a new by-reference type.
        /// </summary>
        public MutableByReferenceType(MutableTypeReference elementType)
            : base(elementType.Namespace, elementType.Name + "&", elementType.Module)
        {
            ElementType = elementType;
        }

        /// <summary>
        /// The element type.
        /// </summary>
        public MutableTypeReference ElementType { get; }

        /// <inheritdoc/>
        public override bool IsByReference => true;
    }

    /// <summary>
    /// Represents a pointer type (T*).
    /// </summary>
    public class MutablePointerType : MutableTypeReference
    {
        /// <summary>
        /// Creates a new pointer type.
        /// </summary>
        public MutablePointerType(MutableTypeReference elementType)
            : base(elementType.Namespace, elementType.Name + "*", elementType.Module)
        {
            ElementType = elementType;
        }

        /// <summary>
        /// The element type.
        /// </summary>
        public MutableTypeReference ElementType { get; }

        /// <inheritdoc/>
        public override bool IsPointer => true;
    }

    /// <summary>
    /// Represents a type with a required or optional custom modifier (modreq/modopt).
    /// This is used for init-only setters (modreq(IsExternalInit)) and other modifier scenarios.
    /// </summary>
    public class MutableModifiedType : MutableTypeReference
    {
        /// <summary>
        /// Creates a new modified type.
        /// </summary>
        /// <param name="modifier">The modifier type (e.g., IsExternalInit).</param>
        /// <param name="elementType">The underlying unmodified type.</param>
        /// <param name="isRequired">True for modreq, false for modopt.</param>
        public MutableModifiedType(MutableTypeReference modifier, MutableTypeReference elementType, bool isRequired)
            : base(elementType.Namespace, elementType.Name, elementType.Module)
        {
            Modifier = modifier;
            ElementType = elementType;
            IsRequired = isRequired;
            IsValueType = elementType.IsValueType;
        }

        /// <summary>
        /// The modifier type.
        /// </summary>
        public MutableTypeReference Modifier { get; }

        /// <summary>
        /// The underlying unmodified type.
        /// </summary>
        public MutableTypeReference ElementType { get; }

        /// <summary>
        /// Whether this is a required modifier (modreq) or optional modifier (modopt).
        /// </summary>
        public bool IsRequired { get; }

        /// <inheritdoc/>
        public override string FullName
        {
            get
            {
                if (_cachedFullName != null)
                    return _cachedFullName;

                _cachedFullName = ElementType?.FullName ?? Name;
                return _cachedFullName;
            }
        }
    }
}
