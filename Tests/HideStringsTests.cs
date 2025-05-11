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

using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Xunit;

namespace ObfuscarTests
{
    public class HideStringsTests
    {
        int TotalStringCount = 10;

        [Fact]
        public void CheckHideStringsClassDoesNotExist()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithStrings.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "AssemblyWithStrings.dll"));

            Assert.Equal(5, assmDef.MainModule.Types.Count);

            TypeDefinition expected = null;
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected = type;
                }
            }

            Assert.Null(expected);
        }

        [Fact]
        public void CheckHideStringsClassExists()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithStrings.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "AssemblyWithStrings.dll"));

            Assert.Equal(6, assmDef.MainModule.Types.Count);

            TypeDefinition expected = null;
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected = type;
                }
            }

            Assert.NotNull(expected);

            Assert.Equal(3, expected.Fields.Count);

            Assert.Equal(TotalStringCount, expected.Methods.Count - 2); // Total strings. 2 methods are not hidden strings.
        }

        [Fact]
        public void CheckHideStringsClassSkip()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithStrings.dll'>" +
                @"  <SkipStringHiding type='TestClasses.PublicClass1' name='*' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "AssemblyWithStrings.dll"));

            Assert.Equal(6, assmDef.MainModule.Types.Count);

            TypeDefinition expected = null;
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected = type;
                }
            }

            Assert.NotNull(expected);

            Assert.Equal(3, expected.Fields.Count);

            Assert.Equal(TotalStringCount - 2, expected.Methods.Count - 2);
        }

        [Fact]
        public void CheckHideStringsClassForce()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithStrings.dll'>" +
                @"  <ForceStringHiding type='TestClasses.PublicClass1' name='*' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "AssemblyWithStrings.dll"));

            Assert.Equal(6, assmDef.MainModule.Types.Count);

            TypeDefinition expected = null;
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected = type;
                }
            }

            Assert.NotNull(expected);

            Assert.Equal(3, expected.Fields.Count);

            Assert.Equal(2, expected.Methods.Count - 2);
        }

        [Fact]
        public void CheckHideStringsClassForce2()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithStrings.dll'>" +
                @"  <ForceType name='TestClasses.PublicClass1' forceStringHiding='true' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "AssemblyWithStrings.dll"));

            Assert.Equal(6, assmDef.MainModule.Types.Count);

            TypeDefinition expected = null;
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected = type;
                }
            }

            Assert.NotNull(expected);

            Assert.Equal(3, expected.Fields.Count);

            Assert.Equal(2, expected.Methods.Count - 2);
        }

        [Fact]
        public void CheckHideStringsClassExistsWithManyStrings()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Module file='$(InPath){2}ManyStrings.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("ManyStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "ManyStrings.dll"));

            Assert.Equal(4, assmDef.MainModule.Types.Count);

            var expected = new List<TypeDefinition>();
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected.Add(type);
                }
            }

            Assert.NotEmpty(expected);

            Assert.Equal(3, expected[0].Fields.Count);
            Assert.Equal(3, expected[1].Fields.Count);

            Assert.Equal(65530, expected[0].Methods.Count + expected[1].Methods.Count - expected.Count * 2); // 65530 strings. 2 methods of each types are not hidden strings.
        }

        [Fact]
        public void CheckHideStringsMethodSkip()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithStrings.dll'>" +
                @"  <SkipStringHiding type='TestClasses.PublicClass4' name='TestAsync3' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithStrings", string.Empty, xml, true, Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);
            AssemblyDefinition assmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(outputPath, "AssemblyWithStrings.dll"));

            Assert.Equal(6, assmDef.MainModule.Types.Count);

            TypeDefinition expected = null;
            foreach (var type in assmDef.MainModule.Types)
            {
                if (type.FullName.Contains("PrivateImplementation"))
                {
                    expected = type;
                }
            }

            Assert.NotNull(expected);

            Assert.Equal(3, expected.Fields.Count);

            // IMPORTANT: strings in async void methods are actually moved by the compiler to the MoveNext method of the state machine, so cannot be easily skipped by rules.
            Assert.Equal(TotalStringCount, expected.Methods.Count - 2);
        }

    }
}
