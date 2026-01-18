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
using System.Text;
using Obfuscar.Metadata.Abstractions;
using Obfuscar.Metadata.Mutable;

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
    }

    class InheritMap
    {
        private readonly Project project;

        // method to group map
        private readonly Dictionary<MethodKey, MethodGroup> methodGroups = new Dictionary<MethodKey, MethodGroup>();

        private readonly Dictionary<TypeKey, TypeKey[]> baseTypes = new Dictionary<TypeKey, TypeKey[]>();
        private readonly Dictionary<ITypeDefinition, TypeDescriptor> typeDescriptors = new Dictionary<ITypeDefinition, TypeDescriptor>();
        private readonly Dictionary<string, IType> typesByFullName = new Dictionary<string, IType>(StringComparer.Ordinal);

        public InheritMap(Project project)
        {
            this.project = project;
            var typeInfos = new List<TypeInfo>();
            foreach (AssemblyInfo info in project.AssemblyList)
            {
                foreach (ITypeDefinition type in info.GetAllTypes())
                {
                    if (type.FullName == "<Module>")
                        continue;

                    var typeKey = new TypeKey(type);
                    typeInfos.Add(new TypeInfo(info, type, typeKey));

                    if (!typesByFullName.ContainsKey(type.FullName))
                        typesByFullName[type.FullName] = type;
                }
            }

            foreach (var typeInfo in typeInfos)
            {
                GetDescriptor(typeInfo.TypeDef);

                baseTypes[typeInfo.Key] = GetBaseTypes(typeInfo.TypeDef);

                int i = 0;
                int j;

                var mutableType = typeInfo.TypeDef as MutableTypeDefinition;
                MethodKey[] methods = mutableType != null
                    ? GetVirtualMethods(mutableType)
                    : Array.Empty<MethodKey>();
                while (i < methods.Length)
                {
                    MethodGroup group;
                    var left = methods[i];
                    if (!methodGroups.TryGetValue(left, out group))
                        group = null;

                    for (j = i + 1; j < methods.Length; j++)
                    {
                        var right = methods[j];
                        if (!MethodsMatch(left, right))
                            continue;

                        // found an override

                        // see if either method is already in a group
                        if (group != null)
                            group = AddToGroup(group, right);
                        else if (methodGroups.TryGetValue(right, out group))
                            group = AddToGroup(group, left);
                        else
                        {
                            group = new MethodGroup();

                            group = AddToGroup(group, left);
                            group = AddToGroup(group, right);
                        }

                        // if the group isn't already external, see if it should be
                        Debug.Assert(group != null, "should have a group by now");
                        if (!group.External && !project.Contains(right.TypeKey))
                            group.External = true;
                    }

                    // if the group isn't already external, see if it should be
                    if (group != null && !group.External && !project.Contains(left.TypeKey))
                        group.External = true;

                    // move on to the next thing that doesn't match
                    i++;
                }
            }
        }

        private sealed class TypeInfo
        {
            public TypeInfo(AssemblyInfo owner, ITypeDefinition typeDef, TypeKey key)
            {
                Owner = owner;
                TypeDef = typeDef;
                Key = key;
            }

            public AssemblyInfo Owner { get; }
            public ITypeDefinition TypeDef { get; }
            public TypeKey Key { get; }
        }

        static bool MethodsMatch(MethodKey left, MethodKey right)
        {
            return MethodKey.MethodMatch(left.Method, right.Method)
                   || MethodKey.MethodMatch(right.Method, left.Method);
        }

        TypeKey[] GetBaseTypes(IType type)
        {
            HashSet<TypeKey> baseTypes = new HashSet<TypeKey>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            CollectBaseTypes(type, baseTypes, visited);
            return new List<TypeKey>(baseTypes).ToArray();
        }

        private void CollectBaseTypes(IType type, HashSet<TypeKey> baseTypes, HashSet<string> visited)
        {
            if (type == null)
                return;

            if (!string.IsNullOrEmpty(type.FullName) && !visited.Add(type.FullName))
                return;

            if (type.InterfaceTypeFullNames != null)
            {
                foreach (var ifaceName in type.InterfaceTypeFullNames)
                {
                    if (string.IsNullOrEmpty(ifaceName))
                        continue;

                    if (typesByFullName.TryGetValue(ifaceName, out var ifaceType))
                        CollectBaseTypes(ifaceType, baseTypes, visited);

                    baseTypes.Add(new TypeKey(ifaceName));
                }
            }

            var baseTypeName = type.BaseTypeFullName;
            if (!string.IsNullOrEmpty(baseTypeName) && baseTypeName != "System.Object")
            {
                if (typesByFullName.TryGetValue(baseTypeName, out var baseType))
                    CollectBaseTypes(baseType, baseTypes, visited);

                baseTypes.Add(new TypeKey(baseTypeName));
            }
        }

        private void GetVirtualMethods(HashSet<MethodKey> methods, MutableTypeDefinition type)
        {
            if (type == null)
                return;

            foreach (var ifaceRef in type.Interfaces)
            {
                var iface = project.GetTypeDefinition(ifaceRef.InterfaceType) ?? project.Cache.GetTypeDefinition(ifaceRef.InterfaceType);
                if (iface != null)
                    GetVirtualMethods(methods, iface);
            }

            var baseType = project.GetTypeDefinition(type.BaseType) ?? project.Cache.GetTypeDefinition(type.BaseType);
            if (baseType != null)
                GetVirtualMethods(methods, baseType);

            foreach (var method in type.Methods)
            {
                if (method.IsVirtual)
                    methods.Add(new MethodKey(method, method));
            }

            foreach (var property in type.Properties)
            {
                if (property.GetMethod != null && property.GetMethod.IsVirtual)
                    methods.Add(new MethodKey(property.GetMethod, property.GetMethod));

                if (property.SetMethod != null && property.SetMethod.IsVirtual)
                    methods.Add(new MethodKey(property.SetMethod, property.SetMethod));
            }

            foreach (var evt in type.Events)
            {
                if (evt.AddMethod != null && evt.AddMethod.IsVirtual)
                    methods.Add(new MethodKey(evt.AddMethod, evt.AddMethod));

                if (evt.RemoveMethod != null && evt.RemoveMethod.IsVirtual)
                    methods.Add(new MethodKey(evt.RemoveMethod, evt.RemoveMethod));
            }
        }

        private MethodKey[] GetVirtualMethods(MutableTypeDefinition type)
        {
            var methods = new HashSet<MethodKey>();
            GetVirtualMethods(methods, type);
            return new List<MethodKey>(methods).ToArray();
        }

        MethodGroup AddToGroup(MethodGroup group, MethodKey methodKey)
        {
            // add the method to the group
            group.Methods.Add(methodKey);

            // point the method at the group
            MethodGroup group2;
            if (methodGroups.TryGetValue(methodKey, out group2) && group2 != group)
            {
                // we have a problem; two unrelated groups come together; merge them
                if (group.Methods.Count > group2.Methods.Count)
                {
                    group.Name = group.Name ?? group2.Name;
                    group.External = group.External | group2.External;
                    foreach (MethodKey mk in group2.Methods)
                    {
                        methodGroups[mk] = group;
                        group.Methods.Add(mk);
                    }
                    return group;
                }
                else
                {
                    group2.Name = group2.Name ?? group.Name;
                    group2.External = group2.External | group.External;
                    foreach (MethodKey mk in group.Methods)
                    {
                        methodGroups[mk] = group2;
                        group2.Methods.Add(mk);
                    }
                    return group2;
                }
            }
            methodGroups[methodKey] = group;

            return group;
        }

        public MethodGroup GetMethodGroup(MethodKey methodKey)
        {
            MethodGroup group;
            if (methodGroups.TryGetValue(methodKey, out group))
                return group;
            else
                return null;
        }

        /// <summary>
        /// Checks if an ITypeDefinition inherits from or implements a type with the given full name.
        /// </summary>
        public bool Inherits(ITypeDefinition type, string interfaceFullName)
        {
            var descriptor = GetDescriptor(type);
            if (descriptor == null)
                return false;

            if (descriptor.FullName == interfaceFullName)
                return true;

            // Check through base types collected earlier
            var key = new TypeKey(type);
            if (baseTypes.TryGetValue(key, out var bases))
            {
                foreach (var baseType in bases)
                {
                    if (baseType.Fullname == interfaceFullName)
                        return true;
                }
            }

            return false;
        }

        public TypeKey[] GetBaseTypes(TypeKey typeKey)
        {
            if (baseTypes.TryGetValue(typeKey, out var bases))
                return bases;

            // Key not found - try to compute base types
            IType adapter = typeKey.TypeDefinition;
            if (adapter == null && !string.IsNullOrEmpty(typeKey.Fullname))
                typesByFullName.TryGetValue(typeKey.Fullname, out adapter);

            if (adapter == null)
                return Array.Empty<TypeKey>();

            bases = GetBaseTypes(adapter);
            baseTypes[typeKey] = bases;
            return bases;
        }

        public bool Inherits(TypeKey typeKey, string interfaceFullName)
        {
            if (typeKey == null)
                return false;

            if (typeKey.Fullname == interfaceFullName)
                return true;

            var bases = GetBaseTypes(typeKey);
            foreach (var baseType in bases)
            {
                if (baseType.Fullname == interfaceFullName)
                    return true;
            }

            return false;
        }

        private TypeDescriptor GetDescriptor(ITypeDefinition type)
        {
            if (type == null)
                return null;

            if (!typeDescriptors.TryGetValue(type, out var descriptor))
            {
                descriptor = TypeDescriptor.FromType(type);
                typeDescriptors[type] = descriptor;
            }

            return descriptor;
        }
    }
}
