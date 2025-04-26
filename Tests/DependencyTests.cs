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
using System.CodeDom.Compiler;
using Obfuscar;
using Xunit;
using Microsoft.CSharp;

namespace ObfuscarTests
{
    public class DependencyTests
    {
        public DependencyTests()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(names: ["AssemblyA", "AssemblyB"]);
        }

        [Fact]
        public void CheckGoodDependency()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Module file='$(InPath){1}AssemblyB.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, Path.DirectorySeparatorChar);

            Obfuscator obfuscator = Obfuscator.CreateFromXml(xml);
        }

        [Fact]
        public void CheckDeletedDependency()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Module file='$(InPath){1}AssemblyB.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, Path.DirectorySeparatorChar);

            // explicitly delete AssemblyA
            File.Delete(Path.Combine(TestHelper.InputPath, "AssemblyA.dll"));
            var exception = Assert.Throws<ObfuscarException>(() => { Obfuscator.CreateFromXml(xml); });
            Assert.Equal("Unable to resolve dependency:  AssemblyA", exception.Message);
        }

        [Fact]
        public void CheckMissingDependency()
        {
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Module file='{0}{1}AssemblyD.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, Path.DirectorySeparatorChar);

            // InPath defaults to '.', which doesn't contain AssemblyA
            string destFileName = Path.Combine(TestHelper.InputPath, "AssemblyD.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "AssemblyD.dll"),
                    destFileName, true);
            }

            var exception = Assert.Throws<ObfuscarException>(() => { Obfuscator.CreateFromXml(xml); });
            Assert.Equal("Unable to resolve dependency:  AssemblyC", exception.Message);
        }
    }
}
