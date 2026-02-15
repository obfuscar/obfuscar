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
using System.Linq;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class PInvokeTests
    {
        [Fact]
        public void CheckReaderLoadsPInvokeMetadata()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly("AssemblyWithPInvoke", string.Empty, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            var assemblyPath = Path.Combine(TestHelper.InputPath, "AssemblyWithPInvoke.dll");
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            var method = assembly.MainModule.GetType("TestClasses.NativeMethods").Methods.Single(m => m.Name == "Roll");

            Assert.True(method.IsPInvokeImpl);
            Assert.NotNull(method.PInvokeInfo);
            Assert.Equal("Roll", method.PInvokeInfo.EntryPoint);
            Assert.NotNull(method.PInvokeInfo.Module);
            Assert.Equal("test", method.PInvokeInfo.Module.Name);
        }

        [Fact]
        public void CheckObfuscationPreservesPInvokeMetadata()
        {
            var outputPath = TestHelper.OutputPath;
            var xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithPInvoke.dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                outputPath,
                Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate(
                "AssemblyWithPInvoke",
                string.Empty,
                xml,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            var inputAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(TestHelper.InputPath, "AssemblyWithPInvoke.dll"));
            var inputMethod = inputAssembly.MainModule.GetType("TestClasses.NativeMethods").Methods.Single(m => m.Name == "Roll");
            var mapping = obfuscator.Mapping.GetMethod(new MethodKey(inputMethod));

            Assert.Equal(ObfuscationStatus.Renamed, mapping.Status);

            var outputAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(outputPath, "AssemblyWithPInvoke.dll"));
            var outputMethod = outputAssembly.MainModule.Types.SelectMany(t => t.Methods).Single(m => m.IsPInvokeImpl);

            Assert.Equal(mapping.StatusText, outputMethod.Name);
            Assert.NotNull(outputMethod.PInvokeInfo);
            Assert.Equal("Roll", outputMethod.PInvokeInfo.EntryPoint);
            Assert.NotNull(outputMethod.PInvokeInfo.Module);
            Assert.Equal("test", outputMethod.PInvokeInfo.Module.Name);
            Assert.Equal(inputMethod.PInvokeInfo.Attributes, outputMethod.PInvokeInfo.Attributes);
            Assert.Contains(outputAssembly.MainModule.ModuleReferences, r => r.Name == "test");
        }
    }
}
