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
using System.Reflection;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
    public class SkipVirtualMethodTest
    {
        [Fact]
        public void CheckSkipsVirtualMethodFromInterface()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}SkipVirtualMethodTest1.dll'>" +
                @"<SkipMethod type='SkipVirtualMethodTest.Interface1' name='Method1' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("SkipVirtualMethodTest", "1", xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string[] expected = new string[]
            {
                "Method1"
            };

            string[] notExpected = new string[]
            {
                "Method2"
            };

            AssemblyHelper.CheckAssembly(Path.Combine(outputPath, "SkipVirtualMethodTest1.dll"), 2, expected,
                notExpected,
                delegate(TypeDefinition typeDef) { return !typeDef.IsInterface; },
                CheckType);
        }

        [Fact]
        public void CheckSkipsVirtualMethodFromClass()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}SkipVirtualMethodTest2.dll'>" +
                @"<SkipMethod type='SkipVirtualMethodTest.Class1' name='Method1' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("SkipVirtualMethodTest", "2", xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string[] expected = new string[]
            {
                "Method1"
            };

            string[] notExpected = new string[]
            {
                "Method2"
            };

            AssemblyHelper.CheckAssembly(Path.Combine(outputPath, "SkipVirtualMethodTest2.dll"), 2, expected,
                notExpected,
                delegate(TypeDefinition typeDef) { return !typeDef.IsInterface; },
                CheckType);
        }

        void CheckType(TypeDefinition typeDef)
        {
            Assembly assm = Assembly.LoadFile(Path.GetFullPath(typeDef.Module.FileName));
            Type type = assm.GetType(typeDef.FullName);

            object obj = Activator.CreateInstance(type);

            object result = type.InvokeMember("Method1",
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, obj, null);
            Assert.IsType<string>(result); // "Method1 returns a string.");

            Assert.Equal("Method1 result", result); // "Method1 is expected to return a specific string.");
        }
    }
}
