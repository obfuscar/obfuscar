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
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;

using NUnit.Framework;
using Mono.Cecil;

namespace ObfuscarTests
{
	[TestFixture]
	public class CustomAttributeTests
	{
		[SetUp]
		public void BuildAndObfuscateAssemblies ()
		{
			string xml = String.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Module file='$(InPath)\AssemblyWithCustomAttr.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithCustomAttr", String.Empty, xml);
		}

		[Test]
		public void CheckClassHasAttribute ()
		{
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine(TestHelper.OutputPath, "AssemblyWithCustomAttr.dll"));

			Assert.AreEqual (3, assmDef.MainModule.Types.Count, "Should contain only one type, and <Module>.");

			bool found = false;
			foreach (TypeDefinition typeDef in assmDef.MainModule.Types) {
				if (typeDef.Name == "<Module>" || typeDef.BaseType.Name == "Attribute")
					continue;
				else
					found = true;

				Assert.AreEqual (1, typeDef.CustomAttributes.Count, "Type should have an attribute.");

				CustomAttribute attr = typeDef.CustomAttributes [0];
				Assert.AreEqual("System.Void A.a::.ctor(System.String)", attr.Constructor.ToString(),
					"Type should have ObsoleteAttribute on it.");

				Assert.AreEqual (1, attr.ConstructorArguments.Count, "ObsoleteAttribute should have one parameter.");
				Assert.AreEqual ("test", attr.ConstructorArguments [0].Value, 
					"ObsoleteAttribute param should have appropriate value.");
			}

			Assert.IsTrue (found, "Should have found non-<Module> type.");
		}

		[Test]
		public void CheckMethodHasAttribute ()
		{
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine(TestHelper.OutputPath, "AssemblyWithCustomAttr.dll"));

			bool found = false;
			foreach (TypeDefinition typeDef in assmDef.MainModule.Types) {
				if (typeDef.Name == "<Module>" || typeDef.BaseType.Name == "Attribute")
					continue;
				else
					found = true;

				Assert.AreEqual (2, typeDef.Methods.Count, "Type is expected to have a single member.");
			
				MethodDefinition methodDef = typeDef.Methods [0];

				CustomAttribute attr = methodDef.CustomAttributes [0];
				Assert.AreEqual("System.Void A.a::.ctor(System.String)", attr.Constructor.ToString(),
					"Type should have ObsoleteAttribute on it.");

				Assert.AreEqual (1, attr.ConstructorArguments.Count, "ObsoleteAttribute should have one parameter.");
				Assert.AreEqual ("test", attr.ConstructorArguments [0].Value,
					"ObsoleteAttribute param should have appropriate value.");
			}

			Assert.IsTrue (found, "Should have found non-<Module> type.");
		}
	}
}
