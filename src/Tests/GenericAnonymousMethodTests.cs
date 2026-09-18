using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ObfuscarTests
{
    /// <summary>
    /// Tests for #607: generic extension methods with Lambda expressions
    /// create &lt;&gt;c__N&lt;T&gt; types that may get duplicate names after obfuscation.
    /// </summary>
    public class GenericAnonymousMethodTests
    {
        private static void BuildAndVerify(string testLabel, string extraVars,
            OptimizationLevel optimizationLevel, bool useNetFramework = false)
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
                @"<Module file='$(InPath){2}AssemblyWithGenericAnonymousMethod.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar, extraVars);

            TestHelper.BuildAndObfuscate(
                "AssemblyWithGenericAnonymousMethod",
                string.Empty,
                xml,
                hideStrings: true,
                languageVersion: LanguageVersion.CSharp10,
                useNetFramework: useNetFramework,
                optimizationLevel: optimizationLevel);

            string assemblyPath = Path.Combine(outputPath, "AssemblyWithGenericAnonymousMethod.dll");
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

            Type[] types = assembly.GetTypes();

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
        public void CheckGenericAnonymousMethods_Debug()
        {
            BuildAndVerify("Debug",
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Debug);
        }

        [Fact]
        public void CheckGenericAnonymousMethods_Release()
        {
            BuildAndVerify("Release",
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Release);
        }

        [Fact]
        public void CheckGenericAnonymousMethods_Debug_KoreanNames()
        {
            BuildAndVerify("Debug+Korean",
                @"<Var name='HideStrings' value='true' />" +
                @"<Var name='UseKoreanNames' value='true' />",
                OptimizationLevel.Debug);
        }

        [Fact]
        public void CheckGenericAnonymousMethods_NetFramework_Debug()
        {
            BuildAndVerify("net48+Debug",
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Debug,
                useNetFramework: true);
        }

        [Fact]
        public void CheckGenericAnonymousMethods_NetFramework_Release()
        {
            BuildAndVerify("net48+Release",
                @"<Var name='HideStrings' value='true' />",
                OptimizationLevel.Release,
                useNetFramework: true);
        }

        [Fact]
        public void CheckGenericAnonymousMethods_CrossAssembly()
        {
            TestHelper.CleanInput();
            TestHelper.BuildAssemblies(
                LanguageVersion.CSharp10,
                false,
                "AssemblyWithGenericAnonymousMethodLib",
                "AssemblyWithGenericAnonymousMethodApp");

            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericAnonymousMethodLib.dll' />" +
                @"<Module file='$(InPath){2}AssemblyWithGenericAnonymousMethodApp.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.Obfuscate(xml);

            string appPath = Path.GetFullPath(Path.Combine(outputPath, "AssemblyWithGenericAnonymousMethodApp.dll"));
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(appPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load obfuscated cross-assembly app: {ex.GetType().Name}: {ex.Message}", ex);
            }

            var types = assembly.GetTypes();
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                Assert.True(seen.Add(type.FullName!), $"Duplicate type full name: {type.FullName}");
            }

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
    }
}
