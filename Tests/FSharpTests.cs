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
using Obfuscar;
using System;
using System.IO;
using Xunit;

namespace ObfuscarTests
{
    public class FSharpTests
    {
        private static MethodDefinition FindByFullName(TypeDefinition typeDef, string name)
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
                @"<Var name='KeyFile' value='$(InPath){2}SigningKey.snk' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='OptimizeMethods' value='false' />" +
                @"<Module file='$(InPath){2}FSharp.Compiler.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.CleanInput();

            // build it with the keyfile option (embeds the public key, and signs the assembly)
            string destFileName = Path.Combine(TestHelper.InputPath, "FSharp.Core.dll");
            if (!File.Exists(destFileName))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "FSharp.Core.dll"),
                    destFileName, true);
            }

            string destFileName1 = Path.Combine(TestHelper.InputPath, "FSharp.Compiler.dll");
            if (!File.Exists(destFileName1))
            {
                File.Copy(Path.Combine(TestHelper.InputPath, @"..", "FSharp.Compiler.dll"),
                    destFileName1, true);
            }

            var map = TestHelper.Obfuscate(xml).Mapping;

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "FSharp.Compiler.dll"));
            {
                TypeDefinition classAType =
                    inAssmDef.MainModule.GetType("Microsoft.FSharp.Compiler.AbstractIL.IL/ldargs@2513");
                var type = map.GetClass(new TypeKey(classAType));
                Assert.True(type.Status == ObfuscationStatus.Renamed, "Type should have been renamed.");

                var method1 = FindByFullName(classAType,
                    "System.Int32 Microsoft.FSharp.Compiler.AbstractIL.IL/ldargs@2513::GenerateNext(System.Collections.Generic.IEnumerable`1<Microsoft.FSharp.Compiler.AbstractIL.IL/ILInstr>&)");
                var m1 = map.GetMethod(new MethodKey(method1));
                Assert.True(m1.Status == ObfuscationStatus.Skipped, "Instance method should have been skipped.");
                Assert.Equal("external base class or interface", m1.StatusText);
            }

            {
                TypeDefinition classAType =
                    inAssmDef.MainModule.GetType("Microsoft.FSharp.Compiler.AbstractIL.IL/mkILMethods@2352");
                var type = map.GetClass(new TypeKey(classAType));
                Assert.True(type.Status == ObfuscationStatus.Renamed, "Type should have been renamed.");

                var method1 = FindByFullName(classAType,
                    "System.Tuple`2<Microsoft.FSharp.Collections.FSharpList`1<Microsoft.FSharp.Compiler.AbstractIL.IL/ILMethodDef>,Microsoft.FSharp.Collections.FSharpMap`2<System.String,Microsoft.FSharp.Collections.FSharpList`1<Microsoft.FSharp.Compiler.AbstractIL.IL/ILMethodDef>>> Microsoft.FSharp.Compiler.AbstractIL.IL/mkILMethods@2352::Invoke(Microsoft.FSharp.Compiler.AbstractIL.IL/ILMethodDef,System.Tuple`2<Microsoft.FSharp.Collections.FSharpList`1<Microsoft.FSharp.Compiler.AbstractIL.IL/ILMethodDef>,Microsoft.FSharp.Collections.FSharpMap`2<System.String,Microsoft.FSharp.Collections.FSharpList`1<Microsoft.FSharp.Compiler.AbstractIL.IL/ILMethodDef>>>)");
                var m1 = map.GetMethod(new MethodKey(method1));
                Assert.True(m1.Status == ObfuscationStatus.Skipped, "Instance method should have been skipped.");
                Assert.Equal("external base class or interface", m1.StatusText);
            }
        }
    }
}
