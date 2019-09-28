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

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Obfuscar
{
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

            if (baseType != null)
            {
                result.Add(new TypeKey(baseType));
            }

            return result;
        }

        public void FillMethodGroup(IList<MethodGroup> groups, Project project)
        {
            if (baseNodes.Count == 0)
            {
                return;
            }

            var methods = new List<MethodDefinition>();
            foreach (var method in type.TypeDefinition.Methods)
            {
                methods.Add(method);
            }

            foreach (var baseType in baseNodes)
            {
                if (baseType.type.TypeDefinition.IsInterface)
                {
                    continue;
                }

                baseType.FillMethods(methods);
            }

            foreach (var method in methods)
            {
                if (method.IsConstructor)
                {
                    continue;
                }

                var newGroup = new MethodGroup();
                newGroup.Methods.Add(new MethodKey(method));
                if (method.Parameters.Any(p => p.ParameterType is GenericParameter))
                {
                    // If the method has generic arguments we need to group it with overloads in the same class so they are renamed the same
                    // way, otherwise the call site updating may fail to choose the right overload
                    MatchMethodGroup(method, newGroup, project);
                }
                else
                {
                    foreach (var baseType in baseNodes)
                    {
                        baseType.MatchMethodGroup(method, newGroup, project);
                    }
                }

                if (newGroup.Methods.Count > 1)
                {
                    groups.Add(newGroup);
                }
            }
        }

        private void FillMethods(IList<MethodDefinition> methods)
        {
            foreach (var method in type.TypeDefinition.Methods)
            {
                if (method.IsPublic || method.IsFamily)
                {
                    // IMPORTANT: Add such methods, as they are also part of this class.
                    methods.Add(method);
                }
            }

            foreach (var baseType in baseNodes)
            {
                baseType.FillMethods(methods);
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
            // Add base type methods recursively as the method might override something further up the hierarchy
            foreach (var baseType in baseNodes)
                baseType.MatchMethodGroup(method, newGroup, project);
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
            // Add base type properties recursively as the property might override something further up the hierarchy
            foreach (var baseType in baseNodes)
                baseType.MatchPropertyGroup(property, newGroup, project);
        }
    }
}
