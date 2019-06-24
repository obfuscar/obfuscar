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
using Mono.Cecil;
using Obfuscar.Helpers;

namespace Obfuscar
{
    class AssemblyCache : DefaultAssemblyResolver
    {
        private List<string> paths = new List<string>();

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
            var path = assembly.GetPortableProfileDirectory();
            if (path != null && Directory.Exists(path))
                paths.Add(path);

            base.RegisterAssembly(assembly);
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition result;
            if (name.IsRetargetable)
            {
                foreach (var path in paths)
                    AddSearchDirectory(path);

                result = base.Resolve(name, parameters);
                foreach (var path in paths)
                    RemoveSearchDirectory(path);
            }
            else
            {
                result = base.Resolve(name, parameters);
            }

            return result;
        }
    }
}
