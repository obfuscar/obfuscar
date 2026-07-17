using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ObfuscarTests
{
    public class CollectionExpressionTests
    {
        [Fact]
        public void CheckCollectionExpressionFieldNotCorrupted()
        {
            // Regression for issue #572: C# 12 collection expressions like
            // LoadPreset([item]) generate compiler-cached empty array fields.
            // Obfuscator must correctly handle these fields so no MissingFieldException occurs.
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Module file='$(InPath){2}AssemblyWithCollectionExpression.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate(
                "AssemblyWithCollectionExpression",
                string.Empty,
                xml,
                languageVersion: LanguageVersion.CSharp12,
                useNetFramework: false);

            string assemblyPath = Path.Combine(outputPath, "AssemblyWithCollectionExpression.dll");
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load obfuscated assembly: {ex.GetType().Name}: {ex.Message}", ex);
            }

            // Enumerate types (will throw if field references are broken)
            Type[] types = assembly.GetTypes();

            // Invoke entry point to exercise collection expression code path
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