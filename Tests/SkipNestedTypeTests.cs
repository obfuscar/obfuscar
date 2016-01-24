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
using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
	public class SkipNestedTypeTests
	{
		[Fact]
		public void CheckNestedTypes ()
		{
			string xml = String.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				@"<Module file='$(InPath)\AssemblyWithNestedTypes.dll'>" +
				@"<SkipType name='TestClasses.ClassA/NestedClassA' />" +
				@"</Module>" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithNestedTypes", string.Empty, xml);

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("A.A");
			typesToFind.Add ("A.A/a");
			typesToFind.Add ("A.A/a/A");
			typesToFind.Add ("A.A/NestedClassA");

			AssemblyHelper.CheckAssembly ("AssemblyWithNestedTypes", 1,
				delegate {
				return true;
			},
				delegate( TypeDefinition typeDef ) {
				Assert.True (typesToFind.Contains (typeDef.ToString ()), string.Format("Type {0} not expected.", typeDef.ToString ()));
				typesToFind.Remove (typeDef.ToString ());
			});
			Assert.True (typesToFind.Count == 0, "Not all types found.");
		}

		[Fact]
		public void CheckDefault ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Var name='KeepPublicApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithNestedTypes2.dll'>" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithNestedTypes2", string.Empty, xml);

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("TestClasses.ClassA");
			typesToFind.Add ("TestClasses.ClassA/A");
			typesToFind.Add ("TestClasses.ClassA/NestedClassB");
			typesToFind.Add ("TestClasses.ClassA/NestedClassB/NestedClassC");

			AssemblyHelper.CheckAssembly ("AssemblyWithNestedTypes2", 1,
				delegate {
					return true;
				},
				delegate( TypeDefinition typeDef) {
					Assert.True (typesToFind.Contains (typeDef.ToString ()), string.Format("Type {0} not expected.", typeDef.ToString ()));
					typesToFind.Remove (typeDef.ToString ());
				});
			Assert.True (typesToFind.Count == 0, "Not all types found.");
		}
	}
}
