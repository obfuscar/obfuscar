using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Xunit;

namespace ObfuscarTests
{
    public class ImportNestedTypeUsingStaticOuterClassTest
    {
        private readonly string outputPath;

        public ImportNestedTypeUsingStaticOuterClassTest()
        {
            this.outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" + 
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Module file='$(InPath){2}ImportNestedTypeUsingStaticOuterClassTest.dll'>" +
                @"</Module>" +
                @"<Module file='$(InPath){2}ImportNestedTypeUsingStaticOuterClassTest.examples.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);
            

            TestHelper.BuildAndObfuscate( names: [
                    "ImportNestedTypeUsingStaticOuterClassTest", // nested type is defined in first assembly
                    "ImportNestedTypeUsingStaticOuterClassTest.examples" // use nested type in second assembly
                ], xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7, useNetFramework: false);
        }

        public static IEnumerable<object[]> ClassNamesToTests => Enumerable.Range(1, 3) // there are 3 example classes only
            .Select(x => new object[] {
                $"Example_{x}"
            }).ToList();

        [Theory()]
        [MemberData(nameof(ClassNamesToTests))]
        public void CheckNestedTypeCanBeLoaded(string className)
        {
            var assemblyLoadContext = new AssemblyLoadContext("ImportNestedTypeUsingStaticOuterClassTest ", true);
            try
            {
                // load two assemblies                
                assemblyLoadContext.LoadFromAssemblyPath(Path.Combine(outputPath, "ImportNestedTypeUsingStaticOuterClassTest.dll"));
                assemblyLoadContext.LoadFromAssemblyPath(Path.Combine(outputPath, "ImportNestedTypeUsingStaticOuterClassTest.examples.dll"));

                var assembly2 = assemblyLoadContext.Assemblies.Last();
                Type type = assembly2.GetTypes().FirstOrDefault(x => x.Name == className);
                object obj = Activator.CreateInstance(type);

                var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(x => !x.IsConstructor)
                    .Single(); // we have only one public method
                
                var exception = Record.Exception(() => method.Invoke(obj, Array.Empty<object>()));

                // Assert
                Assert.Null(exception);
            }
            finally
            {
                assemblyLoadContext.Unload();
            }
        }
    }
}
