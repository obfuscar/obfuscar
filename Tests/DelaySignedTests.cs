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

using Mono.Cecil;
using System;
using System.IO;
using Xunit;

namespace ObfuscarTests
{
    public class DelaySignedTests
    {
        static MethodDefinition FindByFullName(TypeDefinition typeDef, string name)
        {
            foreach (MethodDefinition method in typeDef.Methods)
                if (method.FullName == name)
                    return method;

            Assert.True(false, String.Format("Expected to find method: {0}", name));
            return null; // never here
        }

        [Fact]
        public void CheckGeneric()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeyFile' value='$(InPath){2}..{2}dockpanelsuite.snk' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Module file='$(InPath){2}DelaySigned.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();
            var assembly = Path.Combine(TestHelper.InputPath, "DelaySigned.dll");

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            if (!File.Exists(assembly))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "DelaySigned.dll"), assembly, true);
            }

            var map = TestHelper.Obfuscate(xml).Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(assembly);
            Assert.False(inAssmDef.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned));

            AssemblyDefinition outAssmDef =
                AssemblyDefinition.ReadAssembly(Path.Combine(outputPath, "DelaySigned.dll"));
            Assert.True(outAssmDef.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned));
        }
    }
}
