#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;
using System.Xml.Linq;

namespace Obfuscar
{
    class AssemblyInfo
    {
        private readonly Project project;
        private readonly PredicateCollection<string> skipNamespaces = new PredicateCollection<string>();
        private readonly PredicateCollection<TypeKey> skipTypes = new PredicateCollection<TypeKey>();
        private readonly PredicateCollection<MethodKey> skipMethods = new PredicateCollection<MethodKey>();
        private readonly PredicateCollection<FieldKey> skipFields = new PredicateCollection<FieldKey>();
        private readonly PredicateCollection<PropertyKey> skipProperties = new PredicateCollection<PropertyKey>();
        private readonly PredicateCollection<EventKey> skipEvents = new PredicateCollection<EventKey>();
        private readonly PredicateCollection<string> forceNamespaces = new PredicateCollection<string>();
        private readonly PredicateCollection<TypeKey> forceTypes = new PredicateCollection<TypeKey>();
        private readonly PredicateCollection<MethodKey> forceMethods = new PredicateCollection<MethodKey>();
        private readonly PredicateCollection<FieldKey> forceFields = new PredicateCollection<FieldKey>();
        private readonly PredicateCollection<PropertyKey> forceProperties = new PredicateCollection<PropertyKey>();
        private readonly PredicateCollection<EventKey> forceEvents = new PredicateCollection<EventKey>();
        private readonly PredicateCollection<MethodKey> skipStringHiding = new PredicateCollection<MethodKey>();
        private readonly PredicateCollection<MethodKey> forceStringHiding = new PredicateCollection<MethodKey>();
        private List<MutableTypeReference> unrenamedTypeReferences;
        private List<object> unrenamedReferences;
        private string filename;
        private MutableAssemblyDefinition definition;
        private string name;
        private bool skipEnums;

        public bool Exclude { get; set; }

        bool initialized;

        // to create, use FromXml
        private AssemblyInfo(Project project)
        {
            this.project = project;
        }

        private static bool AssemblyIsSigned(MutableAssemblyDefinition def)
        {
            return def?.Name?.PublicKeyToken != null && def.Name.PublicKeyToken.Length != 0;
        }

        public static AssemblyInfo FromXml(Project project, XElement reader, string file, Variables vars)
        {
            AssemblyInfo info = new AssemblyInfo(project);

            // pull out the file attribute, but don't process anything empty
            info.LoadAssembly(file);

            string isExcluded = Helper.GetAttribute(reader, "Exclude", vars);
            if ((isExcluded.Length > 0) && (isExcluded.ToLowerInvariant() == "true"))
            {
                info.Exclude = true;
            }

            if (!reader.IsEmpty)
            {
                FromXmlReadNode(reader, project, vars, info);
            }

            return info;
        }

        private static void FromXmlReadNode(XElement element, Project project, Variables vars, AssemblyInfo info)
        {
            foreach (var reader in element.Elements())
            {
                string name = Helper.GetAttribute(reader, "name", vars);

                string rxStr = Helper.GetAttribute(reader, "rx");
                Regex rx = null;
                if (!string.IsNullOrEmpty(rxStr))
                {
                    rx = new Regex(rxStr);
                }

                string isStaticStr = Helper.GetAttribute(reader, "static", vars);
                bool? isStatic = null;
                if (!string.IsNullOrEmpty(isStaticStr))
                {
                    isStatic = Convert.ToBoolean(isStaticStr);
                }

                string isSerializableStr = Helper.GetAttribute(reader, "serializable", vars);
                bool? isSerializable = null;
                if (!string.IsNullOrEmpty(isSerializableStr))
                {
                    isSerializable = Convert.ToBoolean(isSerializableStr);
                }

                string attrib = Helper.GetAttribute(reader, "attrib", vars);
                string inherits = Helper.GetAttribute(reader, "typeinherits", vars);
                string type = Helper.GetAttribute(reader, "type", vars);
                string typeattrib = Helper.GetAttribute(reader, "typeattrib", vars);

                string val;
                switch (reader.Name.LocalName)
                {
                    case "Include":
                        {
                            Project.ReadIncludeTag(reader, project,
                                (includeReader, proj) => FromXmlReadNode(includeReader, proj, vars, info));
                            break;
                        }
                    case "SkipNamespace":
                        if (rx != null)
                        {
                            info.skipNamespaces.Add(new NamespaceTester(rx));
                        }
                        else
                        {
                            info.skipNamespaces.Add(new NamespaceTester(name));
                        }
                        break;
                    case "ForceNamespace":
                        if (rx != null)
                        {
                            info.forceNamespaces.Add(new NamespaceTester(rx));
                        }
                        else
                        {
                            info.forceNamespaces.Add(new NamespaceTester(name));
                        }
                        break;
                    case "SkipType":
                        TypeAffectFlags skipFlags = TypeAffectFlags.SkipNone;
                        string decorator = Helper.GetAttribute(reader, "decorator", vars);
                        string decoratorAllStr = Helper.GetAttribute(reader, "decoratorAll", vars);
                        string[] decoratorAll = string.IsNullOrEmpty(decoratorAllStr) ? null : decoratorAllStr.Split(',');

                        val = Helper.GetAttribute(reader, "skipMethods", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            skipFlags |= TypeAffectFlags.AffectMethod;

                        val = Helper.GetAttribute(reader, "skipStringHiding", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            skipFlags |= TypeAffectFlags.AffectString;

                        val = Helper.GetAttribute(reader, "skipFields", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            skipFlags |= TypeAffectFlags.AffectField;

                        val = Helper.GetAttribute(reader, "skipProperties", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            skipFlags |= TypeAffectFlags.AffectProperty;

                        val = Helper.GetAttribute(reader, "skipEvents", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            skipFlags |= TypeAffectFlags.AffectEvent;

                        if (rx != null)
                        {
                            info.skipTypes.Add(new TypeTester(rx, skipFlags, attrib, inherits, isStatic,
                                isSerializable, decorator, decoratorAll));
                        }
                        else
                        {
                            info.skipTypes.Add(new TypeTester(name, skipFlags, attrib, inherits, isStatic,
                                isSerializable, decorator, decoratorAll));
                        }
                        break;
                    case "ForceType":
                        TypeAffectFlags forceFlags = TypeAffectFlags.SkipNone;
                        string forceDecorator = Helper.GetAttribute(reader, "decorator", vars);
                        string forceDecoratorAllStr = Helper.GetAttribute(reader, "decoratorAll", vars);
                        string[] forceDecoratorAll = string.IsNullOrEmpty(forceDecoratorAllStr) ? null : forceDecoratorAllStr.Split(',');

                        val = Helper.GetAttribute(reader, "forceMethods", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            forceFlags |= TypeAffectFlags.AffectMethod;

                        val = Helper.GetAttribute(reader, "forceStringHiding", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            forceFlags |= TypeAffectFlags.AffectString;

                        val = Helper.GetAttribute(reader, "forceFields", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            forceFlags |= TypeAffectFlags.AffectField;

                        val = Helper.GetAttribute(reader, "forceProperties", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            forceFlags |= TypeAffectFlags.AffectProperty;

                        val = Helper.GetAttribute(reader, "forceEvents", vars);
                        if (val.Length > 0 && Convert.ToBoolean(val))
                            forceFlags |= TypeAffectFlags.AffectEvent;

                        if (rx != null)
                        {
                            info.forceTypes.Add(new TypeTester(rx, forceFlags, attrib, inherits, isStatic,
                                isSerializable, forceDecorator, forceDecoratorAll));
                        }
                        else
                        {
                            info.forceTypes.Add(new TypeTester(name, forceFlags, attrib, inherits, isStatic,
                                isSerializable, forceDecorator, forceDecoratorAll));
                        }
                        break;
                    case "SkipMethod":
                        if (rx != null)
                        {
                            info.skipMethods.Add(new MethodTester(rx, type, attrib, typeattrib, inherits,
                                isStatic));
                        }
                        else
                        {
                            info.skipMethods.Add(new MethodTester(name, type, attrib, typeattrib, inherits,
                                isStatic));
                        }
                        break;
                    case "ForceMethod":
                        if (rx != null)
                        {
                            info.forceMethods.Add(
                                new MethodTester(rx, type, attrib, typeattrib, inherits, isStatic));
                        }
                        else
                        {
                            info.forceMethods.Add(new MethodTester(name, type, attrib, typeattrib, inherits,
                                isStatic));
                        }
                        break;
                    case "SkipStringHiding":
                        if (rx != null)
                        {
                            info.skipStringHiding.Add(new MethodTester(rx, type, attrib, typeattrib));
                        }
                        else
                        {
                            info.skipStringHiding.Add(new MethodTester(name, type, attrib, typeattrib));
                        }
                        break;
                    case "ForceStringHiding":
                        if (rx != null)
                        {
                            info.forceStringHiding.Add(new MethodTester(rx, type, attrib, typeattrib));
                        }
                        else
                        {
                            info.forceStringHiding.Add(new MethodTester(name, type, attrib, typeattrib));
                        }
                        break;
                    case "SkipField":
                        string fieldDecorator = Helper.GetAttribute(reader, "decorator", vars);

                        if (rx != null)
                        {
                            info.skipFields.Add(new FieldTester(rx, type, attrib, typeattrib, inherits, fieldDecorator,
                                isStatic, isSerializable));
                        }
                        else
                        {
                            info.skipFields.Add(new FieldTester(name, type, attrib, typeattrib, inherits, fieldDecorator,
                                isStatic, isSerializable));
                        }
                        break;
                    case "ForceField":
                        string forceFieldDecorator = Helper.GetAttribute(reader, "decorator", vars);

                        if (rx != null)
                        {
                            info.forceFields.Add(new FieldTester(rx, type, attrib, typeattrib, inherits, forceFieldDecorator,
                                isStatic, isSerializable));
                        }
                        else
                        {
                            info.forceFields.Add(new FieldTester(name, type, attrib, typeattrib, inherits,
                                forceFieldDecorator, isStatic, isSerializable));
                        }
                        break;
                    case "SkipProperty":
                        if (rx != null)
                        {
                            info.skipProperties.Add(new PropertyTester(rx, type, attrib, typeattrib));
                        }
                        else
                        {
                            info.skipProperties.Add(new PropertyTester(name, type, attrib, typeattrib));
                        }
                        break;
                    case "ForceProperty":
                        if (rx != null)
                        {
                            info.forceProperties.Add(new PropertyTester(rx, type, attrib, typeattrib));
                        }
                        else
                        {
                            info.forceProperties.Add(new PropertyTester(name, type, attrib, typeattrib));
                        }
                        break;
                    case "SkipEvent":
                        if (rx != null)
                        {
                            info.skipEvents.Add(new EventTester(rx, type, attrib, typeattrib));
                        }
                        else
                        {
                            info.skipEvents.Add(new EventTester(name, type, attrib, typeattrib));
                        }
                        break;
                    case "ForceEvent":
                        if (rx != null)
                        {
                            info.forceEvents.Add(new EventTester(rx, type, attrib, typeattrib));
                        }
                        else
                        {
                            info.forceEvents.Add(new EventTester(name, type, attrib, typeattrib));
                        }
                        break;
                    case "SkipEnums":
                        var skipEnumsValue = Helper.GetAttribute(reader, "value");
                        info.skipEnums = skipEnumsValue.Length > 0 && Convert.ToBoolean(skipEnumsValue);
                        break;
                }
            }
        }

        /// <summary>
        /// Called by project to finish initializing the assembly.
        /// </summary>
        internal void Init()
        {
            unrenamedReferences = new List<object>();
            foreach (var member in getMemberReferences())
            {
                var declaringType = GetDeclaringType(member);
                if (declaringType != null && project.Contains(declaringType))
                    unrenamedReferences.Add(member);
            }

            var typerefs = new HashSet<MutableTypeReference>();
            CollectTypeReferences(typerefs);

            unrenamedTypeReferences = new List<MutableTypeReference>();
            foreach (var typeRef in typerefs)
            {
                if (typeRef?.FullName == "<Module>")
                    continue;
                if (typeRef != null && project.Contains(typeRef))
                    unrenamedTypeReferences.Add(typeRef);
            }

            initialized = true;
        }

        private static MutableTypeReference GetDeclaringType(object member)
        {
            if (member is MutableMethodReference methodRef)
                return methodRef.DeclaringType;
            if (member is MutableFieldReference fieldRef)
                return fieldRef.DeclaringType;
            return null;
        }

        private void CollectTypeReferences(HashSet<MutableTypeReference> typerefs)
        {
            if (definition?.CustomAttributes != null)
                CollectCustomAttributeTypeReferences(definition.CustomAttributes, typerefs);

            foreach (var typeDef in GetAllTypes())
            {
                if (typeDef is MutableTypeDefinition type)
                    CollectTypeReferencesFromTypeDefinition(type, typerefs);
            }

            foreach (var member in unrenamedReferences)
            {
                if (member is MutableMethodReference methodRef)
                {
                    CollectTypeReferencesFromType(methodRef.DeclaringType, typerefs);
                    CollectTypeReferencesFromType(methodRef.ReturnType, typerefs);
                    foreach (var param in methodRef.Parameters)
                        CollectTypeReferencesFromType(param.ParameterType, typerefs);

                    if (methodRef is MutableGenericInstanceMethod genericInstanceMethod)
                    {
                        foreach (var genericArgument in genericInstanceMethod.GenericArguments)
                            CollectTypeReferencesFromType(genericArgument, typerefs);
                    }
                }
                else if (member is MutableFieldReference fieldRef)
                {
                    CollectTypeReferencesFromType(fieldRef.DeclaringType, typerefs);
                    CollectTypeReferencesFromType(fieldRef.FieldType, typerefs);
                }
            }
        }

        private void CollectTypeReferencesFromTypeDefinition(MutableTypeDefinition type, HashSet<MutableTypeReference> typerefs)
        {
            if (type == null)
                return;

            CollectTypeReferencesFromType(type.BaseType, typerefs);
            if (type.DeclaringType != null)
                CollectTypeReferencesFromType(type.DeclaringType, typerefs);

            foreach (var iface in type.Interfaces)
                CollectTypeReferencesFromType(iface.InterfaceType, typerefs);

            CollectCustomAttributeTypeReferences(type.CustomAttributes, typerefs);

            foreach (var field in type.Fields)
            {
                CollectTypeReferencesFromType(field.FieldType, typerefs);
                CollectCustomAttributeTypeReferences(field.CustomAttributes, typerefs);
            }

            foreach (var method in type.Methods)
            {
                CollectTypeReferencesFromType(method.ReturnType, typerefs);
                foreach (var param in method.Parameters)
                {
                    CollectTypeReferencesFromType(param.ParameterType, typerefs);
                    CollectCustomAttributeTypeReferences(param.CustomAttributes, typerefs);
                }

                CollectCustomAttributeTypeReferences(method.CustomAttributes, typerefs);

                if (method.Body != null)
                {
                    foreach (var variable in method.Body.Variables)
                        CollectTypeReferencesFromType(variable.VariableType, typerefs);

                    foreach (var handler in method.Body.ExceptionHandlers)
                        CollectTypeReferencesFromType(handler.CatchType, typerefs);

                    foreach (var instruction in method.Body.Instructions)
                    {
                        CollectInstructionTypeReferences(instruction, typerefs);
                    }
                }
            }

            foreach (var prop in type.Properties)
            {
                CollectTypeReferencesFromType(prop.PropertyType, typerefs);
                foreach (var param in prop.Parameters)
                    CollectTypeReferencesFromType(param.ParameterType, typerefs);
                CollectCustomAttributeTypeReferences(prop.CustomAttributes, typerefs);
            }

            foreach (var evt in type.Events)
            {
                CollectTypeReferencesFromType(evt.EventType, typerefs);
                CollectCustomAttributeTypeReferences(evt.CustomAttributes, typerefs);
            }
        }

        private void CollectInstructionTypeReferences(MutableInstruction instruction, HashSet<MutableTypeReference> typerefs)
        {
            if (instruction?.Operand is MutableTypeReference typeRef)
            {
                CollectTypeReferencesFromType(typeRef, typerefs);
            }
            else if (instruction?.Operand is MutableMethodReference methodRef)
            {
                CollectTypeReferencesFromType(methodRef.DeclaringType, typerefs);
                CollectTypeReferencesFromType(methodRef.ReturnType, typerefs);
                foreach (var param in methodRef.Parameters)
                    CollectTypeReferencesFromType(param.ParameterType, typerefs);

                if (methodRef is MutableGenericInstanceMethod genericInstanceMethod)
                {
                    foreach (var genericArgument in genericInstanceMethod.GenericArguments)
                        CollectTypeReferencesFromType(genericArgument, typerefs);
                }
            }
            else if (instruction?.Operand is MutableFieldReference fieldRef)
            {
                CollectTypeReferencesFromType(fieldRef.DeclaringType, typerefs);
                CollectTypeReferencesFromType(fieldRef.FieldType, typerefs);
            }
        }

        private void CollectCustomAttributeTypeReferences(IEnumerable<MutableCustomAttribute> attributes, HashSet<MutableTypeReference> typerefs)
        {
            if (attributes == null)
                return;

            foreach (var customAttribute in attributes)
            {
                if (customAttribute?.Constructor?.DeclaringType != null)
                    CollectTypeReferencesFromType(customAttribute.Constructor.DeclaringType, typerefs);

                foreach (var arg in customAttribute.ConstructorArguments)
                    CollectCustomAttributeArgumentTypeReferences(arg, typerefs);
                foreach (var named in customAttribute.Fields)
                    CollectCustomAttributeArgumentTypeReferences(named.Argument, typerefs);
                foreach (var named in customAttribute.Properties)
                    CollectCustomAttributeArgumentTypeReferences(named.Argument, typerefs);
            }
        }

        private void CollectCustomAttributeArgumentTypeReferences(MutableCustomAttributeArgument arg, HashSet<MutableTypeReference> typerefs)
        {
            if (arg == null)
                return;

            if (arg.Type?.FullName == "System.Type" && arg.Value is MutableTypeReference typeRef)
            {
                CollectTypeReferencesFromType(typeRef, typerefs);
            }

            if (arg.Value is IEnumerable<MutableCustomAttributeArgument> arrayArgs)
            {
                foreach (var item in arrayArgs)
                    CollectCustomAttributeArgumentTypeReferences(item, typerefs);
            }
        }

        /// <summary>
        /// Recursively collects type references from a type, including nested generic type arguments.
        /// </summary>
        private void CollectTypeReferencesFromType(MutableTypeReference type, HashSet<MutableTypeReference> typerefs)
        {
            if (type == null)
                return;

            if (type is MutableGenericParameter)
                return;

            if (type.DeclaringType != null)
                CollectTypeReferencesFromType(type.DeclaringType, typerefs);

            if (type is MutableGenericInstanceType genericInstance)
            {
                CollectTypeReferencesFromType(genericInstance.ElementType, typerefs);
                foreach (var arg in genericInstance.GenericArguments)
                    CollectTypeReferencesFromType(arg, typerefs);
                return;
            }

            if (type is MutableArrayType arrayType)
            {
                CollectTypeReferencesFromType(arrayType.ElementType, typerefs);
                return;
            }

            if (type is MutableByReferenceType byRefType)
            {
                CollectTypeReferencesFromType(byRefType.ElementType, typerefs);
                return;
            }

            if (type is MutablePointerType pointerType)
            {
                CollectTypeReferencesFromType(pointerType.ElementType, typerefs);
                return;
            }

            if (!typerefs.Contains(type))
                typerefs.Add(type);
        }

        private class Graph
        {
            public readonly List<Node<MutableTypeDefinition>> Root = new List<Node<MutableTypeDefinition>>();

            public readonly Dictionary<string, Node<MutableTypeDefinition>> _map =
                new Dictionary<string, Node<MutableTypeDefinition>>();

            public Graph(IEnumerable<MutableTypeDefinition> items)
            {
                foreach (var item in items)
                {
                    // Skip the synthetic <Module> type which can appear in SRM-materialized assemblies
                    var fullName = item.FullName;
                    if (string.IsNullOrEmpty(fullName) || fullName == "<Module>")
                        continue;
                    
                    var node = new Node<MutableTypeDefinition> {Item = item};
                    Root.Add(node);
                    
                    // Use indexer to avoid exceptions on duplicate keys (can happen with SRM reader)
                    if (!_map.ContainsKey(fullName))
                        _map[fullName] = node;
                }

                AddParents(Root);
            }

            private void AddParents(List<Node<MutableTypeDefinition>> nodes)
            {
                foreach (var node in nodes)
                {
                    Node<MutableTypeDefinition> parent;
                    var baseType = node.Item.BaseType;
                    if (baseType != null)
                    {
                        if (TrySearchNode(baseType, out parent))
                        {
                            node.AppendTo(parent);
                        }
                    }

                    if (node.Item.Interfaces.Count > 0)
                    {
                        foreach (var inter in node.Item.Interfaces)
                        {
                            if (TrySearchNode(inter.InterfaceType, out parent))
                            {
                                node.AppendTo(parent);
                            }
                        }
                    }

                    var nestedParent = node.Item.DeclaringType as MutableTypeDefinition;
                    if (nestedParent != null)
                    {
                        if (TrySearchNode(nestedParent, out parent))
                        {
                            node.AppendTo(parent);
                        }
                    }
                }
            }

            private bool TrySearchNode(MutableTypeReference baseType, out Node<MutableTypeDefinition> parent)
            {
                var key = baseType.FullName;
                parent = null;
                if (_map.ContainsKey(key))
                {
                    parent = _map[key];
                }
                return parent != null;
            }

            internal IEnumerable<MutableTypeDefinition> GetOrderedList()
            {
                var result = new List<MutableTypeDefinition>();
                CleanPool(Root, result);
                return result;
            }

            private void CleanPool(List<Node<MutableTypeDefinition>> pool, List<MutableTypeDefinition> result)
            {
                while (pool.Count > 0)
                {
                    var toRemove = new List<Node<MutableTypeDefinition>>();
                    foreach (var node in pool)
                    {
                        if (node.Parents.Count == 0)
                        {
                            toRemove.Add(node);
                            if (result.Contains(node.Item))
                                continue;

                            result.Add(node.Item);
                        }
                    }

                    if (toRemove.Count == 0)
                    {
                        // Find the loop one
                        foreach (var node in pool)
                        {
                            if (IsLoop(node))
                            {
                                toRemove.Add(node);
                                if (result.Contains(node.Item))
                                    continue;

                                result.Add(node.Item);
                            }
                        }

                        bool IsLoop(Node<MutableTypeDefinition> node)
                        {
                            foreach (var nodeParent in node.Parents)
                            {
                                if (nodeParent.Parents.Any(n => ReferenceEquals(n, node)))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    }

                    if (toRemove.Count == 0)
                    {
                        Console.Error.WriteLine("Still in pool:");
                        foreach (var node in pool)
                        {
                            var parents = string.Join(", ",
                                node.Parents.Select(p => p.Item.FullName));
                            Console.Error.WriteLine("{0} : [{1}]", node.Item.FullName,
                                parents);
                        }
                        throw new ObfuscarException("Cannot clean pool");
                    }

                    foreach (var remove in toRemove)
                    {
                        pool.Remove(remove);
                        foreach (var child in remove.Children)
                        {
                            if (result.Contains(child.Item))
                                continue;

                            child.Parents.Remove(remove);
                        }
                    }
                }
            }
        }

        private IEnumerable<Metadata.Abstractions.ITypeDefinition> _cachedTypes;

        /// <summary>
        /// Gets all type definitions using the abstraction layer (Cecil-free interface).
        /// This is the preferred method for new code during Cecil migration.
        /// </summary>
        public IEnumerable<Metadata.Abstractions.ITypeDefinition> GetAllTypes()
        {
            if (_cachedTypes != null)
            {
                return _cachedTypes;
            }

            var assemblyMarkedToRename = definition.MarkedToRename();
            if (!assemblyMarkedToRename)
            {
                return Array.Empty<Metadata.Abstractions.ITypeDefinition>();
            }

            try
            {
                var types = new List<MutableTypeDefinition>();
                foreach (var typeDef in definition.MainModule.Types)
                {
                    AddTypeWithNested(typeDef, types);
                }

                var graph = new Graph(types);
                var ordered = new List<Metadata.Abstractions.ITypeDefinition>();
                foreach (var orderedType in graph.GetOrderedList())
                {
                    ordered.Add(orderedType);
                }

                return _cachedTypes = ordered;
            }
            catch (Exception e)
            {
                throw new ObfuscarException(string.Format("Failed to get types for {0}", definition.Name), e);
            }
        }

        private static void AddTypeWithNested(MutableTypeDefinition typeDef,
            List<MutableTypeDefinition> types)
        {
            types.Add(typeDef);
            foreach (var nested in typeDef.NestedTypes)
            {
                AddTypeWithNested(nested, types);
            }
        }

        public void InvalidateCache()
        {
            _cachedTypes = null;
        }

        public void PrepareDecoratorMatches()
        {
            foreach (TypeTester tester in skipTypes)
                tester.InitializeDecoratorMatches(this);
        }

        private IEnumerable<object> getMemberReferences()
        {
            var memberReferences = new HashSet<object>();
            foreach (var typeDef in GetAllTypes())
            {
                if (typeDef is not MutableTypeDefinition type)
                    continue;

                foreach (var method in type.Methods)
                {
                    if (method.Body == null)
                        continue;

                    foreach (var inst in method.Body.Instructions)
                    {
                        if (inst.Operand is MutableMethodReference methodRef)
                        {
                            if (methodRef is not MutableMethodDefinition)
                                memberReferences.Add(methodRef);
                        }
                        else if (inst.Operand is MutableFieldReference fieldRef)
                        {
                            if (fieldRef is not MutableFieldDefinition)
                                memberReferences.Add(fieldRef);
                        }
                    }
                }

                foreach (var impl in type.MethodImplementations)
                {
                    if (impl.MethodBody is MutableMethodReference bodyRef && impl.MethodBody is not MutableMethodDefinition)
                        memberReferences.Add(bodyRef);
                    if (impl.MethodDeclaration is MutableMethodReference declRef && impl.MethodDeclaration is not MutableMethodDefinition)
                        memberReferences.Add(declRef);
                }
            }
            return memberReferences;
        }

        private void LoadAssembly(string filename)
        {
            this.filename = filename;

            try
            {
                bool readSymbols = project.Settings.RegenerateDebugInfo &&
                                   System.IO.File.Exists(System.IO.Path.ChangeExtension(filename, "pdb"));
                var parameters = new MutableReaderParameters
                {
                    ReadSymbols = readSymbols,
                    AssemblyResolver = project.Cache
                };

                definition = MutableAssemblyDefinition.ReadAssembly(filename, parameters);
                project.Cache.RegisterAssembly(definition);
                name = definition.Name?.Name ?? string.Empty;

                // Validate referenced assemblies exist early to match legacy behavior.
                foreach (var asmRef in definition.MainModule.AssemblyReferences)
                {
                    if (IsFrameworkAssembly(asmRef.Name))
                    {
                        continue;
                    }

                    try
                    {
                        project.Cache.Resolve(asmRef, new MutableReaderParameters { AssemblyResolver = project.Cache });
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        throw new ObfuscarException("Unable to resolve dependency:  " + asmRef.Name, e);
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                throw new ObfuscarException("Unable to find assembly:  " + filename, e);
            }
        }

        private static bool IsFrameworkAssembly(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "netstandard", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "System", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);
        }

        public string FileName
        {
            get
            {
                CheckLoaded();
                return filename;
            }
        }

        public string OutputFileName { get; set; }

        public MutableAssemblyDefinition Definition
        {
            get
            {
                CheckLoaded();
                return definition;
            }
        }

        public string Name
        {
            get
            {
                CheckLoaded();
                return name;
            }
        }

        public List<object> UnrenamedReferences
        {
            get
            {
                CheckInitialized();
                return unrenamedReferences;
            }
        }

        public List<MutableTypeReference> UnrenamedTypeReferences
        {
            get
            {
                CheckInitialized();
                return unrenamedTypeReferences;
            }
        }

        public List<AssemblyInfo> References { get; } = new List<AssemblyInfo>();

        public List<AssemblyInfo> ReferencedBy { get; } = new List<AssemblyInfo>();

        private bool ShouldSkip(string ns, InheritMap map)
        {
            return skipNamespaces.IsMatch(ns, map);
        }

        private bool ShouldForce(string ns, InheritMap map)
        {
            return forceNamespaces.IsMatch(ns, map);
        }

        private bool ShouldSkip(TypeKey type, TypeAffectFlags flag, InheritMap map)
        {
            if (ShouldSkip(type.Namespace, map))
            {
                return true;
            }

            foreach (TypeTester typeTester in skipTypes)
            {
                if ((typeTester.AffectFlags & flag) > 0 && typeTester.Test(type, map))
                    return true;
            }

            return false;
        }

        private bool ShouldForce(TypeKey type, TypeAffectFlags flag, InheritMap map)
        {
            if (ShouldForce(type.Namespace, map))
            {
                return true;
            }

            foreach (TypeTester typeTester in forceTypes)
            {
                if ((typeTester.AffectFlags & flag) > 0 && typeTester.Test(type, map))
                    return true;
            }

            return false;
        }

        private const string CompilerGeneratedAttribute = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

        private static bool HasCompilerGeneratedAttribute(IEnumerable<MutableCustomAttribute> attributes)
        {
            if (attributes == null)
                return false;

            foreach (MutableCustomAttribute attribute in attributes)
            {
                if (attribute?.AttributeTypeName == CompilerGeneratedAttribute)
                    return true;
            }

            return false;
        }

        private static bool IsCompilerGenerated(TypeKey type)
        {
            return type?.Descriptor?.CustomAttributeTypeFullNames?.Contains(CompilerGeneratedAttribute) == true;
        }

        private static bool IsCompilerGenerated(MethodKey method)
        {
            return IsCompilerGenerated(method?.TypeKey) ||
                   HasCompilerGeneratedAttribute(method?.Method?.CustomAttributes);
        }

        private static bool IsCompilerGenerated(FieldKey field)
        {
            return IsCompilerGenerated(field?.TypeKey) ||
                   HasCompilerGeneratedAttribute(field?.Field?.CustomAttributes);
        }

        private static bool IsCompilerGenerated(PropertyKey property)
        {
            return IsCompilerGenerated(property?.TypeKey) ||
                   HasCompilerGeneratedAttribute(property?.Property?.CustomAttributes) ||
                   HasCompilerGeneratedAttribute(property?.Property?.GetMethod?.CustomAttributes) ||
                   HasCompilerGeneratedAttribute(property?.Property?.SetMethod?.CustomAttributes);
        }

        private static bool IsCompilerGenerated(EventKey evt)
        {
            return IsCompilerGenerated(evt?.TypeKey) ||
                   HasCompilerGeneratedAttribute(evt?.Event?.CustomAttributes) ||
                   HasCompilerGeneratedAttribute(evt?.Event?.AddMethod?.CustomAttributes) ||
                   HasCompilerGeneratedAttribute(evt?.Event?.RemoveMethod?.CustomAttributes) ||
                   HasCompilerGeneratedAttribute(evt?.Event?.InvokeMethod?.CustomAttributes);
        }

        public bool ShouldSkip(TypeKey type, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly,
            bool skipCompilerGeneratedTypes, out string message)
        {
            var descriptor = type.Descriptor;
            var typeDef = descriptor?.TypeDefinition;
            bool? attribute = typeDef?.MarkedToRename();
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            if (markedOnly)
            {
                message = "MarkedOnly option in configuration";
                return true;
            }

            if (forceTypes.IsMatch(type, map))
            {
                message = "type rule in configuration";
                return false;
            }

            if (ShouldForce(type.Namespace, map))
            {
                message = "namespace rule in configuration";
                return false;
            }

            if (skipTypes.IsMatch(type, map))
            {
                message = "type rule in configuration";
                return true;
            }

            if (ShouldSkip(type.Namespace, map))
            {
                message = "namespace rule in configuration";
                return true;
            }

            if (skipCompilerGeneratedTypes && IsCompilerGenerated(type))
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (descriptor?.IsEnum == true && skipEnums)
            {
                message = "enum rule in configuration";
                return true;
            }

            if (descriptor?.IsPublic == true)
            {
                message = "KeepPublicApi option in configuration";
                return keepPublicApi;
            }

            message = "HidePrivateApi option in configuration";
            return !hidePrivateApi;
        }

        public bool ShouldSkip(MethodKey method, InheritMap map, bool keepPublicApi, bool hidePrivateApi,
            bool markedOnly, out string message)
        {
            if (method.Method.IsRuntime)
            {
                message = "runtime method";
                return true;
            }

            if (project.Settings.SkipGenerated && IsCompilerGenerated(method))
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (method.Method.IsSpecialName)
            {
                if (project.Settings.SkipSpecialName)
                {
                    message = "special name rule in configuration";
                    return true;
                }

                var semantics = method.Method.SemanticsAttributes;
                if ((semantics & MutableMethodSemanticsAttributes.Getter) != 0 ||
                    (semantics & MutableMethodSemanticsAttributes.Setter) != 0)
                {
                    message = "skipping properties";
                    return !project.Settings.RenameProperties;
                }
                if ((semantics & MutableMethodSemanticsAttributes.AddOn) != 0 ||
                    (semantics & MutableMethodSemanticsAttributes.RemoveOn) != 0)
                {
                    message = "skipping events";
                    return !project.Settings.RenameEvents;
                }
                message = "special name";
                return true;
            }

            return ShouldSkipParams(method, map, keepPublicApi, hidePrivateApi, markedOnly, out message);
        }

        public bool ShouldSkipParams(MethodKey method, InheritMap map, bool keepPublicApi, bool hidePrivateApi,
            bool markedOnly, out string message)
        {
            bool? attribute = method.Method.MarkedToRename();
            // skip runtime methods
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            bool? parent = method.TypeKey.Descriptor?.TypeDefinition?.MarkedToRenameForMembers();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
            }

            if (project.Settings.SkipGenerated && IsCompilerGenerated(method))
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (markedOnly)
            {
                message = "MarkedOnly option in configuration";
                return true;
            }

            if (ShouldForce(method.TypeKey, TypeAffectFlags.AffectMethod, map))
            {
                message = "type rule in configuration";
                return false;
            }

            if (forceMethods.IsMatch(method, map))
            {
                message = "method rule in configuration";
                return false;
            }

            if (ShouldSkip(method.TypeKey, TypeAffectFlags.AffectMethod, map))
            {
                message = "type rule in configuration";
                return true;
            }

            if (skipMethods.IsMatch(method, map))
            {
                message = "method rule in configuration";
                return true;
            }

            var methodIsPublic = method.Method.IsPublic();
            if (methodIsPublic && (
                    method.TypeKey.Descriptor?.IsPublic == true ||
                    map.GetMethodGroup(method)?.Methods.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null
                ))
            {
                message = "KeepPublicApi option in configuration";
                return keepPublicApi;
            }

            message = "HidePrivateApi option in configuration";
            return !hidePrivateApi;
        }

        public bool ShouldSkipStringHiding(MethodKey method, InheritMap map, bool projectHideStrings)
        {
            if (method.DeclaringType.IsResourcesType() &&
                method.Method.ReturnType.FullName == "System.Resources.ResourceManager")
                return true; // IMPORTANT: avoid hiding resource type name, as it might be renamed later.

            if (project.Settings.SkipGenerated && IsCompilerGenerated(method))
                return true;

            if (ShouldForce(method.TypeKey, TypeAffectFlags.AffectString, map))
                return false;

            if (forceStringHiding.IsMatch(method, map))
                return false;

            if (ShouldSkip(method.TypeKey, TypeAffectFlags.AffectString, map))
                return true;

            if (skipStringHiding.IsMatch(method, map))
                return true;

            return !projectHideStrings;
        }

        public bool ShouldSkip(FieldKey field, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly,
            out string message)
        {
            var fieldAttributes = field.Field?.Attributes ?? 0;
            if ((fieldAttributes & System.Reflection.FieldAttributes.RTSpecialName) != 0 && field.Name == "value__")
            {
                message = "special name";
                return true;
            }

            bool? attribute = field.Field?.MarkedToRename();
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            bool? parent = field.DeclaringType?.MarkedToRenameForMembers();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
            }

            if (project.Settings.SkipGenerated && IsCompilerGenerated(field))
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (project.Settings.SkipSpecialName &&
                (fieldAttributes & System.Reflection.FieldAttributes.SpecialName) != 0)
            {
                message = "special name rule in configuration";
                return true;
            }

            if (markedOnly)
            {
                message = "MarkedOnly option in configuration";
                return true;
            }

            if (ShouldForce(field.TypeKey, TypeAffectFlags.AffectField, map))
            {
                message = "type rule in configuration";
                return false;
            }

            if (forceFields.IsMatch(field, map))
            {
                message = "field rule in configuration";
                return false;
            }

            if (ShouldSkip(field.TypeKey, TypeAffectFlags.AffectField, map))
            {
                message = "type rule in configuration";
                return true;
            }

            if (skipFields.IsMatch(field, map))
            {
                message = "field rule in configuration";
                return true;
            }

            if (skipEnums && field.DeclaringType.IsEnum)
            {
                message = "enum rule in configuration";
                return true;
            }

            var fieldIsPublic = field.Field != null && field.Field.IsPublic;
            if (fieldIsPublic && field.DeclaringType.IsTypePublic())
            {
                message = "KeepPublicApi option in configuration";
                return keepPublicApi;
            }

            message = "HidePrivateApi option in configuration";
            return !hidePrivateApi;
        }

        public bool ShouldSkip(PropertyKey prop, InheritMap map, bool keepPublicApi, bool hidePrivateApi,
            bool markedOnly, out string message)
        {
            if (prop.Property?.IsRuntimeSpecialName == true)
            {
                message = "runtime special name";
                return true;
            }

            if (project.Settings.SkipSpecialName && prop.Property != null)
            {
                bool hasSpecialNameAccessor =
                    (prop.Property.GetMethod?.IsSpecialName ?? false) ||
                    (prop.Property.SetMethod?.IsSpecialName ?? false);
                if (hasSpecialNameAccessor)
                {
                    message = "special name rule in configuration";
                    return true;
                }
            }

            bool? attribute = prop.Property?.MarkedToRename();
            if (attribute == null && prop.Property != null)
            {
                attribute = prop.Property.GetMethod?.MarkedToRename() ?? prop.Property.SetMethod?.MarkedToRename();
            }
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            bool? parent = prop.DeclaringType?.MarkedToRenameForMembers();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
            }

            if (project.Settings.SkipGenerated && IsCompilerGenerated(prop))
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (markedOnly)
            {
                message = "MarkedOnly option in configuration";
                return true;
            }

            if (ShouldForce(prop.TypeKey, TypeAffectFlags.AffectProperty, map))
            {
                message = "type rule in configuration";
                return false;
            }

            if (forceProperties.IsMatch(prop, map))
            {
                message = "property rule in configuration";
                return false;
            }

            if (ShouldSkip(prop.TypeKey, TypeAffectFlags.AffectProperty, map))
            {
                message = "type rule in configuration";
                return true;
            }

            if (skipProperties.IsMatch(prop, map))
            {
                message = "property rule in configuration";
                return true;
            }

            var propIsPublic = prop.Property != null && prop.Property.IsPublic();
            var declaringTypeIsPublic = prop.DeclaringType != null && prop.DeclaringType.IsTypePublic();
            var accessorHasPublicDeclaringType = false;
            if (prop.Property != null)
            {
                accessorHasPublicDeclaringType =
                    prop.Property.GetMethod != null && map.GetMethodGroup(new MethodKey(prop.Property.GetMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null ||
                    prop.Property.SetMethod != null && map.GetMethodGroup(new MethodKey(prop.Property.SetMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null;
            }

            if (propIsPublic && (declaringTypeIsPublic || accessorHasPublicDeclaringType))
            {
                message = "KeepPublicApi option in configuration";
                return keepPublicApi;
            }

            message = "HidePrivateApi option in configuration";
            return !hidePrivateApi;
        }

        public bool ShouldSkip(EventKey evt, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly,
            out string message)
        {
            // skip runtime special events
            if (evt.Event?.IsRuntimeSpecialName == true)
            {
                message = "runtime special name";
                return true;
            }

            if (project.Settings.SkipSpecialName && evt.Event != null)
            {
                bool hasSpecialNameAccessor =
                    (evt.Event.AddMethod?.IsSpecialName ?? false) ||
                    (evt.Event.RemoveMethod?.IsSpecialName ?? false) ||
                    (evt.Event.InvokeMethod?.IsSpecialName ?? false);
                if (hasSpecialNameAccessor)
                {
                    message = "special name rule in configuration";
                    return true;
                }
            }

            bool? attribute = evt.Event?.MarkedToRename();
            if (attribute == null && evt.Event != null)
            {
                attribute = evt.Event.AddMethod?.MarkedToRename() ?? evt.Event.RemoveMethod?.MarkedToRename();
            }
            // skip runtime methods
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            bool? parent = evt.DeclaringType?.MarkedToRenameForMembers();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
            }

            if (project.Settings.SkipGenerated && IsCompilerGenerated(evt))
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (markedOnly)
            {
                message = "MarkedOnly option in configuration";
                return true;
            }

            if (ShouldForce(evt.TypeKey, TypeAffectFlags.AffectEvent, map))
            {
                message = "type rule in configuration";
                return false;
            }

            if (forceEvents.IsMatch(evt, map))
            {
                message = "event rule in configuration";
                return false;
            }

            if (ShouldSkip(evt.TypeKey, TypeAffectFlags.AffectEvent, map))
            {
                message = "type rule in configuration";
                return true;
            }

            if (skipEvents.IsMatch(evt, map))
            {
                message = "event rule in configuration";
                return true;
            }

            var evtIsPublic = evt.Event != null && evt.Event.IsPublic();
            var declaringTypeIsPublic = evt.DeclaringType != null && evt.DeclaringType.IsTypePublic();
            var accessorHasPublicDeclaringType = false;
            if (evt.Event != null)
            {
                accessorHasPublicDeclaringType =
                    evt.Event.AddMethod != null && map.GetMethodGroup(new MethodKey(evt.Event.AddMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null ||
                    evt.Event.RemoveMethod != null && map.GetMethodGroup(new MethodKey(evt.Event.RemoveMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null;
            }

            if (evtIsPublic && (declaringTypeIsPublic || accessorHasPublicDeclaringType))
            {
                message = "KeepPublicApi option in configuration";
                return keepPublicApi;
            }

            message = "HidePrivateApi option in configuration";
            return !hidePrivateApi;
        }

        /// <summary>
        /// Makes sure that the assembly definition has been loaded (by <see cref="LoadAssembly"/>).
        /// </summary>
        private void CheckLoaded()
        {
            if (definition == null)
                throw new InvalidOperationException(
                    "Expected that AssemblyInfo.LoadAssembly would be called before use.");
        }

        /// <summary>
        /// Makes sure that the assembly has been initialized (by <see cref="Init"/>).
        /// </summary>
        private void CheckInitialized()
        {
            if (!initialized)
                throw new InvalidOperationException("Expected that AssemblyInfo.Init would be called before use.");
        }



        public override string ToString()
        {
            return Name;
        }
    }
}
