using Mono.Cecil;
using Obfuscar;
using System;
using System.IO;
using Xunit;

namespace ObfuscarTests
{
	public class AutoSkipTypeTests
	{
		MethodDefinition FindByName (TypeDefinition typeDef, string name)
		{
			foreach (MethodDefinition method in typeDef.Methods)
				if (method.Name == name)
					return method;

			Assert.True (false, String.Format ("Expected to find method: {0}", name));
			return null; // never here
		}

		[Fact]
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

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			var classBType = inAssmDef.MainModule.GetType ("TestClasses.InternalClass");
			var classB = map.GetClass (new TypeKey (classBType));

			Assert.True (classB.Status == ObfuscationStatus.Skipped, "Internal class is obfuscated");

			var enumType = inAssmDef.MainModule.GetType ("TestClasses.TestEnum");
			var enum1 = map.GetClass (new TypeKey (enumType));
			Assert.True (enum1.Status == ObfuscationStatus.Skipped, "Internal enum is obfuscated");

			var classAType = inAssmDef.MainModule.GetType ("TestClasses.PublicClass");
			var classA = map.GetClass (new TypeKey (classAType));
			var classAmethod1 = FindByName (classAType, "PrivateMethod");
			var classAmethod2 = FindByName (classAType, "PublicMethod");

			var classAMethod1 = map.GetMethod (new MethodKey (classAmethod1));
			var classAMethod2 = map.GetMethod (new MethodKey (classAmethod2));

			Assert.True (classA.Status == ObfuscationStatus.Renamed, "Public class is not obfuscated");
			Assert.True (classAMethod1.Status == ObfuscationStatus.Skipped, "private method is obfuscated.");
			Assert.True (classAMethod2.Status == ObfuscationStatus.Renamed, "pubilc method is not obfuscated.");
		}

		[Fact]
		public void CheckHidePrivateApiTrue ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			string assmName = "AssemblyWithTypes.dll";
			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			var classBType = inAssmDef.MainModule.GetType ("TestClasses.InternalClass");
			var classB = map.GetClass (new TypeKey (classBType));

			Assert.True (classB.Status == ObfuscationStatus.Renamed, "Internal class should have been obfuscated");

			var enumType = inAssmDef.MainModule.GetType ("TestClasses.TestEnum");
			var enum1 = map.GetClass (new TypeKey (enumType));
			Assert.True (enum1.Status == ObfuscationStatus.Renamed, "Internal enum should have been obfuscated");
		}

		[Fact]
		public void CheckKeepPublicApiFalse ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.PublicClass");
			MethodDefinition classAmethod1 = FindByName (classAType, "PrivateMethod");
			MethodDefinition classAmethod2 = FindByName (classAType, "PublicMethod");

			ObfuscatedThing classAMethod1 = map.GetMethod (new MethodKey (classAmethod1));
			ObfuscatedThing classAMethod2 = map.GetMethod (new MethodKey (classAmethod2));

			var classA = map.GetClass (new TypeKey (classAType));
			Assert.True (classA.Status == ObfuscationStatus.Renamed, "Public class should have been obfuscated");
			Assert.True (classAMethod1.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
			Assert.True (classAMethod2.Status == ObfuscationStatus.Renamed, "pubilc method is not obfuscated.");

			var protectedMethod = FindByName (classAType, "ProtectedMethod");
			var protectedAfter = map.GetMethod (new MethodKey (protectedMethod));
			Assert.True (protectedAfter.Status == ObfuscationStatus.Renamed, "protected method is not obfuscated.");
		}

		[Fact]
		public void CheckKeepPublicApiTrue ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='KeepPublicApi' value='true' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.PublicClass");
			MethodDefinition classAmethod1 = FindByName (classAType, "PrivateMethod");
			MethodDefinition classAmethod2 = FindByName (classAType, "PublicMethod");

			ObfuscatedThing classAMethod1 = map.GetMethod (new MethodKey (classAmethod1));
			ObfuscatedThing classAMethod2 = map.GetMethod (new MethodKey (classAmethod2));
			var classA = map.GetClass (new TypeKey (classAType));
			Assert.True (classA.Status == ObfuscationStatus.Skipped, "Public class shouldn't have been obfuscated");
			Assert.True (classAMethod1.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
			Assert.True (classAMethod2.Status == ObfuscationStatus.Skipped, "pubilc method is obfuscated.");

			var protectedMethod = FindByName (classAType, "ProtectedMethod");
			var protectedAfter = map.GetMethod (new MethodKey (protectedMethod));
			Assert.True (protectedAfter.Status == ObfuscationStatus.Skipped, "protected method is obfuscated.");
		}

		[Fact]
		public void CheckSkipNamespace ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"<Obfuscator>" +
				             @"<Var name='InPath' value='{0}' />" +
				             @"<Var name='OutPath' value='{1}' />" +
				             @"<Var name='KeepPublicApi' value='false' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				             @"<SkipNamespace name='TestClasses1' />" +
				             @"</Module>" +
				             @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses1.PublicClass");
			var classA = map.GetClass (new TypeKey (classAType));
			Assert.True (classA.Status == ObfuscationStatus.Skipped, "Public class shouldn't have been obfuscated");
		}

		[Fact]
		public void CheckSkipEnum ()
		{
			string xml = String.Format (
				                      @"<?xml version='1.0'?>" +
				                      @"<Obfuscator>" +
				                      @"<Var name='InPath' value='{0}' />" +
				                      @"<Var name='OutPath' value='{1}' />" +
				                      @"<Var name='KeepPublicApi' value='false' />" +
				                      @"<Var name='HidePrivateApi' value='true' />" +
				                      @"<Module file='$(InPath)\AssemblyWithTypes.dll'>" +
				                      @"<SkipType name='TestClasses.TestEnum' />" +
				                      @"</Module>" +
				                      @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypes", string.Empty, xml);
			var map = obfuscator.Mapping;

			string assmName = "AssemblyWithTypes.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                                        Path.Combine (TestHelper.InputPath, assmName));

			var enumType = inAssmDef.MainModule.GetType ("TestClasses.TestEnum");
			var enum1 = map.GetClass (new TypeKey (enumType));
			Assert.True (enum1.Status == ObfuscationStatus.Skipped, "Internal enum is obfuscated");
		}
	}
}
