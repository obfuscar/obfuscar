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
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ObfuscarTests
{
    public class FieldOffsetTests
    {
        [Fact]
        public void FieldOffsetAttributeIsPreservedForExplicitLayoutReadonlyStruct()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithFieldOffset.dll' />" +
                @"</Obfuscator>",
                TestHelper.InputPath,
                outputPath,
                Path.DirectorySeparatorChar);

            Obfuscar.Obfuscator item = TestHelper.BuildAndObfuscate(
                "AssemblyWithFieldOffset",
                string.Empty,
                xml,
                languageVersion: LanguageVersion.Latest,
                useNetFramework: false);

            string obfuscatedPath = Path.Combine(item.Project.Settings.OutPath, "AssemblyWithFieldOffset.dll");
            Assembly assembly = Assembly.LoadFile(obfuscatedPath);

            Type type = assembly.GetType("Issue599.MyStruct", throwOnError: true);
            Assert.True(type.IsValueType);

            FieldInfo field = type.GetField("Data", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field);

            var offset = field.GetCustomAttribute<FieldOffsetAttribute>();
            Assert.NotNull(offset);
            Assert.Equal(0, offset.Value);

            MethodInfo execute = assembly.GetType("Issue599.EntryPoint", throwOnError: true)
                .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(execute);
            Assert.True((bool)execute.Invoke(null, Array.Empty<object>()));
        }
    }
}
