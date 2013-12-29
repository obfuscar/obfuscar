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
using NUnit.Framework;
using Mono.Cecil;

namespace ObfuscarTests
{
	[TestFixture]
	public class SkipNestedTypeTests
	{
		[Test]
		public void CheckNestedTypes ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Module file='$(InPath)\AssemblyWithNestedTypes.dll'>" +
				             @"<SkipType name='TestClasses.ClassA/NestedClassA' />" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithNestedTypes", string.Empty, xml);

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("A.A");
			typesToFind.Add ("A.A/a");
			typesToFind.Add ("A.A/a/B");
			typesToFind.Add ("A.A/NestedClassA");

			AssemblyHelper.CheckAssembly ("AssemblyWithNestedTypes", 1,
				delegate {
					return true;
				},
				delegate( TypeDefinition typeDef) {
					Assert.IsTrue (typesToFind.Contains (typeDef.ToString ()), "Type {0} not expected.", typeDef.ToString ());
					typesToFind.Remove (typeDef.ToString ());
				});
			Assert.IsTrue (typesToFind.Count == 0, "Not all types found.");
		}
	}
}
