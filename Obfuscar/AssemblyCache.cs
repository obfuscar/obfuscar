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
using System.Collections.Generic;
using System.IO;
using System;

namespace Obfuscar
{
	class AssemblyCache : BaseAssemblyResolver
	{
		readonly Project project;
		readonly Dictionary<string, AssemblyDefinition> cache =
			new Dictionary<string, AssemblyDefinition> ();

		public AssemblyCache (Project project)
		{
			this.project = project;
		}

		public TypeDefinition GetTypeDefinition (TypeReference type)
		{
			if (type == null)
				return null;

			TypeDefinition typeDef = type as TypeDefinition;
			if (typeDef != null)
				return typeDef;

			AssemblyNameReference name = type.Scope as AssemblyNameReference;
			if (name == null) {
				GenericInstanceType gi = type as GenericInstanceType;
				return gi == null ? null : GetTypeDefinition (gi.ElementType);
			}

			// try to self resolve, fall back to default resolver
			AssemblyDefinition assmDef;
				try {
					Console.WriteLine ("Trying to resolve dependency: " + name);
					assmDef = Resolve (name);
					cache [name.FullName] = assmDef;
				} catch (FileNotFoundException) {
					throw new ObfuscarException ("Unable to resolve dependency:  " + name.Name);
				}

			string fullName = null;
			while (type.IsNested) {
				if (fullName == null)
					fullName = type.Name;
				else
					fullName = type.Name + "/" + fullName;
				type = type.DeclaringType;
			}

			if (fullName == null)
				fullName = type.Namespace + "." + type.Name;
			else
				fullName = type.Namespace + "." + type.Name + "/" + fullName;
			typeDef = assmDef.MainModule.GetType (fullName);
			return typeDef;
		}

		internal void Register (AssemblyDefinition definition)
		{
			cache [definition.FullName] = definition;
		}
	}
}