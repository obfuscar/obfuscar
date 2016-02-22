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
using System.IO;

using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
	public class HideStringsTests
	{
		[Fact]
		public void CheckHideStringsClassDoesNotExist ()
		{
			string xml = string.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='false' />" +
				@"<Module file='$(InPath)\AssemblyWithStrings.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithStrings", string.Empty, xml, true);
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine (TestHelper.OutputPath, "AssemblyWithStrings.dll"));

			Assert.Equal (3, assmDef.MainModule.Types.Count);

			TypeDefinition expected = null;
			foreach (var type in assmDef.MainModule.Types) {
				if (type.FullName.Contains ("PrivateImplementation")) {
					expected = type;
				}
			}

			Assert.Null (expected);
		}

		[Fact]
		public void CheckHideStringsClassExists ()
		{
			string xml = string.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='true' />" +
				@"<Module file='$(InPath)\AssemblyWithStrings.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithStrings", string.Empty, xml, true);
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine (TestHelper.OutputPath, "AssemblyWithStrings.dll"));

			Assert.Equal (4, assmDef.MainModule.Types.Count);

			TypeDefinition expected = null;
			foreach (var type in assmDef.MainModule.Types) {
				if (type.FullName.Contains ("PrivateImplementation")) {
					expected = type;
				}
			}

			Assert.NotNull (expected);

			Assert.Equal (3, expected.Fields.Count);

			Assert.Equal (6, expected.Methods.Count);
		}

		[Fact]
		public void CheckHideStringsClassSkip ()
		{
			string xml = string.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='true' />" +
				@"<Module file='$(InPath)\AssemblyWithStrings.dll'>" +
				@"  <SkipStringHiding type='TestClasses.PublicClass1' name='*' />" +
				@"</Module>" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithStrings", string.Empty, xml, true);
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine (TestHelper.OutputPath, "AssemblyWithStrings.dll"));

			Assert.Equal (4, assmDef.MainModule.Types.Count);

			TypeDefinition expected = null;
			foreach (var type in assmDef.MainModule.Types) {
				if (type.FullName.Contains ("PrivateImplementation")) {
					expected = type;
				}
			}

			Assert.NotNull (expected);

			Assert.Equal (3, expected.Fields.Count);

			Assert.Equal (4, expected.Methods.Count);
		}

		[Fact]
		public void CheckHideStringsClassForce ()
		{
			string xml = string.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='false' />" +
				@"<Module file='$(InPath)\AssemblyWithStrings.dll'>" +
				@"  <ForceStringHiding type='TestClasses.PublicClass1' name='*' />" +
				@"</Module>" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithStrings", string.Empty, xml, true);
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine (TestHelper.OutputPath, "AssemblyWithStrings.dll"));

			Assert.Equal (4, assmDef.MainModule.Types.Count);

			TypeDefinition expected = null;
			foreach (var type in assmDef.MainModule.Types) {
				if (type.FullName.Contains ("PrivateImplementation")) {
					expected = type;
				}
			}

			Assert.NotNull (expected);

			Assert.Equal (3, expected.Fields.Count);

			Assert.Equal (4, expected.Methods.Count);
		}

		[Fact]
		public void CheckHideStringsClassForce2 ()
		{
			string xml = string.Format (
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='false' />" +
				@"<Module file='$(InPath)\AssemblyWithStrings.dll'>" +
				@"  <ForceType name='TestClasses.PublicClass1' forceStringHiding='true' />" +
				@"</Module>" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.BuildAndObfuscate ("AssemblyWithStrings", string.Empty, xml, true);
			AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly (
				Path.Combine (TestHelper.OutputPath, "AssemblyWithStrings.dll"));

			Assert.Equal (4, assmDef.MainModule.Types.Count);

			TypeDefinition expected = null;
			foreach (var type in assmDef.MainModule.Types) {
				if (type.FullName.Contains ("PrivateImplementation")) {
					expected = type;
				}
			}

			Assert.NotNull (expected);

			Assert.Equal (3, expected.Fields.Count);

			Assert.Equal (4, expected.Methods.Count);
		}
	}
}
