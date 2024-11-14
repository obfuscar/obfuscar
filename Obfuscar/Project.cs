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

using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Obfuscar.Helpers;
using System.Xml.Linq;

namespace Obfuscar
{
    class Project
    {
        public List<AssemblyInfo> AssemblyList { get; } = new List<AssemblyInfo>();
        public List<AssemblyInfo> CopyAssemblyList { get; } = new List<AssemblyInfo>();

        private readonly Dictionary<string, AssemblyInfo> assemblyMap = new Dictionary<string, AssemblyInfo>();
        private readonly Variables vars = new Variables();
        private readonly List<string> assemblySearchPaths = new List<string>();

        Settings settings;

        // FIXME: Figure out why this exists if it is never used.
        //private RSA keyvalue;
        // don't create.  call FromXml.
        private Project()
        {
        }

        public IEnumerable<string> ExtraPaths
        {
            get
            {
                return vars.GetValue(Settings.VariableExtraFrameworkFolders, "").Split(new char[] {Path.PathSeparator},
                    StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public IEnumerable<string> AllAssemblySearchPaths
        {
            get
            {
                return
                    ExtraPaths
                        .Concat(assemblySearchPaths)
                        .Concat(new[] {Settings.InPath});
            }
        }

        public string KeyContainerName = null;
        private string keyPair;
        private RSA keyValue;

        public string KeyPair
        {
            get
            {
                if (keyPair != null)
                {
                    return keyPair;
                }

                var lKeyFileName = vars.GetValue(Settings.VariableKeyFile, null);
                var lKeyContainerName = vars.GetValue(Settings.VariableKeyContainer, null);

                if (string.IsNullOrEmpty(lKeyFileName) && string.IsNullOrEmpty(lKeyContainerName))
                    return null;
                if (!string.IsNullOrEmpty(lKeyFileName) && !string.IsNullOrEmpty(lKeyContainerName))
                    throw new ObfuscarException($"'{Settings.VariableKeyFile}' and '{Settings.VariableKeyContainer}' variables cann't be setted together.");

                keyPair = lKeyFileName;

                return keyPair;
            }
        }

        public RSA KeyValue
        {
            get
            {
                if (keyValue != null)
                {
                    return keyValue;
                }

                if (Type.GetType("System.MonoType") != null)
                    throw new ObfuscarException("Key containers are not supported for Mono.");

                var lKeyFileName = vars.GetValue(Settings.VariableKeyFile, null);
                var lKeyContainerName = vars.GetValue(Settings.VariableKeyContainer, null);

                if (string.IsNullOrEmpty(lKeyFileName) && string.IsNullOrEmpty(lKeyContainerName))
                    return null;
                if (!string.IsNullOrEmpty(lKeyFileName) && !string.IsNullOrEmpty(lKeyContainerName))
                    throw new ObfuscarException($"'{Settings.VariableKeyFile}' and '{Settings.VariableKeyContainer}' variables cann't be setted together.");

                KeyContainerName = lKeyContainerName;

                if (KeyContainerName != null)
                {
                    CspParameters cp = new CspParameters();
                    cp.KeyContainerName = KeyContainerName;

                    keyValue = new RSACryptoServiceProvider(cp);
                    return keyValue;
                }
                else
                {
                    return null;
                }
            }
        }

        AssemblyCache m_cache;

        internal AssemblyCache Cache
        {
            get
            {
                if (m_cache == null)
                {
                    m_cache = new AssemblyCache(this);
                }

                return m_cache;
            }
            set { m_cache = value; }
        }

        public static Project FromXml(XDocument reader, string projectFileDirectory)
        {
            Project project = new Project();

            project.vars.Add(Settings.SpecialVariableProjectFileDirectory, string.IsNullOrEmpty(projectFileDirectory) ? "." : projectFileDirectory);

            if (reader.Root.Name != "Obfuscator")
            {
                throw new ObfuscarException("XML configuration file should have <Obfuscator> root tag.");
            }

            FromXmlReadNode(reader.Root, project);

            return project;
        }

        private static void FromXmlReadNode(XElement reader, Project project)
        {
            ReadVariables(reader, project);
            ReadIncludeTags(reader, project);
            ReadAssemblySearchPath(reader, project);
            ReadModules(reader, project);
            ReadModuleGroups(reader, project);
        }

        private static void ReadVariables(XElement reader, Project project)
        {
            var settings = reader.Elements("Var");
            foreach (var setting in settings)
            {
                var name = setting.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    var value = setting.Attribute("value")?.Value;
                    if (!string.IsNullOrEmpty(value))
                        project.vars.Add(name, value);
                    else
                        project.vars.Remove(name);
                }
                else
                    throw new ArgumentNullException("name");
            }
        }

        private static void ReadIncludeTags(XElement reader, Project project)
        {
            var includes = reader.Elements("Include");
            foreach (var include in includes)
            {
                ReadIncludeTag(include, project, FromXmlReadNode);
            }
        }

        internal static void ReadIncludeTag(XElement parentReader, Project project,
            Action<XElement, Project> readAction)
        {
            if (parentReader == null)
                throw new ArgumentNullException("parentReader");

            if (readAction == null)
                throw new ArgumentNullException("readAction");

            string path =
                Environment.ExpandEnvironmentVariables(Helper.GetAttribute(parentReader, "path", project.vars));
            var includeReader = XDocument.Load(path);
            if (includeReader.Root.Name == "Include")
            {
                readAction(includeReader.Root, project);
            }
        }

        private static void ReadAssemblySearchPath(XElement reader, Project project)
        {
            var searchPaths = reader.Elements("AssemblySearchPath");
            foreach (var searchPath in searchPaths)
            {
                string path =
                    Environment.ExpandEnvironmentVariables(Helper.GetAttribute(searchPath, "path", project.vars));
                project.assemblySearchPaths.Add(path);
            }
        }

        private static void ReadModules(XElement reader, Project project)
        {
            var modules = reader.Elements("Module");
            foreach (var module in modules)
            {
                var file = Helper.GetAttribute(module, "file", project.vars);
                if (string.IsNullOrWhiteSpace(file))
                    throw new InvalidOperationException("Need valid file attribute.");
                ReadModule(file, module, project);
            }
        }

        private static void ReadModuleGroups(XElement reader, Project project)
        {
            var modules = reader.Elements("Modules");
            foreach (var module in modules)
            {
                var includes = ReadModuleGroupPattern("IncludeFiles", module, project);
                if (!includes.Any())
                {
                    continue;
                }

                var excludes = ReadModuleGroupPattern("ExcludeFiles", module, project);
                var filter = new Filter(project.Settings.InPath, includes, excludes);
                foreach (var file in filter)
                {
                    ReadModule(file, module, project);
                }
            }
        }

        private static List<string> ReadModuleGroupPattern(string name, XElement module, Project project)
        {
            return (from i in module.Elements(name)
                    let value = project.vars.Replace(i.Value)
                    where !string.IsNullOrWhiteSpace(value)
                    select value).ToList();
        }

        private static void ReadModule(string file, XElement module, Project project)
        {
            var info = AssemblyInfo.FromXml(project, module, file, project.vars);
            if (info.Exclude)
            {
                project.CopyAssemblyList.Add(info);
            }
            else
            {
                Console.WriteLine("Processing assembly: " + info.Definition.Name.FullName);
                project.AssemblyList.Add(info);
                project.assemblyMap[info.Name] = info;
            }
        }

        private class Graph
        {
            public List<Node<AssemblyInfo>> Root = new List<Node<AssemblyInfo>>();

            public Graph(List<AssemblyInfo> items)
            {
                foreach (var item in items)
                    Root.Add(new Node<AssemblyInfo> {Item = item});

                AddParents(Root);
            }

            private static void AddParents(List<Node<AssemblyInfo>> nodes)
            {
                foreach (var node in nodes)
                {
                    var references = node.Item.References;
                    foreach (var reference in references)
                    {
                        var parent = SearchNode(reference, nodes);
                        node.AppendTo(parent);
                    }
                }
            }

            private static Node<AssemblyInfo> SearchNode(AssemblyInfo baseType, List<Node<AssemblyInfo>> nodes)
            {
                return nodes.FirstOrDefault(node => node.Item == baseType);
            }

            internal IEnumerable<AssemblyInfo> GetOrderedList()
            {
                var result = new List<AssemblyInfo>();
                CleanPool(Root, result);
                return result;
            }

            private void CleanPool(List<Node<AssemblyInfo>> pool, List<AssemblyInfo> result)
            {
                while (pool.Count > 0)
                {
                    var toRemoved = new List<Node<AssemblyInfo>>();
                    foreach (var node in pool)
                    {
                        if (node.Parents.Count == 0)
                        {
                            toRemoved.Add(node);
                            if (result.Contains(node.Item))
                                continue;

                            result.Add(node.Item);
                        }
                    }

                    foreach (var remove in toRemoved)
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

        private void ReorderAssemblies()
        {
            var graph = new Graph(AssemblyList);
            AssemblyList.Clear();
            AssemblyList.AddRange(graph.GetOrderedList());
        }

        /// <summary>
        /// Looks through the settings, trys to make sure everything looks ok.
        /// </summary>
        public void CheckSettings()
        {
            for (int i = 0; i < assemblySearchPaths.Count; i++)
            {
                string assemblySearchPath = assemblySearchPaths[i];
                if (!Directory.Exists(assemblySearchPath))
                {
                    //throw new ObfuscarException($"Path specified by AssemblySearchPath must exist:{assemblySearchPath}");
                    assemblySearchPaths.Remove(assemblySearchPath);
                }
            }

            if (!Directory.Exists(Settings.InPath))
                throw new ObfuscarException("Path specified by InPath variable must exist:" + Settings.InPath);

            if (!Directory.Exists(Settings.OutPath))
            {
                try
                {
                    Directory.CreateDirectory(Settings.OutPath);
                }
                catch (IOException e)
                {
                    throw new ObfuscarException("Could not create path specified by OutPath:  " + Settings.OutPath, e);
                }
            }
        }

        internal InheritMap InheritMap { get; private set; }

        internal Settings Settings
        {
            get
            {
                if (settings == null)
                    settings = new Settings(vars);

                return settings;
            }
        }

        public void LoadAssemblies()
        {
            // build reference tree
            foreach (AssemblyInfo info in AssemblyList)
            {
                // add self reference...makes things easier later, when
                // we need to go through the member references
                info.ReferencedBy.Add(info);

                // try to get each assembly referenced by this one.  if it's in
                // the map (and therefore in the project), set up the mappings
                foreach (AssemblyNameReference nameRef in info.Definition.MainModule.AssemblyReferences)
                {
                    AssemblyInfo reference;
                    if (assemblyMap.TryGetValue(nameRef.Name, out reference))
                    {
                        info.References.Add(reference);
                        reference.ReferencedBy.Add(info);
                    }
                }
            }

            // make each assembly's list of member refs
            foreach (AssemblyInfo info in AssemblyList)
            {
                info.Init();
            }

            // build inheritance map
            InheritMap = new InheritMap(this);
            ReorderAssemblies();
        }

        /// <summary>
        /// Returns whether the project contains a given type.
        /// </summary>
        public bool Contains(TypeReference type)
        {
            string name = type.GetScopeName();

            return assemblyMap.ContainsKey(name);
        }

        /// <summary>
        /// Returns whether the project contains a given type.
        /// </summary>
        internal bool Contains(TypeKey type)
        {
            return assemblyMap.ContainsKey(type.Scope);
        }

        public TypeDefinition GetTypeDefinition(TypeReference type)
        {
            if (type == null)
                return null;

            TypeDefinition typeDef = type as TypeDefinition;
            if (typeDef == null)
            {
                string name = type.GetScopeName();

                AssemblyInfo info;
                if (assemblyMap.TryGetValue(name, out info))
                {
                    string fullName = type.Namespace + "." + type.Name;
                    typeDef = info.Definition.MainModule.GetType(fullName);
                }
            }

            return typeDef;
        }
    }
}
