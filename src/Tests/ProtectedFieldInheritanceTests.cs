using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp;
using Obfuscar;
using Xunit;

namespace ObfuscarTests
{
    public class ProtectedFieldInheritanceTests
    {
        [Fact]
        public void CheckProtectedFieldInExternalBaseLibraryIsSkippedInMap()
        {
            var scenario = BuildProtectedFieldScenario();

            var originalBase = AssemblyDefinition.ReadAssembly(
                Path.Combine(TestHelper.InputPath, "AssemblyWithProtectedFieldBase.dll"));
            var originalType = originalBase.MainModule.GetType("Issue601.BaseWithProtectedField");
            var originalField = originalType.GetFieldByName("ProtectedMessage");
            var fieldEntry = scenario.Obfuscator.Mapping.GetField(new FieldKey(originalField));
            Assert.Equal(ObfuscationStatus.Skipped, fieldEntry.Status);
        }

        [Fact]
        public void CheckDerivedAssemblyCompiledAgainstProtectedFieldRunsWithObfuscatedBaseLibrary()
        {
            var scenario = BuildProtectedFieldScenario();

            var loadContext = new AssemblyLoadContext("issue-601-protected-field", true);
            try
            {
                loadContext.Resolving += (_, assemblyName) =>
                {
                    string candidate = Path.Combine(scenario.OutputPath, assemblyName.Name + ".dll");
                    return File.Exists(candidate) ? loadContext.LoadFromAssemblyPath(candidate) : null;
                };

                var derivedAssembly = loadContext.LoadFromAssemblyPath(
                    Path.GetFullPath(Path.Combine(scenario.OutputPath, "AssemblyWithProtectedFieldDerived.dll")));
                var entryPoint = derivedAssembly.GetType("Issue601.EntryPoint", throwOnError: true)
                    .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

                Assert.Equal("protected-field:protected-field", entryPoint.Invoke(null, Array.Empty<object>()));
            }
            finally
            {
                loadContext.Unload();
            }
        }

        private static (Obfuscar.Obfuscator Obfuscator, string OutputPath) BuildProtectedFieldScenario()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssembly(
                "AssemblyWithProtectedFieldBase",
                languageVersion: LanguageVersion.CSharp7,
                useNetFramework: false);
            TestHelper.BuildAssembly(
                "AssemblyWithProtectedFieldDerived",
                customReferences: new List<string>
                {
                    Path.Combine(TestHelper.InputPath, "AssemblyWithProtectedFieldBase.dll")
                },
                languageVersion: LanguageVersion.CSharp7,
                useNetFramework: false);

            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithProtectedFieldBase.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            var obfuscator = TestHelper.Obfuscate(xml);
            File.Copy(
                Path.Combine(TestHelper.InputPath, "AssemblyWithProtectedFieldDerived.dll"),
                Path.Combine(outputPath, "AssemblyWithProtectedFieldDerived.dll"),
                true);

            return (obfuscator, outputPath);
        }
    }
}
