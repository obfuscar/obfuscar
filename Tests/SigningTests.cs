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
using Mono.Cecil;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class SigningTests
    {
        [Fact]
        public void CheckCannotObfuscateSigned()
        {
            string xml = String.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}AssemblyForSigning.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            TestHelper.BuildAssembly("AssemblyForSigning", String.Empty,
                "/keyfile:" + Path.Combine(TestHelper.InputPath, @"SigningKey.snk"));
            var exception = Assert.Throws<ObfuscarException>(() => { TestHelper.Obfuscate(xml); });
            Assert.Equal(
                "Obfuscating a signed assembly would result in an invalid assembly:  AssemblyForSigning; use the KeyFile or KeyContainer property to set a key to use",
                exception.Message);
        }

        // [Fact] //no longer valid due to Cecil changes
        private void CheckCanObfuscateDelaySigned()
        {
            string xml = String.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}AssemblyForSigning.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, TestHelper.OutputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the delaysign option (embeds the public key, reserves space for the signature, but does not sign)
            TestHelper.BuildAssembly("AssemblyForSigning", String.Empty,
                "/delaysign /keyfile:" + Path.Combine(TestHelper.InputPath, @"SigningKey.snk"));

            // this should not throw
            TestHelper.Obfuscate(xml);
        }

        [Fact]
        public void DelaySignedToFullSigned()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeyFile' value='$(InPath){2}SigningKey.snk' />" +
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

        [Fact]
        public void DelaySignedRemains()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeyFile' value='$(InPath){2}public.snk' />" +
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
            Assert.False(outAssmDef.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned));
        }
    }
}
