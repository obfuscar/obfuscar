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
using System.Linq;

namespace ObfuscarTests
{
    public class FunctionOverridingTests
    {
        private string output;

        Obfuscator BuildAndObfuscateAssemblies()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithOverrides.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate("AssemblyWithOverrides", string.Empty, xml);
        }

        MethodDefinition FindByName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
                if (method.Name == name)
                    return method;

            Assert.True(false, string.Format("Expected to find method: {0}", name));
            return null; // never here
        }

        [Fact]
        public void CheckClassHasAttribute()
        {
            Obfuscator item = BuildAndObfuscateAssemblies();
            ObfuscationMap map = item.Mapping;

            string assmName = "AssemblyWithOverrides.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(item.Project.Settings.OutPath, assmName));
            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.ClassA");
                MethodDefinition classAmethod2 = FindByName(classAType, "Method2");
                MethodDefinition classAcompare = FindByName(classAType, "CompareTo");

                TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.ClassB");
                MethodDefinition classBmethod2 = FindByName(classBType, "Method2");
                MethodDefinition classBcompare = FindByName(classBType, "CompareTo");

                TypeDefinition classCType = inAssmDef.MainModule.GetType("TestClasses.ClassC");
                MethodDefinition classCmethod1 = FindByName(classCType, "Method1");

                TypeDefinition classDType = inAssmDef.MainModule.GetType("TestClasses.ClassD");
                MethodDefinition classDmethod1 = FindByName(classDType, "Method1");

                ObfuscatedThing classAEntry = map.GetMethod(new MethodKey(classAmethod2));
                ObfuscatedThing classACompareEntry = map.GetMethod(new MethodKey(classAcompare));
                ObfuscatedThing classBEntry = map.GetMethod(new MethodKey(classBmethod2));
                ObfuscatedThing classBCompareEntry = map.GetMethod(new MethodKey(classBcompare));
                ObfuscatedThing classCEntry = map.GetMethod(new MethodKey(classCmethod1));
                ObfuscatedThing classDEntry = map.GetMethod(new MethodKey(classDmethod1));

                var classFType = inAssmDef.MainModule.GetType("TestClasses.ClassF");
                var classFmethod = FindByName(classFType, "Test");

                var classGType = inAssmDef.MainModule.GetType("TestClasses.ClassG");
                var classGmethod = FindByName(classGType, "Test");

                var classFEntry = map.GetMethod(new MethodKey(classFmethod));
                var classGEntry = map.GetMethod(new MethodKey(classGmethod));

                Assert.True(
                    classAEntry.Status == ObfuscationStatus.Renamed &&
                    classBEntry.Status == ObfuscationStatus.Renamed,
                    "Both methods should have been renamed.");

                Assert.True(
                    classAEntry.StatusText == classBEntry.StatusText,
                    "Both methods should have been renamed to the same thing.");

                Assert.True(classACompareEntry.Status == ObfuscationStatus.Skipped);

                Assert.True(classBCompareEntry.Status == ObfuscationStatus.Skipped);

                Assert.True(classCEntry.Status == ObfuscationStatus.Renamed);

                Assert.True(classDEntry.Status == ObfuscationStatus.Renamed);

                Assert.True(
                    classFEntry.Status == ObfuscationStatus.Renamed && classGEntry.Status == ObfuscationStatus.Renamed,
                    "Both methods should have been renamed.");

                Assert.True(classFEntry.StatusText == classGEntry.StatusText,
                    "Both methods should have been renamed to the same thing.");
            }

            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.CA");
                MethodDefinition classAmethod2 = FindByName(classAType, "get_PropA");

                TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.CB");
                MethodDefinition classBmethod2 = FindByName(classBType, "get_PropB");

                TypeDefinition classCType = inAssmDef.MainModule.GetType("TestClasses.IA");
                MethodDefinition classCmethod1 = FindByName(classCType, "get_PropA");

                TypeDefinition classDType = inAssmDef.MainModule.GetType("TestClasses.IB");
                MethodDefinition classDmethod1 = FindByName(classDType, "get_PropB");

                ObfuscatedThing classAEntry = map.GetMethod(new MethodKey(classAmethod2));
                ObfuscatedThing classBEntry = map.GetMethod(new MethodKey(classBmethod2));
                ObfuscatedThing classCEntry = map.GetMethod(new MethodKey(classCmethod1));
                ObfuscatedThing classDEntry = map.GetMethod(new MethodKey(classDmethod1));

                Assert.True(
                    classAEntry.Status == ObfuscationStatus.Renamed &&
                    classCEntry.Status == ObfuscationStatus.Renamed,
                    "Both methods should have been renamed.");

                Assert.True(
                    classAEntry.StatusText == classCEntry.StatusText,
                    "Both methods should have been renamed to the same thing.");

                Assert.True(
                    classBEntry.Status == ObfuscationStatus.Renamed && classDEntry.Status == ObfuscationStatus.Renamed,
                    "Both methods should have been renamed.");

                Assert.True(classBEntry.StatusText == classDEntry.StatusText,
                    "Both methods should have been renamed to the same thing.");

                Assert.True(classAEntry.StatusText != classBEntry.StatusText,
                    "Both methods shouldn't have been renamed to the same thing.");
            }
        }

        [Fact]
        public void CheckGenericMethodRenaming()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericOverrides.dll' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericOverrides2.dll'>" +
                @"<SkipNamespace name='*' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            var assmName = "AssemblyWithGenericOverrides";
            Obfuscator obfuscator =
                TestHelper.BuildAndObfuscate(new[] {assmName, "AssemblyWithGenericOverrides2"},
                    xml);
            var map = obfuscator.Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, $"{assmName}.dll"));
            TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.Alpha`1");
            TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.IBeta`2");
            var classARenamed = map.GetClass(new TypeKey(classAType));
            var classBRenamed = map.GetClass(new TypeKey(classBType));
            var end = assmName.Length + 2;
            Assert.True(classARenamed.Status == ObfuscationStatus.Renamed, "Type must be obfuscated");
            Assert.True(classBRenamed.Status == ObfuscationStatus.Renamed, "Interface must be obfuscated");
            var formattedString = $"Empty<string, string>=A<B<String, String>>";

            var assembly2Path = Path.Combine(Directory.GetCurrentDirectory(), outputPath,
                "AssemblyWithGenericOverrides2.dll");
            var assembly2 = Assembly.LoadFile(assembly2Path);
            var type = assembly2.GetType("TestClasses.Test");
            var ctor = type.GetConstructor(new Type[0]);
            var instance = ctor.Invoke(new object[0]);
            try
            {
                output = outputPath;
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
                Assert.True(instance.ToString() == formattedString,
                    "Generic override should have been updated");
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), output, args.Name.Split(',')[0] + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
        }

        [Fact]
        public void CheckClosedMethodOverrideGenericMethod()
        {
            string assmName = "AssemblyWithClosedOverrideGeneric.dll";
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}{3}' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, assmName);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithClosedOverrideGeneric", string.Empty, xml);
            var map = obfuscator.Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, assmName));
            TypeDefinition classType = inAssmDef.MainModule.GetType("TestClasses.Generic`1");
            TypeDefinition interfaceType = inAssmDef.MainModule.GetType("TestClasses.IFoos");

            MethodDefinition classMethod = classType.Methods.First(_ => _.Name == "ToArray");
            MethodDefinition interfaceMethod = interfaceType.Methods.First(_ => _.Name == "ToArray");

            var renamedClassMethod = map.GetMethod(new MethodKey(classMethod));
            var renamedInterfaceMethod = map.GetMethod(new MethodKey(interfaceMethod));

            Assert.True(renamedClassMethod.Status == ObfuscationStatus.Renamed, "class method should be renamed");
            Assert.True(renamedInterfaceMethod.Status == ObfuscationStatus.Renamed, "interface method should be renamed");
            Assert.True(renamedClassMethod.StatusText == renamedInterfaceMethod.StatusText, "They should have the same name");

            PropertyDefinition interfaceProperty = interfaceType.Properties.First(_ => _.Name == "Collection");

            var renamedClassProperty = map.GetClass(new TypeKey(classType)).Properties.First(p => p.Key.Name == "Collection").Value;
            var renamedInterfaceProperty = map.GetProperty(new PropertyKey(new TypeKey(interfaceType), interfaceProperty));

            Assert.True(renamedClassProperty.Status == ObfuscationStatus.Renamed, "class property should be renamed");
            Assert.True(renamedInterfaceProperty.Status == ObfuscationStatus.Renamed, "interface property should be renamed");
            Assert.True(renamedClassProperty.StatusText == renamedInterfaceProperty.StatusText, "They should both have been dropped");

            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), outputPath,
                "AssemblyWithClosedOverrideGeneric.dll");
            var assembly = Assembly.LoadFile(assemblyPath);
            Assert.Equal(6, assembly.GetTypes().Length);
        }

        [Fact]
        public void CheckEventRenaming()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithEvent.dll' />" +
                @"<Module file='$(InPath){2}AssemblyWithEvent2.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            var assmName = "AssemblyWithEvent";
            Obfuscator obfuscator =
                TestHelper.BuildAndObfuscate(new[] { assmName, "AssemblyWithEvent2" },
                    xml);
            var map = obfuscator.Mapping;

            {
                AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                    Path.Combine(TestHelper.InputPath, $"{assmName}.dll"));
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.ITest");
                var classARenamed = map.GetClass(new TypeKey(classAType));
                Assert.True(classARenamed.Status == ObfuscationStatus.Skipped, "Type must not be obfuscated");
                var classAEvent = classAType.Events.First(item => item.Name == "TestEvent");
                var classAEventRenamed = map.GetEvent(new EventKey(classAEvent));
                Assert.True(classAEventRenamed.Status == ObfuscationStatus.Skipped, "Interface event must not be obfuscated");
            }

            {
                AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                    Path.Combine(TestHelper.InputPath, $"{assmName}2.dll"));
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.Test");
                var classARenamed = map.GetClass(new TypeKey(classAType));
                Assert.True(classARenamed.Status == ObfuscationStatus.Renamed, "Type must be obfuscated");
                var classAEvent = classAType.Events.First(item => item.Name == "TestEvent");
                var classAEventRenamed = map.GetEvent(new EventKey(classAEvent));
                Assert.True(classAEventRenamed.Status == ObfuscationStatus.Skipped, "Class event must not be obfuscated");
            }

            //var assembly2Path = Path.Combine(Directory.GetCurrentDirectory(), outputPath,
            //    "AssemblyWithEvent2.dll");
            //var assembly2 = Assembly.LoadFile(assembly2Path);
            //var type = assembly2.GetType("A.A");
            //var ctor = type.GetConstructor(new Type[0]);
            //var instance = ctor.Invoke(new object[0]);
        }
    }
}
