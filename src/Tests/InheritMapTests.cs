using System.Linq;
using System.IO;
using LeXtudio.Metadata.Mutable;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class InheritMapTests
    {
        private const string AssemblyName = "AssemblyWithInheritance";

        [Fact]
        public void CheckOverrideMethodsGroupedTogether()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            // Read input assembly BEFORE obfuscation for original type/method keys
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName, string.Empty,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest,
                useNetFramework: false);
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, AssemblyName + ".dll"));

            var item = TestHelper.Obfuscate(xml);

            var inheritMap = item.Project.InheritMap;
            var inBaseType = inAssmDef.MainModule.GetType("TestClasses.BaseClass");
            var inDerivedType = inAssmDef.MainModule.GetType("TestClasses.DerivedClass");
            Assert.NotNull(inBaseType);
            Assert.NotNull(inDerivedType);

            var inBaseVirtualMethod = inBaseType.Methods.First(m => m.Name == "VirtualMethod");
            var inDerivedVirtualMethod = inDerivedType.Methods.First(m => m.Name == "VirtualMethod");

            var baseVirtualKey = new MethodKey(inBaseVirtualMethod);
            var derivedVirtualKey = new MethodKey(inDerivedVirtualMethod);

            var virtualGroup = inheritMap.GetMethodGroup(baseVirtualKey);
            Assert.NotNull(virtualGroup);
            Assert.Contains(baseVirtualKey, virtualGroup.Methods);
            Assert.Contains(derivedVirtualKey, virtualGroup.Methods);
            Assert.False(virtualGroup.External,
                "Custom VirtualMethod overrides should not be marked External");
        }

        [Fact]
        public void CheckObjectOverridesMarkedExternal()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName, string.Empty,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest,
                useNetFramework: false);
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, AssemblyName + ".dll"));

            var item = TestHelper.Obfuscate(xml);

            var inheritMap = item.Project.InheritMap;
            var inBaseType = inAssmDef.MainModule.GetType("TestClasses.BaseClass");
            var inDerivedType = inAssmDef.MainModule.GetType("TestClasses.DerivedClass");
            Assert.NotNull(inBaseType);
            Assert.NotNull(inDerivedType);

            var inBaseToString = inBaseType.Methods.First(m => m.Name == "ToString");
            var inDerivedToString = inDerivedType.Methods.First(m => m.Name == "ToString");

            var baseToStringKey = new MethodKey(inBaseToString);
            var derivedToStringKey = new MethodKey(inDerivedToString);

            var toStringGroup = inheritMap.GetMethodGroup(baseToStringKey);
            Assert.NotNull(toStringGroup);
            Assert.Contains(baseToStringKey, toStringGroup.Methods);
            Assert.Contains(derivedToStringKey, toStringGroup.Methods);
            Assert.True(toStringGroup.External,
                "ToString override group should be External (overrides System.Object)");
        }

        [Fact]
        public void CheckObjectEqualsOverridesMarkedExternal()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName, string.Empty,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest,
                useNetFramework: false);
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, AssemblyName + ".dll"));

            var item = TestHelper.Obfuscate(xml);

            var inheritMap = item.Project.InheritMap;
            var inBaseType = inAssmDef.MainModule.GetType("TestClasses.BaseClass");
            Assert.NotNull(inBaseType);

            var inEquals = inBaseType.Methods.First(m => m.Name == "Equals" && m.Parameters.Count == 1);
            var inGetHashCode = inBaseType.Methods.First(m => m.Name == "GetHashCode");

            var equalsKey = new MethodKey(inEquals);
            var hashCodeKey = new MethodKey(inGetHashCode);

            var equalsGroup = inheritMap.GetMethodGroup(equalsKey);
            Assert.NotNull(equalsGroup);
            Assert.True(equalsGroup.External,
                "Equals override group should be External (overrides System.Object)");

            var hashCodeGroup = inheritMap.GetMethodGroup(hashCodeKey);
            Assert.NotNull(hashCodeGroup);
            Assert.True(hashCodeGroup.External,
                "GetHashCode override group should be External (overrides System.Object)");
        }

        [Fact]
        public void CheckGetBaseTypesContainsBaseClass()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName, string.Empty,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest,
                useNetFramework: false);
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, AssemblyName + ".dll"));

            var item = TestHelper.Obfuscate(xml);

            var inheritMap = item.Project.InheritMap;
            var inBaseType = inAssmDef.MainModule.GetType("TestClasses.BaseClass");
            var inDerivedType = inAssmDef.MainModule.GetType("TestClasses.DerivedClass");
            var inInterfaceType = inAssmDef.MainModule.GetType("TestClasses.IMyInterface");
            Assert.NotNull(inBaseType);
            Assert.NotNull(inDerivedType);
            Assert.NotNull(inInterfaceType);

            var baseKey = new TypeKey(inBaseType);
            var derivedKey = new TypeKey(inDerivedType);

            Assert.True(inheritMap.Inherits(baseKey, "TestClasses.IMyInterface"),
                "BaseClass should implement IMyInterface");

            Assert.True(inheritMap.Inherits(derivedKey, "TestClasses.BaseClass"),
                "DerivedClass should inherit from BaseClass");

            var derivedBaseTypes = inheritMap.GetBaseTypes(derivedKey);
            Assert.Contains(derivedBaseTypes, bt => bt.Fullname == "TestClasses.BaseClass");
        }

        [Fact]
        public void CheckStaticMethodHasNoGroup()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, AssemblyName);

            TestHelper.CleanInput();
            TestHelper.BuildAssembly(AssemblyName, string.Empty,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest,
                useNetFramework: false);
            var inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, AssemblyName + ".dll"));

            var item = TestHelper.Obfuscate(xml);

            var inheritMap = item.Project.InheritMap;
            var inStaticType = inAssmDef.MainModule.GetType("TestClasses.ClassWithStaticMethod");
            Assert.NotNull(inStaticType);

            var inStaticMethod = inStaticType.Methods.First(m => !m.IsSpecialName);
            var staticKey = new MethodKey(inStaticMethod);

            // Static methods are not virtual, should not be in InheritMap
            var group = inheritMap.GetMethodGroup(staticKey);
            Assert.Null(group);
        }
    }
}
