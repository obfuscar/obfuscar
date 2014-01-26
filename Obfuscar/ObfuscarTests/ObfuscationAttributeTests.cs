using Mono.Cecil;
using NUnit.Framework;
using Obfuscar;
using System;
using System.IO;

namespace ObfuscarTests
{
	[TestFixture]
	public class ObfuscationAttributeTests
	{
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

			Assert.IsTrue (classA.Status == ObfuscationStatus.Skipped, "InternalClass shouldn't have been obfuscated.");

			TypeDefinition classBType = inAssmDef.MainModule.GetType ("TestClasses.PublicClass");
			ObfuscatedThing classB = map.GetClass (new TypeKey (classBType));
            var classBmethod1 = FindByName(classBType, "PublicMethod");
            var method2 = map.GetMethod(new MethodKey(classBmethod1));

			Assert.IsTrue (classB.Status == ObfuscationStatus.Renamed, "PublicClass should have been obfuscated.");
            Assert.IsTrue(method2.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");

            TypeDefinition classCType = inAssmDef.MainModule.GetType("TestClasses.InternalClass2");
            ObfuscatedThing classC = map.GetClass(new TypeKey(classCType));
            var classCmethod1 = FindByName(classCType, "PublicMethod");
            var method1 = map.GetMethod(new MethodKey(classCmethod1));

            Assert.IsTrue(classC.Status == ObfuscationStatus.Renamed, "InternalClass2 should have been obfuscated.");
            Assert.IsTrue(method1.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated.");

            TypeDefinition classDType = inAssmDef.MainModule.GetType("TestClasses.PublicClass2");
            ObfuscatedThing classD = map.GetClass(new TypeKey(classDType));
            var classDmethod1 = FindByName(classDType, "PublicMethod");
            var method3 = map.GetMethod(new MethodKey(classDmethod1));

            Assert.IsTrue(classD.Status == ObfuscationStatus.Skipped, "PublicClass2 shouldn't have been obfuscated.");
            Assert.IsTrue(method3.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");
		}
	}
}
