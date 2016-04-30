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
using Obfuscar;
using Xunit;
using System.Reflection;

namespace ObfuscarTests
{
	public class FunctionOverridingTests
	{
		Obfuscar.ObfuscationMap BuildAndObfuscateAssemblies ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='KeepPublicApi' value='false' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithOverrides.dll' />" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithOverrides", String.Empty, xml);

			return obfuscator.Mapping;
		}

		MethodDefinition FindByName (TypeDefinition typeDef, string name)
		{
			foreach (MethodDefinition method in typeDef.Methods)
				if (method.Name == name)
					return method;

			Assert.True (false, String.Format ("Expected to find method: {0}", name));
			return null; // never here
		}

		[Fact]
		public void CheckClassHasAttribute ()
		{
			Obfuscar.ObfuscationMap map = BuildAndObfuscateAssemblies ();

			string assmName = "AssemblyWithOverrides.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly (
				                                Path.Combine (TestHelper.OutputPath, assmName));
			{
				TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.ClassA");
				MethodDefinition classAmethod2 = FindByName (classAType, "Method2");
				MethodDefinition classAcompare = FindByName (classAType, "CompareTo");

				TypeDefinition classBType = inAssmDef.MainModule.GetType ("TestClasses.ClassB");
				MethodDefinition classBmethod2 = FindByName (classBType, "Method2");
				MethodDefinition classBcompare = FindByName (classBType, "CompareTo");

				TypeDefinition classCType = inAssmDef.MainModule.GetType ("TestClasses.ClassC");
				MethodDefinition classCmethod1 = FindByName (classCType, "Method1");

				TypeDefinition classDType = inAssmDef.MainModule.GetType ("TestClasses.ClassD");
				MethodDefinition classDmethod1 = FindByName (classDType, "Method1");

				Obfuscar.ObfuscatedThing classAEntry = map.GetMethod (new Obfuscar.MethodKey (classAmethod2));
				ObfuscatedThing classACompareEntry = map.GetMethod (new MethodKey (classAcompare));
				Obfuscar.ObfuscatedThing classBEntry = map.GetMethod (new Obfuscar.MethodKey (classBmethod2));
				Obfuscar.ObfuscatedThing classBCompareEntry = map.GetMethod (new Obfuscar.MethodKey (classBcompare));
				ObfuscatedThing classCEntry = map.GetMethod (new MethodKey (classCmethod1));
				ObfuscatedThing classDEntry = map.GetMethod (new MethodKey (classDmethod1));

				var classFType = inAssmDef.MainModule.GetType ("TestClasses.ClassF");
				var classFmethod = FindByName (classFType, "Test");

				var classGType = inAssmDef.MainModule.GetType ("TestClasses.ClassG");
				var classGmethod = FindByName (classGType, "Test");

				var classFEntry = map.GetMethod (new MethodKey (classFmethod));
				var classGEntry = map.GetMethod (new MethodKey (classGmethod));

				Assert.True (
					classAEntry.Status == Obfuscar.ObfuscationStatus.Renamed &&
					classBEntry.Status == Obfuscar.ObfuscationStatus.Renamed,
					"Both methods should have been renamed.");

				Assert.True (
					classAEntry.StatusText == classBEntry.StatusText,
					"Both methods should have been renamed to the same thing.");

				Assert.True (classACompareEntry.Status == ObfuscationStatus.Skipped);

				Assert.True (classBCompareEntry.Status == ObfuscationStatus.Skipped);

				Assert.True (classCEntry.Status == ObfuscationStatus.Renamed);

				Assert.True (classDEntry.Status == ObfuscationStatus.Renamed);

				Assert.True (
					classFEntry.Status == ObfuscationStatus.Renamed && classGEntry.Status == ObfuscationStatus.Renamed,
					"Both methods should have been renamed.");

				Assert.True (classFEntry.StatusText == classGEntry.StatusText,
					"Both methods should have been renamed to the same thing.");
			}

			{
				TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.CA");
				MethodDefinition classAmethod2 = FindByName (classAType, "get_PropA");

				TypeDefinition classBType = inAssmDef.MainModule.GetType ("TestClasses.CB");
				MethodDefinition classBmethod2 = FindByName (classBType, "get_PropB");

				TypeDefinition classCType = inAssmDef.MainModule.GetType ("TestClasses.IA");
				MethodDefinition classCmethod1 = FindByName (classCType, "get_PropA");

				TypeDefinition classDType = inAssmDef.MainModule.GetType ("TestClasses.IB");
				MethodDefinition classDmethod1 = FindByName (classDType, "get_PropB");

				Obfuscar.ObfuscatedThing classAEntry = map.GetMethod (new Obfuscar.MethodKey (classAmethod2));
				Obfuscar.ObfuscatedThing classBEntry = map.GetMethod (new Obfuscar.MethodKey (classBmethod2));
				ObfuscatedThing classCEntry = map.GetMethod (new MethodKey (classCmethod1));
				ObfuscatedThing classDEntry = map.GetMethod (new MethodKey (classDmethod1));

				Assert.True (
					classAEntry.Status == Obfuscar.ObfuscationStatus.Renamed &&
					classCEntry.Status == Obfuscar.ObfuscationStatus.Renamed,
					"Both methods should have been renamed.");

				Assert.True (
					classAEntry.StatusText == classCEntry.StatusText,
					"Both methods should have been renamed to the same thing.");

				Assert.True (
					classBEntry.Status == ObfuscationStatus.Renamed && classDEntry.Status == ObfuscationStatus.Renamed,
					"Both methods should have been renamed.");

				Assert.True (classBEntry.StatusText == classDEntry.StatusText,
					"Both methods should have been renamed to the same thing.");

				Assert.True (classAEntry.StatusText != classBEntry.StatusText,
					"Both methods shouldn't have been renamed to the same thing.");
			}
		}

        [Fact]
        public void CheckGenericMethodRenaming ()
        {
            string xml = String.Format(
                             @"<?xml version='1.0'?>" +
                             @"<Obfuscator>" +
                             @"<Var name='InPath' value='{0}' />" +
                             @"<Var name='OutPath' value='{1}' />" +
                             @"<Var name='KeepPublicApi' value='false' />" +
                             @"<Var name='HidePrivateApi' value='true' />" +
                             @"<Module file='$(InPath)\AssemblyWithGenericOverrides.dll' />" +
                             @"<Module file='$(InPath)\AssemblyWithGenericOverrides2.dll'>" +
                             @"<SkipNamespace name='*' />" +
                             @"</Module>" +
                             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

            Obfuscator obfuscator = TestHelper.BuildAndObfuscate(new[] { "AssemblyWithGenericOverrides", "AssemblyWithGenericOverrides2" }, xml);

            var assembly2Path = Path.Combine (Directory.GetCurrentDirectory (), TestHelper.OutputPath, "AssemblyWithGenericOverrides2.dll");
            var assembly2 = Assembly.LoadFile (assembly2Path);
            var type = assembly2.GetType ("TestClasses.Test");
            var ctor = type.GetConstructor(new Type[0]);
            var instance = ctor.Invoke (new object[0]);
            try {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
                Assert.True(instance.ToString () == "Empty<string, string>=A<B<String, String>>",
                    "Generic override should have been updated");
            }
            finally {
                AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory (), TestHelper.OutputPath, args.Name.Split (',')[0] + ".dll");
            if (File.Exists (assemblyPath)) {
                return Assembly.LoadFile (assemblyPath);
            }
            return null;
        }
    }
}
