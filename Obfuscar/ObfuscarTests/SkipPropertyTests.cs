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
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using NUnit.Framework;
using Mono.Cecil;

namespace ObfuscarTests
{
	[TestFixture]
	public class SkipPropertyTests
	{
		protected void CheckProperties (string name, int expectedTypes, string[] expected, string[] notExpected)
		{
			HashSet<string> propsToFind = new HashSet<string> (expected);
			HashSet<string> propsNotToFind = new HashSet<string> (notExpected);

			string[] expectedMethods = new string[expected.Length * 2];
			for (int i = 0; i < expected.Length; i++) {
				expectedMethods [i * 2 + 0] = "get_" + expected [i];
				expectedMethods [i * 2 + 1] = "set_" + expected [i];
			}

			string[] notExpectedMethods = new string[notExpected.Length * 2];
			for (int i = 0; i < notExpected.Length; i++) {
				notExpectedMethods [i * 2 + 0] = "get_" + notExpected [i];
				notExpectedMethods [i * 2 + 1] = "set_" + notExpected [i];
			}

			AssemblyHelper.CheckAssembly (name, expectedTypes, expectedMethods, notExpectedMethods,
				delegate( TypeDefinition typeDef) {
					return true;
				},
				delegate( TypeDefinition typeDef) {
					Assert.AreEqual (expected.Length, typeDef.Properties.Count,
						expected.Length == 1 ? "Type should have 1 property (others dropped by default)." :
						String.Format ("Type should have {0} properties (others dropped by default).", expected.Length));

					foreach (PropertyDefinition prop in typeDef.Properties) {
						Assert.IsFalse (propsNotToFind.Contains (prop.Name), String.Format (
							"Did not expect to find property '{0}'.", prop.Name));

						propsToFind.Remove (prop.Name);
					}

					Assert.IsFalse (propsToFind.Count > 0, "Failed to find all expected properties.");
				});
		}

		[Test]
		public void CheckDropsProperties ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithProperties.dll' />" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithProperties", String.Empty, xml);

			string[] expected = new string[0];

			string[] notExpected = new string[] {
				"Property1",
				"Property2",
				"PropertyA"
			};

			CheckProperties ("AssemblyWithProperties", 1, expected, notExpected);
		}
		// TODO: [Test]
		public void CheckSkipPropertyByName ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Module file='$(InPath)\AssemblyWithProperties.dll'>" +
				             @"<SkipProperty type='TestClasses.ClassA' name='Property2' />" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithProperties", String.Empty, xml);

			string[] expected = new string[] {
				"Property2"
			};

			string[] notExpected = new string[] {
				"Property1",
				"PropertyA"
			};

			CheckProperties ("AssemblyWithProperties", 1, expected, notExpected);
		}
		// TODO: [Test]
		public void CheckSkipPropertyByRx ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Module file='$(InPath)\AssemblyWithProperties.dll'>" +
				             @"<SkipProperty type='TestClasses.ClassA' rx='Property\d' />" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithProperties", String.Empty, xml);

			string[] expected = new string[] {
				"Property1",
				"Property2"
			};

			string[] notExpected = new string[] {
				"PropertyA"
			};

			CheckProperties ("AssemblyWithProperties", 1, expected, notExpected);
		}
	}
}
