using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ObfuscarTests
{
    public class AnonymousMethodTests
    {
        private static void BuildAndVerify(string testLabel, string extraVars,
            OptimizationLevel optimizationLevel)
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"{3}" +
                @"<Module file='$(InPath){2}AssemblyWithAnonymousMethodAndAttribute.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, extraVars);

            TestHelper.BuildAndObfuscate(
                "AssemblyWithAnonymousMethodAndAttribute",
                string.Empty,
                xml,
                languageVersion: LanguageVersion.CSharp10,
                useNetFramework: false,
                optimizationLevel: optimizationLevel);

            string assemblyPath = Path.Combine(outputPath, "AssemblyWithAnonymousMethodAndAttribute.dll");
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load obfuscated assembly [{testLabel}]: {ex.GetType().Name}: {ex.Message}", ex);
            }

            // Must be able to enumerate types
            Type[] types = assembly.GetTypes();

            // Must be able to invoke entry point
            foreach (var type in types)
            {
                var method = type.GetMethod("Test", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var result = method.Invoke(null, Array.Empty<object>());
                    Assert.Equal("ok", result);
                }
            }
        }

        [Fact]
        public void CheckAnonymousMethods_Release_KoreanNames()
        {
            BuildAndVerify("Release+Korean+HideStrings",
                @"<Var name='HideStrings' value='true' />" +
                @"<Var name='UseKoreanNames' value='true' />",
                OptimizationLevel.Release);
        }

        [Fact]
        public void CheckAnonymousMethods_Debug_KoreanNames()
        {
            BuildAndVerify("Debug+Korean+HideStrings",
                @"<Var name='HideStrings' value='true' />" +
                @"<Var name='UseKoreanNames' value='true' />",
                OptimizationLevel.Debug);
        }

        [Fact]
        public void CheckAnonymousMethods_Release_DefaultNames()
        {
            BuildAndVerify("Release+default",
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Release);
        }

        [Fact]
        public void CheckAnonymousMethods_Release_KeepPublicApi()
        {
            BuildAndVerify("Release+KeepPublicApi",
                @"<Var name='KeepPublicApi' value='true' />" +
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Release);
        }

        [Fact]
        public void CheckAnonymousMethods_Release_SkipGenerated()
        {
            BuildAndVerify("Release+SkipGenerated",
                @"<Var name='SkipGenerated' value='true' />" +
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Release);
        }
    }
}