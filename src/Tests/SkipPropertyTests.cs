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
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class SkipPropertyTests
    {
        protected void CheckProperties(string name, int expectedTypes, string[] expected, string[] notExpected, bool keepProperties = false)
        {
            HashSet<string> propsToFind = new HashSet<string>(expected);
            HashSet<string> propsNotToFind = new HashSet<string>(notExpected);

            string[] expectedMethods = new string[expected.Length * 2];
            for (int i = 0; i < expected.Length; i++)
            {
                expectedMethods[i * 2 + 0] = "get_" + expected[i];
                expectedMethods[i * 2 + 1] = "set_" + expected[i];
            }

            string[] notExpectedMethods = new string[notExpected.Length * 2];
            for (int i = 0; i < notExpected.Length; i++)
            {
                notExpectedMethods[i * 2 + 0] = "get_" + notExpected[i];
                notExpectedMethods[i * 2 + 1] = "set_" + notExpected[i];
            }

            AssemblyHelper.CheckAssembly(name, expectedTypes, expectedMethods, notExpectedMethods,
                delegate(TypeDefinition typeDef) { return true; },
                delegate(TypeDefinition typeDef)
                {
                    Assert.Equal(keepProperties ? notExpected.Length : expected.Length, typeDef.Properties.Count);
                    // expected.Length == 1 ? "Type should have 1 property (others dropped by default)." :
                    // String.Format ("Type should have {0} properties (others dropped by default).", expected.Length));

                    foreach (PropertyDefinition prop in typeDef.Properties)
                    {
                        Assert.False(propsNotToFind.Contains(prop.Name), string.Format(
                            "Did not expect to find property '{0}'.", prop.Name));

                        propsToFind.Remove(prop.Name);
                    }

                    Assert.False(propsToFind.Count > 0, "Failed to find all expected properties.");
                });
        }

        [Fact]
        public void CheckDropsProperties()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithProperties.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithProperties", string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string[] expected = new string[0];

            string[] notExpected = new string[]
            {
                "Property1",
                "Property2",
                "PropertyA"
            };

            CheckProperties(Path.Combine(outputPath, "AssemblyWithProperties.dll"), 1, expected, notExpected);
        }

        [Fact]
        public void CheckKeepsProperties()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepProperties' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithProperties.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithProperties", string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string[] expected = new string[0];

            string[] notExpected = new string[]
            {
                "Property1",
                "Property2",
                "PropertyA"
            };

            CheckProperties(Path.Combine(outputPath, "AssemblyWithProperties.dll"), 1, expected, notExpected, true);
        }

        [Fact]
        public void CheckSkipPropertyByName()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithProperties.dll'>" +
                @"<SkipProperty type='TestClasses.ClassA' name='Property2' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithProperties", string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string[] expected = new string[]
            {
                "Property2"
            };

            string[] notExpected = new string[]
            {
                "Property1",
                "PropertyA"
            };

            CheckProperties(Path.Combine(outputPath, "AssemblyWithProperties.dll"), 1, expected, notExpected);
        }

        [Fact]
        public void CheckSkipPropertyByRx()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithProperties.dll'>" +
                @"<SkipProperty type='TestClasses.ClassA' rx='Property\d' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithProperties", string.Empty, xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string[] expected = new string[]
            {
                "Property1",
                "Property2"
            };

            string[] notExpected = new string[]
            {
                "PropertyA"
            };

            CheckProperties(Path.Combine(outputPath, "AssemblyWithProperties.dll"), 1, expected, notExpected);
        }

        [Fact]
        public void CheckInternalsVisibleToBothInProject()
        {
            // When both the assembly with InternalsVisibleTo and the friend assembly are in the
            // same obfuscation project, internal members get renamed on both sides consistently —
            // the obfuscated output works correctly because all references are updated together.
            string inputPath = TestHelper.InputPath;
            string outputPath = TestHelper.OutputPath;

            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest, false,
                "AssemblyWithInternalsVisibleTo", "AssemblyFriendConsumer");

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithInternalsVisibleTo.dll' />" +
                @"<Module file='$(InPath){2}AssemblyFriendConsumer.dll' />" +
                @"</Obfuscator>", inputPath, outputPath, Path.DirectorySeparatorChar);

            // Both assemblies obfuscated together: internal members are renamed on both sides,
            // so the output is consistent and no ResolutionException should occur.
            var obfuscator = TestHelper.Obfuscate(xml);
            var map = obfuscator.Mapping;

            var inAssmDef = AssemblyDefinition.ReadAssembly(Path.Combine(inputPath, "AssemblyWithInternalsVisibleTo.dll"));
            var internalType = inAssmDef.MainModule.GetType("TestClasses.InternalSharedClass");

            // Members of the internal class are renamed — both assemblies are updated consistently
            foreach (var prop in internalType.Properties)
            {
                var entry = map.GetProperty(new PropertyKey(new TypeKey(internalType), prop));
                Assert.True(entry.Status == ObfuscationStatus.Renamed,
                    $"Property '{prop.Name}' should be renamed when both assemblies are in the project");
            }

            // Both output files must exist (no crash)
            Assert.True(File.Exists(Path.Combine(outputPath, "AssemblyWithInternalsVisibleTo.dll")));
            Assert.True(File.Exists(Path.Combine(outputPath, "AssemblyFriendConsumer.dll")));
        }

        [Fact]
        public void CheckInternalsVisibleToFriendNotInProject()
        {
            // When only the library is obfuscated and the friend assembly is NOT in the project,
            // internal members get renamed but the friend assembly still references the old names.
            // This documents the current behavior: Obfuscar does NOT detect InternalsVisibleTo
            // and does NOT preserve internal members for out-of-project friend assemblies.
            // The correct fix would be to treat such members as public API when the friend
            // assembly is not being obfuscated in the same run.
            string inputPath = TestHelper.InputPath;
            string outputPath = TestHelper.OutputPath;

            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest, false,
                "AssemblyWithInternalsVisibleTo", "AssemblyFriendConsumer");

            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithInternalsVisibleTo.dll' />" +
                @"</Obfuscator>", inputPath, outputPath, Path.DirectorySeparatorChar);

            // Only the library is obfuscated — friend assembly is not in the project.
            var obfuscator = TestHelper.Obfuscate(xml);
            var map = obfuscator.Mapping;

            var inAssmDef = AssemblyDefinition.ReadAssembly(Path.Combine(inputPath, "AssemblyWithInternalsVisibleTo.dll"));
            var internalType = inAssmDef.MainModule.GetType("TestClasses.InternalSharedClass");

            // Current behavior: internal members are renamed even though the friend assembly
            // (not in the project) still references them by their original names.
            // This is a known gap: Obfuscar does not check InternalsVisibleTo attributes.
            foreach (var prop in internalType.Properties)
            {
                var entry = map.GetProperty(new PropertyKey(new TypeKey(internalType), prop));
                // Document current behavior — renamed despite friend assembly depending on it
                Assert.True(entry.Status == ObfuscationStatus.Renamed,
                    $"Current behavior: '{prop.Name}' is renamed even when friend assembly is not in project. " +
                    $"Ideally, with InternalsVisibleTo pointing to an out-of-project assembly, it should be skipped.");
            }
        }

        [Fact]
        public void CheckPublicPropOnInternalClassIsRenamed()
        {
            // Regression test for issue #559: a public property declared on an internal class
            // is not part of the externally-visible API surface and must be renamed when
            // HidePrivateApi=true, even though KeepPublicApi=true.
            // A public property on a public class must still be preserved.
            string inputPath = TestHelper.InputPath;
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithInternalClassPublicProp.dll' />" +
                @"</Obfuscator>", inputPath, outputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.BuildAndObfuscate("AssemblyWithInternalClassPublicProp", string.Empty, xml);
            var map = obfuscator.Mapping;

            var inAssmDef = AssemblyDefinition.ReadAssembly(Path.Combine(inputPath, "AssemblyWithInternalClassPublicProp.dll"));

            // Public property on internal class — must be renamed (not part of public API)
            var internalType = inAssmDef.MainModule.GetType("TestClasses.InternalClassWithPublicProp");
            foreach (var prop in internalType.Properties)
            {
                var propEntry = map.GetProperty(new PropertyKey(new TypeKey(internalType), prop));
                Assert.True(propEntry.Status == ObfuscationStatus.Renamed,
                    $"Property '{prop.Name}' on internal class should be renamed, got: {propEntry.Status} ({propEntry.StatusText})");
            }

            // Public property on public class — must be preserved (is part of public API)
            var publicType = inAssmDef.MainModule.GetType("TestClasses.PublicClassWithPublicProp");
            foreach (var prop in publicType.Properties)
            {
                var propEntry = map.GetProperty(new PropertyKey(new TypeKey(publicType), prop));
                Assert.True(propEntry.Status == ObfuscationStatus.Skipped,
                    $"Property '{prop.Name}' on public class should be skipped (KeepPublicApi), got: {propEntry.Status} ({propEntry.StatusText})");
            }
        }
    }
}
