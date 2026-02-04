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
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Obfuscar.Helpers;
using Obfuscar.Metadata.Mutable;

namespace Obfuscar
{
    class SrmAssemblyResolver : IMutableAssemblyResolver
    {
        private readonly Dictionary<string, MutableAssemblyDefinition> assemblies =
            new Dictionary<string, MutableAssemblyDefinition>(StringComparer.OrdinalIgnoreCase);

        // Cache of resolved type references to type definitions to avoid repeated lookups
        private readonly Dictionary<Obfuscar.Metadata.Mutable.MutableTypeReference, Obfuscar.Metadata.Mutable.MutableTypeDefinition> _typeResolveCache
            = new Dictionary<Obfuscar.Metadata.Mutable.MutableTypeReference, Obfuscar.Metadata.Mutable.MutableTypeDefinition>();

        private readonly HashSet<string> searchDirectories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> pathsPortable = new List<string>();
        private readonly List<string> pathsNetCore = new List<string>();

        public SrmAssemblyResolver(Project project)
        {
            foreach (var path in project.AllAssemblySearchPaths)
                AddSearchDirectory(path);

            foreach (AssemblyInfo info in project.AssemblyList)
                AddSearchDirectory(Path.GetDirectoryName(info.FileName));
        }

        public void AddSearchDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (Directory.Exists(path))
                searchDirectories.Add(path);
        }

        public void RegisterAssembly(MutableAssemblyDefinition assembly)
        {
            if (assembly?.Name?.Name == null)
                return;

            if (!assemblies.ContainsKey(assembly.Name.Name))
                assemblies[assembly.Name.Name] = assembly;

            var portablePath = assembly.GetPortableProfileDirectory();
            if (!string.IsNullOrEmpty(portablePath))
            {
                if (Directory.Exists(portablePath))
                    pathsPortable.Add(portablePath);
                else
                    LoggerService.Logger.LogWarning("Portable profile directory does not exist: {Path}", portablePath);
            }

            foreach (var netCorePath in assembly.GetNetCoreDirectories())
            {
                if (Directory.Exists(netCorePath))
                    pathsNetCore.Add(netCorePath);
                else
                    LoggerService.Logger.LogWarning(".NET Core/.NET Standard/.NET referenced assembly directory does not exist: {Path}", netCorePath);
            }
        }

        public MutableAssemblyDefinition Resolve(MutableAssemblyNameReference name)
        {
            return Resolve(name, new MutableReaderParameters { AssemblyResolver = this });
        }

        public MutableAssemblyDefinition Resolve(MutableAssemblyNameReference name, MutableReaderParameters parameters)
        {
            if (name == null)
                return null;

            if (assemblies.TryGetValue(name.Name, out var cached))
                return cached;

            LoggerService.Logger.LogDebug("Resolving assembly '{Name}' using {SearchDirs} search directories.", name.Name, searchDirectories.Count);

            var fromSearchPaths = ResolveFromSearchPaths(name, parameters);
            if (fromSearchPaths != null)
                return fromSearchPaths;

            var runtimeAssembly = ResolveFromRuntime(name, parameters);
            if (runtimeAssembly != null)
                return runtimeAssembly;

            // Fallback: try recursive search in search directories (useful for publish folders)
            var fallback = ResolveByRecursiveSearch(name, parameters);
            if (fallback != null)
                return fallback;

            LoggerService.Logger.LogError("Unable to resolve dependency: {Name}. Searched {Count} directories.", name.Name, searchDirectories.Count);
            throw new FileNotFoundException("Unable to resolve dependency: " + name.Name);
        }

        private MutableAssemblyDefinition ResolveByRecursiveSearch(MutableAssemblyNameReference name, MutableReaderParameters parameters)
        {
            try
            {
                foreach (var dir in searchDirectories)
                {
                    if (!Directory.Exists(dir))
                        continue;

                    // Try to find the dll anywhere under the search directory (published apps may place deps in subfolders)
                    foreach (var file in Directory.EnumerateFiles(dir, name.Name + ".dll", SearchOption.AllDirectories))
                    {
                        LoggerService.Logger.LogDebug("Resolved assembly '{Name}' from recursive search: {Path}", name.Name, file);
                        var asm = LoadAssembly(file, parameters);
                        if (asm != null)
                            return asm;
                    }

                    foreach (var file in Directory.EnumerateFiles(dir, name.Name + ".exe", SearchOption.AllDirectories))
                    {
                        LoggerService.Logger.LogDebug("Resolved assembly '{Name}' from recursive search: {Path}", name.Name, file);
                        var asm = LoadAssembly(file, parameters);
                        if (asm != null)
                            return asm;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Logger.LogWarning(ex, "Recursive search for assembly '{Name}' failed.", name.Name);
            }

            return null;
        }

        private MutableAssemblyDefinition ResolveFromSearchPaths(MutableAssemblyNameReference name, MutableReaderParameters parameters)
        {
            var searchPaths = new List<string>(searchDirectories);
            if ((name.Attributes & AssemblyNameFlags.Retargetable) != 0)
            {
                searchPaths.AddRange(pathsPortable);
            }
            else
            {
                searchPaths.AddRange(pathsNetCore);
            }

            foreach (var dir in searchPaths)
            {
                var dllPath = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(dllPath))
                {
                    LoggerService.Logger.LogDebug("Resolved assembly '{Name}' from search path: {Path}", name.Name, dllPath);
                    return LoadAssembly(dllPath, parameters);
                }

                var exePath = Path.Combine(dir, name.Name + ".exe");
                if (File.Exists(exePath))
                {
                    LoggerService.Logger.LogDebug("Resolved assembly '{Name}' from search path: {Path}", name.Name, exePath);
                    return LoadAssembly(exePath, parameters);
                }
            }

            return null;
        }

        private MutableAssemblyDefinition ResolveFromRuntime(MutableAssemblyNameReference name, MutableReaderParameters parameters)
        {
            if (!IsRuntimeAssemblyName(name.Name))
                return null;

            var runtimePath = typeof(object).Assembly.Location;
            if (!File.Exists(runtimePath))
                return null;

            LoggerService.Logger.LogDebug("Resolved assembly '{Name}' from runtime: {Path}", name.Name, runtimePath);
            var runtimeAssembly = LoadAssembly(runtimePath, parameters);
            if (runtimeAssembly != null && !assemblies.ContainsKey(name.Name))
                assemblies[name.Name] = runtimeAssembly;

            return runtimeAssembly;
        }

        private static bool IsRuntimeAssemblyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return string.Equals(name, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "mscorlib", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "System.Runtime", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "System", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("System.", StringComparison.OrdinalIgnoreCase);
        }

        private MutableAssemblyDefinition LoadAssembly(string path, MutableReaderParameters parameters)
        {
            var readerParams = parameters ?? new MutableReaderParameters();
            if (readerParams.AssemblyResolver == null)
                readerParams.AssemblyResolver = this;

            var assembly = MutableAssemblyDefinition.ReadAssembly(path, readerParams);
            RegisterAssembly(assembly);
            return assembly;
        }

        public MutableTypeDefinition GetTypeDefinition(MutableTypeReference type)
        {
            if (type == null)
                return null;

            if (type is MutableTypeDefinition typeDef)
                return typeDef;

            if (type is MutableGenericInstanceType generic)
                return GetTypeDefinition(generic.ElementType);

            if (type is MutableArrayType arrayType)
                return GetTypeDefinition(arrayType.ElementType);

            if (type is MutableByReferenceType byRefType)
                return GetTypeDefinition(byRefType.ElementType);

            if (type is MutablePointerType pointerType)
                return GetTypeDefinition(pointerType.ElementType);

            // Try a quick cache based on the reference instance
            if (_typeResolveCache.TryGetValue(type, out var cachedDef))
                return cachedDef;

            string scopeName = type.GetScopeName();
            if (string.IsNullOrEmpty(scopeName))
                return null;

            if (!assemblies.TryGetValue(scopeName, out var assembly))
            {
                assembly = Resolve(new MutableAssemblyNameReference(scopeName, new Version(0, 0, 0, 0)));
            }

            var def = assembly?.MainModule?.GetType(type.FullName);
            // Cache result (may be null)
            _typeResolveCache[type] = def;
            return def;
        }
    }
}
