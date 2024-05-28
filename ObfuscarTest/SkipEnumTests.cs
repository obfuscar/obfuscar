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
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Obfuscar;
using Xunit;

namespace ObfuscarTest
{
    public class SkipEnumTests
    {
        protected void CheckEnums(string name, int expectedTypes, string[] expected, string[] notExpected)
        {
            HashSet<string> valsToFind = new HashSet<string>(expected);
            HashSet<string> valsNotToFind = new HashSet<string>(notExpected);

            AssemblyHelper.CheckAssembly(name, expectedTypes,
                delegate(TypeDefinition typeDef) { return typeDef.BaseType.FullName == "System.Enum"; },
                delegate(TypeDefinition typeDef)
                {
                    // num expected + num unexpected + field storage
                    int totalValues = expected.Length + notExpected.Length + 1;
                    Assert.Equal(totalValues, typeDef.Fields.Count);
                    // String.Format( "Type should have {0} values.", totalValues ) );

                    foreach (FieldDefinition field in typeDef.Fields)
                    {
                        Assert.False(valsNotToFind.Contains(field.Name), string.Format(
                            "Did not expect to find event '{0}'.", field.Name));

                        valsToFind.Remove(field.Name);
                    }

                    Assert.False(valsToFind.Count > 0, "Failed to find all expected values.");
                });
        }

        [Fact]
        public void CheckRenamesEnumValues()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithEnums.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithEnums", string.Empty, xml);

            string[] expected = new string[0];

            string[] notExpected = new string[]
            {
                "Value1",
                "Value2",
                "ValueA"
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithEnums.dll"), 2, expected, notExpected);
        }

        [Fact]
        public void CheckSkipEnumsByName()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithEnums.dll'>" +
                @"<SkipField type='TestClasses.Enum1' name='Value2' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithEnums", string.Empty, xml);

            string[] expected = new string[]
            {
                "Value2"
            };

            string[] notExpected = new string[]
            {
                "Value1",
                "ValueA"
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithEnums.dll"), 2, expected, notExpected);
        }

        [Fact]
        public void CheckSkipEnumsByRx()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithEnums.dll'>" +
                @"<SkipField type='TestClasses.Enum1' rx='Value\d' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithEnums", string.Empty, xml);

            string[] expected = new string[]
            {
                "Value1",
                "Value2"
            };

            string[] notExpected = new string[]
            {
                "ValueA"
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithEnums.dll"), 2, expected, notExpected);
        }

        [Fact]
        public void CheckSkipEnums()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}AssemblyWithEnums.dll'>" +
                @"<SkipField type='TestClasses.Enum1' name='*' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithEnums", string.Empty, xml);

            string[] expected = new string[]
            {
                "Value1",
                "Value2",
                "ValueA"
            };

            string[] notExpected = new string[]
            {
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithEnums.dll"), 2, expected, notExpected);
        }

        [Fact]
        public void CheckSkipAllFields()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='RenameFields' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithEnums.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithEnums", string.Empty, xml);

            string[] expected = new string[]
            {
                "Value1",
                "Value2",
                "ValueA"
            };

            string[] notExpected = new string[]
            {
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithEnums.dll"), 2, expected, notExpected);
        }

        [Fact]
        public void CheckSkipAllEnums()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}AssemblyWithEnums.dll'>" +
                @"<SkipEnums value='true' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            var map = TestHelper.BuildAndObfuscate("AssemblyWithEnums", string.Empty, xml).Mapping;

            string[] expected = new string[]
            {
                "Value1",
                "Value2",
                "ValueA"
            };

            string[] notExpected = new string[]
            {
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithEnums.dll"), 2, expected, notExpected);

            string assmName = "AssemblyWithEnums.dll";

            AssemblyDefinition inAssmDef = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, assmName));

            var classBType = inAssmDef.MainModule.GetType("TestClasses.Test");
            var classB = map.GetClass(new TypeKey(classBType));

            Assert.True(classB.Status == ObfuscationStatus.Renamed, "Internal class is not renamed");

            var fieldType = classBType.Fields[0];
            var fieldB = map.GetField(new FieldKey(fieldType));

            Assert.True(fieldB.Status == ObfuscationStatus.Renamed, "Fields of internal class is not renamed");
        }

        [Fact]
        public void CheckSkipEnumByINotifyPropertyChanged()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}AssemblyWithProperties.dll'>" +
                @"<SkipField type='TestClasses.Enum1' name='*' />" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithProperties", string.Empty, xml);

            string[] expected = new string[]
            {
                "Value1",
                "Value2",
            };

            string[] notExpected = new string[]
            {
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithProperties.dll"), 5, expected, notExpected);
        }

        [Fact]
        public void CheckSkipEnumBySerializable()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Module file='$(InPath){2}AssemblyWithSerializableEnum.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("AssemblyWithSerializableEnum", string.Empty, xml);

            string[] expected = new string[]
            {
                "Value1",
                "Value2",
            };

            string[] notExpected = new string[]
            {
            };

            CheckEnums(Path.Combine(outputPath, "AssemblyWithSerializableEnum.dll"), 1, expected, notExpected);
        }
    }
}
