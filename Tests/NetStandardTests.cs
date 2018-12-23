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
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
    public class NetStandardTests
    {
        [Fact]
        public void CheckNetStandard()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Var name='KeyFile' value='$(InPath){2}SigningKey.snk' />" +
                @"<Module file='$(InPath){2}SharpSnmpLib.NetStandard.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "SharpSnmpLib.NetStandard.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "SharpSnmpLib.NetStandard.dll"),
                    destFileName, true);
            }


            string destFileName1 = Path.Combine(TestHelper.InputPath, "System.ComponentModel.TypeConverter.dll");
            if (!File.Exists(destFileName1))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "System.ComponentModel.TypeConverter.dll"),
                    destFileName1, true);
            }

            var map = TestHelper.Obfuscate(xml, true).Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "SharpSnmpLib.NetStandard.dll"));

            AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "SharpSnmpLib.NetStandard.dll"));

            var corlibs = outAssmDef.MainModule.AssemblyReferences.Where(reference => reference.Name == "mscorlib");
            Assert.Empty(corlibs);

            var runtime =
                outAssmDef.MainModule.AssemblyReferences.Where(reference => reference.Name == "System.Runtime");
            Assert.Single(runtime);
            Assert.Equal("4.0.20.0", runtime.First().Version.ToString());
        }

        [Fact]
        public void CheckNetStandard20()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='SuppressIldasm' value='false' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<AssemblySearchPath path='C:\Program Files\dotnet\sdk\NuGetFallbackFolder\microsoft.netcore.app\2.0.0\ref\netcoreapp2.0\' />" +
                @"<Module file='$(InPath){2}NetStandard20.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "NetStandard20.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "NetStandard20.dll"),
                    destFileName, true);
            }

            var map = TestHelper.Obfuscate(xml, true).Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "NetStandard20.dll"));

            Assert.Single(inAssmDef.MainModule.AssemblyReferences);
            Assert.Equal("netstandard", inAssmDef.MainModule.AssemblyReferences[0].Name);

            AssemblyDefinition outAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "NetStandard20.dll"));

            Assert.Single(outAssmDef.MainModule.AssemblyReferences);
            Assert.Equal("netstandard", outAssmDef.MainModule.AssemblyReferences[0].Name);
        }
    }
}
