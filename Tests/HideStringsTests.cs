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
using System.IO;

using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
	public class HideStringsTests
	{
		public void BuildAndObfuscateAssemblies()
		{
			string xml = String.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='true' />" +
				@"<Module file='$(InPath)\AssemblyWithStrings.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate("AssemblyWithStrings", String.Empty, xml, true);
		}

		[Fact]
		public void CheckHideStringsClassExists()
		{
			BuildAndObfuscateAssemblies();
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
				Path.Combine(TestHelper.OutputPath, "AssemblyWithStrings.dll"));

			Assert.Equal(4, assmDef.MainModule.Types.Count); // "Should contain only one type, and <Module>.");
		}

		[Fact]
		public void CheckMethodHasAttribute()
		{
			BuildAndObfuscateAssemblies();
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
				Path.Combine(TestHelper.OutputPath, "AssemblyWithAttrs.dll"));

			bool found = false;
			foreach (TypeDefinition typeDef in assmDef.MainModule.Types)
			{
				if (typeDef.Name == "<Module>")
					continue;
				else
					found = true;

				Assert.Equal(2, typeDef.Methods.Count); // "Type is expected to have a single member.");

				MethodDefinition methodDef = typeDef.Methods[0];

				CustomAttribute attr = methodDef.CustomAttributes[0];
				Assert.Equal("System.Void System.ObsoleteAttribute::.ctor(System.String)", attr.Constructor.ToString());
				// "Type should have ObsoleteAttribute on it.");

				Assert.Equal(1, attr.ConstructorArguments.Count); // "ObsoleteAttribute should have one parameter.");
				Assert.Equal("Message Text", attr.ConstructorArguments[0].Value);
				// "ObsoleteAttribute param should have appropriate value.");
			}

			Assert.True(found, "Should have found non-<Module> type.");
		}
	}
}
