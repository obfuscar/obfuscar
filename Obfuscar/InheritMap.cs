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
using Mono.Cecil;

namespace Obfuscar
{
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

            foreach (AssemblyInfo info in project.AssemblyList)
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
}
