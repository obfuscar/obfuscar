#region Copyright (c) 2017 Lex Li <support@lextm.com>

/// <copyright>
/// Copyright (c) 2017 Lex Li <support@lextm.com>
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

using Mono.Cecil;
using Obfuscar;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace ObfuscarTests
{
    public class IncludeTests
    {
        [Fact]
        public void CheckInclude()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"  <Var name='InPath' value='{0}' />" +
                @"  <Var name='OutPath' value='{1}' />" +
                @"  <Include path='$(InPath){2}TestInclude.xml' />" +
                @"  <Module file='$(InPath){2}AssemblyWithProperties.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            string destFileName = Path.Combine(TestHelper.InputPath, "TestInclude.xml");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "TestInclude.xml"),
                    destFileName, true);
            }

            Obfuscator obfuscator = Obfuscator.CreateFromXml(xml);
            Assert.False(obfuscator.Project.Settings.KeepPublicApi);
        }

        [Fact]
        public void CheckModuleInclude()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"  <Var name='InPath' value='{0}' />" +
                @"  <Var name='OutPath' value='{1}' />" +
                @"  <Module file='$(InPath){2}SkipVirtualMethodTest1.dll'>" +
                @"    <Include path='$(InPath){2}TestIncludeModule.xml' />" +
                @"  </Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            string destFileName = Path.Combine(TestHelper.InputPath, "TestIncludeModule.xml");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "TestIncludeModule.xml"),
                    destFileName, true);
            }

            TestHelper.BuildAndObfuscate("SkipVirtualMethodTest", "1", xml);

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
                delegate (TypeDefinition typeDef) { return !typeDef.IsInterface; },
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

