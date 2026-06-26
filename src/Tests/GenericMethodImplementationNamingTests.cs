using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
    public class GenericMethodImplementationNamingTests
    {
        private readonly string outputPath;

        public GenericMethodImplementationNamingTests()
        {
            this.outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Module file='$(InPath){2}GenericMethodImplementationNamingTest1.dll'>" +
                @"</Module>" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate("GenericMethodImplementationNamingTest", "1", xml, languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7, useNetFramework: false);
        }

        public static IEnumerable<object[]> ClassNamesToTests => Enumerable.Range(1, 2) // there are only 2 classes
            .Select(x => new object[] {
                $"Example_{x}"
                , null
                , new string[] { "Method1" }
            })
            .ToList();

        [Theory()]
        [MemberData(nameof(ClassNamesToTests))]
        public void CheckGenericMethodImplementationDoesNotThrow(string className, string[] expected, string[] notExpected)
        {
            AssemblyHelper.CheckAssemblyExtended(Path.Combine(outputPath, "GenericMethodImplementationNamingTest1.dll"), 
                expectedTypes: 1 + 2 + 1,  // interfaces + example classes + UsageOfExampleClasses
                expected,
                notExpected,
                delegate (TypeDefinition typeDef) { return typeDef.Name == className; },
                CheckType);
        }

        void CheckType(TypeDefinition typeDef)
        {
            Assembly assm = Assembly.LoadFile(Path.GetFullPath(typeDef.Module.FileName));
            Type type = assm.GetType(typeDef.FullName);

            foreach (var interfaceType in type.GetInterfaces())
            {
                var interfaceMap = type.GetInterfaceMap(interfaceType);
                for (int index = 0; index < interfaceMap.InterfaceMethods.Length; index++)
                    Assert.Equal(interfaceMap.InterfaceMethods[index].Name, interfaceMap.TargetMethods[index].Name);
            }
        }
    }
}
