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

using System.IO;
using Xunit;
using Obfuscar;

namespace ObfuscarTests
{
    public class SpecializedGenericsTests
    {
        Obfuscator BuildAndObfuscateAssemblies()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithSpecializedGenerics.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            return TestHelper.BuildAndObfuscate("AssemblyWithSpecializedGenerics", string.Empty, xml);
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

            string assmName = "AssemblyWithSpecializedGenerics.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(item.Project.Settings.OutPath, assmName));

            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.ClassA`1");
                MethodDefinition classAmethod2 = FindByName(classAType, "Method2");
                MethodDefinition classAmethod1 = FindByName(classAType, "Method1");

                TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.ClassB");
                MethodDefinition classBmethod2 = FindByName(classBType, "Method2");

                ObfuscatedThing classAEntry = map.GetMethod(new MethodKey(classAmethod2));
                ObfuscatedThing classAEntryMethod1 = map.GetMethod(new MethodKey(classAmethod1));
                ObfuscatedThing classBEntry = map.GetMethod(new MethodKey(classBmethod2));

                Assert.True(
                    classAEntry.Status == ObfuscationStatus.Renamed &&
                    classBEntry.Status == ObfuscationStatus.Renamed,
                    "Both methods should have been renamed.");

                Assert.True(
                    classAEntry.StatusText == classBEntry.StatusText,
                    "Both methods should have been renamed to the same thing.");

                Assert.True(
                    classAEntryMethod1.Status == ObfuscationStatus.Renamed,
                    "The non-overridden specialized method should still be renamed.");

                Assert.True(
                    classAEntryMethod1.StatusText != classAEntry.StatusText,
                    "Specialized generic methods with different source names should not be merged into one rename group.");
            }

            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.ClassA`1");
                MethodDefinition classAmethod2 = FindByName(classAType, "Method3");

                TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.ClassB");
                MethodDefinition classBmethod2 = FindByName(classBType, "Method3");

                ObfuscatedThing classAEntry = map.GetMethod(new MethodKey(classAmethod2));
                ObfuscatedThing classBEntry = map.GetMethod(new MethodKey(classBmethod2));

                Assert.True(
                    classAEntry.Status == ObfuscationStatus.Renamed &&
                    classBEntry.Status == ObfuscationStatus.Renamed,
                    "Both methods should have been renamed.");

                Assert.True(
                    classAEntry.StatusText == classBEntry.StatusText,
                    "Both methods should have been renamed to the same thing.");
            }

            {
                TypeDefinition classCType = inAssmDef.MainModule.GetType("TestClasses.ClassC`1");
                MethodDefinition classCBridgeMethod = FindByName(classCType, "BridgeMethod");

                TypeDefinition classDType = inAssmDef.MainModule.GetType("TestClasses.ClassD");
                MethodDefinition classDBridgeMethod = FindByName(classDType, "BridgeMethod");
                MethodDefinition classDSiblingMethod = FindByName(classDType, "SiblingMethod");

                ObfuscatedThing classCEntry = map.GetMethod(new MethodKey(classCBridgeMethod));
                ObfuscatedThing classDOverrideEntry = map.GetMethod(new MethodKey(classDBridgeMethod));
                ObfuscatedThing classDSiblingEntry = map.GetMethod(new MethodKey(classDSiblingMethod));

                Assert.True(
                    classCEntry.Status == ObfuscationStatus.Renamed &&
                    classDOverrideEntry.Status == ObfuscationStatus.Renamed &&
                    classDSiblingEntry.Status == ObfuscationStatus.Renamed,
                    "All methods should have been renamed.");

                Assert.True(
                    classCEntry.StatusText == classDOverrideEntry.StatusText,
                    "Override and base virtual method should stay in one rename group.");

                Assert.True(
                    classDSiblingEntry.StatusText != classDOverrideEntry.StatusText,
                    "A non-overridden sibling method with the same specialized shape should not be merged into the override rename group.");
            }
        }
    }
}
