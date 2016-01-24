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
using Obfuscar;
using System;
using System.IO;
using Xunit;

namespace ObfuscarTests
{
	public class UnityTests
	{
		static MethodDefinition FindByFullName (TypeDefinition typeDef, string name)
		{
			foreach (MethodDefinition method in typeDef.Methods)
				if (method.FullName == name)
					return method;

			Assert.True (false, String.Format ("Expected to find method: {0}", name));
			return null; // never here
		}

		[Fact]
		public void CheckGeneric ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='KeyFile' value='$(InPath)\..\dockpanelsuite.snk' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\Microsoft.Practices.Unity.dll' />" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.CleanInput ();

			// build it with the keyfile option (embeds the public key, and signs the assembly)
			File.Copy (Path.Combine (TestHelper.InputPath, @"..\Microsoft.Practices.Unity.dll"), Path.Combine (TestHelper.InputPath, "Microsoft.Practices.Unity.dll"), true);
			File.Copy (Path.Combine (TestHelper.InputPath, @"..\Microsoft.Practices.ServiceLocation.dll"), Path.Combine (TestHelper.InputPath, "Microsoft.Practices.ServiceLocation.dll"), true);

			var map = TestHelper.Obfuscate (xml).Mapping;

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, "Microsoft.Practices.Unity.dll"));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("Microsoft.Practices.ObjectBuilder2.PolicyListExtensions");
			var type = map.GetClass (new TypeKey (classAType));
			Assert.True (type.Status == ObfuscationStatus.Renamed, "Type should have been renamed.");

			var method1 = FindByFullName (classAType, "System.Void Microsoft.Practices.ObjectBuilder2.PolicyListExtensions::Clear(Microsoft.Practices.ObjectBuilder2.IPolicyList,System.Object)");
			var m1 = map.GetMethod (new MethodKey (method1));
			Assert.True (m1.Status == ObfuscationStatus.Renamed, "Instance method should have been renamed.");

			var classB = inAssmDef.MainModule.GetType ("Microsoft.Practices.ObjectBuilder2.IPolicyList");
			var typeB = map.GetClass (new TypeKey (classB));

			AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly (
				                                Path.Combine (TestHelper.OutputPath, "Microsoft.Practices.Unity.dll"));

			var name = type.StatusText.Substring (27);
			var obfuscated = outAssmDef.MainModule.GetType (name);
			var method2 = FindByFullName (obfuscated, "System.Void " + name + "::" + m1.StatusText + "(" + typeB.StatusText.Substring (27) + ",System.Object)");
			Assert.NotNull (method2);
			var first = method2.Parameters[0].Name;
			var second = method2.Parameters[1].Name;
			Assert.Equal("", first);
			Assert.Equal("", second);
		}
	}
}
