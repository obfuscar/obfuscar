using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
    public class SameMethodNamingTest
    {
        private readonly string outputPath;

        public SameMethodNamingTest()
        {
            this.outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Module file='$(InPath){2}SameMethodNamingTest1.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("SameMethodNamingTest", "1", xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7, useNetFramework: false);
        }

        public static IEnumerable<object[]> ClassNamesToTests => Enumerable.Range(1, 8) // there are only 8 classes
            .Select(x => new object[] {
                $"Class_{x}"
                , null
                , new string[] { "Method1", "Method2" }
            })
            .ToList();

        [Theory()]
        [MemberData(nameof(ClassNamesToTests))]
        public void CheckSameNameMethodsDoesNotThrow(string className, string[] expected, string[] notExpected)
        {
            AssemblyHelper.CheckAssemblyExtended(Path.Combine(outputPath, "SameMethodNamingTest1.dll"), 2, expected,
                notExpected,
                delegate (TypeDefinition typeDef) { return typeDef.Name == className; },
                CheckType);
        }

        void CheckType(TypeDefinition typeDef)
        {
            Assembly assm = Assembly.LoadFile(Path.GetFullPath(typeDef.Module.FileName));
            Type type = assm.GetType(typeDef.FullName);

            object obj = type.IsAbstract
                ? null  // class is static
                : Activator.CreateInstance(type);

            var allDeclaredMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(x => !x.IsConstructor)
                .ToList();

            foreach (var m in allDeclaredMethods)
            {
                var exception = Record.Exception(() => m.Invoke(obj, new object[] { "param1", (byte?)255 }));

                // Assert
                Assert.Null(exception);
            }
        }
    }
}
