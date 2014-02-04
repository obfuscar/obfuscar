using Mono.Cecil;
using NUnit.Framework;
using Obfuscar;
using System;
using System.CodeDom.Compiler;
using System.IO;

namespace ObfuscarTests
{
	[TestFixture]
	public class ObfuscationAttributeTests
	{
		[SetUp]
		public void BuildTestAssemblies ()
		{
			TestHelper.CleanInput ();

			Microsoft.CSharp.CSharpCodeProvider provider = new Microsoft.CSharp.CSharpCodeProvider ();

			CompilerParameters cp = new CompilerParameters ();
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = false;
			cp.TreatWarningsAsErrors = true;
			;

			string assemblyAPath = Path.Combine (TestHelper.InputPath, "AssemblyA.dll");
			cp.OutputAssembly = assemblyAPath;
			CompilerResults cr = provider.CompileAssemblyFromFile (cp, Path.Combine (TestHelper.InputPath, "AssemblyA.cs"));
			if (cr.Errors.Count > 0)
				Assert.Fail ("Unable to compile test assembly:  AssemblyA");

			cp.ReferencedAssemblies.Add (assemblyAPath);
			cp.OutputAssembly = Path.Combine (TestHelper.InputPath, "AssemblyB.dll");
			cr = provider.CompileAssemblyFromFile (cp, Path.Combine (TestHelper.InputPath, "AssemblyB.cs"));
			if (cr.Errors.Count > 0)
				Assert.Fail ("Unable to compile test assembly:  AssemblyB");
		}

		static MethodDefinition FindByName (TypeDefinition typeDef, string name)
		{
			foreach (MethodDefinition method in typeDef.Methods)
				if (method.Name == name)
					return method;

			Assert.Fail (String.Format ("Expected to find method: {0}", name));
			return null; // never here
		}

		[Test]
		public void CheckExclusion ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"				<Obfuscator>" +
				             @"				<Var name='InPath' value='{0}' />" +
				             @"				<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"				<Module file='$(InPath)\AssemblyWithTypesAttrs.dll'>" +
				             @"				</Module>" +
				             @"				</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var obfuscator = TestHelper.BuildAndObfuscate ("AssemblyWithTypesAttrs", string.Empty, xml);
			var map = obfuscator.Mapping;

			const string assmName = "AssemblyWithTypesAttrs.dll";

			AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly (
				                               Path.Combine (TestHelper.InputPath, assmName));

			TypeDefinition classAType = inAssmDef.MainModule.GetType ("TestClasses.InternalClass");
			ObfuscatedThing classA = map.GetClass (new TypeKey (classAType));
			var classAmethod1 = FindByName (classAType, "PublicMethod");
			var method = map.GetMethod (new MethodKey (classAmethod1));

			TypeDefinition nestedClassAType = classAType.NestedTypes [0];
			ObfuscatedThing nestedClassA = map.GetClass (new TypeKey (nestedClassAType));
			TypeDefinition nestedClassAType2 = nestedClassAType.NestedTypes [0];
			ObfuscatedThing nestedClassA2 = map.GetClass (new TypeKey (nestedClassAType2));     

			Assert.IsTrue (classA.Status == ObfuscationStatus.Skipped, "InternalClass shouldn't have been obfuscated.");
			Assert.IsTrue (method.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated");
			Assert.IsTrue (nestedClassA.Status == ObfuscationStatus.Renamed, "Nested class should have been obfuscated");
			Assert.IsTrue (nestedClassA2.Status == ObfuscationStatus.Renamed, "Nested class should have been obfuscated");

			TypeDefinition classBType = inAssmDef.MainModule.GetType ("TestClasses.PublicClass");
			ObfuscatedThing classB = map.GetClass (new TypeKey (classBType));
			var classBmethod1 = FindByName (classBType, "PublicMethod");
			var method2 = map.GetMethod (new MethodKey (classBmethod1));

			Assert.IsTrue (classB.Status == ObfuscationStatus.Renamed, "PublicClass should have been obfuscated.");
			Assert.IsTrue (method2.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");

			TypeDefinition classCType = inAssmDef.MainModule.GetType ("TestClasses.InternalClass2");
			ObfuscatedThing classC = map.GetClass (new TypeKey (classCType));
			var classCmethod1 = FindByName (classCType, "PublicMethod");
			var method1 = map.GetMethod (new MethodKey (classCmethod1));

			TypeDefinition nestedClassBType = classCType.NestedTypes [0];
			ObfuscatedThing nestedClassB = map.GetClass (new TypeKey (nestedClassBType));

			TypeDefinition nestedClassBType2 = nestedClassBType.NestedTypes [0];
			ObfuscatedThing nestedClassB2 = map.GetClass (new TypeKey (nestedClassBType2));            

			Assert.IsTrue (classC.Status == ObfuscationStatus.Renamed, "InternalClass2 should have been obfuscated.");
			Assert.IsTrue (method1.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated.");
			Assert.IsTrue (nestedClassB.Status == ObfuscationStatus.Renamed, "Nested class should have been obfuscated");
			Assert.IsTrue (nestedClassB2.Status == ObfuscationStatus.Renamed, "Nested class should have been obfuscated");

			TypeDefinition classDType = inAssmDef.MainModule.GetType ("TestClasses.PublicClass2");
			ObfuscatedThing classD = map.GetClass (new TypeKey (classDType));
			var classDmethod1 = FindByName (classDType, "PublicMethod");
			var method3 = map.GetMethod (new MethodKey (classDmethod1));

			Assert.IsTrue (classD.Status == ObfuscationStatus.Skipped, "PublicClass2 shouldn't have been obfuscated.");
			Assert.IsTrue (method3.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");
		}

		[Test]
		public void CheckException ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"								<Obfuscator>" +
				             @"								<Var name='InPath' value='{0}' />" +
				             @"								<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"								<Module file='$(InPath)\AssemblyWithTypesAttrs2.dll'>" +
				             @"								</Module>" +
				             @"								</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);

			var exception = Assert.Throws<ObfuscarException> (() => TestHelper.BuildAndObfuscate ("AssemblyWithTypesAttrs2", string.Empty, xml));
			Assert.IsTrue (exception.Message.StartsWith ("Inconsistent virtual method obfuscation"));
		}

		[Test]
		public void CheckCrossAssembly ()
		{
			string xml = String.Format (
				             @"<?xml version='1.0'?>" +
				             @"								<Obfuscator>" +
				             @"								<Var name='InPath' value='{0}' />" +
				             @"								<Var name='OutPath' value='{1}' />" +
				             @"<Var name='HidePrivateApi' value='true' />" +
				             @"								<Module file='$(InPath)\AssemblyF.dll'>" +
				             @"								</Module>" +
				             @"								<Module file='$(InPath)\AssemblyG.dll' />" +
				             @"								</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath);
			Directory.Delete (TestHelper.OutputPath, true);
			File.Copy (Path.Combine (TestHelper.InputPath, @"..\AssemblyG.dll"), Path.Combine (TestHelper.InputPath, "AssemblyG.dll"), true);
			File.Copy (Path.Combine (TestHelper.InputPath, @"..\AssemblyF.dll"), Path.Combine (TestHelper.InputPath, "AssemblyF.dll"), true);

			var exception = Assert.Throws<ObfuscarException> (() => TestHelper.Obfuscate (xml));
			Assert.IsTrue (exception.Message.StartsWith ("Inconsistent virtual method obfuscation"));

			Assert.IsFalse (File.Exists (Path.Combine (TestHelper.OutputPath, @"AssemblyG.dll")));
			Assert.IsFalse (File.Exists (Path.Combine (TestHelper.OutputPath, @"AssemblyF.dll")));
		}
	}
}
