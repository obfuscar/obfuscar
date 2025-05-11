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
using Mono.Cecil;
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

                TypeDefinition classBType = inAssmDef.MainModule.GetType("TestClasses.ClassB");
                MethodDefinition classBmethod2 = FindByName(classBType, "Method2");

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
        }
    }
}
