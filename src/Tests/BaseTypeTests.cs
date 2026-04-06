using System;
using System.IO;
using System.Reflection;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class BaseTypeTests
    {
        [Fact]
        public void CheckBaseTypeIsPreservedWhenBaseTypeAppearsLaterInMetadata()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithBaseTypeOrder.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate(
                "AssemblyWithBaseTypeOrder",
                string.Empty,
                xml,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string outputAssemblyPath = Path.Combine(outputPath, "AssemblyWithBaseTypeOrder.dll");
            Assembly assembly = Assembly.LoadFile(outputAssemblyPath);

            Type derivedType = assembly.GetType("TestClasses.DerivedClass", throwOnError: true);
            Assert.NotNull(derivedType.BaseType);
            Assert.Equal("TestClasses.BaseClass", derivedType.BaseType.FullName);
        }
    }
}
