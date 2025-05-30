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
using System.IO;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Obfuscar.Helpers;

namespace Obfuscar
{
    class AssemblyCache : DefaultAssemblyResolver
    {
        private List<string> pathsPortable = new List<string>();
        private List<string> pathsNetCore = new List<string>();

        public AssemblyCache(Project project)
        {
            foreach (var path in project.AllAssemblySearchPaths)
                AddSearchDirectory(path);

            foreach (AssemblyInfo info in project.AssemblyList)
                AddSearchDirectory(Path.GetDirectoryName(info.FileName));
        }

        public TypeDefinition GetTypeDefinition(TypeReference type)
        {
            if (type == null)
                return null;

            TypeDefinition typeDef = type as TypeDefinition;
            if (typeDef != null)
                return typeDef;

            AssemblyNameReference name = type.Scope as AssemblyNameReference;
            if (name == null)
            {
                GenericInstanceType gi = type as GenericInstanceType;
                return gi == null ? null : GetTypeDefinition(gi.ElementType);
            }

            AssemblyDefinition assmDef;
            try
            {
                assmDef = Resolve(name);
            }
            catch (FileNotFoundException)
            {
                throw new ObfuscarException("Unable to resolve dependency:  " + name.Name);
            }

            string fullName = type.GetFullName();
            typeDef = assmDef.MainModule.GetType(fullName);
            if (typeDef != null)
                return typeDef;

            // IMPORTANT: handle type forwarding
            if (!assmDef.MainModule.HasExportedTypes)
                return null;

            foreach (var exported in assmDef.MainModule.ExportedTypes)
            {
                if (exported.FullName == fullName)
                    return exported.Resolve();
            }

            return null;
        }

        public new void RegisterAssembly(AssemblyDefinition assembly)
        {
            var portablePath = assembly.GetPortableProfileDirectory();
            if (portablePath != null)
            {
                if (Directory.Exists(portablePath))
                {
                    pathsPortable.Add(portablePath);
                }
                else
                {
                    LoggerService.Logger.LogWarning("Portable profile directory does not exist: {Path}", portablePath);
                }
            }

            foreach (var netCorePath in assembly.GetNetCoreDirectories())
            {
                if (Directory.Exists(netCorePath))
                {
                    pathsNetCore.Add(netCorePath);
                }
                else
                {
                    LoggerService.Logger.LogWarning(".NET Core/.NET Standard/.NET referenced assembly directory does not exist: {Path}", netCorePath);
                }
            }

            base.RegisterAssembly(assembly);
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition result;
            if (name.IsRetargetable)
            {
                LoggerService.Logger.LogDebug("Assembly {Name} is retargetable, adding {paths} search paths", name.Name, pathsPortable.Count);
                foreach (var path in pathsPortable)
                {
                    LoggerService.Logger.LogDebug("Adding search path {Path}", path);
                    AddSearchDirectory(path);
                }

                result = base.Resolve(name, parameters);
                foreach (var path in pathsPortable)
                    RemoveSearchDirectory(path);
            }
            else if (pathsNetCore.Count > 0)
            {
                LoggerService.Logger.LogDebug("Assembly {Name} is not retargetable, adding {paths} search paths", name.Name, pathsNetCore.Count);
                foreach (var path in pathsNetCore)
                {
                    LoggerService.Logger.LogDebug("Adding search path {Path}", path);
                    AddSearchDirectory(path);
                }

                result = base.Resolve(name, parameters);
                foreach (var path in pathsNetCore)
                    RemoveSearchDirectory(path);
            }
            else
            {
                LoggerService.Logger.LogDebug("Assembly {Name} is using default search paths", name.Name);
                result = base.Resolve(name, parameters);
            }

            return result;
        }
    }
}
