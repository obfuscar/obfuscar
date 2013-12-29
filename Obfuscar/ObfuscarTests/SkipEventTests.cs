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
	public class SkipEventTests
	{
		protected void CheckEvents (string name, int expectedTypes, string[] expected, string[] notExpected)
		{
			HashSet<string> eventsToFind = new HashSet<string> (expected);
			HashSet<string> eventsNotToFind = new HashSet<string> (notExpected);

			HashSet<string> methodsToFind = new HashSet<string> ();
			for (int i = 0; i < expected.Length; i++) {
				methodsToFind.Add ("add_" + expected [i]);
				methodsToFind.Add ("remove_" + expected [i]);
			}

			HashSet<string> methodsNotToFind = new HashSet<string> ();
			for (int i = 0; i < notExpected.Length; i++) {
				methodsNotToFind.Add ("add_" + notExpected [i]);
				methodsNotToFind.Add ("remove_" + notExpected [i]);
			}

			bool foundDelType = false;

			AssemblyHelper.CheckAssembly (name, expectedTypes,
				delegate( TypeDefinition typeDef) {
					if (typeDef.BaseType.FullName == "System.MulticastDelegate") {
						foundDelType = true;
						return false;
					} else
						return true;
				},
				delegate( TypeDefinition typeDef) {
					// make sure we have enough methods...
					// 2 methods / event + a method to fire them
					Assert.AreEqual (methodsToFind.Count + methodsNotToFind.Count + 2, typeDef.Methods.Count,
						"Some of the methods for the type are missing.");

					foreach (MethodDefinition method in typeDef.Methods) {
						Assert.IsFalse (methodsNotToFind.Contains (method.Name), String.Format (
							"Did not expect to find method '{0}'.", method.Name));

						methodsToFind.Remove (method.Name);
					}

					Assert.AreEqual (expected.Length, typeDef.Events.Count,
						expected.Length == 1 ? "Type should have 1 event (others dropped by default)." :
						String.Format ("Type should have {0} events (others dropped by default).", expected.Length));

					foreach (EventDefinition evt in typeDef.Events) {
						Assert.IsFalse (eventsNotToFind.Contains (evt.Name), String.Format (
							"Did not expect to find event '{0}'.", evt.Name));

						eventsToFind.Remove (evt.Name);
					}

					Assert.IsFalse (methodsToFind.Count > 0, "Failed to find all expected methods.");
					Assert.IsFalse (eventsToFind.Count > 0, "Failed to find all expected events.");
				});

			Assert.IsTrue (foundDelType, "Should have found the delegate type.");
		}

		[Test]
		public void CheckDropsEvents ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithEvents.dll' />" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithEvents", String.Empty, xml);

			string[] expected = new string[0];

			string[] notExpected = new string[] {
				"Event1",
				"Event2",
				"EventA"
			};

			CheckEvents ("AssemblyWithEvents", 1, expected, notExpected);
		}
		// TODO: [Test]
		public void CheckSkipEventsByName ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Module file='$(InPath)\AssemblyWithEvents.dll'>" +
				             @"<SkipEvent type='TestClasses.ClassA' name='Event2' attrib='public' />" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithEvents", String.Empty, xml);

			string[] expected = new string[] {
				"Event2"
			};

			string[] notExpected = new string[] {
				"Event1",
				"EventA"
			};

			CheckEvents ("AssemblyWithEvents", 1, expected, notExpected);
		}
		// TODO: [Test]
		public void CheckSkipEventsByRx ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Module file='$(InPath)\AssemblyWithEvents.dll'>" +
				             @"<SkipEvent type='TestClasses.ClassA' rx='Event\d' />" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithEvents", String.Empty, xml);

			string[] expected = new string[] {
				"Event1",
				"Event2"
			};

			string[] notExpected = new string[] {
				"EventA"
			};

			CheckEvents ("AssemblyWithEvents", 1, expected, notExpected);
		}
	}
}
