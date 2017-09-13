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
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Obfuscar.Helpers;
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

        public static AssemblyInfo FromXml(Project project, XElement reader, Variables vars)
        {
            AssemblyInfo info = new AssemblyInfo(project);

            // pull out the file attribute, but don't process anything empty
            string val = Helper.GetAttribute(reader, "file", vars);
            if (val.Length > 0)
            {
                info.LoadAssembly(val);

                if (AssemblyIsSigned(info.Definition) && project.Settings.KeyFile == null)
                    throw new ObfuscarException("Obfuscating a signed assembly would result in an invalid assembly:  " +
                                                info.Name + "; use the KeyFile property to set a key to use");
            }
            else
                throw new InvalidOperationException("Need valid file attribute.");

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
                                isSerializable));
                        }
                        else
                        {
                            info.skipTypes.Add(new TypeTester(name, skipFlags, attrib, inherits, isStatic,
                                isSerializable));
                        }
                        break;
                    case "ForceType":
                        TypeAffectFlags forceFlags = TypeAffectFlags.SkipNone;

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
                                isSerializable));
                        }
                        else
                        {
                            info.forceTypes.Add(new TypeTester(name, forceFlags, attrib, inherits, isStatic,
                                isSerializable));
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
                        string decorator = Helper.GetAttribute(reader, "decorator", vars);

                        if (rx != null)
                        {
                            info.skipFields.Add(new FieldTester(rx, type, attrib, typeattrib, inherits, decorator,
                                isStatic, isSerializable));
                        }
                        else
                        {
                            info.skipFields.Add(new FieldTester(name, type, attrib, typeattrib, inherits, decorator,
                                isStatic, isSerializable));
                        }
                        break;
                    case "ForceField":
                        string decorator1 = Helper.GetAttribute(reader, "decorator", vars);

                        if (rx != null)
                        {
                            info.forceFields.Add(new FieldTester(rx, type, attrib, typeattrib, inherits, decorator1,
                                isStatic, isSerializable));
                        }
                        else
                        {
                            info.forceFields.Add(new FieldTester(name, type, attrib, typeattrib, inherits,
                                decorator1, isStatic, isSerializable));
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

        private class Graph
        {
            public readonly List<Node<TypeDefinition>> Root = new List<Node<TypeDefinition>>();

            public readonly Dictionary<string, Node<TypeDefinition>> _map =
                new Dictionary<string, Node<TypeDefinition>>();

            public Graph(IEnumerable<TypeDefinition> items)
            {
                foreach (var item in items)
                {
                    var node = new Node<TypeDefinition> {Item = item};
                    Root.Add(node);
                    _map.Add(item.FullName, node);
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
                        Console.Error.WriteLine("Still in pool:");
                        foreach (var node in pool)
                        {
                            var parents = String.Join(", ",
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
                bool readSymbols = project.Settings.RegenerateDebugInfo &&
                                   System.IO.File.Exists(System.IO.Path.ChangeExtension(filename, "pdb"));
                try
                {
                    definition = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters
                    {
                        ReadingMode = ReadingMode.Deferred,
                        ReadSymbols = readSymbols,
                        AssemblyResolver = project.Cache
                    });
                }
                catch
                {
                    // If there's a non-matching pdb next to it, this fails, else just try again
                    if (!readSymbols)
                        throw;
                    definition = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters
                    {
                        ReadingMode = ReadingMode.Deferred,
                        ReadSymbols = false,
                        AssemblyResolver = project.Cache
                    });
                }

                project.Cache.RegisterAssembly(definition);

                // IMPORTANT: read again but with full mode.
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
                    // If there's a non-matching pdb next to it, this fails, else just try again
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
            catch (System.IO.IOException e)
            {
                throw new ObfuscarException("Unable to find assembly:  " + filename, e);
            }
        }

        public string Filename
        {
            get
            {
                CheckLoaded();
                return filename;
            }
        }

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
            out string message)
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

            if (method.DeclaringType.IsTypePublic() && method.Method.IsPublic())
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

            if (skipEnums)
            {
                message = "enum rule in configuration";
                return true;
            }

            if (field.DeclaringType.IsTypePublic() && (field.Field.IsPublic || field.Field.IsFamily))
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

            if (prop.DeclaringType.IsTypePublic() && prop.Property.IsPublic())
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

            if (evt.DeclaringType.IsTypePublic() && evt.Event.IsPublic())
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
