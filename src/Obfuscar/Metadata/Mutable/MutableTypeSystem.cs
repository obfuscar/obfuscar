using System;
using System.Collections.Generic;
using System.Reflection;

namespace Obfuscar.Metadata.Mutable
{
    /// <summary>
    /// Provides built-in type references for a module.
    /// This replaces legacy Mono.Cecil.TypeSystem.
    /// </summary>
    public class MutableTypeSystem
    {
        private readonly MutableModuleDefinition _module;
        private readonly Dictionary<Type, MutableTypeReference> _typeCache = new Dictionary<Type, MutableTypeReference>();
        private readonly Dictionary<MethodBase, MutableMethodReference> _methodCache = new Dictionary<MethodBase, MutableMethodReference>();
        private readonly Dictionary<FieldInfo, MutableFieldReference> _fieldCache = new Dictionary<FieldInfo, MutableFieldReference>();

        // Core types assembly reference
        private MutableAssemblyNameReference _corlib;

        /// <summary>
        /// Creates a new type system for the specified module.
        /// </summary>
        public MutableTypeSystem(MutableModuleDefinition module)
        {
            _module = module;
            InitializeCoreTypes();
        }

        private void InitializeCoreTypes()
        {
            // Create corlib reference
            var corlibName = typeof(object).Assembly.GetName();
            _corlib = new MutableAssemblyNameReference(corlibName.Name, corlibName.Version)
            {
                PublicKeyToken = corlibName.GetPublicKeyToken()
            };

            // Initialize built-in types
            Void = CreateCoreType("System", "Void", true);
            Boolean = CreateCoreType("System", "Boolean", true);
            Char = CreateCoreType("System", "Char", true);
            SByte = CreateCoreType("System", "SByte", true);
            Byte = CreateCoreType("System", "Byte", true);
            Int16 = CreateCoreType("System", "Int16", true);
            UInt16 = CreateCoreType("System", "UInt16", true);
            Int32 = CreateCoreType("System", "Int32", true);
            UInt32 = CreateCoreType("System", "UInt32", true);
            Int64 = CreateCoreType("System", "Int64", true);
            UInt64 = CreateCoreType("System", "UInt64", true);
            Single = CreateCoreType("System", "Single", true);
            Double = CreateCoreType("System", "Double", true);
            IntPtr = CreateCoreType("System", "IntPtr", true);
            UIntPtr = CreateCoreType("System", "UIntPtr", true);
            String = CreateCoreType("System", "String", false);
            Object = CreateCoreType("System", "Object", false);
            TypedReference = CreateCoreType("System", "TypedReference", true);
        }

        private MutableTypeReference CreateCoreType(string @namespace, string name, bool isValueType)
        {
            var type = new MutableTypeReference(@namespace, name, _module)
            {
                Scope = _corlib,
                IsValueType = isValueType,
                IsPrimitive = isValueType && name != "Void" && name != "IntPtr" && name != "UIntPtr" && name != "TypedReference"
            };
            return type;
        }

        /// <summary>System.Void</summary>
        public MutableTypeReference Void { get; private set; }
        /// <summary>System.Boolean</summary>
        public MutableTypeReference Boolean { get; private set; }
        /// <summary>System.Char</summary>
        public MutableTypeReference Char { get; private set; }
        /// <summary>System.SByte</summary>
        public MutableTypeReference SByte { get; private set; }
        /// <summary>System.Byte</summary>
        public MutableTypeReference Byte { get; private set; }
        /// <summary>System.Int16</summary>
        public MutableTypeReference Int16 { get; private set; }
        /// <summary>System.UInt16</summary>
        public MutableTypeReference UInt16 { get; private set; }
        /// <summary>System.Int32</summary>
        public MutableTypeReference Int32 { get; private set; }
        /// <summary>System.UInt32</summary>
        public MutableTypeReference UInt32 { get; private set; }
        /// <summary>System.Int64</summary>
        public MutableTypeReference Int64 { get; private set; }
        /// <summary>System.UInt64</summary>
        public MutableTypeReference UInt64 { get; private set; }
        /// <summary>System.Single</summary>
        public MutableTypeReference Single { get; private set; }
        /// <summary>System.Double</summary>
        public MutableTypeReference Double { get; private set; }
        /// <summary>System.IntPtr</summary>
        public MutableTypeReference IntPtr { get; private set; }
        /// <summary>System.UIntPtr</summary>
        public MutableTypeReference UIntPtr { get; private set; }
        /// <summary>System.String</summary>
        public MutableTypeReference String { get; private set; }
        /// <summary>System.Object</summary>
        public MutableTypeReference Object { get; private set; }
        /// <summary>System.TypedReference</summary>
        public MutableTypeReference TypedReference { get; private set; }

        /// <summary>
        /// Imports a CLR type as a type reference.
        /// </summary>
        public MutableTypeReference Import(Type type)
        {
            if (type == null)
                return null;

            if (_typeCache.TryGetValue(type, out var cached))
                return cached;

            MutableTypeReference result;

            if (type.IsArray)
            {
                var elementType = Import(type.GetElementType());
                result = new MutableArrayType(elementType, type.GetArrayRank());
            }
            else if (type.IsByRef)
            {
                var elementType = Import(type.GetElementType());
                result = new MutableByReferenceType(elementType);
            }
            else if (type.IsPointer)
            {
                var elementType = Import(type.GetElementType());
                result = new MutablePointerType(elementType);
            }
            else if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var genericDef = Import(type.GetGenericTypeDefinition());
                var genericInstance = new MutableGenericInstanceType(genericDef);
                foreach (var arg in type.GetGenericArguments())
                {
                    genericInstance.GenericArguments.Add(Import(arg));
                }
                result = genericInstance;
            }
            else
            {
                // Regular type
                var asmName = type.Assembly.GetName();
                var asmRef = FindOrCreateAssemblyReference(asmName);

                result = new MutableTypeReference(type.Namespace, type.Name, _module)
                {
                    Scope = asmRef,
                    IsValueType = type.IsValueType,
                    IsPrimitive = type.IsPrimitive
                };

                if (type.DeclaringType != null)
                {
                    result.DeclaringType = Import(type.DeclaringType);
                }
            }

            _typeCache[type] = result;
            return result;
        }

        /// <summary>
        /// Imports a CLR method as a method reference.
        /// </summary>
        public MutableMethodReference Import(MethodInfo method)
        {
            if (method == null)
                return null;

            if (_methodCache.TryGetValue(method, out var cached))
                return (MutableMethodReference)cached;

            var declaringType = Import(method.DeclaringType);
            var returnType = Import(method.ReturnType);
            var result = new MutableMethodReference(method.Name, returnType, declaringType)
            {
                HasThis = !method.IsStatic
            };

            foreach (var param in method.GetParameters())
            {
                result.Parameters.Add(new MutableParameterDefinition(
                    param.Name,
                    (ParameterAttributes)param.Attributes,
                    Import(param.ParameterType))
                {
                    Index = param.Position
                });
            }

            _methodCache[method] = result;
            return result;
        }

        /// <summary>
        /// Imports a CLR constructor as a method reference.
        /// </summary>
        public MutableMethodReference Import(ConstructorInfo constructor)
        {
            if (constructor == null)
                return null;

            if (_methodCache.TryGetValue(constructor, out var cached))
                return (MutableMethodReference)cached;

            var declaringType = Import(constructor.DeclaringType);
            var result = new MutableMethodReference(constructor.IsStatic ? ".cctor" : ".ctor", Void, declaringType)
            {
                HasThis = !constructor.IsStatic
            };

            foreach (var param in constructor.GetParameters())
            {
                result.Parameters.Add(new MutableParameterDefinition(
                    param.Name,
                    (ParameterAttributes)param.Attributes,
                    Import(param.ParameterType))
                {
                    Index = param.Position
                });
            }

            _methodCache[constructor] = result;
            return result;
        }

        /// <summary>
        /// Imports a CLR field as a field reference.
        /// </summary>
        public MutableFieldReference Import(FieldInfo field)
        {
            if (field == null)
                return null;

            if (_fieldCache.TryGetValue(field, out var cached))
                return cached;

            var declaringType = Import(field.DeclaringType);
            var fieldType = Import(field.FieldType);
            var result = new MutableFieldReference(field.Name, fieldType, declaringType);

            _fieldCache[field] = result;
            return result;
        }

        private MutableAssemblyNameReference FindOrCreateAssemblyReference(AssemblyName asmName)
        {
            foreach (var existing in _module.AssemblyReferences)
            {
                if (existing.Name == asmName.Name)
                    return existing;
            }

            var newRef = new MutableAssemblyNameReference(asmName.Name, asmName.Version)
            {
                PublicKeyToken = asmName.GetPublicKeyToken(),
                Culture = asmName.CultureName
            };
            _module.AssemblyReferences.Add(newRef);
            return newRef;
        }
    }
}
