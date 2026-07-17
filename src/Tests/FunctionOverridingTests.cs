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
using Obfuscar;
using Xunit;
using System.Reflection;

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

            return TestHelper.BuildAndObfuscate("AssemblyWithOverrides", string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
        }

        MethodDefinition FindByName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
                if (method.Name == name)
                    return method;

            Assert.Fail(string.Format("Expected to find method: {0}", name));
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

            {
                TypeDefinition classType = inAssmDef.MainModule.GetType("TestClasses.ClassH");
                MethodDefinition classMethod = FindByName(classType, "GetObjectData");

                ObfuscatedThing classEntry = map.GetMethod(new MethodKey(classMethod));

                Assert.True(
                    classEntry.Status == ObfuscationStatus.Skipped,
                    "GetObjectData method should have been skipped.");

                Assert.Equal("external base class or interface", classEntry.StatusText);
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

            Obfuscator obfuscator =
                TestHelper.BuildAndObfuscate(new[] {"AssemblyWithGenericOverrides", "AssemblyWithGenericOverrides2"},
                    xml, useNetFramework: false);

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
                Assert.True(instance.ToString() == "Empty<string, string>=A<B<String, String>>",
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
        public void CheckAbstractExternalBaseClassOverridesNotRenamed()
        {
            // Regression test for issue #549: internal class overriding abstract members of an
            // external base class (e.g. System.IO.Stream) must not have those overrides renamed
            // in .NET Core/.NET 5+ builds, matching .NET Framework behavior.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithAbstractBaseOverrides.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                "AssemblyWithAbstractBaseOverrides", string.Empty, xml, useNetFramework: false);
            ObfuscationMap map = item.Mapping;

            string assmName = "AssemblyWithAbstractBaseOverrides.dll";
            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition streamType = inAssmDef.MainModule.GetType("TestClasses.InternalStream");

            string[] overriddenMethodNames = { "get_CanRead", "get_CanSeek", "get_CanWrite", "get_Length",
                "get_Position", "set_Position", "Flush", "Read", "Seek", "SetLength", "Write" };

            foreach (var methodName in overriddenMethodNames)
            {
                MethodDefinition method = FindByName(streamType, methodName);
                ObfuscatedThing entry = map.GetMethod(new MethodKey(method));
                Assert.True(
                    entry.Status == ObfuscationStatus.Skipped,
                    $"Method '{methodName}' overrides an external base class member and must not be renamed, but got status: {entry.Status} ({entry.StatusText})");
            }
        }

        [Fact]
        public void CheckGenericAbstractMethodsGetDifferentNames()
        {
            // Regression test for issue #485: two methods with the same parameter signature in a generic
            // abstract class must not be renamed to the same obfuscated name, which causes infinite recursion.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericAbstractMethods.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                "AssemblyWithGenericAbstractMethods", string.Empty, xml, useNetFramework: false);
            ObfuscationMap map = item.Mapping;

            string assmName = "AssemblyWithGenericAbstractMethods.dll";
            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            TypeDefinition abstractType = inAssmDef.MainModule.GetType("TestClasses.AbstractReadService`3");
            MethodDefinition checkRequest = FindByName(abstractType, "CheckRequest");
            MethodDefinition checkRequestInner = FindByName(abstractType, "CheckRequestInner");

            ObfuscatedThing checkRequestEntry = map.GetMethod(new MethodKey(checkRequest));
            ObfuscatedThing checkRequestInnerEntry = map.GetMethod(new MethodKey(checkRequestInner));

            // Both methods should be renamed (not skipped)
            Assert.True(checkRequestEntry.Status == ObfuscationStatus.Renamed,
                $"CheckRequest should be renamed, got: {checkRequestEntry.Status} ({checkRequestEntry.StatusText})");
            Assert.True(checkRequestInnerEntry.Status == ObfuscationStatus.Renamed ||
                        checkRequestInnerEntry.Status == ObfuscationStatus.WillRename,
                $"CheckRequestInner should be renamed, got: {checkRequestInnerEntry.Status} ({checkRequestInnerEntry.StatusText})");

            // They must NOT share the same obfuscated name — that causes infinite recursion
            Assert.True(checkRequestEntry.StatusText != checkRequestInnerEntry.StatusText,
                $"CheckRequest and CheckRequestInner must not be renamed to the same name '{checkRequestEntry.StatusText}'");
        }

        [Fact]
        public void CheckObjectOverridesNotRenamed()
        {
            // Regression test for issue #608: override Equals, GetHashCode, ToString, Finalize
            // must not be renamed, otherwise TypeLoadException occurs at runtime
            // because they no longer override System.Object methods.
            //
            // Also validates:
            // - Chain inheritance: Base -> Derived where root is external (both skipped)
            // - Chained Equals: multiple levels of Equals overrides (all skipped)
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithObjectOverrides.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            Obfuscator item = TestHelper.BuildAndObfuscate(
                "AssemblyWithObjectOverrides", string.Empty, xml, useNetFramework: false);
            ObfuscationMap map = item.Mapping;

            string assmName = "AssemblyWithObjectOverrides.dll";
            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            // 1. Direct System.Object overrides
            TypeDefinition classType = inAssmDef.MainModule.GetType("TestClasses.ClassWithObjectOverrides");
            string[] directOverrideNames = { "Equals", "GetHashCode", "ToString" };
            foreach (var methodName in directOverrideNames)
            {
                MethodDefinition method = FindByName(classType, methodName);
                ObfuscatedThing entry = map.GetMethod(new MethodKey(method));
                Assert.True(
                    entry.Status == ObfuscationStatus.Skipped,
                    $"Method '{methodName}' overrides System.Object member and must not be renamed, but got status: {entry.Status} ({entry.StatusText})");
            }

            // 2. Finalize override (generated by destructor)
            TypeDefinition finalizeType = inAssmDef.MainModule.GetType("TestClasses.ClassWithFinalizeOverride");
            MethodDefinition finalizeMethod = FindByName(finalizeType, "Finalize");
            ObfuscatedThing finalizeEntry = map.GetMethod(new MethodKey(finalizeMethod));
            Assert.True(
                finalizeEntry.Status == ObfuscationStatus.Skipped,
                $"Method 'Finalize' overrides System.Object member and must not be renamed, but got status: {finalizeEntry.Status} ({finalizeEntry.StatusText})");

            // 3. Chain inheritance: ExternalMethodOverrideBase overrides ToString
            TypeDefinition chainBaseType = inAssmDef.MainModule.GetType("TestClasses.ExternalMethodOverrideBase");
            MethodDefinition chainBaseMethod = FindByName(chainBaseType, "ToString");
            ObfuscatedThing chainBaseEntry = map.GetMethod(new MethodKey(chainBaseMethod));
            Assert.True(
                chainBaseEntry.Status == ObfuscationStatus.Skipped,
                $"Method 'ExternalMethodOverrideBase.ToString' overrides external base and must not be renamed, but got status: {chainBaseEntry.Status} ({chainBaseEntry.StatusText})");

            // 4. Chain inheritance: ExternalMethodOverrideDerived overrides ToString (from ExternalMethodOverrideBase -> Object)
            TypeDefinition chainDerivedType = inAssmDef.MainModule.GetType("TestClasses.ExternalMethodOverrideDerived");
            MethodDefinition chainDerivedMethod = FindByName(chainDerivedType, "ToString");
            ObfuscatedThing chainDerivedEntry = map.GetMethod(new MethodKey(chainDerivedMethod));
            Assert.True(
                chainDerivedEntry.Status == ObfuscationStatus.Skipped,
                $"Method 'ExternalMethodOverrideDerived.ToString' overrides chain from external base and must not be renamed, but got status: {chainDerivedEntry.Status} ({chainDerivedEntry.StatusText})");

            // 5. Chained Equals: both levels should be skipped (root is Object)
            TypeDefinition chainedBaseType = inAssmDef.MainModule.GetType("TestClasses.ChainedEqualsBase");
            MethodDefinition chainedBaseMethod = FindByName(chainedBaseType, "Equals");
            ObfuscatedThing chainedBaseEntry = map.GetMethod(new MethodKey(chainedBaseMethod));
            Assert.True(
                chainedBaseEntry.Status == ObfuscationStatus.Skipped,
                $"Method 'ChainedEqualsBase.Equals' overrides System.Object and must not be renamed, but got status: {chainedBaseEntry.Status} ({chainedBaseEntry.StatusText})");

            TypeDefinition chainedDerivedType = inAssmDef.MainModule.GetType("TestClasses.ChainedEqualsDerived");
            MethodDefinition chainedDerivedMethod = FindByName(chainedDerivedType, "Equals");
            ObfuscatedThing chainedDerivedEntry = map.GetMethod(new MethodKey(chainedDerivedMethod));
            Assert.True(
                chainedDerivedEntry.Status == ObfuscationStatus.Skipped,
                $"Method 'ChainedEqualsDerived.Equals' overrides chain from System.Object and must not be renamed, but got status: {chainedDerivedEntry.Status} ({chainedDerivedEntry.StatusText})");

            // 6. Runtime validation: load obfuscated assembly and invoke entry point
            string obfuscatedPath = System.IO.Path.Combine(outputPath, assmName);
            Assembly obfuscatedAssembly = Assembly.LoadFile(System.IO.Path.GetFullPath(obfuscatedPath));
            foreach (var type in obfuscatedAssembly.GetTypes())
            {
                var method = type.GetMethod("Test", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var result = method.Invoke(null, Array.Empty<object>());
                    Assert.Equal("ok", result);
                }
            }
        }

        [Fact]
        public void CheckClosedMethodOverrideGenericMethod()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithClosedOverrideGeneric.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithClosedOverrideGeneric", string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), outputPath,
                "AssemblyWithClosedOverrideGeneric.dll");
            var assembly = Assembly.LoadFile(assemblyPath);
            Assert.Equal(5, assembly.GetTypes().Length);
        }
    }
}
