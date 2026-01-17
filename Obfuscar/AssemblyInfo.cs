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
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Obfuscar.Helpers;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

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
        private List<TypeReference> unrenamedTypeReferences;
        private List<MemberReference> unrenamedReferences;
        private string filename;
        private AssemblyDefinition definition;
        private string name;
        private bool skipEnums;

        public bool Exclude { get; set; }

        bool initialized;

        // to create, use FromXml
        private AssemblyInfo(Project project)
        {
            this.project = project;
        }

        private static bool AssemblyIsSigned(AssemblyDefinition def)
        {
            return def.Name.PublicKeyToken.Length != 0;
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
            unrenamedReferences = new List<MemberReference>();
            var items = getMemberReferences();
            foreach (MemberReference member in items)
            {
                // FIXME: Figure out why these exist if they are never used.
                // MethodReference mr = member as MethodReference;
                // FieldReference fr = member as FieldReference;
                if (project.Contains(member.DeclaringType))
                    unrenamedReferences.Add(member);
            }

            HashSet<TypeReference> typerefs = new HashSet<TypeReference>();
            foreach (TypeReference type in definition.MainModule.GetTypeReferences())
            {
                if (type.FullName == "<Module>")
                    continue;

                if (project.Contains(type))
                    typerefs.Add(type);
            }
            
            // Also add DeclaringType of member references - these may not be in GetTypeReferences()
            // when the member is accessed via MethodSpec (generic method instantiation)
            foreach (MemberReference member in unrenamedReferences)
            {
                TypeReference declaringType = member.DeclaringType;
                if (declaringType != null && project.Contains(declaringType))
                {
                    // Get the element type to handle generic instances
                    TypeReference elementType = declaringType.GetElementType();
                    if (elementType != null && !typerefs.Contains(elementType))
                        typerefs.Add(elementType);
                    if (!typerefs.Contains(declaringType))
                        typerefs.Add(declaringType);
                }
                
                // Also collect type references from method signatures (return type and parameters)
                // These may contain GenericInstanceTypes with nested type references
                MethodReference methodRef = member as MethodReference;
                if (methodRef != null)
                {
                    CollectTypeReferencesFromType(methodRef.ReturnType, typerefs);
                    foreach (var param in methodRef.Parameters)
                    {
                        CollectTypeReferencesFromType(param.ParameterType, typerefs);
                    }
                }
                
                FieldReference fieldRef = member as FieldReference;
                if (fieldRef != null)
                {
                    CollectTypeReferencesFromType(fieldRef.FieldType, typerefs);
                }
            }

            // Type references in CustomAttributes
            List<CustomAttribute> customattributes = new List<CustomAttribute>();
            customattributes.AddRange(this.Definition.CustomAttributes);
            foreach (TypeDefinition type in GetAllTypeDefinitions())
            {
                customattributes.AddRange(type.CustomAttributes);
                foreach (MethodDefinition methoddef in type.Methods)
                    customattributes.AddRange(methoddef.CustomAttributes);
                foreach (FieldDefinition fielddef in type.Fields)
                    customattributes.AddRange(fielddef.CustomAttributes);
                foreach (EventDefinition eventdef in type.Events)
                    customattributes.AddRange(eventdef.CustomAttributes);
                foreach (PropertyDefinition propertydef in type.Properties)
                    customattributes.AddRange(propertydef.CustomAttributes);

                foreach (CustomAttribute customattribute in customattributes)
                {
                    // Add the attribute constructor's declaring type so it gets updated when renamed
                    if (customattribute.Constructor?.DeclaringType != null &&
                        project.Contains(customattribute.Constructor.DeclaringType))
                    {
                        typerefs.Add(customattribute.Constructor.DeclaringType);
                    }
                    
                    // Check Constructor and named parameter for argument of type "System.Type". i.e. typeof()
                    List<CustomAttributeArgument> customattributearguments = new List<CustomAttributeArgument>();
                    customattributearguments.AddRange(customattribute.ConstructorArguments);
                    foreach (CustomAttributeNamedArgument namedargument in customattribute.Properties)
                        customattributearguments.Add(namedargument.Argument);

                    foreach (CustomAttributeArgument ca in customattributearguments)
                    {
                        if (ca.Type.FullName == "System.Type" && ca.Value != null)
                            typerefs.Add((TypeReference) ca.Value);
                    }
                }
                customattributes.Clear();
            }

            unrenamedTypeReferences = new List<TypeReference>(typerefs);

            initialized = true;
        }
        
        /// <summary>
        /// Recursively collects type references from a type, including nested generic type arguments.
        /// This is needed to update type references in method signatures when types are renamed.
        /// </summary>
        private void CollectTypeReferencesFromType(TypeReference type, HashSet<TypeReference> typerefs)
        {
            if (type == null)
                return;
            
            // Skip generic parameters (like !0, !1, !!0, !!1) - they are not actual type references
            if (type is GenericParameter)
                return;
            
            // Handle GenericInstanceType - collect the element type and all generic arguments
            GenericInstanceType genericInstance = type as GenericInstanceType;
            if (genericInstance != null)
            {
                // Add the element type (the generic type definition)
                TypeReference elementType = genericInstance.ElementType;
                if (elementType != null && project.Contains(elementType))
                {
                    if (!typerefs.Contains(elementType))
                        typerefs.Add(elementType);
                }
                
                // Recursively collect from generic arguments
                foreach (TypeReference arg in genericInstance.GenericArguments)
                {
                    CollectTypeReferencesFromType(arg, typerefs);
                }
                return;
            }
            
            // Handle arrays, pointers, etc.
            TypeSpecification typeSpec = type as TypeSpecification;
            if (typeSpec != null)
            {
                CollectTypeReferencesFromType(typeSpec.ElementType, typerefs);
                return;
            }
            
            // For regular type references
            if (project.Contains(type) && !typerefs.Contains(type))
            {
                typerefs.Add(type);
            }
        }

        private class Graph
        {
            public readonly List<Node<TypeDefinition>> Root = new List<Node<TypeDefinition>>();

            public readonly Dictionary<string, Node<TypeDefinition>> _map =
                new Dictionary<string, Node<TypeDefinition>>();

            public Graph(IEnumerable<TypeDefinition> items)
            {
                foreach (var item in items)
                {
                    // Skip the synthetic <Module> type which can appear in SRM-materialized assemblies
                    var fullName = item.FullName;
                    if (string.IsNullOrEmpty(fullName) || fullName == "<Module>")
                        continue;
                    
                    var node = new Node<TypeDefinition> {Item = item};
                    Root.Add(node);
                    
                    // Use indexer to avoid exceptions on duplicate keys (can happen with SRM reader)
                    if (!_map.ContainsKey(fullName))
                        _map[fullName] = node;
                }

                AddParents(Root);
            }

            private void AddParents(List<Node<TypeDefinition>> nodes)
            {
                foreach (var node in nodes)
                {
                    Node<TypeDefinition> parent;
                    var baseType = node.Item.BaseType;
                    if (baseType != null)
                    {
                        if (TrySearchNode(baseType, out parent))
                        {
                            node.AppendTo(parent);
                        }
                    }

                    if (node.Item.HasInterfaces)
                    {
                        foreach (var inter in node.Item.Interfaces)
                        {
                            if (TrySearchNode(inter.InterfaceType, out parent))
                            {
                                node.AppendTo(parent);
                            }
                        }
                    }

                    var nestedParent = node.Item.DeclaringType;
                    if (nestedParent != null)
                    {
                        if (TrySearchNode(nestedParent, out parent))
                        {
                            node.AppendTo(parent);
                        }
                    }
                }
            }

            private bool TrySearchNode(TypeReference baseType, out Node<TypeDefinition> parent)
            {
                var key = baseType.FullName;
                parent = null;
                if (_map.ContainsKey(key))
                {
                    parent = _map[key];
                    if (parent.Item.Scope.Name != baseType.Scope.Name)
                    {
                        parent = null;
                    }
                }
                return parent != null;
            }

            internal IEnumerable<TypeDefinition> GetOrderedList()
            {
                var result = new List<TypeDefinition>();
                CleanPool(Root, result);
                return result;
            }

            private void CleanPool(List<Node<TypeDefinition>> pool, List<TypeDefinition> result)
            {
                while (pool.Count > 0)
                {
                    var toRemove = new List<Node<TypeDefinition>>();
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

                        bool IsLoop(Node<TypeDefinition> node)
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
                                node.Parents.Select(p => p.Item.FullName + " " + p.Item.Scope.Name));
                            Console.Error.WriteLine("{0} {1} : [{2}]", node.Item.FullName, node.Item.Scope.Name,
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

        private IEnumerable<TypeDefinition> _cached;

        public IEnumerable<TypeDefinition> GetAllTypeDefinitions()
        {
            if (_cached != null)
            {
                return _cached;
            }

            if (!definition.MarkedToRename())
            {
                return new TypeDefinition[0];
            }

            try
            {
                var result = definition.MainModule.GetAllTypes();
                var graph = new Graph(result);
                return _cached = graph.GetOrderedList();
            }
            catch (Exception e)
            {
                throw new ObfuscarException(string.Format("Failed to get type definitions for {0}", definition.Name),
                    e);
            }
        }

        public void InvalidateCache()
        {
            _cached = null;
        }

        public void PrepareDecoratorMatches()
        {
            foreach (TypeTester tester in skipTypes)
                tester.InitializeDecoratorMatches(this);
        }

        private IEnumerable<MemberReference> getMemberReferences()
        {
            HashSet<MemberReference> memberReferences = new HashSet<MemberReference>();
            foreach (TypeDefinition type in this.GetAllTypeDefinitions())
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    foreach (MethodReference memberRef in method.Overrides)
                    {
                        if (IsOnlyReference(memberRef))
                        {
                            memberReferences.Add(memberRef);
                        }
                    }
                    if (method.Body != null)
                    {
                        foreach (Instruction inst in method.Body.Instructions)
                        {
                            MemberReference memberRef = inst.Operand as MemberReference;
                            if (memberRef != null)
                            {
                                if (IsOnlyReference(memberRef) ||
                                    memberRef is FieldReference && !(memberRef is FieldDefinition))
                                {
                                    // FIXME: Figure out why this exists if it is never used.
                                    // int c = memberreferences.Count;
                                    memberReferences.Add(memberRef);
                                }
                            }
                        }
                    }
                }
            }
            return memberReferences;
        }

        private bool IsOnlyReference(MemberReference memberref)
        {
            if (memberref is MethodReference)
            {
                if (memberref is MethodDefinition)
                {
                    return false;
                }

                if (memberref is MethodSpecification)
                {
                    if (memberref is GenericInstanceMethod)
                    {
                        return true;
                    }

                    return false;
                }

                return !(memberref is CallSite);
            }

            return false;
        }

        private void LoadAssembly(string filename)
        {
            this.filename = filename;

            try
            {
                // Use the configured factory to create an assembly reader. By default this is the Cecil-based reader.
                using (var reader = Metadata.AssemblyReaderFactory.Create(filename))
                {
                    // If the reader exposes a Mono.Cecil AssemblyDefinition, use it to preserve behavior.
                    if (reader is Metadata.CecilAssemblyReader cecilReader)
                    {
                        // Preserve previous two-pass read behavior by re-reading via AssemblyDefinition.ReadAssembly
                        bool readSymbols = project.Settings.RegenerateDebugInfo &&
                                           System.IO.File.Exists(System.IO.Path.ChangeExtension(filename, "pdb"));

                        try
                        {
                            definition = cecilReader.AssemblyDefinition;
                        }
                        catch
                        {
                            // fall back to direct read if wrapper couldn't provide it
                            definition = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters
                            {
                                ReadingMode = ReadingMode.Deferred,
                                ReadSymbols = readSymbols,
                                AssemblyResolver = project.Cache
                            });
                        }

                        project.Cache.RegisterAssembly(definition);

                        // Re-read with immediate reading mode to populate structures.
                        try
                        {
                            definition = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters
                            {
                                ReadingMode = ReadingMode.Immediate,
                                ReadSymbols = readSymbols,
                                AssemblyResolver = project.Cache
                            });
                        }
                        catch
                        {
                            if (!readSymbols)
                                throw;
                            definition = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters
                            {
                                ReadingMode = ReadingMode.Immediate,
                                ReadSymbols = false,
                                AssemblyResolver = project.Cache
                            });
                        }

                        name = definition.Name.Name;
                    }
                    else if (reader is Metadata.SrmAssemblyReader srm)
                    {
                        bool fallbackToCecil = false;
                        bool readSymbols = project.Settings.RegenerateDebugInfo &&
                                           System.IO.File.Exists(System.IO.Path.ChangeExtension(filename, "pdb"));

                        try
                        {
                            // Materialize a minimal AssemblyDefinition from SRM and register it in the cache.
                            definition = srm.CreateAssemblyDefinition();
                        }
                        catch (OverflowException ex)
                        {
                            fallbackToCecil = true;
                            LoggerService.Logger.LogInformation("Srm reader failed for {0} ({1}); falling back to Cecil reader.",
                                filename, ex.Message);
                            definition = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters
                            {
                                ReadingMode = ReadingMode.Immediate,
                                ReadSymbols = readSymbols,
                                AssemblyResolver = project.Cache
                            });
                        }

                        project.Cache.RegisterAssembly(definition);
                        name = definition.Name.Name;
                        // Try to resolve a core library module (mscorlib / System.Private.CoreLib) and patch
                        // the TypeSystem.CoreLibrary via reflection so Resolve() works for core types.
                        if (!fallbackToCecil)
                        {
                            try
                            {
                                var module = definition.MainModule;
                                var candidates = new[] { "mscorlib", "System.Private.CoreLib", "netstandard", "System.Runtime" };
                                Mono.Cecil.AssemblyDefinition coreAsm = null;
                                foreach (var cand in candidates)
                                {
                                    var aref = module.AssemblyReferences.FirstOrDefault(a => a.Name == cand);
                                    if (aref == null)
                                        continue;

                                    try
                                    {
                                        coreAsm = project.Cache.Resolve(aref);
                                        if (coreAsm != null)
                                            break;
                                    }
                                    catch
                                    {
                                    }
                                }

                                if (coreAsm != null)
                                {
                                    var ts = module.TypeSystem;
                                    var tsType = ts.GetType();
                                    var fi = tsType.GetField("core_library", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                             ?? tsType.GetField("_coreLibrary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                             ?? tsType.GetField("coreLibrary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (fi != null)
                                    {
                                        fi.SetValue(ts, coreAsm.MainModule);
                                    }
                                }
                            }
                            catch
                            {
                                // best-effort only
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Non-Cecil assembly readers are not yet supported for loading into AssemblyInfo.");
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                throw new ObfuscarException("Unable to find assembly:  " + filename, e);
            }
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

        public AssemblyDefinition Definition
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

        public List<MemberReference> UnrenamedReferences
        {
            get
            {
                CheckInitialized();
                return unrenamedReferences;
            }
        }

        public List<TypeReference> UnrenamedTypeReferences
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

        public bool ShouldSkip(TypeKey type, InheritMap map, bool keepPublicApi, bool hidePrivateApi, bool markedOnly,
            bool skipCompilerGeneratedTypes, out string message)
        {
            var attribute = type.TypeDefinition.MarkedToRename();
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

            if (skipCompilerGeneratedTypes && type.TypeDefinition.HasCompilerGeneratedAttributes())
            {
                message = "compiler generated attribute rule in configuration";
                return true;
            }

            if (type.TypeDefinition.IsEnum && skipEnums)
            {
                message = "enum rule in configuration";
                return true;
            }

            if (type.TypeDefinition.IsTypePublic())
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

            if (method.Method.IsSpecialName)
            {
                switch (method.Method.SemanticsAttributes)
                {
                    case MethodSemanticsAttributes.Getter:
                    case MethodSemanticsAttributes.Setter:
                        message = "skipping properties";
                        return !project.Settings.RenameProperties;
                    case MethodSemanticsAttributes.AddOn:
                    case MethodSemanticsAttributes.RemoveOn:
                        message = "skipping events";
                        return !project.Settings.RenameEvents;
                    default:
                        message = "special name";
                        return true;
                }
            }

            return ShouldSkipParams(method, map, keepPublicApi, hidePrivateApi, markedOnly, out message);
        }

        public bool ShouldSkipParams(MethodKey method, InheritMap map, bool keepPublicApi, bool hidePrivateApi,
            bool markedOnly, out string message)
        {
            var attribute = method.Method.MarkedToRename();
            // skip runtime methods
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            var parent = method.DeclaringType.MarkedToRename();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
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

            if (method.Method.IsPublic() && (
                method.DeclaringType.IsTypePublic() ||
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
            // skip runtime methods
            if ((field.Field.IsRuntimeSpecialName && field.Field.Name == "value__"))
            {
                message = "special name";
                return true;
            }

            var attribute = field.Field.MarkedToRename();
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            var parent = field.DeclaringType.MarkedToRename();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
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

            if (field.Field.IsPublic() && field.DeclaringType.IsTypePublic())
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
            if (prop.Property.IsRuntimeSpecialName)
            {
                message = "runtime special name";
                return true;
            }

            var attribute = prop.Property.MarkedToRename();
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            var parent = prop.DeclaringType.MarkedToRename();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
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

            if (prop.Property.IsPublic() && (
                prop.DeclaringType.IsTypePublic() ||
                prop.Property.GetMethod != null && map.GetMethodGroup(new MethodKey(prop.Property.GetMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null ||
                prop.Property.SetMethod != null && map.GetMethodGroup(new MethodKey(prop.Property.SetMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null
            ))
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
            if (evt.Event.IsRuntimeSpecialName)
            {
                message = "runtime special name";
                return true;
            }

            var attribute = evt.Event.MarkedToRename();
            // skip runtime methods
            if (attribute != null)
            {
                message = "attribute";
                return !attribute.Value;
            }

            var parent = evt.DeclaringType.MarkedToRename();
            if (parent != null)
            {
                message = "type attribute";
                return !parent.Value;
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

            if (evt.Event.IsPublic() && (
                evt.DeclaringType.IsTypePublic() ||
                evt.Event.AddMethod != null && map.GetMethodGroup(new MethodKey(evt.Event.AddMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null ||
                evt.Event.RemoveMethod != null && map.GetMethodGroup(new MethodKey(evt.Event.RemoveMethod))?.Methods?.FirstOrDefault(m => m.DeclaringType.IsTypePublic()) != null
            ))
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
