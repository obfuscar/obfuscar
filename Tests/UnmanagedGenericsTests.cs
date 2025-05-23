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
    public class UnmanagedGenericsTests
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
                @"<Module file='$(InPath){2}AssemblyWithUnmanagedGenerics.dll'>" +
                @"  <SkipType name='System.Runtime.CompilerServices.IsUnmanagedAttribute' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            // IMPORTANT: The assembly must be built with C# 10 or later to include the IsUnmanagedAttribute.
            return TestHelper.BuildAndObfuscate("AssemblyWithUnmanagedGenerics", string.Empty, xml);
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

            string assmName = "AssemblyWithUnmanagedGenerics.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("System.Runtime.CompilerServices.IsUnmanagedAttribute");
                if (classAType != null)
                {
                    ObfuscatedThing classAEntry = map.GetClass(new TypeKey(classAType));

                    Assert.True(classAEntry.Status == ObfuscationStatus.Skipped,
                        "Type should have been skipped.");
                }
                else
                {
                    Assert.Fail("Type should have been found.");
                }
            }

            {
                TypeDefinition classAType = inAssmDef.MainModule.GetType("TestClasses.Generic`1");
                MethodDefinition classAmethod2 = FindByName(classAType, "GetEnumerator");

                ObfuscatedThing classAEntry = map.GetMethod(new MethodKey(classAmethod2));

                Assert.True(classAEntry.Status == ObfuscationStatus.Skipped,
                    "Method should have been skipped.");
            }
        }
    }
}
