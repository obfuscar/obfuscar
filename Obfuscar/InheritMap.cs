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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Obfuscar
{
    class MethodGroup
    {
        public HashSet<MethodKey> Methods { get; } = new HashSet<MethodKey>();

        public string Name { get; set; } = null;

        public bool External { get; set; } = false;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name);
            if (External)
                sb.Append("(ext)");
            else
                sb.Append("(int)");
            sb.Append(": ");
            foreach (MethodKey k in Methods)
            {
                sb.Append(k.ToString());
                sb.Append(" ");
            }
            return sb.ToString();
        }

        internal void Merge(MethodGroup group)
        {
            foreach (var item in group.Methods)
            {
                Methods.Add(item);
            }

            External |= group.External;
        }
    }

    class PropertyGroup
    {
        public HashSet<PropertyKey> Properties { get; } = new HashSet<PropertyKey>();

        public string Name { get; set; } = null;

        public bool External { get; set; } = false;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name);
            if (External)
                sb.Append("(ext)");
            else
                sb.Append("(int)");
            sb.Append(": ");
            foreach (PropertyKey k in Properties)
            {
                sb.Append(k.ToString());
                sb.Append(" ");
            }
            return sb.ToString();
        }

        internal void Merge(PropertyGroup group)
        {
            foreach (var item in group.Properties)
            {
                Properties.Add(item);
            }

            External |= group.External;
        }
    }

    class GraphNode
    {
        private TypeKey type;
        private bool scaned;
        private List<GraphNode> baseNodes = new List<GraphNode>();

        public GraphNode(TypeKey type)
        {
            this.type = type;
        }

        internal void Scan(Dictionary<TypeKey, GraphNode> nodes, HashSet<TypeKey> toRemove, Project project)
        {
            if (scaned)
            {
                return;
            }

            var baseTypes = GetBaseTypes(project, type.TypeDefinition);
            foreach (var type in baseTypes)
            {
                if (nodes.ContainsKey(type))
                {
                    nodes[type].Scan(nodes, toRemove, project);
                    toRemove.Add(type);
                    baseNodes.Add(nodes[type]);
                }
                else
                {
                    // external types.
                    baseNodes.Add(new GraphNode(type));
                }
            }

            scaned = true;
        }

        public static List<TypeKey> GetBaseTypes(Project project, TypeDefinition type)
        {
            var result = new List<TypeKey>();
            // check the interfaces
            foreach (var ifaceRef in type.Interfaces)
            {
                TypeDefinition iface = project.GetTypeDefinition(ifaceRef.InterfaceType);
                
                // if it's not in the project, try to get it via the cache
                if (iface == null)
                    iface = project.Cache.GetTypeDefinition(ifaceRef.InterfaceType);

                if (iface != null)
                {
                    result.Add(new TypeKey(iface));
                }
            }

            // check the base type unless it isn't in the project, or we don't have one
            TypeDefinition baseType = project.GetTypeDefinition(type.BaseType);
            
            // if it's not in the project, try to get it via the cache
            if (baseType == null)
                baseType = project.Cache.GetTypeDefinition(type.BaseType);

            if (baseType != null && baseType.FullName != "System.Object")
            {
                result.Add(new TypeKey(baseType));
            }

            return result;
        }

        public void FillMethodGroup(List<MethodGroup> groups, Project project)
        {
            if (baseNodes.Count == 0)
            {
                return;
            }

            foreach (var method in type.TypeDefinition.Methods)
            {
                //if (!method.IsVirtual)
                //{
                //    continue;
                //}

                var newGroup = new MethodGroup();
                newGroup.Methods.Add(new MethodKey(method));
                foreach (var baseType in baseNodes)
                {
                    baseType.MatchMethodGroup(method, newGroup, project);
                }

                if (newGroup.Methods.Count > 1)
                {
                    groups.Add(newGroup);
                }
            }
        }

        private void MatchMethodGroup(MethodDefinition method, MethodGroup newGroup, Project project)
        {
            foreach (var baseMethod in type.TypeDefinition.Methods)
            {
                if (MethodKey.MethodMatch(baseMethod, method) 
                    || MethodKey.MethodMatch(method, baseMethod))
                {
                    newGroup.Methods.Add(new MethodKey(baseMethod));
                    newGroup.External |= !project.Contains(type);
                }
            }
        }

        internal TypeKey[] GetBaseTypes()
        {
            var result = new List<TypeKey>();
            foreach (var baseType in baseNodes)
            {
                result.Add(baseType.type);
                result.AddRange(baseType.GetBaseTypes());
            }

            return result.ToArray();
        }

        internal void FillPropertyGroup(List<PropertyGroup> groups, Project project)
        {
            if (baseNodes.Count == 0)
            {
                return;
            }

            foreach (var property in type.TypeDefinition.Properties)
            {
                if ((property.GetMethod != null && property.GetMethod.IsVirtual)
                    || (property.SetMethod != null && property.SetMethod.IsVirtual))
                {
                    var newGroup = new PropertyGroup();
                    newGroup.Properties.Add(new PropertyKey(type, property));
                    foreach (var baseType in baseNodes)
                    {
                        baseType.MatchPropertyGroup(property, newGroup, project);
                    }

                    groups.Add(newGroup);
                }
            }
        }

        private void MatchPropertyGroup(PropertyDefinition property, PropertyGroup newGroup, Project project)
        {
            foreach (var baseProperty in type.TypeDefinition.Properties)
            {
                if (PropertyKey.PropertyMatch(baseProperty, property)
                    || PropertyKey.PropertyMatch(property, baseProperty))
                {
                    newGroup.Properties.Add(new PropertyKey(type, baseProperty));
                    newGroup.External |= !project.Contains(type);
                }
            }
        }
    }

    class InheritMap
    {
        private Dictionary<TypeKey, GraphNode> nodes = new Dictionary<TypeKey, GraphNode>();
        private readonly Dictionary<MethodKey, MethodGroup> methodGroups = new Dictionary<MethodKey, MethodGroup>();
        private readonly Dictionary<PropertyKey, PropertyGroup> propertyGroups = new Dictionary<PropertyKey, PropertyGroup>();

        public InheritMap(Project project)
        {
            Project = project;
            // cache for assemblies not in the project
            project.Cache = new AssemblyCache(project);

            foreach (AssemblyInfo info in project)
            {
                foreach (TypeDefinition type in info.GetAllTypeDefinitions())
                {
                    if (type.FullName == "<Module>")
                        continue;

                    TypeKey typeKey = new TypeKey(type);
                    nodes.Add(typeKey, new GraphNode(typeKey));
                }
            }

            var toRemove = new HashSet<TypeKey>();
            foreach (var item in nodes)
            {
                item.Value.Scan(nodes, toRemove, project);
            }

            //foreach (var item in toRemove)
            //{
            //    nodes.Remove(item);
            //}

            var groups = new List<MethodGroup>();
            var properties = new List<PropertyGroup>();
            foreach (var node in nodes)
            {
                node.Value.FillMethodGroup(groups, Project);
                node.Value.FillPropertyGroup(properties, Project);
            }

            foreach (var group in groups)
            {
                foreach (var item in group.Methods)
                {
                    if (methodGroups.ContainsKey(item))
                    {
                        methodGroups[item].Merge(group);
                        continue;
                    }

                    methodGroups.Add(item, group);
                }
            }

            foreach (var group in properties)
            {
                foreach (var item in group.Properties)
                {
                    if (propertyGroups.ContainsKey(item))
                    {
                        propertyGroups[item].Merge(group);
                        continue;
                    }

                    propertyGroups.Add(item, group);
                }
            }
        }

        internal Project Project { get; private set; }

        public MethodGroup GetMethodGroup(MethodKey methodKey)
        {
            return methodGroups.ContainsKey(methodKey) ? methodGroups[methodKey] : null;
        }

        public PropertyGroup GetPropertyGroup(PropertyKey propertyKey)
        {
            return propertyGroups.ContainsKey(propertyKey) ? propertyGroups[propertyKey] : null;
        }

        public bool Inherits(TypeDefinition type, string interfaceFullName)
        {
            if (type.FullName == interfaceFullName)
            {
                return true;
            }

            if (type.BaseType != null)
            {
                var typeDef = Project.Cache.GetTypeDefinition(type.BaseType);
                return Inherits(typeDef, interfaceFullName);
            }

            return false;
        }

        public TypeKey[] GetBaseTypes(TypeKey typeKey)
        {
            if (nodes.ContainsKey(typeKey))
            {
                return nodes[typeKey].GetBaseTypes();
            }

            return new TypeKey[0];
        }

        public static void GetBaseTypes(Project project, HashSet<TypeKey> baseTypes, TypeDefinition type)
        {
            foreach (var item in project.InheritMap.GetBaseTypes(new TypeKey(type)))
            {
                baseTypes.Add(item);
            }
        }
    }

    //class InheritMap
    //{
    //    private readonly Project project;

    //    // method to group map
    //    private readonly Dictionary<MethodKey, MethodGroup> methodGroups = new Dictionary<MethodKey, MethodGroup>();

    //    private readonly Dictionary<TypeKey, TypeKey[]> baseTypes = new Dictionary<TypeKey, TypeKey[]>();

    //    public InheritMap(Project project)
    //    {
    //        this.project = project;

    //        // cache for assemblies not in the project
    //        project.Cache = new AssemblyCache(project);

    //        foreach (AssemblyInfo info in project)
    //        {
    //            foreach (TypeDefinition type in info.GetAllTypeDefinitions())
    //            {
    //                if (type.FullName == "<Module>")
    //                    continue;

    //                TypeKey typeKey = new TypeKey(type);

    //                baseTypes[typeKey] = GetBaseTypes(type, project);

    //                int i = 0;
    //                int j;

    //                MethodKey[] methods = GetVirtualMethods(project.Cache, type);
    //                while (i < methods.Length)
    //                {
    //                    MethodGroup group;
    //                    var left = methods[i];
    //                    if (!methodGroups.TryGetValue(left, out group))
    //                        group = null;

    //                    for (j = i + 1; j < methods.Length; j++)
    //                    {
    //                        var right = methods[j];
    //                        if (!MethodsMatch(left, right))
    //                            continue;

    //                        // found an override

    //                        // see if either method is already in a group
    //                        if (group != null)
    //                            group = AddToGroup(group, right);
    //                        else if (methodGroups.TryGetValue(right, out group))
    //                            group = AddToGroup(group, left);
    //                        else
    //                        {
    //                            group = new MethodGroup();

    //                            group = AddToGroup(group, left);
    //                            group = AddToGroup(group, right);
    //                        }

    //                        // if the group isn't already external, see if it should be
    //                        Debug.Assert(group != null, "should have a group by now");
    //                        if (!group.External && !project.Contains(right.TypeKey))
    //                            group.External = true;
    //                    }

    //                    // if the group isn't already external, see if it should be
    //                    if (group != null && !group.External && !project.Contains(left.TypeKey))
    //                        group.External = true;

    //                    // move on to the next thing that doesn't match
    //                    i++;
    //                }
    //            }
    //        }
    //    }

    //    static bool MethodsMatch(MethodKey left, MethodKey right)
    //    {
    //        return MethodKey.MethodMatch(left.Method, right.Method)
    //               || MethodKey.MethodMatch(right.Method, left.Method);
    //    }

    //    public static void GetBaseTypes(Project project, HashSet<TypeKey> baseTypes, TypeDefinition type)
    //    {
    //        // check the interfaces
    //        foreach (var ifaceRef in type.Interfaces)
    //        {
    //            TypeDefinition iface = project.GetTypeDefinition(ifaceRef.InterfaceType);
    //            if (iface != null)
    //            {
    //                GetBaseTypes(project, baseTypes, iface);
    //                baseTypes.Add(new TypeKey(iface));
    //            }
    //        }

    //        // check the base type unless it isn't in the project, or we don't have one
    //        TypeDefinition baseType = project.GetTypeDefinition(type.BaseType);
    //        if (baseType != null && baseType.FullName != "System.Object")
    //        {
    //            GetBaseTypes(project, baseTypes, baseType);
    //            baseTypes.Add(new TypeKey(baseType));
    //        }
    //    }

    //    public static TypeKey[] GetBaseTypes(TypeDefinition type, Project project)
    //    {
    //        HashSet<TypeKey> baseTypes = new HashSet<TypeKey>();
    //        GetBaseTypes(project, baseTypes, type);
    //        return new List<TypeKey>(baseTypes).ToArray();
    //    }

    //    void GetVirtualMethods(AssemblyCache cache, HashSet<MethodKey> methods, TypeDefinition type)
    //    {
    //        // check the interfaces
    //        foreach (var ifaceRef in type.Interfaces)
    //        {
    //            TypeDefinition iface = project.GetTypeDefinition(ifaceRef.InterfaceType);

    //            // if it's not in the project, try to get it via the cache
    //            if (iface == null)
    //                iface = cache.GetTypeDefinition(ifaceRef.InterfaceType);

    //            // search interface
    //            if (iface != null)
    //                GetVirtualMethods(cache, methods, iface);
    //        }

    //        // check the base type unless it isn't in the project, or we don't have one
    //        TypeDefinition baseType = project.GetTypeDefinition(type.BaseType);

    //        // if it's not in the project, try to get it via the cache
    //        if (baseType == null)
    //            baseType = cache.GetTypeDefinition(type.BaseType);

    //        // search base
    //        if (baseType != null)
    //            GetVirtualMethods(cache, methods, baseType);

    //        foreach (MethodDefinition method in type.Methods)
    //        {
    //            if (method.IsVirtual)
    //                methods.Add(new MethodKey(method));
    //        }

    //        foreach (PropertyDefinition property in type.Properties)
    //        {
    //            if (property.GetMethod != null && property.GetMethod.IsVirtual)
    //                methods.Add(new MethodKey(property.GetMethod));

    //            if (property.SetMethod != null && property.SetMethod.IsVirtual)
    //                methods.Add(new MethodKey(property.SetMethod));
    //        }

    //        foreach (EventDefinition @event in type.Events)
    //        {
    //            if (@event.AddMethod != null && @event.AddMethod.IsVirtual)
    //                methods.Add(new MethodKey(@event.AddMethod));

    //            if (@event.RemoveMethod != null && @event.RemoveMethod.IsVirtual)
    //                methods.Add(new MethodKey(@event.RemoveMethod));
    //        }
    //    }

    //    MethodKey[] GetVirtualMethods(AssemblyCache cache, TypeDefinition type)
    //    {
    //        HashSet<MethodKey> methods = new HashSet<MethodKey>();
    //        GetVirtualMethods(cache, methods, type);
    //        return new List<MethodKey>(methods).ToArray();
    //    }

    //    MethodGroup AddToGroup(MethodGroup group, MethodKey methodKey)
    //    {
    //        // add the method to the group
    //        group.Methods.Add(methodKey);

    //        // point the method at the group
    //        MethodGroup group2;
    //        if (methodGroups.TryGetValue(methodKey, out group2) && group2 != group)
    //        {
    //            // we have a problem; two unrelated groups come together; merge them
    //            if (group.Methods.Count > group2.Methods.Count)
    //            {
    //                group.Name = group.Name ?? group2.Name;
    //                group.External = group.External | group2.External;
    //                foreach (MethodKey mk in group2.Methods)
    //                {
    //                    methodGroups[mk] = group;
    //                    group.Methods.Add(mk);
    //                }
    //                return group;
    //            }
    //            else
    //            {
    //                group2.Name = group2.Name ?? group.Name;
    //                group2.External = group2.External | group.External;
    //                foreach (MethodKey mk in group.Methods)
    //                {
    //                    methodGroups[mk] = group2;
    //                    group2.Methods.Add(mk);
    //                }
    //                return group2;
    //            }
    //        }
    //        methodGroups[methodKey] = group;

    //        return group;
    //    }

    //    public MethodGroup GetMethodGroup(MethodKey methodKey)
    //    {
    //        MethodGroup group;
    //        if (methodGroups.TryGetValue(methodKey, out group))
    //            return group;
    //        else
    //            return null;
    //    }

    //    public bool Inherits(TypeDefinition type, string interfaceFullName)
    //    {
    //        if (type.FullName == interfaceFullName)
    //        {
    //            return true;
    //        }


    //        if (type.BaseType != null)
    //        {
    //            var typeDef = project.Cache.GetTypeDefinition(type.BaseType);

    //            return Inherits(typeDef, interfaceFullName);
    //        }

    //        return false;
    //    }

    //    public TypeKey[] GetBaseTypes(TypeKey typeKey)
    //    {
    //        return baseTypes[typeKey];
    //    }
    //}
}
