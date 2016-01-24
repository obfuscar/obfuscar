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
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
	public class PortableTests
	{
		[Fact]
		public void CheckPortable()
		{
			string xml = string.Format(
				@"<?xml version='1.0'?>" +
				@"<Obfuscator>" +
				@"<Var name='InPath' value='{0}' />" +
				@"<Var name='OutPath' value='{1}' />" +
				@"<Var name='HideStrings' value='false' />" +
				@"<Var name='KeyFile' value='$(InPath)\..\dockpanelsuite.snk' />" +
				@"<Module file='$(InPath)\SharpSnmpLib.Portable.dll' />" +
				@"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			TestHelper.CleanInput();

			// build it with the keyfile option (embeds the public key, and signs the assembly)
			File.Copy(Path.Combine(TestHelper.InputPath, @"..\SharpSnmpLib.Portable.dll"), Path.Combine(TestHelper.InputPath, "SharpSnmpLib.Portable.dll"), true);

			var map = TestHelper.Obfuscate(xml).Mapping;

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
				Path.Combine(TestHelper.InputPath, "SharpSnmpLib.Portable.dll"));

			AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
				Path.Combine(TestHelper.OutputPath, "SharpSnmpLib.Portable.dll"));

			var corlibs = outAssmDef.MainModule.AssemblyReferences.Where(reference => reference.Name == "mscorlib");
			Assert.Equal(1, corlibs.Count());
			Assert.Equal("2.0.5.0", corlibs.First().Version.ToString());
		}
	}
}
