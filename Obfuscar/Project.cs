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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Obfuscar.Helpers;
using System.Xml.Linq;

namespace Obfuscar
{
    class Project : IEnumerable<AssemblyInfo>
    {
        private const string SPECIALVAR_PROJECTFILEDIRECTORY = "ProjectFileDirectory";
        private readonly List<AssemblyInfo> assemblyList = new List<AssemblyInfo>();

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
                return vars.GetValue("ExtraFrameworkFolders", "").Split(new char[] {Path.PathSeparator},
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
        public byte[] keyPair;

        public byte[] KeyPair
        {
            get
            {
                if (keyPair != null)
                    return keyPair;

                var lKeyFileName = vars.GetValue("KeyFile", null);
                var lKeyContainerName = vars.GetValue("KeyContainer", null);

                if (lKeyFileName == null && lKeyContainerName == null)
                    return null;
                if (lKeyFileName != null && lKeyContainerName != null)
                    throw new ObfuscarException("'Key file' and 'Key container' properties cann't be setted together.");

                try
                {
                    keyPair = File.ReadAllBytes(vars.GetValue("KeyFile", null));
                }
                catch (Exception ex)
                {
                    throw new ObfuscarException(
                        String.Format("Failure loading key file \"{0}\"", vars.GetValue("KeyFile", null)), ex);
                }

                return keyPair;
            }
        }

        public RSA KeyValue
        {
            get
            {
                //if (keyvalue != null)
                //	return keyvalue;

                var lKeyFileName = vars.GetValue("KeyFile", null);
                var lKeyContainerName = vars.GetValue("KeyContainer", null);

                if (lKeyFileName == null && lKeyContainerName == null)
                    return null;
                if (lKeyFileName != null && lKeyContainerName != null)
                    throw new ObfuscarException("'Key file' and 'Key container' properties cann't be setted together.");

                if (vars.GetValue("KeyContainer", null) != null)
                {
                    KeyContainerName = vars.GetValue("KeyContainer", null);
                    if (Type.GetType("System.MonoType") != null)
                        throw new ObfuscarException("Key containers are not supported for Mono.");
                }

                return null;
                //return keyvalue;
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

            project.vars.Add(SPECIALVAR_PROJECTFILEDIRECTORY,
                string.IsNullOrEmpty(projectFileDirectory) ? "." : projectFileDirectory);

            if (reader.Root.Name != "Obfuscator")
            {
                throw new ObfuscarException("XML configuration file should have <Obfuscator> root tag.");
            }

            FromXmlReadNode(reader.Root, project);

            return project;
        }

        private static void FromXmlReadNode(XElement reader, Project project)
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
            }

            var includes = reader.Elements("Include");
            foreach (var include in includes)
            {
                ReadIncludeTag(include, project, FromXmlReadNode);
            }

            var searchPaths = reader.Elements("AssemblySearchPath");
            foreach (var searchPath in searchPaths)
            {
                string path =
                    Environment.ExpandEnvironmentVariables(Helper.GetAttribute(searchPath, "path", project.vars));
                project.assemblySearchPaths.Add(path);
            }

            var modules = reader.Elements("Module");
            foreach (var module in modules)
            {
                AssemblyInfo info = AssemblyInfo.FromXml(project, module, project.vars);
                if (info.Exclude)
                {
                    project.CopyAssemblyList.Add(info);
                    break;
                }

                Console.WriteLine("Processing assembly: " + info.Definition.Name.FullName);
                project.assemblyList.Add(info);
                project.assemblyMap[info.Name] = info;
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
            var graph = new Graph(assemblyList);
            assemblyList.Clear();
            assemblyList.AddRange(graph.GetOrderedList());
        }

        /// <summary>
        /// Looks through the settings, trys to make sure everything looks ok.
        /// </summary>
        public void CheckSettings()
        {
            foreach (string assemblySearchPath in assemblySearchPaths)
            {
                if (!Directory.Exists(assemblySearchPath))
                    throw new ObfuscarException("Path specified by AssemblySearchPath must exist:" +
                                                assemblySearchPath);
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
            foreach (AssemblyInfo info in assemblyList)
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
            foreach (AssemblyInfo info in assemblyList)
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return assemblyList.GetEnumerator();
        }

        public IEnumerator<AssemblyInfo> GetEnumerator()
        {
            return assemblyList.GetEnumerator();
        }
    }
}
