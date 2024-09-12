using Mono.Cecil;
using Obfuscar;
using System.IO;
using Xunit;

namespace ObfuscarTests
{
    public class AutoSkipTypeTests
    {
        MethodDefinition FindByName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
                if (method.Name == name)
                    return method;

            Assert.Fail(string.Format("Expected to find method: {0}", name));
            return null; // never here
        }

        [Fact]
        public void CheckHidePrivateApiFalse()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            var classBType = inAssmDef.MainModule.GetType("TestClasses.InternalClass");
            var classB = map.GetClass(new TypeKey(classBType));

            Assert.True(classB.Status == ObfuscationStatus.Skipped, "Internal class is obfuscated");

            var enumType = inAssmDef.MainModule.GetType("TestClasses.TestEnum");
            var enum1 = map.GetClass(new TypeKey(enumType));
            Assert.True(enum1.Status == ObfuscationStatus.Skipped, "Internal enum is obfuscated");

            var classAType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            var classA = map.GetClass(new TypeKey(classAType));
            var classAmethod1 = FindByName(classAType, "PrivateMethod");
            var classAmethod2 = FindByName(classAType, "PublicMethod");

            var classAMethod1 = map.GetMethod(new MethodKey(classAmethod1));
            var classAMethod2 = map.GetMethod(new MethodKey(classAmethod2));

            Assert.True(classA.Status == ObfuscationStatus.Renamed, "Public class is not obfuscated");
            Assert.True(classAMethod1.Status == ObfuscationStatus.Skipped, "private method is obfuscated.");
            Assert.True(classAMethod2.Status == ObfuscationStatus.Renamed, "public method is not obfuscated.");
        }

        [Fact]
        public void CheckHidePrivateApiTrue()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";
            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            var classBType = inAssmDef.MainModule.GetType("TestClasses.InternalClass");
            var classB = map.GetClass(new TypeKey(classBType));

            Assert.True(classB.Status == ObfuscationStatus.Renamed, "Internal class should have been obfuscated");

            var enumType = inAssmDef.MainModule.GetType("TestClasses.TestEnum");
            var enum1 = map.GetClass(new TypeKey(enumType));
            Assert.True(enum1.Status == ObfuscationStatus.Renamed, "Internal enum should have been obfuscated");
        }

        [Fact]
        public void CheckKeepPublicApiFalse()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            MethodDefinition classAmethod1 = FindByName(classAType, "PrivateMethod");
            MethodDefinition classAmethod2 = FindByName(classAType, "PublicMethod");

            ObfuscatedThing classAMethod1 = map.GetMethod(new MethodKey(classAmethod1));
            ObfuscatedThing classAMethod2 = map.GetMethod(new MethodKey(classAmethod2));

            var classA = map.GetClass(new TypeKey(classAType));
            Assert.True(classA.Status == ObfuscationStatus.Renamed, "Public class should have been obfuscated");
            Assert.True(classAMethod1.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
            Assert.True(classAMethod2.Status == ObfuscationStatus.Renamed, "public method is not obfuscated.");

            var protectedMethod = FindByName(classAType, "ProtectedMethod");
            var protectedAfter = map.GetMethod(new MethodKey(protectedMethod));
            Assert.True(protectedAfter.Status == ObfuscationStatus.Renamed, "protected method is not obfuscated.");
        }

        [Fact]
        public void CheckKeepPublicApiTrue()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            MethodDefinition classAmethod1 = FindByName(classAType, "PrivateMethod");
            MethodDefinition classAmethod2 = FindByName(classAType, "PublicMethod");
            MethodDefinition classAmethod3 = FindByName(classAType, "InternalProtectedMethod");

            ObfuscatedThing classAMethod1 = map.GetMethod(new MethodKey(classAmethod1));
            ObfuscatedThing classAMethod2 = map.GetMethod(new MethodKey(classAmethod2));
            ObfuscatedThing classAMethod3 = map.GetMethod(new MethodKey(classAmethod3));
            var classA = map.GetClass(new TypeKey(classAType));
            Assert.True(classA.Status == ObfuscationStatus.Skipped, "Public class shouldn't have been obfuscated");
            Assert.True(classAMethod1.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
            Assert.True(classAMethod2.Status == ObfuscationStatus.Skipped, "public method is obfuscated.");
            Assert.True(classAMethod3.Status == ObfuscationStatus.Skipped, "internal protected method is obfuscated.");

            var protectedMethod = FindByName(classAType, "ProtectedMethod");
            var protectedAfter = map.GetMethod(new MethodKey(protectedMethod));
            Assert.True(protectedAfter.Status == ObfuscationStatus.Skipped, "protected method is obfuscated.");
        }

        [Fact]
        public void CheckKeepInternalApiFalse()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='KeepInternalApi' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            MethodDefinition privateMethodDefinition = FindByName(classAType, "PrivateMethod");
            MethodDefinition internalMethodDefinition = FindByName(classAType, "InternalMethod");
            MethodDefinition internalProtectedMethodDefinition = FindByName(classAType, "InternalProtectedMethod");
            MethodDefinition protectedInternalMethodDefinition = FindByName(classAType, "ProtectedInternalMethod");
            MethodDefinition protectedMethodDefinition = FindByName(classAType, "ProtectedMethod");
            MethodDefinition publicMethodDefinition = FindByName(classAType, "PublicMethod");

            ObfuscatedThing privateMethod = map.GetMethod(new MethodKey(privateMethodDefinition));
            ObfuscatedThing internalMethod = map.GetMethod(new MethodKey(internalMethodDefinition));
            ObfuscatedThing internalProtectedMethod = map.GetMethod(new MethodKey(internalProtectedMethodDefinition));
            ObfuscatedThing protectedInternalMethod = map.GetMethod(new MethodKey(protectedInternalMethodDefinition));
            ObfuscatedThing protectedMethod = map.GetMethod(new MethodKey(protectedMethodDefinition));
            ObfuscatedThing publicMethod = map.GetMethod(new MethodKey(publicMethodDefinition));

            var classA = map.GetClass(new TypeKey(classAType));
            Assert.True(classA.Status == ObfuscationStatus.Skipped, "Public class shouldn't have been obfuscated");
            Assert.True(privateMethod.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
            Assert.True(internalMethod.Status == ObfuscationStatus.Renamed, "internal method is not obfuscated.");
            Assert.True(internalProtectedMethod.Status == ObfuscationStatus.Skipped, "internal protected method is obfuscated.");
            Assert.True(protectedInternalMethod.Status == ObfuscationStatus.Skipped, "protected internal method is obfuscated.");
            Assert.True(protectedMethod.Status == ObfuscationStatus.Skipped, "protected method is obfuscated.");
            Assert.True(publicMethod.Status == ObfuscationStatus.Skipped, "public method is obfuscated.");
        }

        [Fact]
        public void CheckKeepInternalApiTrue()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='KeepInternalApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition publicClassType = inAssmDef.MainModule.GetType("TestClasses.PublicClass");
            MethodDefinition privateMethodDefinition = FindByName(publicClassType, "PrivateMethod");
            MethodDefinition internalMethodDefinition = FindByName(publicClassType, "InternalMethod");
            MethodDefinition internalProtectedMethodDefinition = FindByName(publicClassType, "InternalProtectedMethod");
            MethodDefinition protectedInternalMethodDefinition = FindByName(publicClassType, "ProtectedInternalMethod");
            MethodDefinition protectedMethodDefinition = FindByName(publicClassType, "ProtectedMethod");
            MethodDefinition publicMethodDefinition = FindByName(publicClassType, "PublicMethod");

            ObfuscatedThing privateMethod = map.GetMethod(new MethodKey(privateMethodDefinition));
            ObfuscatedThing internalMethod = map.GetMethod(new MethodKey(internalMethodDefinition));
            ObfuscatedThing internalProtectedMethod = map.GetMethod(new MethodKey(internalProtectedMethodDefinition));
            ObfuscatedThing protectedInternalMethod = map.GetMethod(new MethodKey(protectedInternalMethodDefinition));
            ObfuscatedThing protectedMethod = map.GetMethod(new MethodKey(protectedMethodDefinition));
            ObfuscatedThing publicMethod = map.GetMethod(new MethodKey(publicMethodDefinition));
            
            TypeDefinition publicStaticClassType = inAssmDef.MainModule.GetType("TestClasses.PublicStaticClass");
            MethodDefinition internalStaticMethodDefinition = FindByName(publicStaticClassType, "InternalStaticMethod");
            ObfuscatedThing internalStaticMethod = map.GetMethod(new MethodKey(internalStaticMethodDefinition));
            
            TypeDefinition internalStaticClassType = inAssmDef.MainModule.GetType("TestClasses.InternalStaticClass");
            MethodDefinition publicStaticMethodDefinition = FindByName(internalStaticClassType, "PublicStaticMethod");
            ObfuscatedThing publicStaticMethod = map.GetMethod(new MethodKey(publicStaticMethodDefinition));

            var publicClass = map.GetClass(new TypeKey(publicClassType));
            Assert.True(publicClass.Status == ObfuscationStatus.Skipped, "Public class shouldn't have been obfuscated");
            Assert.True(privateMethod.Status == ObfuscationStatus.Renamed, "private method is not obfuscated.");
            Assert.True(internalMethod.Status == ObfuscationStatus.Skipped, "internal method is obfuscated.");
            Assert.True(internalProtectedMethod.Status == ObfuscationStatus.Skipped, "internal protected method is obfuscated.");
            Assert.True(protectedInternalMethod.Status == ObfuscationStatus.Skipped, "protected internal method is obfuscated.");
            Assert.True(protectedMethod.Status == ObfuscationStatus.Skipped, "protected method is obfuscated.");
            Assert.True(publicMethod.Status == ObfuscationStatus.Skipped, "public method is obfuscated.");
            
            var publicStaticClass = map.GetClass(new TypeKey(publicStaticClassType));
            Assert.True(publicStaticClass.Status == ObfuscationStatus.Skipped, "Public static class shouldn't have been obfuscated");
            Assert.True(internalStaticMethod.Status == ObfuscationStatus.Skipped, "internal static method is obfuscated.");
            
            var internalStaticClass = map.GetClass(new TypeKey(internalStaticClassType));
            Assert.True(internalStaticClass.Status == ObfuscationStatus.Skipped, "Internal static class shouldn't have been obfuscated");
            Assert.True(publicStaticMethod.Status == ObfuscationStatus.Skipped, "public static method is obfuscated.");
        }

        [Fact]
        public void CheckSkipNamespace()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"<SkipNamespace name='TestClasses1' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses1.PublicClass");
            var classA = map.GetClass(new TypeKey(classAType));
            Assert.True(classA.Status == ObfuscationStatus.Skipped, "Public class shouldn't have been obfuscated");
        }

        [Fact]
        public void CheckSkipEnum()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"<SkipType name='TestClasses.TestEnum' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            Obfuscar.Obfuscator obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            var enumType = inAssmDef.MainModule.GetType("TestClasses.TestEnum");
            var enum1 = map.GetClass(new TypeKey(enumType));
            Assert.True(enum1.Status == ObfuscationStatus.Skipped, "Internal enum is obfuscated");
        }

        [Fact]
        public void CheckSkipAllEnums()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithTypes.dll'>" +
                @"<SkipEnums value='true' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            Obfuscator obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithTypes", string.Empty, xml);
            var map = obfuscator.Mapping;

            string assmName = "AssemblyWithTypes.dll";

            var inAssmDef = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, assmName));

            var enumType = inAssmDef.MainModule.GetType("TestClasses.TestEnum");
            var enum1 = map.GetClass(new TypeKey(enumType));
            Assert.True(enum1.Status == ObfuscationStatus.Skipped, "Internal enum is obfuscated");
        }
    }
}
