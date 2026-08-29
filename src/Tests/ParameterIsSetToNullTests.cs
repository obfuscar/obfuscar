using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ObfuscarTests
{
    public class ParameterIsSetToNullTests
    {
        private readonly string outputAssemblyPath;

        public ParameterIsSetToNullTests() 
        {
            string outputPath = TestHelper.OutputPath;
            string xml = string.Format(
                @"<?xml version='1.0'?>" +
                @"<Obfuscator>" +
                @"<Var name='InPath' value='{0}' />" +
                @"<Var name='OutPath' value='{1}' />" +
                @"<Var name='KeepPublicApi' value='false' />" +
                @"<Var name='HidePrivateApi' value='true' />" +
                @"<Module file='$(InPath){2}ParameterIsSetToNullTest.dll' />" +
                @"</Obfuscator>", TestHelper.InputPath, outputPath, Path.DirectorySeparatorChar);

            TestHelper.BuildAndObfuscate(
                "ParameterIsSetToNullTest",
                string.Empty,
                xml,
                languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7);

            this.outputAssemblyPath = Path.Combine(outputPath, "ParameterIsSetToNullTest.dll");
        }

        public static IEnumerable<object[]> MethodNamesToTests => new [] {
                "PublicMethod_ListAsync",
                "PublicMethod_EnumerableAsync",
                "PublicMethod_CollectionAsync",
                "PublicMethod_ArrayAsync",
                "PublicMethod_NullableBooleanAsync",
                "PublicMethod_NullableIntAsync",
                "PublicMethod_NullableDateTimeAsync",
                "PublicMethod_NullableTimeSpanAsync",
            }
            .Select(x => new object[] { x })
            .ToList();

        [Theory()]
        [MemberData(nameof(MethodNamesToTests))]
        public async Task CheckParameterIsNotSetToNull(string method)
        {
            Assembly assembly = Assembly.LoadFile(outputAssemblyPath);

            Type derivedType = assembly.GetType("ParameterIsSetToNullTestNamespace.ParameterIsSetToNullExample_ChildClass", throwOnError: true);

            MethodInfo methodInfo = derivedType.GetMethod(method);
            Assert.NotNull(methodInfo);

            var instance = Activator.CreateInstance(derivedType);

            bool? result = null;

            // Act
            var exception = await Record.ExceptionAsync( async () =>  {
                result = await (Task<bool>)methodInfo.Invoke(instance, Array.Empty<object>());
            });

            // Assert
            Assert.Null(exception);
            Assert.True(result);
        }
    }
}
