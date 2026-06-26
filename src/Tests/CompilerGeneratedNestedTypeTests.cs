using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ObfuscarTests
{
    public class CompilerGeneratedNestedTypeTests
    {
        [Fact]
        public void CheckCompilerGeneratedNestedTypesLoadWithKoreanNames()
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HideStrings' value='true' />" +
                @"<Var name='ReuseNames' value='false' />" +
                @"<Var name='UseKoreanNames' value='true' />" +
                @"<Module file='$(InPath){2}AssemblyWithCompilerGeneratedNestedTypes.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate(
                "AssemblyWithCompilerGeneratedNestedTypes",
                string.Empty,
                xml,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            string assemblyPath = Path.Combine(outputPath, "AssemblyWithCompilerGeneratedNestedTypes.dll");
            Assembly assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
            Type[] types = assembly.GetTypes();

            Assert.Contains(types, type => type.IsNested && IsCompilerGenerated(type));

            var nestedNames = new Dictionary<string, string>();
            foreach (var type in types.Where(type => type.IsNested && IsCompilerGenerated(type)))
            {
                Assert.NotNull(type.DeclaringType);

                string key = type.Name;
                Assert.False(nestedNames.TryGetValue(key, out var existing),
                    $"Compiler-generated nested type name '{key}' is duplicated by '{existing}' and '{type.FullName}'.");
                nestedNames.Add(key, type.FullName);
            }

            foreach (var type in types.Where(type => type.GetMethod("Test", BindingFlags.Public | BindingFlags.Static) != null))
            {
                var result = (string)type.GetMethod("Test", BindingFlags.Public | BindingFlags.Static).Invoke(null, Array.Empty<object>());
                Assert.False(string.IsNullOrEmpty(result));
            }
        }

        private static bool IsCompilerGenerated(Type type)
        {
            return type.GetCustomAttributes(false).Any(attribute =>
                attribute.GetType().FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        }
    }
}
