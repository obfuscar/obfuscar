using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Represents a method reference in the mutable object model.
    /// This replaces Mono.Cecil.MethodReference.
    /// </summary>
    public class MutableMethodReference
    {
        /// <summary>
        /// Creates a new method reference.
        /// </summary>
        public MutableMethodReference(string name, MutableTypeReference returnType, MutableTypeReference declaringType)
        {
            Name = name;
            ReturnType = returnType;
            DeclaringType = declaringType;
            Parameters = new List<MutableParameterDefinition>();
            GenericParameters = new List<MutableGenericParameter>();
        }

        /// <summary>
        /// The name of the method.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The return type of the method.
        /// </summary>
        public MutableTypeReference ReturnType { get; set; }

        /// <summary>
        /// The type that declares this method.
        /// </summary>
        public MutableTypeReference DeclaringType { get; set; }

        /// <summary>
        /// The parameters of the method.
        /// </summary>
        public List<MutableParameterDefinition> Parameters { get; }

        /// <summary>
        /// Generic parameters of this method.
        /// </summary>
        public List<MutableGenericParameter> GenericParameters { get; }

        /// <summary>
        /// Whether this is an instance method.
        /// </summary>
        public bool HasThis { get; set; }

        /// <summary>
        /// Whether this method has generic parameters.
        /// </summary>
        public bool HasGenericParameters => GenericParameters.Count > 0;

        /// <summary>
        /// The calling convention.
        /// </summary>
        public MutableCallingConvention CallingConvention { get; set; }

        /// <summary>
        /// Resolves this method reference to a method definition.
        /// </summary>
        public virtual MutableMethodDefinition Resolve()
        {
            return this as MutableMethodDefinition;
        }

        /// <summary>
        /// Gets the full name of the method.
        /// </summary>
        public string FullName
        {
            get
            {
                var paramTypes = string.Join(",", Parameters.ConvertAll(p => p.ParameterType?.FullName ?? "?"));
                return $"{ReturnType?.FullName ?? "void"} {DeclaringType?.FullName ?? "?"}::{Name}({paramTypes})";
            }
        }

        /// <inheritdoc/>
        public override string ToString() => FullName;
    }

    /// <summary>
    /// Represents a method definition in the mutable object model.
    /// This replaces Mono.Cecil.MethodDefinition.
    /// </summary>
    public class MutableMethodDefinition : MutableMethodReference, IMethodDefinition
    {
        /// <summary>
        /// Creates a new method definition.
        /// </summary>
        public MutableMethodDefinition(string name, MethodAttributes attributes, MutableTypeReference returnType)
            : base(name, returnType, null)
        {
            Attributes = attributes;
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The method attributes.
        /// </summary>
        public MethodAttributes Attributes { get; set; }

        public int MetadataToken { get; set; }

        /// <summary>
        /// Implementation attributes.
        /// </summary>
        public MethodImplAttributes ImplAttributes { get; set; }

        /// <summary>
        /// The method body (null for abstract/extern methods).
        /// </summary>
        public MutableMethodBody Body { get; set; }

        /// <summary>
        /// Custom attributes applied to this method.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        /// <summary>
        /// Whether this method is public.
        /// </summary>
        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        /// <summary>
        /// Whether this method is private.
        /// </summary>
        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

        /// <summary>
        /// Whether this method is family (protected).
        /// </summary>
        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

        /// <summary>
        /// Whether this method is assembly (internal).
        /// </summary>
        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

        /// <summary>
        /// Whether this method is static.
        /// </summary>
        public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;

        /// <summary>
        /// Whether this method is virtual.
        /// </summary>
        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

        /// <summary>
        /// Whether this method is abstract.
        /// </summary>
        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;

        /// <summary>
        /// Whether this method is sealed (final).
        /// </summary>
        public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;

        /// <summary>
        /// Whether this method is a new slot.
        /// </summary>
        public bool IsNewSlot => (Attributes & MethodAttributes.NewSlot) != 0;

        /// <summary>
        /// Whether this method is special name (property accessor, etc.).
        /// </summary>
        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;

        /// <summary>
        /// Whether this method is runtime special name.
        /// </summary>
        public bool IsRuntimeSpecialName => (Attributes & MethodAttributes.RTSpecialName) != 0;

        /// <summary>
        /// Whether this method is a constructor.
        /// </summary>
        public bool IsConstructor => IsRuntimeSpecialName && IsSpecialName && (Name == ".ctor" || Name == ".cctor");

        /// <summary>
        /// Whether this method is a static constructor.
        /// </summary>
        public bool IsStaticConstructor => IsConstructor && IsStatic;

        /// <summary>
        /// Whether this method is a P/Invoke.
        /// </summary>
        public bool IsPInvokeImpl => (Attributes & MethodAttributes.PinvokeImpl) != 0;

        /// <summary>
        /// Whether this method has a body.
        /// </summary>
        public bool HasBody => Body != null && Body.Instructions.Count > 0;

        /// <summary>
        /// Semantic attributes (getter, setter, add, remove, etc.).
        /// </summary>
        public MutableMethodSemanticsAttributes SemanticsAttributes { get; set; }

        /// <summary>
        /// P/Invoke information.
        /// </summary>
        public MutablePInvokeInfo PInvokeInfo { get; set; }

        public string ReturnTypeFullName => ReturnType?.FullName ?? string.Empty;

        public string DeclaringTypeFullName => DeclaringType?.FullName ?? string.Empty;

        public bool IsRuntime => (ImplAttributes & MethodImplAttributes.Runtime) != 0;

        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

        public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) != 0;

        public bool IsCompilerControlled => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;

        public bool HasCustomAttributes => CustomAttributes.Count > 0;

        public IReadOnlyList<string> ParameterTypeFullNames =>
            Parameters.Select(Helper.GetParameterTypeName).ToArray();

        /// <summary>
        /// Gets the IL processor for this method's body.
        /// </summary>
        public MutableILProcessor GetILProcessor()
        {
            if (Body == null)
            {
                Body = new MutableMethodBody(this);
            }
            return new MutableILProcessor(Body);
        }

        /// <inheritdoc/>
        public override MutableMethodDefinition Resolve() => this;

        IMethodBody IMethodDefinition.Body => Body;

        IEnumerable<IParameter> IMethodDefinition.Parameters => Parameters;

        IEnumerable<IGenericParameter> IMethodDefinition.GenericParameters => GenericParameters;

        IEnumerable<ICustomAttribute> IMethodDefinition.CustomAttributes => CustomAttributes;

        ITypeDefinition IMethodDefinition.DeclaringType => DeclaringType as MutableTypeDefinition;

        MethodSemantics IMethod.SemanticsAttributes => MapSemantics(SemanticsAttributes);

        private static MethodSemantics MapSemantics(MutableMethodSemanticsAttributes semantics)
        {
            if ((semantics & MutableMethodSemanticsAttributes.Getter) != 0)
                return MethodSemantics.Getter;
            if ((semantics & MutableMethodSemanticsAttributes.Setter) != 0)
                return MethodSemantics.Setter;
            if ((semantics & MutableMethodSemanticsAttributes.AddOn) != 0)
                return MethodSemantics.AddOn;
            if ((semantics & MutableMethodSemanticsAttributes.RemoveOn) != 0)
                return MethodSemantics.RemoveOn;
            if ((semantics & MutableMethodSemanticsAttributes.Fire) != 0)
                return MethodSemantics.Fire;
            if ((semantics & MutableMethodSemanticsAttributes.Other) != 0)
                return MethodSemantics.Other;
            return MethodSemantics.None;
        }
    }

    /// <summary>
    /// Represents a generic instance of a method.
    /// </summary>
    public class MutableGenericInstanceMethod : MutableMethodReference
    {
        /// <summary>
        /// Creates a new generic instance method.
        /// </summary>
        public MutableGenericInstanceMethod(MutableMethodReference elementMethod)
            : base(elementMethod.Name, elementMethod.ReturnType, elementMethod.DeclaringType)
        {
            ElementMethod = elementMethod;
            GenericArguments = new List<MutableTypeReference>();
        }

        /// <summary>
        /// The element method (the generic method definition).
        /// </summary>
        public MutableMethodReference ElementMethod { get; }

        /// <summary>
        /// The generic type arguments.
        /// </summary>
        public List<MutableTypeReference> GenericArguments { get; }
    }

    /// <summary>
    /// Represents a method parameter.
    /// This replaces Mono.Cecil.ParameterDefinition.
    /// </summary>
    public class MutableParameterDefinition : IParameter
    {
        /// <summary>
        /// Creates a new parameter definition.
        /// </summary>
        public MutableParameterDefinition(string name, ParameterAttributes attributes, MutableTypeReference parameterType)
        {
            Name = name;
            Attributes = attributes;
            ParameterType = parameterType;
            CustomAttributes = new List<MutableCustomAttribute>();
        }

        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The parameter attributes.
        /// </summary>
        public ParameterAttributes Attributes { get; set; }

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public MutableTypeReference ParameterType { get; set; }

        /// <summary>
        /// The index of this parameter (0-based).
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Custom attributes applied to this parameter.
        /// </summary>
        public List<MutableCustomAttribute> CustomAttributes { get; }

        public string ParameterTypeName => ParameterType?.FullName ?? string.Empty;

        public bool IsOut => (Attributes & ParameterAttributes.Out) != 0;

        public bool IsIn => (Attributes & ParameterAttributes.In) != 0;

        /// <summary>
        /// Whether this parameter is optional.
        /// </summary>
        public bool IsOptional => (Attributes & ParameterAttributes.Optional) != 0;

        /// <summary>
        /// Whether this parameter has a default value.
        /// </summary>
        public bool HasDefault => (Attributes & ParameterAttributes.HasDefault) != 0;

        /// <summary>
        /// The default value of this parameter.
        /// </summary>
        public object DefaultValue { get; set; }

        IEnumerable<ICustomAttribute> IParameter.CustomAttributes => CustomAttributes;
    }

    /// <summary>
    /// P/Invoke information for a method.
    /// </summary>
    public class MutablePInvokeInfo
    {
        /// <summary>
        /// The entry point name.
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// The module reference.
        /// </summary>
        public MutableModuleReference Module { get; set; }

        /// <summary>
        /// P/Invoke attributes.
        /// </summary>
        public int Attributes { get; set; }
    }

    /// <summary>
    /// Method semantics attributes.
    /// </summary>
    [System.Flags]
    public enum MutableMethodSemanticsAttributes
    {
        /// <summary>No semantics.</summary>
        None = 0,
        /// <summary>Property setter.</summary>
        Setter = 0x0001,
        /// <summary>Property getter.</summary>
        Getter = 0x0002,
        /// <summary>Other method (for property).</summary>
        Other = 0x0004,
        /// <summary>Event add.</summary>
        AddOn = 0x0008,
        /// <summary>Event remove.</summary>
        RemoveOn = 0x0010,
        /// <summary>Event fire.</summary>
        Fire = 0x0020,
    }

    /// <summary>
    /// Calling conventions.
    /// </summary>
    public enum MutableCallingConvention
    {
        /// <summary>Default calling convention.</summary>
        Default = 0,
        /// <summary>Vararg calling convention.</summary>
        VarArg = 5,
        /// <summary>Generic calling convention.</summary>
        Generic = 0x10,
    }
}
