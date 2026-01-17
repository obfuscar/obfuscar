using Mono.Cecil;
using Obfuscar;
using System.IO;
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
    public class ObfuscationAttributeTests
    {
        public ObfuscationAttributeTests()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(names: ["AssemblyA", "AssemblyB"]);
        }

        static MethodDefinition FindByName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
                if (method.Name == name)
                    return method;

            Assert.Fail(string.Format("Expected to find method: {0}", name));
            return null; // never here
        }

        [Fact]
        public void CheckExclusion()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"				<Obfuscator>" +
                @"				<Var name='InPath' value='{0}' />" +
                @"				<Var name='OutPath' value='{1}' />" +
                @"              <Var name='HidePrivateApi' value='true' />" +
                @"				<Module file='$(InPath){2}AssemblyWithTypesAttrs.dll'>" +
                @"				</Module>" +
                @"				</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypesAttrs", string.Empty, xml);
            var map = obfuscator.Mapping;

            const string assmName = "AssemblyWithTypesAttrs.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));
            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.InternalClass");
                ObfuscatedThing classA = map.GetClass(new TypeKey(classAType));
                var classAmethod1 = FindByName(classAType, "PublicMethod");
                var method = map.GetMethod(new MethodKey(classAmethod1));

                TypeDefinition nestedClassAType = classAType.NestedTypes[0];
                ObfuscatedThing nestedClassA = map.GetClass(new TypeKey(nestedClassAType));
                TypeDefinition nestedClassAType2 = nestedClassAType.NestedTypes[0];
                ObfuscatedThing nestedClassA2 = map.GetClass(new TypeKey(nestedClassAType2));

                Assert.True(classA.Status == ObfuscationStatus.Skipped,
                    "InternalClass shouldn't have been obfuscated.");
                Assert.True(method.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated");
                Assert.True(nestedClassA.Status == ObfuscationStatus.Skipped,
                    "Nested class shouldn't have been obfuscated");
                Assert.True(nestedClassA2.Status == ObfuscationStatus.Skipped,
                    "Nested class shouldn't have been obfuscated");
            }

            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.InternalClass3");
                ObfuscatedThing classA = map.GetClass(new TypeKey(classAType));
                var classAmethod1 = FindByName(classAType, "PublicMethod");
                var method = map.GetMethod(new MethodKey(classAmethod1));

                TypeDefinition nestedClassAType = classAType.NestedTypes[0];
                ObfuscatedThing nestedClassA = map.GetClass(new TypeKey(nestedClassAType));
                TypeDefinition nestedClassAType2 = nestedClassAType.NestedTypes[0];
                ObfuscatedThing nestedClassA2 = map.GetClass(new TypeKey(nestedClassAType2));

                Assert.True(classA.Status == ObfuscationStatus.Skipped,
                    "InternalClass shouldn't have been obfuscated.");
                Assert.True(method.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated");
                Assert.True(nestedClassA.Status == ObfuscationStatus.Renamed,
                    "Nested class should have been obfuscated");
                Assert.True(nestedClassA2.Status == ObfuscationStatus.Renamed,
                    "Nested class should have been obfuscated");
            }

            TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            ObfuscatedThing classB = map.GetClass(new TypeKey(classBType));
            var classBmethod1 = FindByName(classBType, "PublicMethod");
            var method2 = map.GetMethod(new MethodKey(classBmethod1));

            Assert.True(classB.Status == ObfuscationStatus.Renamed, "PublicClass should have been obfuscated.");
            Assert.True(method2.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");

            TypeDefinition classCType = inAssmDef.MainModule.GetType("TestClasses.InternalClass2");
            ObfuscatedThing classC = map.GetClass(new TypeKey(classCType));
            var classCmethod1 = FindByName(classCType, "PublicMethod");
            var method1 = map.GetMethod(new MethodKey(classCmethod1));

            TypeDefinition nestedClassBType = classCType.NestedTypes[0];
            ObfuscatedThing nestedClassB = map.GetClass(new TypeKey(nestedClassBType));

            TypeDefinition nestedClassBType2 = nestedClassBType.NestedTypes[0];
            ObfuscatedThing nestedClassB2 = map.GetClass(new TypeKey(nestedClassBType2));

            Assert.True(classC.Status == ObfuscationStatus.Renamed, "InternalClass2 should have been obfuscated.");
            Assert.True(method1.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated.");
            Assert.True(nestedClassB.Status == ObfuscationStatus.Renamed, "Nested class should have been obfuscated");
            Assert.True(nestedClassB2.Status == ObfuscationStatus.Renamed, "Nested class should have been obfuscated");

            TypeDefinition classDType = inAssmDef.MainModule.GetType("TestClasses.PublicClass2");
            ObfuscatedThing classD = map.GetClass(new TypeKey(classDType));
            var classDmethod1 = FindByName(classDType, "PublicMethod");
            var method3 = map.GetMethod(new MethodKey(classDmethod1));

            Assert.True(classD.Status == ObfuscationStatus.Skipped, "PublicClass2 shouldn't have been obfuscated.");
            Assert.True(method3.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");
        }

        [Fact]
        public void CheckException()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"								<Obfuscator>" +
                @"								<Var name='InPath' value='{0}' />" +
                @"								<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"								<Module file='$(InPath){2}AssemblyWithTypesAttrs2.dll'>" +
                @"								</Module>" +
                @"								</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            var exception = Assert.Throws<ObfuscarException>(() =>
                TestHelper.BuildAndObfuscate("AssemblyWithTypesAttrs2", string.Empty, xml));
            Assert.StartsWith("Inconsistent virtual method obfuscation", exception.Message);
        }

        [Fact]
        public void CheckCrossAssembly()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"								<Module file='$(InPath){2}AssemblyF.dll'>" +
                @"								</Module>" +
                @"								<Module file='$(InPath){2}AssemblyG.dll' />" +
                @"								</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);
            // Directory.Delete (TestHelper.OutputPath, true);
            string destFileName = Path.Combine(TestHelper.InputPath, "AssemblyG.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "AssemblyG.dll"),
                    destFileName, true);
            }

            string destFileName1 = Path.Combine(TestHelper.InputPath, "AssemblyF.dll");
            if (!File.Exists(destFileName1))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "AssemblyF.dll"),
                    destFileName1, true);
            }

            var exception = Assert.Throws<ObfuscarException>(() => TestHelper.Obfuscate(xml));
            Assert.StartsWith("Inconsistent virtual method obfuscation", exception.Message);

            Assert.False(File.Exists(Path.Combine(TestHelper.OutputPath, @"AssemblyG.dll")));
            Assert.False(File.Exists(Path.Combine(TestHelper.OutputPath, @"AssemblyF.dll")));
        }

        [Fact]
        public void CheckMakedOnly()
        {
            string name = "AssemblyWithTypesAttrs3";
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"				<Obfuscator>" +
                @"				<Var name='InPath' value='{0}' />" +
                @"				<Var name='OutPath' value='{1}' />" +
                @"             <Var name='KeepPublicApi' value='false' />" +
                @"             <Var name='HidePrivateApi' value='true' />" +
                @"             <Var name='MarkedOnly' value='true' />" +
                @"				<Module file='$(InPath){2}{3}.dll'>" +
                @"				</Module>" +
                @"				</Obfuscator>",
                TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar, name);

            var obfuscator = TestHelper.BuildAndObfuscate(name, string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            var map = obfuscator.Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(obfuscator.Project.AssemblyList[0].FileName);

            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.TestEnum");
            ObfuscatedThing classA = map.GetClass(new TypeKey(classAType));
            var field = classAType.Fields.FirstOrDefault(item => item.Name == "Default");
            var f1 = map.GetField(new FieldKey(field));
            var field2 = classAType.Fields.FirstOrDefault(item => item.Name == "Test");
            var f2 = map.GetField(new FieldKey(field2));
            Assert.True(classA.Status == ObfuscationStatus.Skipped, "Public enum shouldn't have been obfuscated.");
            Assert.True(f1.Status == ObfuscationStatus.Skipped, "Public enum field should not be obfuscated");
            Assert.True(f2.Status == ObfuscationStatus.Skipped, "Public enum field should not be obfuscated");

            TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            ObfuscatedThing classB = map.GetClass(new TypeKey(classBType));
            var classBmethod1 = FindByName(classBType, "PublicMethod");
            var method2 = map.GetMethod(new MethodKey(classBmethod1));

            Assert.True(classB.Status == ObfuscationStatus.Renamed, "PublicClass should have been obfuscated.");
            Assert.True(method2.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");

            AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(obfuscator.Project.AssemblyList[0].OutputFileName);
            Assert.True(outAssmDef.CustomAttributes.Count == inAssmDef.CustomAttributes.Count - 1, "obfuscation attribute on assembly should have been removed.");
            TypeDefinition classTypeRenamed = outAssmDef.MainModule.Types[2];
            
            // DEBUG: Log type info
            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                $"CheckMakedOnly: Type={classTypeRenamed.FullName}, PropertiesCount={classTypeRenamed.Properties.Count}\n");
            System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                $"  All types in output: {string.Join(", ", outAssmDef.MainModule.Types.Select(t => t.FullName + "[P:" + t.Properties.Count + "]"))}\n");
            foreach (var prop in classTypeRenamed.Properties)
            {
                System.IO.File.AppendAllText("/tmp/obfuscar_debug.log", 
                    $"  Property: {prop.Name}, Type={prop.PropertyType?.FullName}\n");
            }
            
            Assert.False(classTypeRenamed.CustomAttributes.Count == 2, "obfuscation attribute on type should have been removed.");
            MethodDefinition testMethod = classTypeRenamed.Methods.First(_ => _.Name == "Test");
            Assert.False(testMethod.HasCustomAttributes, "obfuscation attribute on method should have been removed.");
            MethodDefinition test2Method = classTypeRenamed.Methods.First(_ => _.Name == "Test2");
            Assert.True(test2Method.HasCustomAttributes, "obfuscation attribute on method should not have been removed.");

            PropertyDefinition token = classTypeRenamed.Properties[0];
            Assert.Equal("access_token", token.CustomAttributes[0].Properties[0].Argument.Value);
            PropertyDefinition type = classTypeRenamed.Properties[1];
            Assert.Equal("token_type", type.CustomAttributes[0].Properties[0].Argument.Value);
        }

        [Fact]
        public void CheckMarkedOnly2()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"				<Obfuscator>" +
                @"				<Var name='InPath' value='{0}' />" +
                @"				<Var name='OutPath' value='{1}' />" +
                @"                <Var name='KeepPublicApi' value='false' />" +
                @"                <Var name='HidePrivateApi' value='true' />" +
                @"                <Var name='MarkedOnly' value='true' />" +
                @"				<Module file='$(InPath){2}AssemblyWithTypesAttrs.dll'>" +
                @"				</Module>" +
                @"				</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypesAttrs", string.Empty, xml);
            var map = obfuscator.Mapping;

            const string assmName = "AssemblyWithTypesAttrs.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.InternalClass");
            ObfuscatedThing classA = map.GetClass(new TypeKey(classAType));
            var classAmethod1 = FindByName(classAType, "PublicMethod");
            var method = map.GetMethod(new MethodKey(classAmethod1));

            TypeDefinition nestedClassAType = classAType.NestedTypes[0];
            ObfuscatedThing nestedClassA = map.GetClass(new TypeKey(nestedClassAType));
            TypeDefinition nestedClassAType2 = nestedClassAType.NestedTypes[0];
            ObfuscatedThing nestedClassA2 = map.GetClass(new TypeKey(nestedClassAType2));

            Assert.True(classA.Status == ObfuscationStatus.Skipped, "InternalClass shouldn't have been obfuscated.");
            Assert.True(method.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated");
            Assert.True(nestedClassA.Status == ObfuscationStatus.Skipped,
                "Nested class shouldn't have been obfuscated");
            Assert.True(nestedClassA2.Status == ObfuscationStatus.Skipped,
                "Nested class shouldn't have been obfuscated");

            TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            Assert.True(classBType.HasCustomAttributes, "Obfuscation attribute must exist");
            ObfuscatedThing classB = map.GetClass(new TypeKey(classBType));
            var classBmethod1 = FindByName(classBType, "PublicMethod");
            var method2 = map.GetMethod(new MethodKey(classBmethod1));

            Assert.True(classB.Status == ObfuscationStatus.Renamed, "PublicClass should have been obfuscated.");
            Assert.True(method2.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");

            AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, assmName));
            TypeDefinition renamed = outAssmDef.MainModule.GetType(classB.StatusText.Substring(assmName.Length - 4 + 2));
            Assert.False(renamed.HasCustomAttributes, "Obfuscation attribute must not exist");

            TypeDefinition classCType = inAssmDef.MainModule.GetType("TestClasses.InternalClass2");
            ObfuscatedThing classC = map.GetClass(new TypeKey(classCType));
            var classCmethod1 = FindByName(classCType, "PublicMethod");
            var method1 = map.GetMethod(new MethodKey(classCmethod1));

            TypeDefinition nestedClassBType = classCType.NestedTypes[0];
            ObfuscatedThing nestedClassB = map.GetClass(new TypeKey(nestedClassBType));

            TypeDefinition nestedClassBType2 = nestedClassBType.NestedTypes[0];
            ObfuscatedThing nestedClassB2 = map.GetClass(new TypeKey(nestedClassBType2));

            Assert.True(classC.Status == ObfuscationStatus.Skipped, "InternalClass2 shouldn't have been obfuscated.");
            Assert.True(method1.Status == ObfuscationStatus.Skipped, "PublicMethod shouldn't have been obfuscated.");
            Assert.True(nestedClassB.Status == ObfuscationStatus.Skipped,
                "Nested class shouldn't have been obfuscated");
            Assert.True(nestedClassB2.Status == ObfuscationStatus.Skipped,
                "Nested class shouldn't have been obfuscated");

            TypeDefinition classDType = inAssmDef.MainModule.GetType("TestClasses.PublicClass2");
            ObfuscatedThing classD = map.GetClass(new TypeKey(classDType));
            var classDmethod1 = FindByName(classDType, "PublicMethod");
            var method3 = map.GetMethod(new MethodKey(classDmethod1));

            Assert.True(classD.Status == ObfuscationStatus.Skipped, "PublicClass2 shouldn't have been obfuscated.");
            Assert.True(method3.Status == ObfuscationStatus.Renamed, "PublicMethod should have been obfuscated.");
        }
    }
}
