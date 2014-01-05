using Mono.Cecil;
using NUnit.Framework;
using Obfuscar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObfuscarTests
{
	[TestFixture]
	public class AutoSkipTypeTests
	{
		MethodDefinition FindByName (TypeDefinition typeDef, string name)
		{
			foreach (MethodDefinition method in typeDef.Methods)
				if (method.Name == name)
					return method;

			Assert.Fail (String.Format ("Expected to find method: {0}", name));
			return null; // never here
		}

		[Test]
		public void CheckHidePrivateApiFalse ()
		{
			string xml = String.Format (
				                      @"<?xml version='1.0'?>" +
				                      @"<Obfuscator>" +
				                      @"<Var name='InPath' value='{0}' />" +
				                      @"<Var name='OutPath' value='{1}' />" +
				                      @"<Var name='HidePrivateApi' value='false' />" +
				                      @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				                      @"</Module>" +
				                      @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("TestClasses.ClassA");

			AssemblyHelper.CheckAssembly ("AssemblyWithTypes", 2,
				delegate {
					return true;
				},
				delegate(TypeDefinition typeDef) {
					if (typesToFind.Contains (typeDef.ToString ())) {
						typesToFind.Remove (typeDef.ToString ());
					}
				});
			Assert.IsTrue (typesToFind.Count == 1, "could not find ClassA, which should not have been obfuscated.");

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                                        Path.Combine (TestHelper.InputPath, assmName));

			AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly (
				                                         Path.Combine (TestHelper.OutputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.ClassB");
			MethodDefinition classAmethod1 = FindByName (classAType, "TestA");
			MethodDefinition classAmethod2 = FindByName (classAType, "TestB");

			ObfuscatedThing classAMethod1 = map.GetMethod (new MethodKey (classAmethod1));
			ObfuscatedThing classAMethod2 = map.GetMethod (new MethodKey (classAmethod2));

			Assert.IsTrue (classAMethod1.Status == ObfuscationStatus.Skipped, "private method is obfuscated.");
			Assert.IsTrue (classAMethod2.Status == ObfuscationStatus.Renamed, "pubilc method is not obfuscated.");
		}

		[Test]
		public void CheckHidePrivateApiTrue ()
		{
			string xml = String.Format (
				                      @"<?xml version='1.0'?>" +
				                      @"<Obfuscator>" +
				                      @"<Var name='InPath' value='{0}' />" +
				                      @"<Var name='OutPath' value='{1}' />" +
				                      @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				                      @"</Module>" +
				                      @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("TestClasses.ClassA");

			AssemblyHelper.CheckAssembly ("AssemblyWithTypes", 2,
				delegate {
					return true;
				},
				delegate(TypeDefinition typeDef) {
					if (typesToFind.Contains (typeDef.ToString ())) {
						typesToFind.Remove (typeDef.ToString ());
					}
				});
			Assert.IsTrue (typesToFind.Count == 1, "could find ClassA, which should have been obfuscated.");
		}

		[Test]
		public void CheckKeepPublicApiFalse ()
		{
			string xml = String.Format (
				                      @"<?xml version='1.0'?>" +
				                      @"<Obfuscator>" +
				                      @"<Var name='InPath' value='{0}' />" +
				                      @"<Var name='OutPath' value='{1}' />" +
				                      @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				                      @"</Module>" +
				                      @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("TestClasses.ClassB");

			AssemblyHelper.CheckAssembly ("AssemblyWithTypes", 2,
				delegate {
					return true;
				},
				delegate(TypeDefinition typeDef) {
					if (typesToFind.Contains (typeDef.ToString ())) {
						typesToFind.Remove (typeDef.ToString ());
					}
				});
			Assert.IsTrue (typesToFind.Count == 1, "could find ClassB, which should have been obfuscated.");

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                                        Path.Combine (TestHelper.InputPath, assmName));

			AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly (
				                                         Path.Combine (TestHelper.OutputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.ClassB");
			MethodDefinition classAmethod1 = FindByName (classAType, "TestA");
			MethodDefinition classAmethod2 = FindByName (classAType, "TestB");

			ObfuscatedThing classAMethod1 = map.GetMethod (new MethodKey (classAmethod1));
			ObfuscatedThing classAMethod2 = map.GetMethod (new MethodKey (classAmethod2));

			Assert.IsTrue (classAMethod1.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
			Assert.IsTrue (classAMethod2.Status == ObfuscationStatus.Renamed, "pubilc method is not obfuscated.");
		}

		[Test]
		public void CheckKeepPublicApiTrue ()
		{
			string xml = String.Format (
				                      @"<?xml version='1.0'?>" +
				                      @"<Obfuscator>" +
				                      @"<Var name='InPath' value='{0}' />" +
				                      @"<Var name='OutPath' value='{1}' />" +
				                      @"<Var name='KeepPublicApi' value='true' />" +
				                      @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				                      @"</Module>" +
				                      @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			HashSet<string> typesToFind = new HashSet<string> ();
			typesToFind.Add ("TestClasses.ClassB");

			AssemblyHelper.CheckAssembly ("AssemblyWithTypes", 2,
				delegate {
					return true;
				},
				delegate(TypeDefinition typeDef) {
					if (typesToFind.Contains (typeDef.ToString ())) {
						typesToFind.Remove (typeDef.ToString ());
					}
				});
			Assert.IsTrue (typesToFind.Count == 0, "could not find ClassB, which should not have been obfuscated.");

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                                        Path.Combine (TestHelper.InputPath, assmName));

			AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly (
				                                         Path.Combine (TestHelper.OutputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.ClassB");
			MethodDefinition classAmethod1 = FindByName (classAType, "TestA");
			MethodDefinition classAmethod2 = FindByName (classAType, "TestB");

			ObfuscatedThing classAMethod1 = map.GetMethod (new MethodKey (classAmethod1));
			ObfuscatedThing classAMethod2 = map.GetMethod (new MethodKey (classAmethod2));

			Assert.IsTrue (classAMethod1.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
			Assert.IsTrue (classAMethod2.Status == ObfuscationStatus.Skipped, "pubilc method is obfuscated.");
		}
	}
}
