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
                , new string[] { "A", "A", "B" }    // two Method1 and one Method2   
                , new string[] { "Method1", "Method2" }
            })
            .ToList();

        [Theory()]
        [MemberData(nameof(ClassNamesToTests))]
        public void CheckSameNameMethodsDoesNotThrow(string className, string[] expected, string[] notExpected)
        {
            CheckAssemblyExtended(Path.Combine(outputPath, "SameMethodNamingTest1.dll"), 2, expected,
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

        /// <summary>
        /// Modified version of
        /// <seealso cref=" AssemblyHelper.CheckAssembly(string, int, string[], string[], Predicate{LeXtudio.Metadata.Mutable.MutableTypeDefinition}, Action{LeXtudio.Metadata.Mutable.MutableTypeDefinition})"/>
        /// </summary>  
        /// <remarks>
        /// May be merged with original 
        /// </remarks>
        private static void CheckAssemblyExtended(string name, int expectedTypes, string[] expectedMethods,
            string[] notExpectedMethods,
            Predicate<TypeDefinition> isType, Action<TypeDefinition> checkType)
        {
            HashSet<string> methodsToFind = new HashSet<string>(expectedMethods ?? new string[] { });
            HashSet<string> methodsNotToFind = new HashSet<string>(notExpectedMethods ?? new string[] { });

            AssemblyHelper.CheckAssembly(name, expectedTypes, isType,
                delegate (TypeDefinition typeDef)
                {
                    if (expectedMethods != null && notExpectedMethods != null)
                    {
                        // make sure we have enough methods...
                        var expectedCount = expectedMethods.Length
                            + typeDef.Methods.Count(x => x.IsConstructor | x.IsStaticConstructor);

                        Assert.Equal(expectedCount, typeDef.Methods.Count);
                        // "Some of the methods for the type are missing.");
                    }

                    foreach (MethodDefinition method in typeDef.Methods)
                    {
                        Assert.False(methodsNotToFind.Contains(method.Name), string.Format(
                            "Did not expect to find method '{0}'.", method.Name));

                        methodsToFind.Remove(method.Name);
                    }

                    checkType?.Invoke(typeDef);
                });

            Assert.False(methodsToFind.Count > 0, "Failed to find all expected methods.");
        }
    }
}
